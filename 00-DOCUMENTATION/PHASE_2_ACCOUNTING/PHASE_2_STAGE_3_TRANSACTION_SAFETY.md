# MAGIC ERP – Phase 2 Stage 3: Transaction Safety & Concurrency Guide

**Project:** MAGIC – Universal Business Management ERP  
**Phase:** 2 – Accounting Foundation  
**Stage:** 3 – PostgreSQL Persistence & EF Core Infrastructure  
**Priority:** Financial Correctness, Non-Repudiation, Concurrency Safety  

---

## 1. Concurrency Model & Sequence Allocation

### 1.1 The Challenge of Voucher Numbering
In ERP systems, voucher numbers must be strictly sequential, human-readable, and without gaps (statutory Indian GST compliance requirement). Naive implementations relying on `MAX(number) + 1` or database auto-increment identity columns fail under concurrent transactions:
- `MAX(number) + 1` causes duplicate key collisions and transaction deadlocks.
- Standard PostgreSQL `IDENTITY` or `SEQUENCE` objects advance even on aborted transactions, creating non-compliant gaps.

### 1.2 The Atomic Upsert Solution
MAGIC utilizes row-level locking via PostgreSQL `INSERT ... ON CONFLICT (branch_id, fiscal_year_id, voucher_type) DO UPDATE ... RETURNING current_sequence`.

```sql
INSERT INTO accounting.voucher_sequences (id, branch_id, fiscal_year_id, voucher_type, current_sequence, updated_at_utc)
VALUES (@pId, @pBranchId, @pFiscalYearId, @pVoucherType, 1, @pNow)
ON CONFLICT (branch_id, fiscal_year_id, voucher_type)
DO UPDATE SET current_sequence = accounting.voucher_sequences.current_sequence + 1, updated_at_utc = @pNow
RETURNING current_sequence;
```

#### Concurrency & Safety Properties:
1. **Isolated Granularity:** Sequence allocation locks only the exact single row for `(branch_id, fiscal_year_id, voucher_type)`. Parallel allocations across distinct branches or voucher types proceed concurrently without blocking each other.
2. **Gapless Guarantees:** Because sequence incrementation is executed within the ambient posting transaction, if the transaction rolls back due to a business invariant failure, the sequence change rolls back with it, preventing sequence holes.
3. **Verified Under Load:** Validated by `VoucherConcurrencyAndTransactionTests.ConcurrentSequenceGenerator_AllocatesContiguousNumbers_WithZeroGapsOrDuplicates` running 15 parallel asynchronous tasks with zero race conditions, zero gaps, and zero duplicate numbers.

---

## 2. All-or-Nothing Atomic Posting Boundary

Posting a financial transaction requires synchronizing 4 distinct system boundaries:
1. Double-entry general ledger voucher header (`accounting.vouchers`).
2. Balanced double-entry line items (`accounting.journal_lines`).
3. Asynchronous outbox event message (`common.outbox_messages`).
4. Tamper-evident audit log record (`audit.system_audit_logs`).

### 2.1 Implementation Pattern
Posting is coordinated via `IPostingService` using `IUnitOfWork.ExecuteInTransactionAsync`:

```csharp
return await unitOfWork.ExecuteInTransactionAsync(async ct =>
{
    // 1. Idempotency Check
    var existing = await generalLedgerRepository.GetVoucherByIdempotencyKeyAsync(voucher.BranchId, voucher.IdempotencyKey, ct);
    if (existing is not null) return existing;

    // 2. Fiscal Governance Status Check
    var period = await fiscalGovernanceRepository.GetPeriodByIdAsync(voucher.FiscalPeriodId, ct);
    if (period.IsClosed || (period.IsLocked && !hasSupervisorElevation))
        throw new DomainException("FISCAL_PERIOD_LOCKED", "Cannot post to a closed or locked period");

    // 3. Control Account & Detail Account Integrity
    // 4. Domain Invariant Validation (PostingValidator)
    // 5. Concurrency-Safe Sequential Voucher Number
    var seqNumber = await voucherNumberGenerator.NextVoucherNumberAsync(voucher.BranchId, period.FiscalYearId, branchCode, fyCode, voucher.VoucherType, ct);

    // 6. Transition State to Posted
    voucher.Post(period, seqNumber, userId);

    // 7. Persist Voucher & Lines
    await generalLedgerRepository.AddVoucherAsync(voucher, ct);

    // 8. Publish Outbox Event Atomically
    await outboxDispatcher.DispatchAsync(new VoucherPostedIntegrationEvent(...), ct);

    // 9. Append Immutable System Audit Log Atomically
    await auditLogger.LogAsync(new AuditLogEntry(...), ct);

    await unitOfWork.SaveChangesAsync(ct);
    return voucher;
});
```

### 2.2 Failure & Rollback Semantics
If an exception occurs at any point before commit (e.g., account deactivated, database trigger constraint violation, network timeout):
- PostgreSQL initiates a complete `ROLLBACK`.
- Zero vouchers, lines, outbox events, or audit records are written.
- Verified by `VoucherConcurrencyAndTransactionTests.TransactionRollback_EnsuresZeroRowsInAllTables_WhenPostingFails`.

---

## 3. Idempotency & Duplicate Prevention

1. **Unique Index Guard:** `accounting.vouchers` enforces `UNIQUE (branch_id, idempotency_key)`.
2. **Deterministic Response:** If an external caller retries a request with an existing idempotency key, `IPostingService` detects the existing voucher and returns it safely without re-posting or re-allocating sequence numbers.
3. **Database-Level Rejection:** If concurrent requests with the identical idempotency key bypass application locks, PostgreSQL rejects the second write with unique constraint violation `uq_vouchers_branch_idempotency`. Verified by `VoucherConcurrencyAndTransactionTests.DuplicateIdempotencyKey_WithinSameBranch_IsRejectedByUniqueConstraint`.

---

## 4. Posted Ledger Immutability & Reversals

In compliance with statutory accounting standards (Section 128 of the Indian Companies Act, 2013 and Rule 3 of Companies (Accounts) Rules):
1. **Zero Updates to Posted Financials:** Posted vouchers and lines cannot be updated or deleted. Enforced by PostgreSQL triggers `trg_protect_posted_journal_lines` and `trg_protect_posted_voucher_header`.
2. **Companion Reversal Mechanism:** Correction of a posted voucher requires posting an explicit companion reversal voucher (`VoucherType.Reversal`), referencing the original voucher and swapping debit and credit entries.
3. **Single Reversal Invariant:** A posted voucher can only be reversed once (`IsReversed == true`, pointing to `ReversalVoucherId`). Attempting a second reversal throws `VOUCHER_ALREADY_REVERSED`. Verified by `JournalPersistenceAndImmutabilityTests.NonDestructiveReversal_PostsCompanionVoucher_AndPreventsDuplicateReversal`.
