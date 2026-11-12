# MAGIC ERP – Phase 2 Stage 3: PostgreSQL Persistence & EF Core Infrastructure Report

**Project:** MAGIC – Universal Business Management ERP  
**Phase:** 2 – Accounting Foundation  
**Stage:** 3 – PostgreSQL Persistence & EF Core Infrastructure  
**Status:** IMPLEMENTATION COMPLETE & VERIFIED  
**Architecture:** .NET 10 + ASP.NET Core + EF Core 10 + PostgreSQL 18.6  
**Security Boundary:** Fail-Closed (No Mock Auth, Zero Unauthenticated Posting Endpoints)  

---

## 1. Executive Summary

MAGIC Phase 2 Stage 3 establishes the authoritative relational persistence layer and infrastructure foundation for the accounting domain. All entities designed in Stage 1 and implemented in Stage 2 have been mapped using Entity Framework Core 10 into a dedicated `accounting` PostgreSQL schema. 

Key architectural achievements in Stage 3:
1. **Isolated Schema Architecture**: Completely separated accounting persistence from `platform`, `audit`, and `common` tables into a clean `accounting` schema across 12 relational tables.
2. **Database-Level Invariant Enforcement**: Multi-row balance integrity ($\sum \text{Debit} \equiv \sum \text{Credit} > 0$) is enforced at the database transaction boundary via PostgreSQL `CONSTRAINT TRIGGER ... DEFERRABLE INITIALLY DEFERRED`. Unbalanced vouchers cannot be committed even by direct SQL commands.
3. **Immutable Posted Ledger**: Database triggers block direct `UPDATE` or `DELETE` on posted vouchers and posted journal lines, guaranteeing audit immutability at the storage engine level.
4. **Concurrency-Safe Sequence Allocation**: Concurrency-safe, gapless sequential voucher number generation per `(branch_id, fiscal_year_id, voucher_type)` utilizing PostgreSQL atomic row-locking upserts (`INSERT ... ON CONFLICT DO UPDATE RETURNING`).
5. **Strict Test Database Isolation**: All integration tests execute exclusively against a dedicated `magic_test` database, keeping the local development database (`magic_dev`) clean and uncontaminated.

---

## 2. Implemented Schema & Entity Configurations

All configurations are located in `01-SRC/Backend/Magic.Infrastructure/Persistence/Configurations/AccountingSchemaConfigurations.cs` and mapped to `MagicDbContext`:

| Table Name | Schema | Description | Key Invariants / Indexes |
| :--- | :--- | :--- | :--- |
| `fiscal_years` | `accounting` | Fiscal accounting governance years | Unique `(company_id, code)`, Date span checks |
| `fiscal_periods` | `accounting` | Fiscal periods with locking controls | Unique `(fiscal_year_id, period_number)`, Unique `(fiscal_year_id, period_code)` |
| `account_groups` | `accounting` | Hierarchical Chart of Accounts classification | Unique `(company_id, code)`, Self-referencing FK `parent_group_id` (`Restrict`) |
| `accounts` | `accounting` | General ledger detail accounts | Unique `(company_id, code)`, `control_account_type` check, FK `account_group_id` (`Restrict`) |
| `vouchers` | `accounting` | Double-entry journal voucher headers | Unique `(branch_id, idempotency_key)`, Unique `voucher_number`, FK `fiscal_period_id` |
| `journal_lines` | `accounting` | Balanced double-entry line items | Check `(debit_paise = 0 AND credit_paise > 0) OR (debit_paise > 0 AND credit_paise = 0)`, FK `voucher_id` (`Cascade`), FK `account_id` (`Restrict`) |
| `voucher_sequences` | `accounting` | Atomic sequence counters | Unique `(branch_id, fiscal_year_id, voucher_type)` |
| `account_period_balances` | `accounting` | Periodic account balance snapshots | Unique `(account_id, branch_id, fiscal_period_id)` |
| `ar_subledger_items` | `accounting` | Accounts Receivable subledger open items | Status checks, balance tracking in integer paise |
| `ar_allocations` | `accounting` | AR receipt-to-invoice settlement allocations | Linked allocation tracking in integer paise |
| `ap_subledger_items` | `accounting` | Accounts Payable subledger open items | Status checks, balance tracking in integer paise |
| `ap_allocations` | `accounting` | AP payment-to-bill settlement allocations | Linked allocation tracking in integer paise |

---

## 3. Storage Engine Guard Triggers

Three PostgreSQL PL/pgSQL triggers were implemented inside migration `20261112081209_AddAccountingSchemaAndLedgerTables`:

