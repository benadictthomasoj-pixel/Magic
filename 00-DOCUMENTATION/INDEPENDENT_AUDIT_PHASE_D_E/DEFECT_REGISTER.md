# MAGIC ERP — Comprehensive Independent Audit
## Module 11: Defect Register & Vulnerability Catalog
**Scope:** Phase D (POS & Sales Management) & Phase E (Financial Management & Treasury)  
**Date:** November 2026  
**Auditor:** Independent Technical & Financial Architecture Auditor  
**Audit Mode:** Read-Only Source Code, Database & Runtime Verification  

---

## 1. Defect Severity Matrix

| Severity Level | Definition | Count |
| :--- | :--- | :--- |
| **CRITICAL** | System-breaking architectural flaw, data loss, ledger desynchronization, financial misstatement, or regulatory non-compliance. Blocks production deployment. | **2** |
| **HIGH** | Significant business logic flaw, financial calculation error, audit trail distortion, or state inconsistency. Requires mandatory fix before approval. | **4** |
| **MEDIUM** | Edge-case failure, concurrency risk, missing boundary validation, or suboptimal performance. Must be remediated in the current release cycle. | **4** |
| **LOW** | Minor UI cosmetic issue, non-critical logging omission, or developer ergonomics enhancement. | **0** |
| **TOTAL DEFECTS** | | **10** |

---

## 2. Comprehensive Defect Catalog

### DEF-DE-001: Missing General Ledger Journal Voucher on POS Checkout
- **Severity:** **CRITICAL**
- **Subsystem:** Phase D (POS Billing) $\leftrightarrow$ Accounting (General Ledger)
- **Source File:** `01-SRC/Backend/Magic.Core/Application/Sales/SalesServices.cs`
- **Source Location:** `PosService.CheckoutAsync` (Lines 302–352)
- **Description:**
  When a POS sale is completed, the system writes the invoice header, line items, payment records, stock movements, and updates cashier cash drawers. However, it never instantiates or persists a `Sales` journal voucher into `accounting.journal_vouchers` and `accounting.journal_lines`.
- **Financial & Business Impact:**
  - The General Ledger has zero visibility into retail turnover, daily sales revenue, cash in hand, or card receivables.
  - Statutory Output GST liability accounts (`2121 - Output CGST`, `2122 - Output SGST`) are never credited. Any GST return (GSTR-1, GSTR-3B) generated from the GL reports ₹0 liability.
  - Cost of Goods Sold (COGS) and inventory valuation accounts are never updated.
  - Trial Balance and Balance Sheet are materially misstated.
- **Steps to Reproduce:**
  1. Open a cashier shift and add items to a cart in POS.
  2. Tender full payment via cash and click `Complete Sale`.
  3. Query `sales.sales_invoices`: record exists with `Status = 'Completed'`.
  4. Query `accounting.journal_vouchers WHERE source_document_id = invoice.id`: returns **0 records**.
  5. Check Trial Balance for Account `4100 - Sales Revenue`: balance remains unchanged.
- **Expected Behavior:**
  Checkout must atomically post a balanced Journal Voucher:
  - Debit: Cash / Bank GL Account (`1110` / `1120`)
  - Credit: Sales Revenue (`4100`)
  - Credit: Output CGST (`2121`)
  - Credit: Output SGST (`2122`)
  - Debit/Credit: Round-off Expense/Income (`5190`)
- **Remediation Guidance:**
  In `PosService.CheckoutAsync`, inject `IJournalVoucherService` or create a `JournalVoucherRecord` inside the same database transaction scope.

---

### DEF-DE-002: Missing AR Subledger Posting & Inability to Perform Credit Sales
- **Severity:** **CRITICAL**
- **Subsystem:** Phase D (Sales) $\leftrightarrow$ Phase E (Accounts Receivable Subledger)
- **Source File:** `01-SRC/Backend/Magic.Core/Application/Sales/SalesServices.cs`
- **Source Location:** `PosService.CheckoutAsync` (Line 307) & `SalesServices.cs` (Lines 111–135)
- **Description:**
  `PosService.CheckoutAsync` strictly validates that `totalPaidPaise >= grandTotalPaise` and explicitly sets `OutstandingBalancePaise = 0`. No code anywhere in Phase D instantiates `ArSubledgerItemRecord`. Consequently, credit sales cannot be executed, and the Accounts Receivable Subledger never tracks sales invoices.
- **Financial & Business Impact:**
  - B2B and wholesale customers who purchase on commercial credit terms (e.g. Net 30 days) cannot be billed through the system.
  - The Customer Receivables aging report (`/accounting/receivables`) displays empty records for all sales customers.
  - The Customer Receipts subsystem (`CustomerReceiptsComponent`) cannot allocate incoming customer payments against commercial sales invoices because no AR subledger items exist to match.
