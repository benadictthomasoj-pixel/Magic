# MAGIC ERP – Phase 2 Stage 3: Database Migrations Specification

**Project:** MAGIC – Universal Business Management ERP  
**Phase:** 2 – Accounting Foundation  
**Stage:** 3 – PostgreSQL Persistence & EF Core Infrastructure  
**Migration Name:** `20261112081209_AddAccountingSchemaAndLedgerTables`  
**Database Engines Tested:** PostgreSQL 18.6 (`magic_dev`, `magic_test`)  

---

## 1. Migration Overview

The migration `20261112081209_AddAccountingSchemaAndLedgerTables` adds the complete `accounting` schema, establishing double-entry tables, subledgers, balance tables, sequence counters, check constraints, foreign key restrictions, and storage-engine PL/pgSQL triggers.

```
platform.__ef_migrations_history
├── 20261111100000_InitialPlatformTables
├── 20261111120000_AddOutboxAndAuditLogs
└── 20261112081209_AddAccountingSchemaAndLedgerTables  <-- Stage 3
```

---

## 2. Table Definitions

### 2.1 `accounting.fiscal_years`
- `id` (uuid, PK)
- `company_id` (uuid, NOT NULL)
- `branch_id` (uuid, NOT NULL)
- `code` (varchar(20), NOT NULL)
- `name` (varchar(100), NOT NULL)
- `start_date` (date, NOT NULL)
- `end_date` (date, NOT NULL)
- `is_closed` (boolean, NOT NULL, DEFAULT false)
- `closed_at_utc` (timestamptz, NULL)
- `closed_by_user_id` (uuid, NULL)
- `created_at_utc` (timestamptz, NOT NULL)
- `created_by_user_id` (uuid, NOT NULL)
- `updated_at_utc` (timestamptz, NOT NULL)
- `updated_by_user_id` (uuid, NULL)
- `concurrency_version` (int, NOT NULL, DEFAULT 0)
- **Constraints:**
  - `uq_fiscal_years_company_code`: UNIQUE `(company_id, code)`
  - `ck_fiscal_years_date_order`: CHECK `(start_date < end_date)`

### 2.2 `accounting.fiscal_periods`
- `id` (uuid, PK)
- `company_id` (uuid, NOT NULL)
- `branch_id` (uuid, NOT NULL)
- `fiscal_year_id` (uuid, NOT NULL, FK -> `fiscal_years.id` RESTRICT)
- `period_number` (int, NOT NULL)
- `period_code` (varchar(20), NOT NULL)
- `start_date` (date, NOT NULL)
- `end_date` (date, NOT NULL)
- `is_locked` (boolean, NOT NULL, DEFAULT false)
- `is_closed` (boolean, NOT NULL, DEFAULT false)
- `locked_at_utc` (timestamptz, NULL)
- `locked_by_user_id` (uuid, NULL)
- `closed_at_utc` (timestamptz, NULL)
- `closed_by_user_id` (uuid, NULL)
- `created_at_utc` (timestamptz, NOT NULL)
- `created_by_user_id` (uuid, NOT NULL)
- `updated_at_utc` (timestamptz, NOT NULL)
- `updated_by_user_id` (uuid, NULL)
- `concurrency_version` (int, NOT NULL, DEFAULT 0)
- **Constraints:**
  - `uq_fiscal_periods_fy_period_number`: UNIQUE `(fiscal_year_id, period_number)`
  - `uq_fiscal_periods_fy_period_code`: UNIQUE `(fiscal_year_id, period_code)`
  - `ck_fiscal_periods_date_order`: CHECK `(start_date <= end_date)`
  - `ck_fiscal_periods_period_number`: CHECK `(period_number >= 1 AND period_number <= 13)`

