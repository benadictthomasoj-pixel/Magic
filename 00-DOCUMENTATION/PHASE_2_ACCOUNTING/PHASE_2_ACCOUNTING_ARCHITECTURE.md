# MAGIC — Phase 2: Accounting Foundation
## Architectural Specification: Double-Entry Accounting Core

**Document Version:** 2.0.0-DRAFT  
**Phase:** 2 — Accounting Foundation  
**Stage:** 1 — Architecture and Database Design  
**Implementation Status:** NOT AUTHORIZED (Planning & Design Only)  
**Author:** Antigravity AI & Architecture Team  
**Last Updated:** 2026-11-12  

---

## 1. Executive Summary & Domain Scope

In the MAGIC Universal Business Management ERP, the **Accounting Module** is not merely an isolated record-keeping tool; it is the **authoritative, central financial foundation** of the entire enterprise. Every operational event that impacts company assets, liabilities, revenues, or expenses—including retail and wholesale sales, purchase receipts, inventory adjustments, payroll, fixed asset lifecycle, and taxation—must flow into the general ledger through structured, auditable, and immutable accounting transactions.

### 1.1 Absolute Greenfield Mandate
In strict accordance with the MAGIC Project Charter:
- The accounting architecture is designed completely from scratch.
- Zero source code, database tables, stored procedures, APIs, or business logic are imported, copied, migrated, or reused from legacy systems (such as Billing Logics) or external templates.
- All domain rules, invariants, and workflows are grounded in approved MAGIC architectural standards and Indian GAAP / statutory frameworks.

### 1.2 Core Responsibilities of the Accounting Engine
1. **Hierarchical Chart of Accounts (COA):** Maintain a standardized, company-scoped account classification covering Assets, Liabilities, Equity, Revenue, and Expenses.
2. **Double-Entry Journal & Voucher Management:** Enforce the fundamental accounting postulate ($\sum \text{Debit} \equiv \sum \text{Credit}$) across all manual and automated transactions in 64-bit integer paise.
3. **Immutable General Ledger (GL):** Maintain an unalterable history of financial movements. Corrections are executed strictly via balanced reversal vouchers.
4. **Trial Balance Engine:** Validate and report periodic financial equality at both detail account and group summary levels.
5. **Fiscal Period Governance:** Manage fiscal years and accounting periods with strict locking protocols to prevent unauthorized backdated entries.
6. **Accounts Receivable (AR) & Accounts Payable (AP) Subledgers:** Track open customer and supplier items with itemized allocation, aging analysis, and real-time reconciliation against GL control accounts.
7. **Derived Financial Statements:** Generate Profit & Loss, Balance Sheet, and Cash Flow statements derived directly from posted GL records with zero independent summary totals.

---

## 2. Mandatory Financial Invariants

The accounting engine enforces the following non-negotiable architectural and financial invariants across all layers:

| Invariant ID | Rule Specification | Architectural Enforcement Mechanism |
|---|---|---|
| **INV-FIN-01** | **64-Bit Integer Monetary Units** | All INR currency values are represented and stored as 64-bit signed integers (`long` in C#, `BIGINT` in PostgreSQL), representing exact integer paise (₹1.00 = 100 paise). Floating-point representations (`float`, `double`, `real`) are strictly prohibited. |
| **INV-FIN-02** | **Absolute Double-Entry Invariance** | For every posted journal voucher $V$: $\sum_{i=1}^n \text{DebitPaise}_i = \sum_{j=1}^m \text{CreditPaise}_j$ where $\text{DebitPaise}, \text{CreditPaise} \in \mathbb{Z}_{\ge 0}$. An unbalanced voucher can never be saved in a `POSTED` state. |
| **INV-FIN-03** | **Immutability of Posted Records** | Once a journal voucher is transitioned to the `POSTED` state, it becomes immutable. Database-level constraints, application boundaries, and security triggers strictly prohibit `UPDATE` or `DELETE` operations on posted records. |
| **INV-FIN-04** | **Non-Destructive Reversals** | Errors in posted entries must be corrected exclusively by posting a companion Reversal Voucher (`TX_REVERSAL`) that exactly mirrors the original debit and credit legs with inverted sign or swapped positions, followed by a new replacement voucher. |
| **INV-FIN-05** | **Closed Period Posting Rejection** | Any transaction attempting to post with an effective accounting date falling in a closed or locked `FiscalPeriod` is rejected unconditionally, unless a privileged supervisor elevation token is validated. |
| **INV-FIN-06** | **Subledger-to-GL Control Parity** | The sum of all active customer subledger balances must equal GL Control Account `1130 (Accounts Receivable)`. The sum of all active supplier subledger balances must equal GL Control Account `2110 (Accounts Payable)`. Subledgers are updated atomically within the same database transaction. |
| **INV-FIN-07** | **Deterministic Tax Math** | GST amounts are calculated in integer paise using basis points ($1\% = 100 \text{ bps}$) with deterministic odd-cent split algorithms ($\text{CGST} = \lfloor \text{TotalGST}/2 \rfloor$, $\text{SGST} = \text{TotalGST} - \text{CGST}$), ensuring exact zero leakage. |
| **INV-FIN-08** | **Atomic Multi-Domain Mutations** | Commercial transactions spanning multiple modules (e.g. Sales checkout creating an invoice, updating inventory, booking accounting vouchers, and updating customer credit) must execute inside a single PostgreSQL ACID transaction. In-memory locks must never be the sole line of defense. |
| **INV-FIN-09** | **Idempotent Operations** | Every financial mutation requires a unique `Idempotency-Key` (UUIDv7) scoped to the branch. Duplicate submissions are absorbed without duplicate ledger postings or double-billing. |
| **INV-FIN-10** | **Complete Audit Traceability** | Every accounting entry records its source document, triggering module, creator user ID, authorizing supervisor ID, client IP, terminal ID, correlation ID, and server timestamps. |

---

## 3. Detailed Architecture of Accounting Domains

```
+---------------------------------------------------------------------------------------------------+
| MAGIC ACCOUNTING DOMAIN BOUNDARIES                                                                |
+---------------------------------------------------------------------------------------------------+
|                                                                                                   |
|  +---------------------------+       +---------------------------------------------------------+  |
|  |  Chart of Accounts (COA)  |       |  Accounting Period Governance                           |  |
|  |  - 5 Fundamental Classes  |       |  - Fiscal Years (1 April - 31 March)                    |  |
|  |  - Groups & Sub-Groups    |       |  - Monthly / Quarterly Accounting Periods               |  |
|  |  - Detail GL Accounts     |       |  - Period States: OPEN, CLOSING, LOCKED                 |  |
|  |  - Normal Balance Rules   |       |  - Supervisor Reopening Protocol                        |  |
|  +---------------------------+       +---------------------------------------------------------+  |
|                |                                                  |                               |
|                +--------------------+     +-----------------------+                               |
|                                     |     |                                                       |
|                                     v     v                                                       |
|  +---------------------------------------------------------------------------------------------+  |
|  |  Voucher & Journal Engine                                                                   |  |
|  |  - Journal Header: Voucher Number, Date, State (Draft, Approved, Posted, Reversed)         |  |
|  |  - Journal Lines: AccountId, DebitPaise, CreditPaise, Description, Dimensions              |  |
|  |  - Invariant Check: ∑ Debits == ∑ Credits                                                  |  |
|  |  - Sequential Gapless Numbering per Branch & Voucher Type                                   |  |
|  +---------------------------------------------------------------------------------------------+  |
|                |                                                  |                               |
|                +--------------------+     +-----------------------+                               |
|                                     |     |                                                       |
|                                     v     v                                                       |
|  +------------------------------------------------+    +---------------------------------------+  |
|  |  General Ledger & Trial Balance                |    |  Subledgers & Party Accounts          |  |
|  |  - Account Transaction Ledgers                 |    |  - Accounts Receivable (AR)           |  |
|  |  - Materialized Period Balance Rollups         |    |  - Accounts Payable (AP)              |  |
|  |  - Trial Balance Equality Validation          |    |  - Open Item Allocation & Aging       |  |
|  +------------------------------------------------+    +---------------------------------------+  |
|                                     |                                                             |
|                                     v                                                             |
|  +---------------------------------------------------------------------------------------------+  |
|  |  Derived Financial Statements                                                               |  |
|  |  - Profit & Loss (Operating Revenue - COGS - Opex = Net Profit)                            |  |
|  |  - Balance Sheet (Assets == Liabilities + Equity)                                           |  |
|  |  - Cash Flow Statement (Direct / Indirect from Cash & Bank Ledgers)                         |  |
|  +---------------------------------------------------------------------------------------------+  |
|                                                                                                   |
+---------------------------------------------------------------------------------------------------+
```

### 3.1 Domain A: Chart of Accounts (COA)
The Chart of Accounts establishes the taxonomy of financial accounts:
1. **Classification:** Every account belongs to exactly one of the five root accounting classes:
   - `1000–1999: ASSETS` (Normal Balance: **Debit**)
   - `2000–2999: LIABILITIES` (Normal Balance: **Credit**)
   - `3000–3999: EQUITY` (Normal Balance: **Credit**)
   - `4000–4999: REVENUE` (Normal Balance: **Credit**)
   - `5000–5999: EXPENSES` (Normal Balance: **Debit**)
2. **Account Hierarchy:**
   - **Account Groups:** Hierarchical container nodes (e.g. `Current Assets` $\rightarrow$ `Bank Accounts`). Groups cannot accept direct journal lines; they aggregate the balances of child groups and accounts.
   - **Detail Accounts:** Leaf accounts (e.g. `1121: HDFC Bank Current A/c`). All journal lines attach strictly to detail accounts.
3. **Control Accounts:**
   - Specific detail accounts are designated as **System Control Accounts** (e.g. `1130: Trade Debtors`, `2110: Trade Creditors`, `1140: Inventory Asset`, `2121: Output CGST`).
   - Direct manual journal entry into control accounts is prohibited; mutations must originate from verified subledger postings (Sales, Purchases, Inventory).
4. **Scoping & Lifecycle:**
   - Accounts are scoped to a legal company entity and can optionally be restricted to specific operating branches.
   - Accounts support soft lifecycle states: `Active`, `Inactive` (disallows new postings while retaining historical balances), and `SystemLocked` (system accounts that cannot be deleted or reclassified).

### 3.2 Domain B: Journal and Voucher Management
The journal engine records every economic transaction:
1. **Voucher Types:**
   - `JOURNAL` (JV): General adjustments, non-cash entries, accruals, period-end entries.
   - `SALES` (SV): Automated entries generated upon posting Sales Invoices.
   - `PURCHASE` (PV): Automated entries generated upon posting Purchase Invoices.
   - `PAYMENT` (PMT): Outward cash/bank disbursements to suppliers, employees, or expenses.
   - `RECEIPT` (RCT): Inward cash/bank collections from customers or debtors.
   - `CONTRA` (CNTR): Internal transfers between Cash and Bank accounts.
   - `REVERSAL` (REV): Non-destructive corrective entries neutralizing prior posted vouchers.
2. **Voucher Lifecycle States:**
   - `DRAFT`: Preliminary voucher under preparation. Not reflected in GL or subledgers. Can be edited or discarded.
   - `APPROVED`: Verified by an accounting supervisor. Awaiting posting.
   - `POSTED`: Formally committed to the General Ledger and subledgers. **Strictly immutable.**
   - `REVERSED`: Indicates that a companion `REVERSAL` voucher has been posted against this entry. The original record remains immutable with `is_reversed = true` and `reversal_voucher_id` pointing to the cancelling entry.
   - `CANCELLED`: Only valid for unposted `DRAFT` entries.
3. **Voucher Numbering:**
   - Sequential, gapless voucher numbering scoped per Company, Branch, Fiscal Year, and Voucher Type:  
     $$\text{Pattern: } \text{VCH}-\{\text{BranchCode}\}-\{\text{FYYear}\}-\{\text{TypePrefix}\}-\{\text{000001}\}$$
   - Number allocation utilizes pessimistic database row locking on the sequence counter table within the posting transaction to guarantee zero duplicate numbers and zero gaps under concurrent load.

### 3.3 Domain C: General Ledger
The General Ledger provides the complete chronological history of every financial account:
1. **Ledger Derivation:**
   - The ledger for any account over a given date range $[t_{\text{start}}, t_{\text{end}}]$ is derived by querying all posted `journal_lines` associated with the account.
   - **Opening Balance:** Calculated by summing all debit and credit movements prior to $t_{\text{start}}$, plus the fiscal year opening balance.
   - **Period Movements:** Detailed list of individual lines showing Date, Voucher Number, Line Narration, Source Transaction Type, Source Document ID, Debit Amount, and Credit Amount.
   - **Closing Balance:** Opening Balance + Period Debits - Period Credits (adjusted for account normal balance).
2. **Materialized Period Balance Rollups:**
   - To maintain sub-second reporting performance when querying ledgers across millions of transactions, MAGIC maintains an append-only `account_period_balances` table storing monthly debit and credit aggregates.
   - Real-time queries read the materialized monthly balances up to the prior closed month, then evaluate only the delta lines in the current open period.
3. **Reconciliation Features:**
   - Bank Reconciliation: Supports matching posted bank transactions with imported bank statement feeds (date, reference number, amount) to track uncredited cheques and unpresented payments.

### 3.4 Domain D: Trial Balance
The Trial Balance is the primary mathematical check of double-entry integrity:
1. **Formula:**
   $$\sum_{a \in \text{All Accounts}} \text{ClosingDebitPaise}_a \equiv \sum_{a \in \text{All Accounts}} \text{ClosingCreditPaise}_a$$
2. **Reporting Views:**
   - **Summary Trial Balance:** Displays account groups with aggregated opening, debit movement, credit movement, and closing balances.
   - **Detailed Trial Balance:** Displays every individual detail account with drill-down capability into the underlying General Ledger lines.
3. **Validation Routine:** An automated diagnostic verification job asserts trial balance equality across all active branches daily, raising system telemetry alerts if any variance is detected.

### 3.5 Domain E: Accounting Period Management
Enforces fiscal governance and chronological integrity:
1. **Structure:**
   - **Fiscal Year:** Standard 12-month accounting cycle (e.g. 1 April 2026 to 31 March 2027 in India).
   - **Fiscal Periods:** 12 monthly operational sub-periods (`2026-04`, `2026-05`, etc.).
2. **Period States:**
   - `OPEN`: Standard operational state. Accepts validated postings within the period date boundaries.
   - `CLOSING`: Soft-lock state during monthly reconciliation. Normal clerk postings blocked; supervisor adjustments permitted.
   - `LOCKED`: Hard-lock state. Completely frozen. Rejects 100% of incoming posting attempts.
3. **Authorized Reopening Protocol:**
   - If a statutory audit or retrospective tax correction requires posting into a locked period, an Administrator must execute an authorized reopening request.
   - The action requires multi-factor authentication, supervisor PIN elevation, justification recording, and produces an immutable security audit entry in `audit.system_audit_logs`.

### 3.6 Domain F: Accounts Receivable (AR) Subledger
Manages customer balances, credit terms, and cash collection:
1. **Open-Item Tracking:**
   - Each posted sales invoice creates an open AR debit item (`ar_subledger_items`) with `original_amount_paise`, `settled_amount_paise`, and `outstanding_amount_paise`.
2. **Receipt Allocation:**
   - Inward customer payments (`RECEIPT`) are allocated against specific open invoices (`ar_allocations`), supporting full settlements and partial payments.
   - Cash discounts allowed for early payment are posted to `4210: Cash Discounts Allowed` with appropriate credit note documentation.
3. **Customer Advances:**
   - Payments received prior to invoice generation are recorded as Unallocated Receipts booked to `2145: Customer Advance Liabilities`.
   - Upon invoice finalization, advances are applied against the invoice, reclassifying the liability to AR.
4. **Aging Analysis:**
   - Outstanding balances are bucketed into chronological aging intervals: `Current (0–30 days)`, `31–60 days`, `61–90 days`, and `90+ days` based on invoice due date.
5. **GL Reconciliation:**
   $$\sum \text{OutstandingCustomerBalances} \equiv \text{GL Account 1130 Balance}$$

### 3.7 Domain G: Accounts Payable (AP) Subledger
Manages supplier liabilities, payment terms, and vendor disbursements:
1. **Open-Item Tracking:**
   - Each verified purchase invoice creates an open AP credit item (`ap_subledger_items`).
2. **Disbursement Allocation:**
   - Outward payments (`PAYMENT`) are allocated against specific purchase bills (`ap_allocations`), reducing supplier liability.
   - Cash discounts received are credited to `4310: Cash Discounts Received`.
3. **Supplier Advances:**
   - Advances paid to vendors prior to goods receipt are recorded under `1165: Supplier Advances (Asset)` and cleared upon bill verification.
4. **Aging Analysis:**
   - Tracks overdue payables across 30/60/90-day intervals to optimize working capital and support statutory reporting (e.g. MSME payment compliance within 45 days under Section 43B(h) of the Income Tax Act).
5. **GL Reconciliation:**
   $$\sum \text{OutstandingSupplierBalances} \equiv \text{GL Account 2110 Balance}$$

### 3.8 Domain H: Financial Statements (Derived Strictly from GL)
Financial statements are pure projections derived directly from the General Ledger; they do not maintain separate or disconnected totals:
1. **Profit & Loss Statement (Income Statement):**
   $$\text{Operating Revenue (4000s)} - \text{Cost of Goods Sold (5100s)} = \text{Gross Profit}$$
   $$\text{Gross Profit} - \text{Operating Expenses (5200s–5400s)} + \text{Other Income (4300s)} = \text{Net Profit / (Loss)}$$
   - Evaluated across any user-defined date range within the fiscal year.
2. **Balance Sheet:**
   $$\text{Total Assets (1000s)} \equiv \text{Total Liabilities (2000s)} + \text{Equity (3000s)} + \text{Current Year Net Profit}$$
   - Evaluated as of a specific date $T_{\text{snapshot}}$.
   - Current Year Net Profit is dynamically derived from the Income Statement for the period up to $T_{\text{snapshot}}$ and projected into the Equity section.
3. **Cash Flow Statement:**
   - Summarizes net cash generated or consumed across:
     - Operating Activities (collections from sales, payments to suppliers and employees, tax settlements).
     - Investing Activities (capital expenditures on fixed assets, equipment purchases).
     - Financing Activities (drawings, capital contributions, loan repayments).
   - Reconciles directly with the net change in GL Accounts `1110 (Cash)` and `1120 (Bank)`.

---

## 4. Cross-Module Integration Architecture & Security Boundaries

To maintain Clean Architecture and prevent spaghetti dependencies, MAGIC establishes strict integration protocols between operational modules and the Accounting engine:

```
+---------------------------------------------------------------------------------------------------+
| CROSS-MODULE INTEGRATION & POSTING PROTOCOL                                                       |
+---------------------------------------------------------------------------------------------------+
|                                                                                                   |
|  [Operational Modules]          [Contracts Gateway]             [Accounting Domain Core]          |
|                                                                                                   |
|  +--------------------+                                         +------------------------------+  |
|  |  Sales / POS       |                                         |  Accounting Unit of Work     |  |
|  +--------------------+                                         +------------------------------+  |
|  |  Purchases / GRN   | ---- calls ----> [IAccountingPosting] -------> Validates Double-Entry     |  |
|  +--------------------+                   Contract Facade       -----> Checks Period Lock         |  |
|  |  Inventory / Stock |                                         -----> Allocates Voucher Number   |  |
|  +--------------------+                                         -----> Persists Voucher & Lines   |  |
|  |  Payroll / HR      |                                         -----> Updates Subledger Items    |  |
|  +--------------------+                                         +------------------------------+  |
|                                                                                |                  |
|                                                                                v                  |
|                                                                 +------------------------------+  |
|                                                                 |  PostgreSQL ACID Transaction |  |
|                                                                 |  (Commit or Total Rollback)  |  |
|                                                                 +------------------------------+  |
|                                                                                |                  |
|                                                                                v                  |
|                                                                 +------------------------------+  |
|                                                                 |  PostgreSQL Outbox Table     |  |
|                                                                 |  (VoucherPosted Integration) |  |
|                                                                 +------------------------------+  |
|                                                                                                   |
+---------------------------------------------------------------------------------------------------+
```

### 4.1 Strict Port Inversion
1. **Zero Direct Database Writes:** Operational modules (Sales, Purchases, Inventory, HR) possess zero EF Core `DbSet` mappings and zero database permissions to mutate tables in the `accounting` schema directly.
2. **Contract-Based Posting:** Modules interact with Accounting exclusively through strongly-typed contracts defined in `Magic.Contracts`:
   - `IAccountingPostingContract`: Atomic execution of financial vouchers within the caller's transaction scope.
   - `IGeneralLedgerQueryContract`: Read-only queries for account balances, trial balance, and statements.
3. **Two-Way Document Linkage:**
   - Every journal voucher stores the originating module identifier (`source_module`), source transaction type (`source_transaction_type`), and UUID (`source_document_id`).
   - The operational document (e.g. `sales.invoices`) stores the generated `accounting_voucher_id`, establishing bidirectional traceability.
4. **Outbox Post-Commit Decoupling:**
   - When a voucher is successfully committed, an `IntegrationEventEnvelope` is written to `platform.outbox_messages` (`EventType = "ACCOUNTING_VOUCHER_POSTED"`).
   - Asynchronous background consumers handle non-critical downstream side effects (e.g. updating dashboard KPI widgets, sending customer payment acknowledgement emails, caching reporting metrics) without delaying the critical database transaction.

---

## 5. Architectural Risks & Mitigation Matrix

| Risk Area | Threat / Failure Mode | Architectural Mitigation Strategy |
|---|---|---|
| **Rounding Drift** | Accumulation of fractional paise discrepancies across line items. | Enforce basis-point integer math with line-level rounding off to GL `4250 (Rounding Off Adjustment)` per Section 170 CGST Act. |
| **Concurrency Contention** | Multiple POS counters competing for sequential voucher numbers. | Implement dedicated sequence allocation tables with lightweight `FOR UPDATE` row locks scoped per branch and voucher type. |
| **Silent Backdating** | Operators altering past months' financial figures after GST filing. | Enforce database-level period lock validation in the command pipeline, backed by supervisor elevation protocols. |
| **Subledger Desync** | Direct GL journal entry causing AR control account to diverge from customer balances. | Prohibit direct manual journal entries to system control accounts (`is_control_account = true`). Mutations require subledger allocation payloads. |
| **Double Posting** | Network retry resubmitting payment voucher twice. | `IdempotencyMiddleware` combined with unique database constraint on `(branch_id, idempotency_key)` guarantees at-most-once execution. |
| **Tampering** | Direct SQL mutation of posted financial vouchers. | PostgreSQL immutability trigger `BEFORE UPDATE OR DELETE` rejecting mutations on posted rows, combined with least-privilege `magic_app` role permissions. |

