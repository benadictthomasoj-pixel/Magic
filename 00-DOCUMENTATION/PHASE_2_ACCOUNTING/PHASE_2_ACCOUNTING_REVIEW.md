# MAGIC — Phase 2: Accounting Foundation
## Architectural Review & Stage 1 Readiness Evaluation

**Document Version:** 2.0.0-DRAFT  
**Phase:** 2 — Accounting Foundation  
**Stage:** 1 — Architecture and Database Design  
**Implementation Status:** NOT AUTHORIZED (Review Document Only — Zero Code/Migrations Executed)  
**Author:** Antigravity AI & Architecture Team  
**Last Updated:** 2026-11-12  

---

## 1. Executive Summary & Design Posture

This document provides a comprehensive synthesis and technical review of **Stage 1 (Architecture & Database Design)** for the **MAGIC Phase 2 Accounting Foundation**.

In accordance with strict project guidelines:
1. **Zero Legacy Coupling:** The entire accounting foundation is designed from first principles as an independent greenfield domain. Zero code, database tables, or business logic have been imported, migrated, or adapted from Billing Logics or any prior codebase.
2. **Strict No-Build Rule Enforced:** This stage has produced **documentation and architectural specifications only**. Zero application source files, database tables, EF Core migrations, repositories, API controllers, services, or frontend views have been implemented.
3. **Defense in Depth:** The architecture guarantees mathematical double-entry equality ($\sum \text{Debit} \equiv \sum \text{Credit}$), immutable posted ledgers, 64-bit integer paise precision, chronological period locking, and concurrency-safe sequential voucher numbering.

---

## 2. Architecture Overview & Domain Boundaries

The Accounting Module is structured within a Clean Architecture framework as the single source of financial truth:

```
+---------------------------------------------------------------------------------------------------+
| MAGIC ACCOUNTING DOMAIN MAP                                                                       |
+---------------------------------------------------------------------------------------------------+
|  [Chart of Accounts]         - 5 Fundamental Classes, Parent-Child Hierarchy, Control Accounts    |
|  [Period Governance]         - Fiscal Years (1 Apr - 31 Mar), Monthly Periods, Hard Locking       |
|  [Voucher Engine]            - Balanced Invariant Check, Gapless Sequential Numbering, Reversals   |
|  [General Ledger]            - Account-Wise Ledger, Materialized Monthly Balances, Reconciliations|
|  [Trial Balance]             - Group and Account Level Debit-Credit Equality Assertion            |
|  [AR Subledger]              - Customer Invoices, Receipts, Advances, 30/60/90-Day Aging, GL 1130 |
|  [AP Subledger]              - Supplier Bills, Payments, Advances, MSME Due Dates, GL 2110        |
|  [Financial Statements]      - P&L, Balance Sheet, Cash Flow derived 100% from GL Records         |
+---------------------------------------------------------------------------------------------------+
```

### Domain Boundaries & Port Inversion:
- **Zero Direct Cross-Schema Writes:** Operational modules (Sales, Purchases, Inventory, POS, Payroll) are completely prohibited from executing SQL queries or EF Core commands directly against the `accounting` schema.
- **Contract Boundary:** Operational modules communicate with Accounting exclusively via strongly-typed C# contracts (`IAccountingPostingContract`) defined in `Magic.Contracts`.
- **Atomic Operations:** Multi-domain business operations (e.g. Sales checkout) execute synchronously inside a single PostgreSQL ACID transaction. Secondary notifications flow asynchronously through the transactional Outbox (`platform.outbox_messages`).

---

## 3. Proposed Database Design Summary

The proposed logical schema **`accounting`** comprises 12 highly-specialized, third-normal-form relational tables:

| Table Name | Entity Purpose | Key Columns & Data Types | Invariants & Constraints |
|---|---|---|---|
| `fiscal_years` | Annual tax/accounting cycle | `id UUID`, `code VARCHAR`, `start_date DATE`, `end_date DATE`, `is_closed BOOLEAN` | `UQ(company_id, code)`, `end_date > start_date` |
| `fiscal_periods` | Monthly operational sub-period | `id UUID`, `fiscal_year_id UUID`, `period_number INT`, `state VARCHAR` | `UQ(year_id, period_num)`, `state IN ('OPEN','CLOSING','LOCKED')` |
| `account_groups` | Taxonomy hierarchy | `id UUID`, `parent_group_id UUID`, `group_code VARCHAR`, `root_class VARCHAR` | `UQ(company, code)`, `root_class IN (ASSET..EXPENSE)`, tree depth check |
| `accounts` | Detail leaf accounts | `id UUID`, `account_code VARCHAR`, `normal_balance VARCHAR`, `is_control_account` | `UQ(company, code)`, `normal_balance IN ('DEBIT','CREDIT')` |
| `voucher_sequences` | Concurrency-safe counter | `id UUID`, `branch_id UUID`, `voucher_type VARCHAR`, `current_sequence BIGINT` | `UQ(branch, fy, type)`, updated via `FOR UPDATE` |
| `vouchers` | Journal transaction header | `id UUID`, `voucher_number VARCHAR`, `total_debit_paise BIGINT`, `status VARCHAR` | `total_debit = total_credit`, `UQ(branch, number)`, `UQ(branch, idempotency)` |
| `journal_lines` | Detail debit/credit legs | `id UUID`, `voucher_id UUID`, `account_id UUID`, `debit_paise BIGINT`, `credit_paise` | One-sided check: `(dr > 0 AND cr = 0) OR (cr > 0 AND dr = 0)` |
| `ar_subledger_items`| Open customer debt items | `id UUID`, `customer_id UUID`, `original_amount_paise`, `outstanding_amount_paise`| `settled + outstanding = original`, aging index on `due_date` |
| `ar_allocations` | Receipt-to-invoice match | `id UUID`, `debit_item_id UUID`, `credit_voucher_id UUID`, `allocated_amount_paise`| `allocated_amount > 0`, audit timestamp |
| `ap_subledger_items`| Open supplier liabilities | `id UUID`, `supplier_id UUID`, `original_amount_paise`, `outstanding_amount_paise`| `settled + outstanding = original`, aging index on `due_date` |
| `ap_allocations` | Payment-to-bill match | `id UUID`, `credit_item_id UUID`, `debit_voucher_id UUID`, `allocated_amount_paise`| `allocated_amount > 0`, audit timestamp |
| `account_period_balances` | Materialized GL rollups | `id UUID`, `account_id UUID`, `fiscal_period_id UUID`, `closing_debit`, `closing_credit`| `UQ(account, period, branch)` for high-throughput reporting |

---

## 4. Financial Invariants & Integrity Assurance

1. **64-Bit Integer Currency:** All money fields use signed 64-bit integer paise (`BIGINT` / `long`). Zero floating-point representation.
2. **Double-Entry Invariance:** Enforced both in domain logic (`DomainGuard`) and at database level (`chk_vouchers_balance: total_debit_paise = total_credit_paise`).
3. **Database-Level Immutability:** Trigger `trg_prevent_posted_voucher_tampering` intercepts `UPDATE` and `DELETE` on posted vouchers, raising exception code `55000`.
4. **Non-Destructive Corrections:** Corrections require companion reversal vouchers (`is_reversed = true`, `reversal_voucher_id`) followed by replacement entries.
5. **Deterministic Tax Splitting:** GST calculated in basis points with odd-cent integer splits ($\text{CGST} = \lfloor \text{Tax}/2 \rfloor$, $\text{SGST} = \text{Tax} - \text{CGST}$) and Section 170 CGST Act invoice round-off posted to `4250`.
6. **Subledger Control Parity:** $\sum \text{AR Items} \equiv \text{GL 1130}$ and $\sum \text{AP Items} \equiv \text{GL 2110}$.

---

## 5. Posting Workflow Summary

Operational transactions map cleanly through the Master Posting Matrix:
- **Sales & Revenue (TX-01 to TX-05):** Cash sales, digital settlements, B2B wholesale credit sales, and return credit notes.
- **Customer Collections (TX-06 to TX-08):** Full/partial collections, unadjusted advance deposits, and advance applications.
- **Purchases & Payables (TX-09 to TX-14):** Intra/inter-state inventory purchases, debit notes, vendor disbursements, and advance settlements.
- **Warehouse & COGS (TX-15 to TX-17):** Sales COGS recognition, physical stock loss/scrap, and surplus gain adjustments.
- **Operating Expenses & Assets (TX-18 to TX-20):** Direct expense payments, fixed asset capitalization, and monthly depreciation.
- **Payroll (TX-21 to TX-22):** Salary accruals, statutory deductions (PF, ESIC, PT, TDS), and bank disbursements.
- **Statutory GST (TX-23 to TX-24):** Monthly input tax offset (ITC vs Output) and net cash challan payments.

---

## 6. Security, Privilege Elevation & Concurrency Risks

