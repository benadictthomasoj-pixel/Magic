# MAGIC ERP — PHASE E: ARCHITECTURE & FINANCIAL DESIGN

**Project:** MAGIC – Universal Business Management ERP  
**Phase:** E — Financial Management & Treasury  
**Status:** Approved & Under Active Implementation  
**Version:** 1.0.0  
**Date:** 2026-11-13  
**Authors:** MAGIC Architecture & Financial Core Engineering Team  

---

## 1. Executive Summary & Objective

Phase E implements the operational financial management and treasury subsystem for MAGIC ERP, seamlessly unifying Commercial Masters (Phase B), Inventory & Procurement (Phase C), POS & Sales Management (Phase D), and the Double-Entry Accounting Core.

The objective is to establish an authoritative financial lifecycle for:
1. **Accounts Receivable (AR):** Customer receivables ledger, invoice-wise open items, partial/full settlements, unallocated advances, credit notes, aging analysis (Current, 1–30, 31–60, 61–90, 90+ days), and customer statements.
2. **Customer Receipts & Collections:** Multi-tender collections (Cash, Bank Transfer, UPI, Cheque), partial and multi-invoice settlement distribution, advance receipt tracking, non-destructive voucher reversals, and receipt printing.
3. **Accounts Payable (AP):** Commercial supplier invoice/bill recognition, purchase invoice register, AP aging, payment tracking, debit note applications, and supplier statements. *(Separating commercial invoices from physical GRNs per accounting standards)*.
4. **Supplier Payments & Settlements:** Multi-invoice vendor settlements, partial disbursements, advance payments, advice generation, and companion reversal workflows.
5. **Bank & Cash Account Management:** Bank and cash account masters, opening balances, IFSC/branch routing, Chart of Accounts GL mapping, default account masking with audit-logged authorized reveals.
6. **Treasury Operations:** Balanced Contra vouchers for cash deposits, cash withdrawals, inter-bank transfers, cash-to-cash transfers, bank charges, and interest receipts.
7. **Bank Reconciliation (BRS):** CSV statement parser, duplicate detection, automated suggested matching against internal ledger entries, manual matching, adjustment vouchers, and difference reconciliation.
8. **Financial Voucher Workflows:** Real-time integration into the immutable double-entry `JournalVoucher` aggregate root.
9. **Financial Reports:** Comprehensive AR/AP aging, customer/supplier ledgers, Cash Book, Bank Book, and Treasury Movement reports.

---

## 2. Fundamental Accounting Invariants & Policies

| Rule ID | Invariant Specification | Enforcement Mechanism |
|---|---|---|
| **INV-FIN-01** | Zero Floating-Point Money: All monetary amounts, prices, allocations, and balances are stored and calculated as signed 64-bit integer paise (`long` / `BIGINT`). | C# `Money` value object & PostgreSQL `BIGINT` columns. |
| **INV-FIN-02** | Double-Entry Balancing: $\sum \text{Debits} \equiv \sum \text{Credits} > 0$ for every financial voucher. | `JournalVoucher.Validate()` & `AccountingApplicationService`. |
| **INV-FIN-03** | Immutability: Posted journal vouchers and allocation records are strictly append-only and cannot be updated or deleted. | Domain invariants, PostgreSQL triggers, and non-destructive reversal vouchers. |
| **INV-FIN-04** | Dual Control AR/AP Reconciliation: Total open subledger items for AR/AP must reconcile with GL Control Accounts (1201 Trade Debtors, 2111 Trade Creditors). | Automated integrity checks and subledger reconcilers. |
| **INV-FIN-05** | Allocation Bounds: Allocation amount must be positive, cannot exceed receipt/payment unallocated balance, and cannot exceed invoice outstanding balance. | Service validation & database check constraints (`allocated_amount_paise > 0`). |
| **INV-FIN-06** | Concurrency Protection: Multi-session simultaneous settlements of the same invoice are protected via row-level locks (`SELECT ... FOR UPDATE`) or transactional versioning. | Database transaction boundaries & status checks. |
| **INV-FIN-07** | Fiscal Period Guard: No financial transaction, voucher, or allocation can be posted into a locked or closed fiscal period. | `FiscalPeriod` domain status verification. |
| **INV-FIN-08** | Multi-Branch & Multi-Tenant Isolation: Every entity belongs strictly to a `CompanyId` and `BranchId`. Cross-tenant queries fail closed. | Enforced in Base Controller and Service queries. |

---

## 3. Subsystem Architectural Decomposition

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                                 MAGIC ERP FRONTEND (Angular 22)                         │
│  /accounting/receivables       /accounting/customer-receipts    /accounting/payables   │
│  /accounting/supplier-payments /accounting/bank-accounts        /accounting/treasury   │
│  /accounting/bank-reconciliation /accounting/financial-vouchers /accounting/financial-reports │
└──────────────────────────────────────────┬─────────────────────────────────────────────┘
                                           │ REST API (HTTPS / JSON / BigInt Paise)
┌──────────────────────────────────────────▼─────────────────────────────────────────────┐
│                            MAGIC.API (ASP.NET Core 10 Web API)                         │
│  ReceivablesController    CustomerReceiptsController    PayablesController             │
│  SupplierPaymentsController BankAccountsController      TreasuryController             │
│  BankReconciliationController FinancialVouchersController FinancialReportsController   │
└──────────────────────────────────────────┬─────────────────────────────────────────────┘
                                           │