### 3.1 `fn_prevent_posted_journal_modification()`
- **Target:** `accounting.journal_lines` (BEFORE UPDATE OR DELETE)
- **Behavior:** Checks the status of the parent voucher header. If the voucher status is `POSTED` (2) or `REVERSED` (3), the trigger throws SQLSTATE `55000` (`OBJECT NOT IN PREREQUISITE STATE`), preventing any modification or deletion of posted lines.

### 3.2 `fn_protect_posted_voucher_header()`
- **Target:** `accounting.vouchers` (BEFORE UPDATE OR DELETE)
- **Behavior:** Rejects `DELETE` operations on any voucher in `POSTED` or `REVERSED` status. Rejects `UPDATE` operations on critical financial fields (`voucher_date`, `voucher_type`, `fiscal_period_id`, `total_debit_paise`, `total_credit_paise`, `company_id`, `branch_id`, `idempotency_key`) once posted.

### 3.3 `fn_verify_posted_voucher_balance()`
- **Target:** `accounting.vouchers` (AFTER INSERT OR UPDATE) as a `CONSTRAINT TRIGGER ... DEFERRABLE INITIALLY DEFERRED`
- **Behavior:** At the end of the transaction before commit, iterates over newly inserted/updated vouchers in `POSTED` status. Aggregates line sums:
  $$\sum \text{Debit Paise} \equiv \sum \text{Credit Paise} \equiv \text{Header Total} > 0$$
  Verifies at least 2 lines exist. If unbalanced or line sum mismatches header totals, throws SQLSTATE `23514` (`CHECK VIOLATION`), aborting the transaction.

---

## 4. Repository & Posting Infrastructure

1. **`IVoucherNumberGenerator` / `PostgresVoucherNumberGenerator`**:
   - Executes atomic SQL upsert on `accounting.voucher_sequences` within the ambient EF Core transaction.
   - Generates standardized human-readable sequential voucher numbers: `VCH-{BRANCH}-{FY}-{TYPE}-{SEQUENCE:D6}`.
   - Tested under high concurrency (15 parallel tasks) with zero gaps, zero duplicates, and zero race conditions.
2. **`IChartOfAccountsRepository` / `PostgresChartOfAccountsRepository`**:
   - Implements query and persistence methods for account groups and detail accounts with parent/child navigation and unique constraint handling.
3. **`IFiscalGovernanceRepository` / `PostgresFiscalGovernanceRepository`**:
   - Manages fiscal years, period queries, and lock enforcement.
4. **`IGeneralLedgerRepository` / `PostgresGeneralLedgerRepository`**:
   - Manages voucher queries, line loading, companion reversals, and idempotency key lookups.
5. **`IPostingService` / `PostingService`**:
   - Coordinates double-entry voucher posting inside a retriable transaction unit (`IUnitOfWork.ExecuteInTransactionAsync`).
   - Sequence:
     1. Idempotency verification.
     2. Fiscal period status check (fails if closed or locked without elevation).
     3. Account active status and control account rules check.
     4. Domain invariants check via `PostingValidator`.
     5. Atomic voucher sequence allocation.
     6. State transition: `voucher.Post(period, voucherNumber, userId)`.
     7. Persistence of voucher and lines.
     8. Atomic creation of transactional outbox message (`VOUCHER_POSTED`).
     9. Atomic creation of tamper-evident system audit log (`POST_JOURNAL_VOUCHER`).

---

## 5. Security & Statutory Boundaries Maintained

- **Fail-Closed Security Boundary (`OPEN-003`)**: No mock identity, fake claims, or unauthenticated HTTP posting endpoints were introduced. All services operate strictly within authenticated domain contexts.
- **Statutory Decision Retentions**:
  - `DEC-ACC-002` (Inventory Valuation Costing: FIFO vs Weighted Average): Unresolved; no inventory posting assumptions made.
  - `DEC-ACC-003` (GST Rounding Rule: Invoice-Level vs Line-Level): Unresolved; money structures preserve integer paise without lossy rounding assumptions.
  - `DEC-ACC-004` (NIC e-Invoicing Direct API vs GSP Intermediary): Unresolved; schema provides flexible fields without vendor lock-in.

---

## 6. Verification Summary

| Test Suite | Tests Run | Passed | Failed | Skipped | Status |
| :--- | :---: | :---: | :---: | :---: | :---: |
| `Magic.Domain.UnitTests` | 36 | 36 | 0 | 0 | **PASSED** |
| `Magic.ArchitectureTests` | 4 | 4 | 0 | 0 | **PASSED** |
| `Magic.Api.IntegrationTests` | 26 | 26 | 0 | 0 | **PASSED** |
| Angular Frontend Tests | 13 | 13 | 0 | 0 | **PASSED** |
| **Total Solution Verification** | **79** | **79** | **0** | **0** | **PASSED** |
