# MAGIC ERP — PHASE 2, STAGE 4 SECURITY REVIEW
## Security Boundaries, Authentication Controls, Invariant Protection & Auditability

| Attribute | Value |
| :--- | :--- |
| **Policy Identifier** | `OPEN-003` Fail-Closed Security Policy |
| **Financial Concurrency** | PostgreSQL Session Advisory Locking (`magic_txn_advisory_lock`) |
| **Audit Traceability** | Immutable Correlated Audit Logs (`trace_correlation_id`, `created_at_utc`, `posted_at_utc`) |
| **Vulnerability Class** | Unauthorized Financial State Mutation, Imbalanced Journal Injections, Double-Posting |
| **Assessment Result** | **PASSED — STRICT COMPLIANCE** |

---

## 1. Fail-Closed Authentication Boundary (`OPEN-003`)

### 1.1 Policy Overview
Under requirement `OPEN-003`, MAGIC strictly forbids mock authentication, default bypass accounts, hardcoded credentials, or developer shortcuts for any state-modifying operations. 

### 1.2 Implementation in `AccountingBaseController`
```csharp
protected Guid ResolveAuthorizedUserId()
{
    if (currentUserContext.IsAuthenticated && currentUserContext.UserId.HasValue && currentUserContext.UserId.Value != Guid.Empty)
    {
        return currentUserContext.UserId.Value;
    }

    throw new UnauthorizedAccessException("Authentication required: No valid authenticated principal context was supplied for financial operation (OPEN-003).");
}
```
All endpoints performing financial mutations (`POST /accounts`, `PUT /accounts/{id}`, `POST /vouchers/draft`, `POST /vouchers/{id}/post`, `POST /vouchers/{id}/reverse`, `POST /fiscal-periods/{id}/lock`, `POST /fiscal-periods/{id}/close`, `POST /fiscal-periods/{id}/reopen`) call `ResolveAuthorizedUserId()`.

If an unauthenticated request attempts to create or mutate financial records:
- The controller throws `UnauthorizedAccessException`.
- The global error handling middleware maps this to HTTP 401 Unauthorized with standard Problem Details.
- The transaction is immediately aborted; no state is written to PostgreSQL.

### 1.3 Verified Integration Test
In `03-TESTS/Integration/Magic.Api.IntegrationTests/AccountingApiEndpointsTests.cs`:
```csharp
[Fact]
public async Task Mutations_WithoutAuthentication_FailClosedPerOpen003()
{
    var client = factory.CreateClient();
    var draftReq = new CreateJournalDraftRequest(...);
    var response = await client.PostAsJsonAsync("/api/v1/accounting/vouchers/draft", draftReq);
    
    // Verifies HTTP 401 Unauthorized
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```
Status: **VERIFIED PASSING**.

---

## 2. Double-Entry Invariant Enforcement

### 2.1 Integer Paise Financial Calculations
Floating-point rounding errors represent a catastrophic vulnerability in financial software. In MAGIC:
- All financial numbers are stored and calculated strictly as 64-bit integer paise (`long`).
- A rupee value of ₹1,000.50 is stored as `100050`.
- Multiplication and division operations apply explicit statutory rounding rules (`GstBreakdown.cs`, `Money.cs`).

### 2.2 Mathematical Equilibrium Verification
Before a voucher can be committed to the General Ledger:
1. Client-side Angular form dynamically checks $\sum \text{Debit} = \sum \text{Credit}$ and disables the post action if imbalanced.
2. The `/vouchers/validate` endpoint verifies domain preconditions without database persistence.
3. The domain entity `JournalVoucher.Post()` executes `PostingValidator.Validate()`, asserting:
   - At least 2 lines exist.
   - Total debits exactly equal total credits.
   - All referenced accounts are active and exist.
   - Target accounting period is not locked.
   - Voucher is currently in `Draft` state.

---

## 3. Concurrency Protection & Transaction Safety

### 3.1 Advisory Locking
To eliminate race conditions during high-concurrency voucher creation or period closing, `PostingService` acquires a PostgreSQL session-level advisory lock:
```sql
SELECT pg_advisory_xact_lock(hashtext('magic_company_' || company_id::text));
```
This guarantees serializable posting order per company without deadlocking unrelated tenants.

### 3.2 Immutability of Posted Records
Once a voucher has transitioned to `Posted` status:
- SQL `UPDATE` and `DELETE` operations on `journal_vouchers` and `journal_lines` are disallowed.
- Error corrections must be performed via `POST /vouchers/{id}/reverse`, which creates an opposite compensating entry referencing the original voucher (`reversal_voucher_id`).
- Complete audit trail is preserved permanently.

---

## 4. Multi-Tenant Isolation
- All database queries filter strictly on `CompanyId`.
- Controllers resolve `CompanyId` from validated HTTP headers, guaranteeing tenant boundary isolation.
- Integration tests confirm no cross-tenant data leakage occurs.