┌──────────────────────────────────────────▼─────────────────────────────────────────────┐
│                           APPLICATION & DOMAIN LAYER                                   │
│  ┌─────────────────────────┐  ┌──────────────────────────┐  ┌───────────────────────┐  │
│  │   ReceivablesService    │  │     PayablesService      │  │    TreasuryService    │  │
│  └────────────┬────────────┘  └────────────┬─────────────┘  └───────────┬───────────┘  │
│               │                            │                            │              │
│  ┌────────────▼────────────┐  ┌────────────▼─────────────┐  ┌───────────▼───────────┐  │
│  │   CustomerReceiptService│  │  SupplierPaymentService  │  │ BankReconcileService  │  │
│  └────────────┬────────────┘  └────────────┬─────────────┘  └───────────┬───────────┘  │
│               │                            │                            │              │
│               └────────────────────────────┼────────────────────────────┘              │
│                                            ▼                                           │
│                     ┌──────────────────────────────────────────────┐                   │
│                     │  IAccountingApplicationService / GL Posting  │                   │
│                     └──────────────────────┬───────────────────────┘                   │
└────────────────────────────────────────────┼───────────────────────────────────────────┘
                                             │ EF Core 10 / Npgsql
┌────────────────────────────────────────────▼───────────────────────────────────────────┐
│                               POSTGRESQL 18 DATABASE                                   │
│  Schemas:                                                                              │
│  - accounting.* (journal_vouchers, journal_lines, accounts, ar_subledger_items, etc.)  │
│  - finance.*    (customer_receipts, supplier_invoices, supplier_payments,              │
│                  bank_cash_accounts, treasury_transactions, bank_statements)           │
│  - commercial.* (customers, suppliers, products)                                       │
│  - sales.*      (sales_invoices, sales_returns, credit_notes)                          │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Key Accounting Flow Details

### 4.1. Customer Receivables & Receipts Flow
1. **Invoice Recognition:** POS credit invoice or standard commercial sales invoice creates an open `ArSubledgerItemRecord` (`status = OPEN`, `outstanding_amount_paise = total_amount_paise`).
2. **Customer Receipt Creation:**
   - Cashier/Accountant registers a receipt from customer (e.g., ₹10,000 via Bank Transfer).
   - Receipt generates a balanced `Receipt` Journal Voucher:
     - **Debit:** Bank Account (GL 1112) ₹10,000
     - **Credit:** Customer Debtors Control (GL 1201) ₹10,000
3. **Allocation:**
   - Receipt is allocated against open AR items.
   - For each allocated invoice:
     - `SettledAmountPaise += allocation`
     - `OutstandingAmountPaise -= allocation`
     - `Status = Outstanding == 0 ? 'PAID' : 'PARTIALLY_PAID'`
   - Unallocated balance is tracked on the receipt as `UnallocatedAmountPaise` (Customer Advance).

### 4.2. Supplier Payables & Payments Flow
1. **Commercial Bill Recognition:** Supplier submits Bill/Tax Invoice. Finance user approves `SupplierInvoiceRecord`:
   - Generates a balanced `Purchase` Journal Voucher:
     - **Debit:** Commercial Purchases (GL 5101) ₹Net
     - **Debit:** Input CGST/SGST Credit (GL 1131/1132) ₹Tax
     - **Credit:** Trade Creditors Control (GL 2111) ₹Total
   - Creates open `ApSubledgerItemRecord` (`status = OPEN`, `outstanding_amount_paise = total`).
2. **Supplier Payment Disbursement:**
   - Finance user creates `SupplierPaymentRecord` (e.g., ₹15,000 from Bank Account).
   - Generates a balanced `Payment` Journal Voucher:
     - **Debit:** Trade Creditors Control (GL 2111) ₹15,000
     - **Credit:** Bank Account (GL 1112) ₹15,000
3. **Allocation:**
   - Settles open AP bills. Excess payment is recorded as `UnallocatedAmountPaise` (Supplier Advance).

### 4.3. Treasury Contra Movements Flow
- **Cash Deposit to Bank:**
  - **Debit:** Destination Bank (GL 1112)
  - **Credit:** Source Cash on Hand (GL 1111)
  - Voucher Type: `Contra`. Total cash & bank balance net change = 0.
- **Inter-Bank Transfer:**
  - **Debit:** Destination Bank B (GL 1112)
  - **Credit:** Source Bank A (GL 1112)
  - Voucher Type: `Contra`.

### 4.4. Bank Reconciliation (BRS) Flow
1. Select Bank Account and Period (e.g., Nov 1–30, 2026).
2. Upload Statement CSV (columns: Date, Description, Reference, Withdrawal, Deposit, Balance).
3. Validate and parse into `BankStatementLineRecord`.
4. Run Matching Algorithm:
   - Match internal GL bank journal lines (`accounting.journal_lines` for that account) by exact amount and date.
   - User reviews suggested matches and confirms.
   - For unrecorded items (e.g., bank service charges ₹250 or interest credit ₹1,200), user clicks "Create Adjustment Voucher" which generates and posts a balanced voucher directly to GL.
5. Reconciliation difference reaches ₹0. User clicks "Complete Reconciliation".

---

## 5. Unresolved Accounting Decision Isolation

- `DEC-ACC-002` (Inventory Valuation): Bounded to physical quantity flows; no fabricated automated COGS entries.
- `DEC-ACC-003` (Statutory GST Rounding): Encapsulated inside `IGstCalculationService`.
- `DEC-ACC-007` (MSME 45-day & Statutory Withholding): Due dates reflect explicit contractual dates; compliance flags isolated from core ledger arithmetic.