- **Steps to Reproduce:**
  1. Attempt checkout with a registered customer where `totalPaidPaise < grandTotalPaise`.
  2. Backend throws `InvalidOperationException("Total paid amount ... cannot be less than invoice grand total")`.
  3. Inspect `accounting.ar_subledger_items`: zero records linked to `SalesInvoice`.
- **Expected Behavior:**
  When selling to an approved credit customer, `totalPaidPaise` can be less than `grandTotalPaise`. The system must:
  - Set `OutstandingBalancePaise = grandTotalPaise - totalPaidPaise`.
  - Create an `ArSubledgerItemRecord` in `accounting.ar_subledger_items` with `DebitAmountPaise = grandTotalPaise`, `PendingAmountPaise = OutstandingBalancePaise`, and `DueDate = InvoiceDate + CustomerCreditDays`.
- **Remediation Guidance:**
  Allow partial payment for customers with `CreditLimit > 0` and credit status active; instantiate and save `ArSubledgerItemRecord` during invoice completion.

---

### DEF-DE-003: Sales Return Pricing Leak (Discount Exclusion on Refunds)
- **Severity:** **HIGH**
- **Subsystem:** Phase D (Sales Returns & Credit Notes)
- **Source File:** `01-SRC/Backend/Magic.Core/Application/Sales/SalesServices.cs`
- **Source Location:** `SalesReturnService.CreateReturnAsync` (Lines 880–883)
- **Description:**
  When generating a sales return, the refund amount is calculated using:
  `itemTotalPaise = returnItem.Quantity * originalLine.UnitPricePaise;`
  It completely ignores `originalLine.DiscountPaise` and any proportional invoice-level discount granted during the original checkout.
- **Financial & Business Impact:**
  - Direct financial leak: Customers returning discounted items receive a refund based on the full list price, resulting in the business losing cash.
  - Credit Notes overestimate tax reversals and liability reductions.
- **Steps to Reproduce:**
  1. Sell an item priced at ₹1,000 with a ₹200 promotional discount (customer paid ₹800 + tax).
  2. Initiate a sales return for 1 unit.
  3. The return calculates refund as ₹1,000 + tax, paying out ₹200 more than was originally collected.
- **Expected Behavior:**
  Refund amount must be computed using net realized line price:
  `EffectiveUnitPrice = (originalLine.LineTotalPaise - originalLine.DiscountPaise) / originalLine.Quantity`.
- **Remediation Guidance:**
  Update `CreateReturnAsync` to derive refund basis from net paid amounts inclusive of item and bill discount prorations.

---

### DEF-DE-004: Cash Drawer Variance on Non-Cash Returns
- **Severity:** **HIGH**
- **Subsystem:** Phase D (Cash Management) $\leftrightarrow$ Phase D (Sales Returns)
- **Source File:** `01-SRC/Backend/Magic.Core/Application/Sales/SalesServices.cs`
- **Source Location:** `SalesReturnService.CreateReturnAsync` (Line 989)
- **Description:**
  When a sales return is processed during an active cashier session, the system unconditionally executes:
  `session.ExpectedCashPaise -= refundAmountPaise;`
  This deduction happens regardless of whether the refund was paid via Cash, Card chargeback, UPI refund, or issued as an uncashed Store Credit Note.
- **Financial & Business Impact:**
  - When closing the cash register (`CashManagementComponent`), the physical cash drawer will show an artificial cash surplus because the system deducted non-cash amounts from expected physical cash.
  - Corrupts cashier accountability and daily reconciliation records.
- **Steps to Reproduce:**
  1. Open cashier session with ₹5,000 opening cash.
  2. Process a sales return of ₹1,000 selecting `RefundMethod = "CreditNote"`.
  3. View cash closing: `ExpectedCashPaise` is reduced to ₹4,000 even though ₹0 physical cash left the drawer. Cashier counts ₹5,000, creating an unexplained ₹1,000 variance.
- **Expected Behavior:**
  Only deduct `ExpectedCashPaise` when `returnRecord.RefundMethod == "Cash"`.
- **Remediation Guidance:**
  Add conditional check before modifying `session.ExpectedCashPaise`.

---

### DEF-DE-005: Arbitrary Warehouse Fallback on Invoice Cancellation
- **Severity:** **HIGH**
- **Subsystem:** Phase D (Sales Invoices) $\leftrightarrow$ Phase C (Inventory)
- **Source File:** `01-SRC/Backend/Magic.Core/Application/Sales/SalesServices.cs`
- **Source Location:** `SalesInvoiceService.CancelInvoiceAsync` (Lines 804–812)
- **Description:**
  `SalesInvoiceRecord` does not persist the `WarehouseId` from which items were originally dispatched. When an invoice is cancelled, the method restores stock by executing:
  `var defaultWarehouse = await dbContext.Warehouses.FirstOrDefaultAsync(w => w.IsActive, ct);`
  restoring inventory into whichever warehouse is returned first by the database engine.