### 2.3 `accounting.account_groups`
- `id` (uuid, PK)
- `company_id` (uuid, NOT NULL)
- `branch_id` (uuid, NOT NULL)
- `parent_group_id` (uuid, NULL, FK -> `account_groups.id` RESTRICT)
- `group_code` (varchar(30), NOT NULL)
- `group_name` (varchar(150), NOT NULL)
- `root_class` (varchar(20), NOT NULL)
- `hierarchy_depth` (int, NOT NULL, DEFAULT 1)
- `is_system` (boolean, NOT NULL, DEFAULT false)
- `is_active` (boolean, NOT NULL, DEFAULT true)
- `created_at_utc` (timestamptz, NOT NULL)
- `created_by_user_id` (uuid, NOT NULL)
- `updated_at_utc` (timestamptz, NOT NULL)
- `updated_by_user_id` (uuid, NULL)
- `concurrency_version` (int, NOT NULL, DEFAULT 0)
- **Constraints:**
  - `uq_account_groups_company_code`: UNIQUE `(company_id, group_code)`
  - `ck_account_groups_root_class`: CHECK `(root_class IN ('Asset', 'Liability', 'Equity', 'Revenue', 'Expense'))`
  - `ck_account_groups_depth`: CHECK `(hierarchy_depth >= 1 AND hierarchy_depth <= 10)`

### 2.4 `accounting.accounts`
- `id` (uuid, PK)
- `company_id` (uuid, NOT NULL)
- `branch_id` (uuid, NOT NULL)
- `account_group_id` (uuid, NOT NULL, FK -> `account_groups.id` RESTRICT)
- `account_code` (varchar(30), NOT NULL)
- `account_name` (varchar(150), NOT NULL)
- `root_class` (varchar(20), NOT NULL)
- `currency_code` (varchar(3), NOT NULL, DEFAULT 'INR')
- `is_active` (boolean, NOT NULL, DEFAULT true)
- `control_account_type` (varchar(30), NULL)
- `created_at_utc` (timestamptz, NOT NULL)
- `created_by_user_id` (uuid, NOT NULL)
- `updated_at_utc` (timestamptz, NOT NULL)
- `updated_by_user_id` (uuid, NULL)
- `concurrency_version` (int, NOT NULL, DEFAULT 0)
- **Constraints:**
  - `uq_accounts_company_code`: UNIQUE `(company_id, account_code)`
  - `ck_accounts_currency`: CHECK `(currency_code = 'INR')`
  - `ck_accounts_control_type`: CHECK `(control_account_type IS NULL OR control_account_type IN ('CustomerControl', 'VendorControl', 'GstControl', 'BankControl', 'CashControl', 'InventoryControl'))`

### 2.5 `accounting.vouchers`
- `id` (uuid, PK)
- `company_id` (uuid, NOT NULL)
- `branch_id` (uuid, NOT NULL)
- `fiscal_period_id` (uuid, NOT NULL, FK -> `fiscal_periods.id` RESTRICT)
- `voucher_number` (varchar(50), NOT NULL)
- `voucher_type` (varchar(30), NOT NULL)
- `voucher_date` (date, NOT NULL)
- `status` (int, NOT NULL)
- `reference_number` (varchar(100), NULL)
- `narration` (varchar(1000), NOT NULL, DEFAULT '')
- `is_reversed` (boolean, NOT NULL, DEFAULT false)
- `reversal_voucher_id` (uuid, NULL, FK -> `vouchers.id` RESTRICT)
- `idempotency_key` (uuid, NOT NULL)
- `source_module` (varchar(50), NULL)
- `source_transaction_type` (varchar(50), NULL)
- `source_document_id` (uuid, NULL)
- `posted_at_utc` (timestamptz, NULL)
- `posted_by_user_id` (uuid, NULL)
- `supervisor_approved_by_user_id` (uuid, NULL)
- `trace_correlation_id` (uuid, NOT NULL)
- `total_debit_paise` (bigint, NOT NULL, DEFAULT 0)
- `total_credit_paise` (bigint, NOT NULL, DEFAULT 0)
- `created_at_utc` (timestamptz, NOT NULL)
- `created_by_user_id` (uuid, NOT NULL)
- `updated_at_utc` (timestamptz, NOT NULL)
- `updated_by_user_id` (uuid, NULL)
- `concurrency_version` (int, NOT NULL, DEFAULT 0)
- **Constraints:**
  - `uq_vouchers_branch_idempotency`: UNIQUE `(branch_id, idempotency_key)`
  - `uq_vouchers_voucher_number`: UNIQUE `(voucher_number)`
  - `ck_vouchers_status`: CHECK `(status IN (1, 2, 3))`
  - `ck_vouchers_positive_totals`: CHECK `(total_debit_paise >= 0 AND total_credit_paise >= 0)`

