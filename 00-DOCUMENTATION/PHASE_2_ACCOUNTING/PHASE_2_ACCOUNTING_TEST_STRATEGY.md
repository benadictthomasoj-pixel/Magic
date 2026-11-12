# MAGIC — Phase 2: Accounting Foundation
## Comprehensive Accounting Test Strategy & Quality Protocols

**Document Version:** 2.0.0-DRAFT  
**Phase:** 2 — Accounting Foundation  
**Stage:** 1 — Architecture and Database Design  
**Implementation Status:** PROPOSED (Testing Strategy Specification Only)  
**Author:** Antigravity AI & Architecture Team  
**Last Updated:** 2026-11-12  

---

## 1. Testing Philosophy & Quality Gate Principle

**Core Financial Law:** In enterprise accounting software, no feature or posting endpoint may be declared complete merely because a happy-path test passes or a screen renders a voucher. Every accounting primitive, validation rule, and posting mechanism must undergo rigorous, deterministic verification across all precision limits, race conditions, and error recovery workflows.

---

## 2. Multi-Tiered Accounting Testing Pyramid

```
                                 / \
                                /   \
                               / E2E \       <-- Playwright UI Automation: Full Cashier / Accounting Workflows
                              /-------\
                             / Concurr \     <-- Multi-terminal race conditions, concurrent voucher numbering
                            /-----------\
                           / Integration \   <-- Real PostgreSQL 18: Foreign keys, triggers, constraints, locks
                          /---------------\
                         / Property-Based  \ <-- 100,000 synthetic vouchers, odd-cent tax splits, Section 170
                        /-------------------\
                       /     Unit Tests      \ <-- Pure domain models, Money, Invariant equations (xUnit)
                      /-----------------------\
```

---

## 3. Detailed Test Suites & Verification Protocols

### 3.1 Suite 1: Pure Domain & Mathematical Invariant Tests (xUnit)
- **Execution Target:** In-memory, sub-millisecond execution (< 1ms per fact). Zero database dependencies.
- **Key Test Cases:**
  1. **`Voucher_WithEqualDebitsAndCredits_PassesValidation`:**
     - Creates voucher with 3 debits (10,000, 25,000, 15,000 paise = 50,000 paise) and 2 credits (30,000, 20,000 paise = 50,000 paise).
     - Asserts `voucher.ValidateBalance()` succeeds with zero discrepancy.
  2. **`Voucher_WithUnbalancedDebitsAndCredits_ThrowsUnbalancedVoucherException`:**
     - Debits = 50,000 paise; Credits = 49,999 paise (Delta = 1 paise).
     - Asserts that creating or attempting to transition to `POSTED` throws `DomainException("ACC_VOUCHER_UNBALANCED", ...)` with exact delta (1 paise).
  3. **`VoucherLine_WithBothDebitAndCredit_ThrowsInvalidOperationException`:**
     - Attempts to construct a journal line with `Debit = 500` and `Credit = 500`.
     - Asserts immediate rejection (a line must be strictly one-sided).
  4. **`VoucherLine_WithNegativeAmount_ThrowsDomainException`:**
     - Asserts that negative paise values are rejected at construction (`Money.Paise >= 0`).
  5. **`IntegerPaise_AdditionOverflow_ThrowsOverflowException`:**
     - Asserts `checked` arithmetic throws `OverflowException` if sum exceeds `long.MaxValue`.
  6. **`Section170Rounding_OddRemainderCases_BehavesDeterministically`:**
     - Tests all remainders $0 \dots 99$:
       - Remainder $49 \rightarrow \text{Delta} = -49 \text{ paise}$ (rounded down to ₹0.00).
       - Remainder $50 \rightarrow \text{Delta} = +50 \text{ paise}$ (rounded up to ₹1.00).
       - Remainder $99 \rightarrow \text{Delta} = +1 \text{ paise}$ (rounded up to ₹1.00).
       - Asserts $\text{ExactTotal} + \text{Delta} \pmod{100} \equiv 0$.

---

### 3.2 Suite 2: Property-Based Financial Math Tests (FsCheck / Synthetic Generation)
- **Execution Target:** 100,000 randomized synthetic gross prices ($₹0.01 \dots ₹10,000,000.00$) across standard GST brackets (0%, 5%, 12%, 18%, 28%).
- **Key Invariant Invariants Asserted:**
  1. **Zero Tax Leakage:**
     $$\text{TaxablePaise} + \text{CGSTPaise} + \text{SGSTPaise} \equiv \text{GrossPaise}$$
     Asserted for every single case with intra-state split ($\text{CGST} = \lfloor \text{TotalGST}/2 \rfloor$, $\text{SGST} = \text{TotalGST} - \text{CGST}$).
  2. **Section 170 Invariant:**
     $$\text{FinalPayablePaise} - \text{ExactInvoiceTotalPaise} = \text{RoundOffDeltaPaise}$$
     where $\text{FinalPayablePaise} \pmod{100} \equiv 0$ and $|\text{RoundOffDeltaPaise}| \le 50$.

