# MAGIC ERP — Phase D & E Comprehensive Remediation Plan
## Master Implementation Blueprint for R1–R10

**Project:** MAGIC — Universal Business Management ERP  
**Baseline Git Commit:** `a78c2e3` (following audit documentation commit on `b3dcd48`)  
**Scope:** Phase D (POS & Sales Management) & Phase E (Financial Management & Treasury)  
**Authorization:** Approved by Project Owner  
**Auditor/Implementer:** Antigravity AI Engineering Assistant  
**Date:** November 2026  

---

## 1. Remediation Scope & Defect Mapping

| Finding ID | Title | Severity | Remediation Code | Target Modules |
| :--- | :--- | :---: | :---: | :--- |
| **DEF-DE-001** | Missing General Ledger Journal Voucher on POS Checkout | CRITICAL | **R1** | `Magic.Core/Application/Sales/SalesServices.cs`, GL Voucher Generation |
| **DEF-DE-002** | Missing AR Subledger Posting & Inability to Perform Credit Sales | CRITICAL | **R2** | `SalesServices.cs`, AR Subledger, `PosCheckoutComponent` |
| **DEF-DE-003** | Sales Return Pricing Leak (Discount Exclusion on Refunds) | HIGH | **R3** | `SalesServices.cs` (`SalesReturnService`) |
| **DEF-DE-004** | Cash Drawer Variance on Non-Cash Returns | HIGH | **R4** | `SalesServices.cs` (`SalesReturnService`, Cashier Session) |
| **DEF-DE-005** | Arbitrary Warehouse Selection on Invoice Cancellation | HIGH | **R5** | `SalesInvoiceRecord.cs`, `SalesServices.cs`, Inventory Restoration |
| **DEF-DE-006** | Unvalidated Bank Statement Line Matching | HIGH | **R6** | `FinancialServices.cs` (`BankReconciliationService`) |
| **DEF-DE-007** | Missing Inter-State Procurement Tax (IGST) | HIGH | **R7** | `FinancialServices.cs`, Chart of Accounts Seed (1133/2123) |
| **DEF-DE-008** | Optimistic Concurrency Failure on Contended Stock Deduction | MEDIUM | **R8** | `SalesServices.cs` (Retry policy / concurrency handling) |
| **DEF-DE-009** | Frontend 64-Bit Integer Representation Boundaries | MEDIUM | **R9** | Frontend TypeScript models, Money formatting pipes/utilities |
| **DEF-DE-010** | Cashier Session Not Updated on Invoice Cancellation | MEDIUM | **R10** | `SalesServices.cs` (`SalesInvoiceService`, Cashier Session) |

---

## 2. Execution Phases & Strict Order of Implementation

### Phase 1: Critical Financial Integration (Blockers)
- **R1:** General Ledger Posting on POS Checkout (Atomic Sales Journal Voucher creation, balanced double-entry GL lines).
- **R2:** Customer Credit Sales & Accounts Receivable Integration (Credit-limit validation, partial tender, AR Subledger items).
- *Verification:* Add unit and integration tests verifying GL vouchers and AR subledger generation upon checkout.

### Phase 2: High-Severity Sales & Inventory Corrections
- **R3:** Discount-Aware Sales Returns (Prorated net unit prices, preventing refund leaks).
- **R4:** Tender-Aware Cash Drawer Deductions (Non-cash returns do not reduce physical cash in drawer).
- **R5:** Warehouse Restoration on Invoice Cancellation (Store originating `WarehouseId` and restore to exact warehouse).
- *Verification:* Integration tests for partial/discounted returns, non-cash returns, and multi-warehouse cancellation.

### Phase 3: High-Severity Financial & Tax Corrections
- **R6:** Bank Reconciliation Safeguards (Account identity validation, duplicate-match prevention, locked statement protection).
- **R7:** Inter-State Procurement IGST Accounting (Seed Account 1133/2123, place-of-supply tax classification in AP).
- *Verification:* BRS invalid match tests, locked statement tests, IGST bill creation tests.

### Phase 4: Medium-Severity Reliability & Precision Corrections
- **R8:** POS Stock Concurrency Resilience (Exponential backoff retry on stock balance concurrency conflict).
- **R9:** Frontend 64-Bit Money Precision & Safe Aggregations (Safe BigInt/string serialization & display scaling).
- **R10:** Cashier Shift Reversal on Invoice Cancellation (Deduct payment contributions from active cashier session).
- *Verification:* Concurrency stress test, cancellation shift test, large number formatting test.

### Phase 5: Cross-Module Regression & Independent Verification
- Execute complete backend test suite (`Magic.sln`).
- Execute complete frontend test suite (`npm test`).
- Execute end-to-end multi-step workflow verification.
- Author all remaining documentation in `00-DOCUMENTATION/PHASE_DE_REMEDIATION/`.