### 2.6 `accounting.journal_lines`
- `id` (uuid, PK)
- `company_id` (uuid, NOT NULL)
- `branch_id` (uuid, NOT NULL)
- `voucher_id` (uuid, NOT NULL, FK -> `vouchers.id` CASCADE)
- `account_id` (uuid, NOT NULL, FK -> `accounts.id` RESTRICT)
- `line_number` (int, NOT NULL)
- `debit_paise` (bigint, NOT NULL)
- `credit_paise` (bigint, NOT NULL)
- `narration` (varchar(500), NOT NULL, DEFAULT '')
- `party_id` (uuid, NULL)
- `party_type` (varchar(30), NULL)
- `tax_id` (uuid, NULL)
- `cost_center_id` (uuid, NULL)
- `concurrency_version` (int, NOT NULL, DEFAULT 0)
- **Constraints:**
  - `uq_journal_lines_voucher_line_number`: UNIQUE `(voucher_id, line_number)`
  - `ck_journal_lines_strictly_one_sided`: CHECK `((debit_paise = 0 AND credit_paise > 0) OR (debit_paise > 0 AND credit_paise = 0))`
  - `ck_journal_lines_positive`: CHECK `(debit_paise >= 0 AND credit_paise >= 0)`
  - `ck_journal_lines_line_number`: CHECK `(line_number >= 1)`

### 2.7 `accounting.voucher_sequences`
- `id` (uuid, PK)
- `branch_id` (uuid, NOT NULL)
- `fiscal_year_id` (uuid, NOT NULL)
- `voucher_type` (varchar(30), NOT NULL)
- `current_sequence` (bigint, NOT NULL, DEFAULT 0)
- `updated_at_utc` (timestamptz, NOT NULL)
- **Constraints:**
  - `uq_voucher_sequences_branch_fy_type`: UNIQUE `(branch_id, fiscal_year_id, voucher_type)`

### 2.8 `accounting.account_period_balances`
- `id` (uuid, PK)
- `company_id` (uuid, NOT NULL)
- `branch_id` (uuid, NOT NULL)
- `account_id` (uuid, NOT NULL, FK -> `accounts.id` RESTRICT)
- `fiscal_period_id` (uuid, NOT NULL, FK -> `fiscal_periods.id` RESTRICT)
- `opening_balance_paise` (bigint, NOT NULL, DEFAULT 0)
- `total_debit_paise` (bigint, NOT NULL, DEFAULT 0)
- `total_credit_paise` (bigint, NOT NULL, DEFAULT 0)
- `closing_balance_paise` (bigint, NOT NULL, DEFAULT 0)
- `updated_at_utc` (timestamptz, NOT NULL)
- **Constraints:**
  - `uq_account_period_balances`: UNIQUE `(account_id, branch_id, fiscal_period_id)`

### 2.9 Subledger Tables (`ar_subledger_items`, `ar_allocations`, `ap_subledger_items`, `ap_allocations`)
- Track customer/vendor open balances and settlements in integer paise with foreign key protections (`DeleteBehavior.Restrict`).

---

## 3. Database Triggers Specification