| Risk Scenario | Technical Threat | Architectural Defense |
|---|---|---|
| **Multi-Terminal Sequence Race** | Two POS counters request next voucher number at the same millisecond, risking duplicate numbers or numbering gaps. | Sequence allocation table with row-level `FOR UPDATE` lock scoped to `(branch_id, fiscal_year_id, voucher_type)` inside the posting transaction. |
| **Network Retry Duplicate Post** | Network latency causes cashier to click "Complete" twice or terminal retries POST. | `IdempotencyMiddleware` combined with unique index `uq_vouchers_branch_idempotency` absorbs duplicate without re-posting. |
| **Backdating Past Year Accounts** | Clerk creates backdated invoice into closed fiscal period after tax audit. | Period locking engine checks `fiscal_periods.state == 'OPEN'`. Backdating into `LOCKED` periods fails with 403 Forbidden unless authorized by supervisor elevation token. |
| **Control Account Direct Mutation** | Direct manual journal entry into `1130 Accounts Receivable` causing subledger and GL to diverge. | Control account guard (`chk_accounts_control`) rejects direct manual journal entries; mutations must flow through subledger-allocating contracts. |
| **Database Tampering** | Unauthorized SQL query executes `DELETE FROM accounting.vouchers`. | PostgreSQL trigger `trg_prevent_posted_voucher_tampering` (SQLState `55000`) and least-privilege `magic_app` role (`REVOKE UPDATE, DELETE`). |

---

## 7. Outstanding Decisions & Statutory Review Points

The following architectural and statutory questions remain open and require explicit user or qualified accounting confirmation:

1. **DEC-ACC-002: Inventory Valuation Model:** Recommendation is Moving Weighted Average Cost (AVCO) for retail/bulk and FIFO for batch-tracked goods, locked per fiscal year. *Requires user confirmation.*
2. **DEC-ACC-003: Statutory GST Rounding:** Recommendation is exact line-level integer paise basis-point math with Section 170 CGST Act invoice-level round-off to GL `4250`. *Requires Chartered Accountant sign-off.*
3. **DEC-ACC-004: NIC e-Invoicing / e-Way Bill:** Recommendation for V1 is offline JSON/Excel export matching NIC Schema v1.1, avoiding third-party GSP fees. *Requires user confirmation.*
4. **DEC-ACC-007: Indian Statutory Compliance Verification:**
   - Section 43B(h) Income Tax Act: MSME 45-day payment tracking in AP.
   - Section 194Q TDS on purchase of goods $> ₹50\text{L}$.
   - Section 206C(1H) TCS on sale of goods $> ₹50\text{L}$.
   - Rule 42/43 ITC reversal for non-business use.
   *Flagged as schema hooks; formulas awaiting formal CA review.*

---

## 8. Dependencies on Future MAGIC Modules

The accounting foundation serves as the core consumer for multiple subsequent phases:
- **Phase 3 (POS, Product Catalogue, Inventory, Sales, Purchases):** Depends on `IAccountingPostingContract` for real-time sales invoicing, purchase booking, and COGS recognition.
- **Phase 4 (Financial Management & Tax Engine):** Depends on `accounting.vouchers` for Bank Reconciliation Statements (BRS) and GST electronic ledgers.
- **Phase 5 (Customer CRM & Supplier Management):** Depends on `ar_subledger_items` and `ap_subledger_items` for party credit limits and payment terms.
- **Phase 6 (HR & Payroll):** Depends on payroll accrual posting (TX-21).
- **Phase 7 (BI & Analytics):** Depends on `account_period_balances` for financial dashboards and ratios.

---

## 9. Recommended Stage-by-Stage Implementation Sequence

When authorized, Phase 2 should be executed in four disciplined, reviewable stages:

```
[Stage 1: Architecture & DB Design]  <-- COMPLETED (This Delivery)
                 |
                 v
[Stage 2: Pure Domain Primitives & Entities]
  - Money, Quantity, GstRate, GstBreakdown, Section170Rounding
  - AccountGroup, Account, FiscalYear, FiscalPeriod, JournalVoucher, JournalLine
  - Invariant validation & property-based xUnit test suites
                 |
                 v
[Stage 3: Persistence, Migrations & Contracts]
  - EF Core configurations in logical schema `accounting`
  - PostgreSQL 18 Migration: `AddAccountingFoundation` (+ idempotent SQL script)
  - Immutability trigger `trg_prevent_posted_voucher_tampering`
  - IAccountingPostingContract & IGeneralLedgerQueryContract
                 |
                 v
[Stage 4: Application Services, Subledgers & Reports]
  - PostVoucherCommand, ReverseVoucherCommand, LockPeriodCommand
  - AR/AP open-item allocation & aging services
  - Trial Balance & Financial Statement generation services
  - Integration tests, concurrency tests, and Angular CDK COA viewer
```

---

## 10. Gate Criteria for Stage 2 Authorization

Phase 2 Stage 2 (Domain Primitives & Entities) may be authorized once the user:
1. Confirms the Stage 1 architectural and database design.
2. Formally confirms or notes recommendations on the unresolved decisions (`DEC-ACC-002`, `DEC-ACC-003`, `DEC-ACC-004`).
3. Explicitly authorizes proceeding to code implementation for Stage 2.