- **Financial & Business Impact:**
  - In a multi-branch or multi-warehouse enterprise, cancelling a sale made at Warehouse B will arbitrarily restore inventory to Warehouse A.
  - Creates phantom stock in Warehouse A and stock depletion in Warehouse B, causing false stockouts and corrupted valuation.
- **Steps to Reproduce:**
  1. Configure two active warehouses: Warehouse A (Main) and Warehouse B (Outlet).
  2. Bill an item dispatched from Warehouse B.
  3. Cancel the invoice.
  4. Inspect stock ledger: stock is added back to Warehouse A.
- **Expected Behavior:**
  `SalesInvoiceRecord` (or each `SalesInvoiceItemRecord`) must record `WarehouseId`, and cancellation must restore stock strictly to that warehouse.
- **Remediation Guidance:**
  Add `WarehouseId` column to `sales.sales_invoices` and use it during cancellation.

---

### DEF-DE-006: Unvalidated Bank Statement Line Matching in Reconciliation
- **Severity:** **HIGH**
- **Subsystem:** Phase E (Treasury & Bank Reconciliation)
- **Source File:** `01-SRC/Backend/Magic.Core/Application/Accounting/FinancialServices.cs`
- **Source Location:** `BankReconciliationService.MatchStatementLineAsync` (Lines 475–520)
- **Description:**
  When matching a bank statement line to a General Ledger journal line:
  1. It does not verify that `JournalLine.AccountId == bankAccount.GlAccountId`.
  2. It does not verify whether the journal line was already matched to another statement line.
  3. It permits matching even after the reconciliation statement status has been marked as `"RECONCILED"`.
- **Financial & Business Impact:**
  - Allows matching a bank deposit against an unrelated Expense or Revenue journal line.
  - Allows double-matching the same ledger entry against multiple bank credits, falsifying the adjusted book balance.
  - Allows retroactively altering closed historical bank reconciliation statements.
- **Steps to Reproduce:**
  1. Call `MatchStatementLineAsync` passing a `JournalLineId` belonging to Account `5100 - Rent Expense`.
  2. The operation succeeds with status code 200 and marks the line as matched.
- **Expected Behavior:**
  System must validate account identity, ensure journal line is unmatched, and prevent modifications to locked reconciliation periods.
- **Remediation Guidance:**
  Add strict assertions on GL account identity, duplicate match status, and statement reconciliation status.

---

### DEF-DE-007: Missing Inter-State Procurement Tax (IGST) in Accounts Payable
- **Severity:** **HIGH**
- **Subsystem:** Phase E (Accounts Payable) & Chart of Accounts
- **Source File:** `01-SRC/Backend/Magic.Core/Application/Accounting/FinancialServices.cs`
- **Source Location:** `PayablesService.CreateSupplierBillAsync` (Lines 185–192)
- **Description:**
  When creating a supplier bill, tax amounts are unconditionally split 50/50 into CGST and SGST. The system lacks support for Integrated Goods and Services Tax (IGST). Furthermore, the Chart of Accounts lacks Account `1133 - Input IGST` and `2123 - Output IGST`.
- **Financial & Business Impact:**
  - MAGIC cannot process inter-state procurement or supply without violating statutory GST reporting requirements.
  - Inter-state input tax credit (ITC) cannot be claimed under IGST, leading to tax penalties and incorrect GSTR-2B reconciliation.
- **Steps to Reproduce:**
  1. Create a bill for an out-of-state vendor located in Karnataka while operating in Maharashtra.
  2. Submit invoice with 18% IGST.
  3. Backend records it as 9% CGST and 9% SGST.
- **Expected Behavior:**
  If supplier state differs from company branch state, tax must post to `Input IGST` (`1133`).
- **Remediation Guidance:**
  Seed accounts `1133` and `2123` into `chart_of_accounts`, and add place-of-supply tax logic in `CreateSupplierBillAsync`.

---

### DEF-DE-008: Optimistic Concurrency Failure on High-Volume POS Checkout
- **Severity:** **MEDIUM**
- **Subsystem:** Phase D (Sales) $\leftrightarrow$ Phase C (Inventory)
- **Source File:** `01-SRC/Backend/Magic.Core/Application/Sales/SalesServices.cs`
- **Source Location:** `PosService.CheckoutAsync` (Lines 318–335)
- **Description:**
  `StockBalanceRecord` enforces optimistic concurrency via a `xmin` or version token. When two cashiers bill the same fast-moving SKU concurrently, the second transaction fails with `DbUpdateConcurrencyException`. The service has no automatic retry loop or pessimistic row-locking (`FOR UPDATE`).