```sql
-- 1. Posted line immutability trigger
CREATE OR REPLACE FUNCTION accounting.fn_prevent_posted_journal_modification()
RETURNS TRIGGER AS $$
DECLARE
    v_status integer;
BEGIN
    SELECT status INTO v_status FROM accounting.vouchers WHERE id = OLD.voucher_id;
    IF v_status IN (2, 3) THEN
        RAISE EXCEPTION 'POSTED_JOURNAL_IMMUTABLE: Cannot modify or delete journal lines of a posted or reversed voucher'
            USING ERRCODE = '55000';
    END IF;
    RETURN OLD;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_protect_posted_journal_lines
BEFORE UPDATE OR DELETE ON accounting.journal_lines
FOR EACH ROW EXECUTE FUNCTION accounting.fn_prevent_posted_journal_modification();

-- 2. Posted voucher header immutability trigger
CREATE OR REPLACE FUNCTION accounting.fn_protect_posted_voucher_header()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        IF OLD.status IN (2, 3) THEN
            RAISE EXCEPTION 'POSTED_VOUCHER_IMMUTABLE: Cannot delete posted or reversed voucher'
                USING ERRCODE = '55000';
        END IF;
        RETURN OLD;
    ELSIF TG_OP = 'UPDATE' THEN
        IF OLD.status IN (2, 3) THEN
            IF NEW.voucher_date <> OLD.voucher_date OR
               NEW.voucher_type <> OLD.voucher_type OR
               NEW.fiscal_period_id <> OLD.fiscal_period_id OR
               NEW.total_debit_paise <> OLD.total_debit_paise OR
               NEW.total_credit_paise <> OLD.total_credit_paise OR
               NEW.company_id <> OLD.company_id OR
               NEW.branch_id <> OLD.branch_id OR
               NEW.idempotency_key <> OLD.idempotency_key THEN
                RAISE EXCEPTION 'POSTED_VOUCHER_IMMUTABLE: Cannot alter financial header attributes of a posted voucher'
                    USING ERRCODE = '55000';
            END IF;
        END IF;
        RETURN NEW;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_protect_posted_voucher_header
BEFORE UPDATE OR DELETE ON accounting.vouchers
FOR EACH ROW EXECUTE FUNCTION accounting.fn_protect_posted_voucher_header();

-- 3. Cross-row double-entry balance constraint trigger
CREATE OR REPLACE FUNCTION accounting.fn_verify_posted_voucher_balance()
RETURNS TRIGGER AS $$
DECLARE
    v_rec RECORD;
    v_debit_sum bigint;
    v_credit_sum bigint;
    v_line_count integer;
BEGIN
    FOR v_rec IN SELECT id, status, total_debit_paise, total_credit_paise FROM accounting.vouchers WHERE status IN (2, 3) LOOP
        SELECT COALESCE(SUM(debit_paise), 0), COALESCE(SUM(credit_paise), 0), COUNT(*)
        INTO v_debit_sum, v_credit_sum, v_line_count
        FROM accounting.journal_lines
        WHERE voucher_id = v_rec.id;

        IF v_line_count < 2 THEN
            RAISE EXCEPTION 'DOUBLE_ENTRY_VIOLATION: Posted voucher % must contain at least 2 journal lines (found %)', v_rec.id, v_line_count
                USING ERRCODE = '23514';
        END IF;

        IF v_debit_sum <> v_credit_sum OR v_debit_sum <= 0 THEN
            RAISE EXCEPTION 'DOUBLE_ENTRY_VIOLATION: Posted voucher % is unbalanced. Debit sum: %, Credit sum: %', v_rec.id, v_debit_sum, v_credit_sum
                USING ERRCODE = '23514';
        END IF;

        IF v_debit_sum <> v_rec.total_debit_paise OR v_credit_sum <> v_rec.total_credit_paise THEN
            RAISE EXCEPTION 'DOUBLE_ENTRY_VIOLATION: Header total does not match line sum for voucher %', v_rec.id
                USING ERRCODE = '23514';
        END IF;
    END LOOP;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE CONSTRAINT TRIGGER trg_verify_posted_voucher_balance
AFTER INSERT OR UPDATE ON accounting.vouchers
DEFERRABLE INITIALLY DEFERRED
FOR EACH ROW EXECUTE FUNCTION accounting.fn_verify_posted_voucher_balance();
```