---

### 3.3 Suite 3: PostgreSQL Database Integration & Constraint Tests
- **Execution Target:** Isolated PostgreSQL 18 instance (`magic_dev`) via `WebApplicationFactory<Program>`.
- **Key Test Cases:**
  1. **`Database_RejectsUnbalancedVoucherInsert`:**
     - Attempts raw SQL insert of voucher where `total_debit_paise != total_credit_paise`.
     - Asserts database check constraint `chk_vouchers_balance` violation.
  2. **`Database_RejectsUpdateOrDeleteOnPostedVoucher`:**
     - Inserts and commits a voucher with `status = 'POSTED'`.
     - Attempts raw `UPDATE accounting.vouchers SET narration = 'HACKED' WHERE id = @id`.
     - Asserts database trigger `trg_prevent_posted_voucher_tampering` raises PostgreSQL exception code `55000`.
     - Attempts raw `DELETE FROM accounting.vouchers WHERE id = @id`.
     - Asserts PostgreSQL exception code `55000`.
  3. **`Database_RejectsDuplicateVoucherNumberForSameBranchAndYear`:**
     - Inserts voucher with number `VCH-MAIN-2026-JV-000001`.
     - Attempts second insert with identical branch, fiscal year, and voucher number.
     - Asserts unique constraint `uq_vouchers_branch_number` violation.
  4. **`Database_RejectsDuplicateIdempotencyKey`:**
     - Asserts unique constraint `uq_vouchers_branch_idempotency` blocks duplicate concurrent inserts.

---

### 3.4 Suite 4: Period Governance & Chronological Locking Tests
- **Key Test Cases:**
  1. **`PostVoucher_InOpenPeriod_Succeeds`:**
     - Posts voucher dated `2026-05-15` in `OPEN` period `2026-05`.
     - Asserts voucher status is `POSTED` and GL lines are accessible.
  2. **`PostVoucher_InLockedPeriod_WithoutSupervisor_FailsWith403`:**
     - Sets period `2026-04` state to `LOCKED`.
     - Attempts posting voucher dated `2026-04-20` with standard clerk identity.
     - Asserts HTTP `403 Forbidden` (`ACC_PERIOD_LOCKED`).
  3. **`PostVoucher_InLockedPeriod_WithSupervisorElevation_SucceedsAndAudits`:**
     - Supplies valid supervisor elevation token.
     - Asserts posting succeeds and records supervisor user ID in `audit.system_audit_logs`.

---

### 3.5 Suite 5: High-Concurrency Stress & Sequence Allocation Tests
- **Key Test Cases:**
  1. **`ConcurrentVoucherPosting_AllocatesGaplessSequentialNumbers`:**
     - Spawns 10 parallel asynchronous tasks posting vouchers simultaneously to the same branch.
     - Uses database transaction with `FOR UPDATE` sequence row locking.
     - Asserts:
       - Exactly 10 vouchers committed.
       - Sequence numbers are consecutive: `000001` through `000010`.
       - Zero duplicate voucher numbers.
       - Zero gaps in numbering.
  2. **`ConcurrentIdempotentRequests_CommitExactlyOnce`:**
     - Submits identical `Idempotency-Key` across 5 simultaneous threads.
     - Asserts exactly 1 journal voucher created; 4 threads receive identical cached response with `X-Cache-Lookup: HIT`.

---

### 3.6 Suite 6: Subledger-to-GL Control Parity & Financial Statement Tests
- **Key Test Cases:**
  1. **`CustomerInvoiceAndPayment_MaintainsGL1130Parity`:**
     - Posts ₹100,000 credit sale.
     - Verifies AR subledger balance = ₹100,000 and GL account `1130` balance = ₹100,000.
     - Posts ₹40,000 partial receipt.
     - Verifies AR subledger open item = ₹60,000 and GL account `1130` balance = ₹60,000.
     - Asserts: $\sum \text{AR Items} \equiv \text{GL 1130 Balance}$.
  2. **`TrialBalance_EqualityAssertion`:**
     - Executes full Trial Balance query after 50 mixed transactions (Sales, Purchases, Payments, Expenses).
     - Asserts: $\sum \text{Debit} - \sum \text{Credit} = 0$.
  3. **`BalanceSheet_EquationAssertion`:**
     - Derives Balance Sheet.
     - Asserts: $\text{Assets} \equiv \text{Liabilities} + \text{Equity} + \text{Net Profit}$.

