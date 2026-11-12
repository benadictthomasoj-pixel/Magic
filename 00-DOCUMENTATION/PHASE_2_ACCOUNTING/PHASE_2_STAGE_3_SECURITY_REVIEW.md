# MAGIC ERP – Phase 2 Stage 3: Security & Compliance Review

**Project:** MAGIC – Universal Business Management ERP  
**Phase:** 2 – Accounting Foundation  
**Stage:** 3 – PostgreSQL Persistence & EF Core Infrastructure  
**Security Boundary:** Fail-Closed Architecture  
**Compliance Standard:** Indian Companies Act, 2013 (Section 128 / Rule 3 Audit Trail) & ISO/IEC 27001 Controls  

---

## 1. Security Architecture & Boundary Verification

### 1.1 Strict Fail-Closed Boundary (`OPEN-003`)
In Phase 1 technical audit, `OPEN-003` was cataloged:
> "System endpoints have no authentication or authorization attributes, relying entirely on upstream network security. If the API is ever exposed beyond localhost without reverse-proxy authentication, all endpoints are publicly reachable."

**Enforcement in Stage 3:**
1. **Zero Unauthenticated Posting Endpoints:** No controllers, Minimal APIs, or public HTTP endpoints were introduced for accounting operations in Stage 3. Posting infrastructure exists purely at the application service and persistence layers (`IPostingService`, `IGeneralLedgerRepository`).
2. **No Mock Identity or Bypass Tokens:** All repository methods and domain operations require explicit `Guid userId` and tenant identifiers (`Guid companyId`, `Guid branchId`). No fake authentication handlers, dummy claims, or testing bypasses were injected into the API runtime.
3. **Database Principal Least Privilege:** The application connects using the dedicated `magic_dev` database user. Foreign keys enforce `ON DELETE RESTRICT` on master tables, preventing cascading deletion attacks.

---

## 2. Immutability & Audit Trail Compliance

### 2.1 Indian Companies Act, 2013 Compliance
Under the Companies (Accounts) Rules (amended 2021/2023), ERP and accounting software operating for Indian entities must maintain:
- An audit trail of every transaction.
- An immutable record with no ability to disable or edit the audit trail.
- Immutability of posted financial books.

### 2.2 Storage Engine Enforcement
Application-level checks alone are insufficient because database administrators or compromised services can execute direct SQL queries. MAGIC enforces tamper-evident immutability at the database engine level:

1. **`trg_protect_posted_journal_lines`:** Any `UPDATE` or `DELETE` statement targeting `accounting.journal_lines` belonging to a `POSTED` or `REVERSED` voucher is rejected with PostgreSQL error code `55000`.
2. **`trg_protect_posted_voucher_header`:** Any attempt to `DELETE` a posted voucher or `UPDATE` its financial parameters (`voucher_date`, `voucher_type`, `fiscal_period_id`, `total_debit_paise`, `total_credit_paise`, `idempotency_key`) is rejected with error code `55000`.
3. **Audit Log Table Immutability:** Pre-existing triggers on `audit.system_audit_logs` reject all `UPDATE` and `DELETE` operations on audit entries.
4. **Non-Destructive Companion Reversals:** The only mechanism to correct an erroneous posted transaction is posting a reciprocal reversal voucher (`VoucherType.Reversal`). Both the original transaction and its reversal remain permanently in the general ledger.

---

## 3. Data Integrity & Financial Invariants

1. **Integer Arithmetic Protection:** All monetary fields (`total_debit_paise`, `total_credit_paise`, `debit_paise`, `credit_paise`, balance amounts) are stored strictly as `BIGINT` (64-bit integer paise). Floating-point data types (`FLOAT`, `REAL`, `DOUBLE PRECISION`) are prohibited in the `accounting` schema, completely preventing binary rounding drift.
2. **Double-Entry Constraint Trigger:** Multi-row ledger balancing cannot be validated by standard SQL `CHECK` constraints. The PostgreSQL deferred constraint trigger `trg_verify_posted_voucher_balance` guarantees that transactions committing an unbalanced voucher are aborted.
3. **Strict One-Sided Line Check:** Every journal line is constrained by `ck_journal_lines_strictly_one_sided`, enforcing that a line is either strictly debit or strictly credit, never both and never zero.

---

## 4. Statutory Decision Status

The following statutory decisions remain explicitly pending customer/domain confirmation and have not been silently resolved:

| Decision ID | Area | Current Status | Stage 3 Guardrail |
| :--- | :--- | :---: | :--- |
| **`DEC-ACC-002`** | Inventory Costing (FIFO vs Weighted Average) | **PENDING** | No inventory posting mechanisms implemented. |
| **`DEC-ACC-003`** | GST Rounding Policy (Line-level vs Invoice-level) | **PENDING** | Integer paise structures preserved; no lossy rounding assumptions introduced. |
| **`DEC-ACC-004`** | NIC e-Invoicing Integration (Direct API vs GSP) | **PENDING** | Generic reference and correlation fields created without vendor-specific bindings. |

---

## 5. Security Verdict

Stage 3 implementation complies with all security guidelines, preserves the fail-closed authentication boundary, adheres to statutory audit trail requirements, and introduces zero security vulnerabilities.