- **Financial & Business Impact:**
  - Cashier checkout screen displays an unexpected error modal during peak retail hours.
  - Causes customer delays at the retail checkout counter.
- **Steps to Reproduce:**
  1. Trigger 2 concurrent checkouts for Product A at the same second.
  2. Transaction 2 throws `DbUpdateConcurrencyException` and aborts.
- **Expected Behavior:**
  The checkout service should implement a Polly retry policy (up to 3 retries) or acquire a database row lock on the affected stock balance record.
- **Remediation Guidance:**
  Wrap stock deduction in an exponential backoff retry loop or use `PostgreSqlExtensions.ForUpdate()`.

---

### DEF-DE-009: 64-Bit Integer Precision Boundary in Frontend TypeScript
- **Severity:** **MEDIUM**
- **Subsystem:** Cross-Phase (All Financial & Sales Screens)
- **Source File:** Frontend TypeScript models across `src/app/core/models/*`
- **Description:**
  All backend monetary amounts are represented as 64-bit signed integers (`long`, `Int64`) in paise. JavaScript/TypeScript numbers are 64-bit IEEE-754 floats with an integer safety boundary of `Number.MAX_SAFE_INTEGER` ($2^{53}-1 = 9,007,199,254,740,991$ paise $\approx$ ₹90,071,992,547,409). While this limit is massive for individual transactions, cumulative enterprise aggregations spanning multi-year ledgers could exceed safe float precision.
- **Financial & Business Impact:**
  - Negligible for SME turnover; theoretical risk for large enterprise cumulative ledger balances.
- **Remediation Guidance:**
  Accept strings or use `BigInt` for multi-year aggregation fields in TypeScript.

---

### DEF-DE-010: Cashier Session Not Updated on Invoice Cancellation
- **Severity:** **MEDIUM**
- **Subsystem:** Phase D (Sales Invoices & Cash Management)
- **Source File:** `01-SRC/Backend/Magic.Core/Application/Sales/SalesServices.cs`
- **Source Location:** `SalesInvoiceService.CancelInvoiceAsync` (Lines 810–830)
- **Description:**
  When an invoice is cancelled on the same day it was issued, the invoice status changes to `Cancelled`, but the associated `CashierShiftRecord.ExpectedCashPaise` is not adjusted, and payment records remain marked as `Completed`.
- **Financial & Business Impact:**
  - If a cashier bills an invoice in error and immediately cancels it, the cash closing screen still expects the cashier to surrender the cash from the cancelled invoice, causing an unexplained cash shortage.
- **Steps to Reproduce:**
  1. Open cashier session.
  2. Bill ₹500 cash invoice. Expected cash = ₹500.
  3. Cancel invoice.
  4. View cashier shift: Expected cash remains ₹500 even though the transaction was cancelled.
- **Expected Behavior:**
  Invoice cancellation during an open cashier shift must decrement the cashier's expected cash and update payment records to `Cancelled`.
- **Remediation Guidance:**
  Locate open shift for the cashier and reverse payment impact upon cancellation.

---

## 3. Summary of Identified Vulnerabilities & Remediation Priority

| Finding ID | Title | Severity | Remediation Priority |
| :--- | :--- | :--- | :--- |
| **DEF-DE-001** | Missing General Ledger Journal Voucher on POS Checkout | CRITICAL | Priority 1 (Blocker) |
| **DEF-DE-002** | Missing AR Subledger Posting & Inability to Perform Credit Sales | CRITICAL | Priority 1 (Blocker) |
| **DEF-DE-003** | Sales Return Pricing Leak (Discount Exclusion on Refunds) | HIGH | Priority 2 (Mandatory) |
| **DEF-DE-004** | Cash Drawer Variance on Non-Cash Returns | HIGH | Priority 2 (Mandatory) |
| **DEF-DE-006** | Unvalidated Bank Statement Line Matching in Reconciliation | HIGH | Priority 2 (Mandatory) |
| **DEF-DE-007** | Missing Inter-State Procurement Tax (IGST) in Accounts Payable | HIGH | Priority 2 (Mandatory) |
| **DEF-DE-005** | Arbitrary Warehouse Fallback on Invoice Cancellation | MEDIUM | Priority 3 (Release Cycle) |
| **DEF-DE-008** | Optimistic Concurrency Failure on High-Volume POS Checkout | MEDIUM | Priority 3 (Release Cycle) |
| **DEF-DE-010** | Cashier Session Not Updated on Invoice Cancellation | MEDIUM | Priority 3 (Release Cycle) |
| **DEF-DE-009** | 64-Bit Integer Precision Boundary in Frontend TypeScript | MEDIUM | Priority 4 (Hardening) |

