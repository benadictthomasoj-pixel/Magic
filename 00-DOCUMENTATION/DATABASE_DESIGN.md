# MAGIC — DATABASE ARCHITECTURE & DESIGN

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  

---

## 1. Relational Database Strategy: PostgreSQL

PostgreSQL is the authoritative transactional system of record for MAGIC (one PostgreSQL database per business deployment initially). Its enterprise-grade ACID guarantees, rich indexing capabilities, transactional schema migrations (DDL), native JSONB support, and rock-solid 64-bit integer performance make it ideal for high-integrity business ERP systems.

---

## 2. Logical Schema Architecture

Rather than dumping hundreds of tables into the default `public` schema, MAGIC implements **Logical Schema Separation**. Each business domain owns an isolated PostgreSQL schema (aligning schema naming with the Master Architecture specification):

```
+----------------------------------------------------------------------------------------------------+
| POSTGRESQL DATABASE: magic_db                                                                      |
+----------------------------------------------------------------------------------------------------+
|  platform      | System configuration, telemetry, backup metadata, outbox_messages, idempotency    |
|  auth          | Users, roles, permissions, role_permissions, user_sessions, override_pins         |
|  audit         | Append-only immutable change logs, security events, audit snapshots                |
|  organization  | Companies, branches, terminals, terminal_tokens, branch_settings                  |
|  products      | Categories, brands, items, variants, units, unit_conversions, barcodes, price_lists|
|  inventory     | Warehouses, stock_balances, stock_ledger, stock_batches, stock_transfers          |
|  sales         | Orders, invoices, invoice_items, delivery_challans, credit_notes, payments         |
|  purchases     | Requisitions, purchase_orders, goods_receipt_notes, purchase_invoices, debit_notes |
|  accounting    | Fiscal_years, periods, account_groups, accounts, vouchers, journal_lines          |
|  finance       | Bank_accounts, bank_transactions, reconciliations, fixed_assets, depreciation     |
|  tax           | Hsn_sac_codes, tax_rates, tax_transactions, tax_settlements                       |
|  customers     | Customers, customer_addresses, loyalty_accounts, loyalty_transactions             |
|  suppliers     | Suppliers, supplier_bank_details, supplier_ratings                                |
|  expenses      | Categories, claims, claim_items, petty_cash_floats                                |
|  employees     | Employees, departments, designations, shifts, attendance_logs, leaves             |
|  payroll       | Salary_components, salary_structures, payruns, payrun_details, payslips           |
|  services      | Service_items, job_cards, job_spare_parts, amc_contracts                          |
|  marketing     | Promotions, coupons, campaigns, coupon_redemptions                                |
|  automation    | Staged_purchase_invoices, staged_invoice_items, ai_parsing_logs                   |
+----------------------------------------------------------------------------------------------------+
```

> [!NOTE]
> Physical schema names (`products`, `customers`, `employees`) align with Section 2.4 of the Master Architecture Specification; final physical DDL tables will be locked only after formal domain modeling approval.

---

## 3. Core Database Design Conventions

1. **Naming Conventions:**
   - Schemas: `snake_case` (e.g., `accounting`, `inventory`).
   - Tables: Plural `snake_case` (e.g., `journal_lines`, `stock_batches`).
   - Columns: `snake_case` (e.g., `gross_amount_paise`, `created_at_utc`).
   - Foreign Keys: `fk_{target_table}_{source_column}`.
   - Indexes: `ix_{table}_{columns}`.
   - Unique Constraints: `uq_{table}_{columns}`.
2. **Primary Key Strategy:**
   - **Entity Identity:** Sequential UUIDs (**UUIDv7**) are the preferred primary key for distributed entities, vouchers, invoices, and master records. UUIDv7 embeds a millisecond-precision timestamp, ensuring sequential B-Tree index insertion without fragmenting disk pages.
   - **Internal High-Volume Lines:** `BIGINT GENERATED ALWAYS AS IDENTITY` may be used for append-only line items (e.g., `stock_ledger_id`, `audit_log_id`).
3. **Monetary Storage Invariant:**
   - Monetary values: **`BIGINT NOT NULL`** representing amounts in integer paise (₹1 = 100 paise).
   - **Never** use `FLOAT`, `DOUBLE`, or `REAL` for financial balances.
   - Unit prices and rates: `NUMERIC(18, 4)` for fractional pricing prior to line-level rounding.
   - Quantities: `NUMERIC(14, 4)` to support fractional units (e.g., kilograms, liters, hours).
4. **Audit and Multi-Branch Scoping:**
   - Master and transactional tables must include:
     - `branch_id UUID NOT NULL REFERENCES organization.branches(id)`
     - `created_at_utc TIMESTAMPTZ NOT NULL DEFAULT CLOCK_TIMESTAMP()`
     - `created_by_user_id UUID NOT NULL`
     - `updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT CLOCK_TIMESTAMP()`
     - `updated_by_user_id UUID`
     - `is_deleted BOOLEAN NOT NULL DEFAULT FALSE` (Soft delete flag where applicable)
     - `concurrency_version INT NOT NULL DEFAULT 1` (Optimistic concurrency control)

---

## 4. Representative Relational Schemas (DDL Design)

### 4.1 Schema: `accounting`

```sql
-- Chart of Accounts
CREATE TABLE accounting.account_groups (
    id UUID PRIMARY KEY, -- UUIDv7 generated by .NET 10 Guid.CreateVersion7()
    branch_id UUID NOT NULL,
    group_name VARCHAR(100) NOT NULL,
    group_code VARCHAR(20) NOT NULL UNIQUE,
    parent_group_id UUID REFERENCES accounting.account_groups(id),
    nature VARCHAR(20) NOT NULL CHECK (nature IN ('ASSET', 'LIABILITY', 'EQUITY', 'REVENUE', 'EXPENSE')),
    is_system BOOLEAN NOT NULL DEFAULT FALSE,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT CLOCK_TIMESTAMP()
);

CREATE TABLE accounting.accounts (
    id UUID PRIMARY KEY, -- UUIDv7 generated by .NET 10 Guid.CreateVersion7()
    branch_id UUID NOT NULL,
    account_group_id UUID NOT NULL REFERENCES accounting.account_groups(id),
    account_code VARCHAR(30) NOT NULL,
    account_name VARCHAR(150) NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'INR',
    opening_balance_paise BIGINT NOT NULL DEFAULT 0,
    current_balance_paise BIGINT NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_reconciliation_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    concurrency_version INT NOT NULL DEFAULT 1,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT CLOCK_TIMESTAMP(),
    CONSTRAINT uq_accounts_branch_code UNIQUE (branch_id, account_code)
);

-- Immutable Double-Entry Journal System
CREATE TABLE accounting.vouchers (
    id UUID PRIMARY KEY, -- UUIDv7
    branch_id UUID NOT NULL,
    voucher_number VARCHAR(50) NOT NULL,
    voucher_type VARCHAR(30) NOT NULL CHECK (voucher_type IN (
        'SALES', 'PURCHASE', 'RECEIPT', 'PAYMENT', 'CONTRA', 'JOURNAL', 'CREDIT_NOTE', 'DEBIT_NOTE'
    )),
    status VARCHAR(20) NOT NULL DEFAULT 'POSTED' CHECK (status IN ('DRAFT', 'POSTED', 'REVERSED')),
    posting_date DATE NOT NULL,
    period_id UUID NOT NULL,
    narration TEXT,
    reference_module VARCHAR(50),      -- e.g. 'Sales', 'Purchases', 'Payroll'
    reference_document_id UUID,        -- ID of source invoice/bill
    total_debit_paise BIGINT NOT NULL,
    total_credit_paise BIGINT NOT NULL,
    is_reversed BOOLEAN NOT NULL DEFAULT FALSE,
    reversal_of_voucher_id UUID REFERENCES accounting.vouchers(id),
    reversal_voucher_id UUID REFERENCES accounting.vouchers(id),
    posted_by_user_id UUID NOT NULL,
    posted_at_utc TIMESTAMPTZ NOT NULL DEFAULT CLOCK_TIMESTAMP(),
    CONSTRAINT chk_voucher_balanced CHECK (total_debit_paise = total_credit_paise),
    CONSTRAINT uq_voucher_branch_number UNIQUE (branch_id, voucher_number)
);
-- Note: A deferred PostgreSQL CONSTRAINT TRIGGER enforces that SUM(journal_lines.debit_paise)
-- equals vouchers.total_debit_paise, SUM(journal_lines.credit_paise) equals vouchers.total_credit_paise,
-- and prevents any UPDATE/DELETE on journal_lines or immutable fields when status = 'POSTED'.

CREATE TABLE accounting.journal_lines (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    voucher_id UUID NOT NULL REFERENCES accounting.vouchers(id) ON DELETE RESTRICT,
    account_id UUID NOT NULL REFERENCES accounting.accounts(id) ON DELETE RESTRICT,
    line_number INT NOT NULL,
    debit_paise BIGINT NOT NULL DEFAULT 0,
    credit_paise BIGINT NOT NULL DEFAULT 0,
    line_narration VARCHAR(255),
    cost_center_id UUID,
    CONSTRAINT chk_debit_or_credit CHECK (
        (debit_paise > 0 AND credit_paise = 0) OR 
        (credit_paise > 0 AND debit_paise = 0)
    ),
    CONSTRAINT uq_journal_line UNIQUE (voucher_id, line_number)
);

CREATE INDEX ix_journal_lines_account_posting ON accounting.journal_lines(account_id);
CREATE INDEX ix_vouchers_posting_date ON accounting.vouchers(branch_id, posting_date);
```

### 4.2 Schema: `inventory`

```sql
CREATE TABLE inventory.warehouses (
    id UUID PRIMARY KEY, -- UUIDv7 generated by .NET 10 Guid.CreateVersion7()
    branch_id UUID NOT NULL,
    warehouse_code VARCHAR(30) NOT NULL,
    warehouse_name VARCHAR(100) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT uq_warehouse_code UNIQUE (branch_id, warehouse_code)
);

CREATE TABLE inventory.stock_batches (
    id UUID PRIMARY KEY, -- UUIDv7 generated by .NET 10 Guid.CreateVersion7()
    item_id UUID NOT NULL,
    batch_number VARCHAR(50) NOT NULL,
    manufacturing_date DATE,
    expiry_date DATE,
    purchase_rate_paise BIGINT NOT NULL,
    mrp_paise BIGINT NOT NULL,
    selling_price_paise BIGINT NOT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT CLOCK_TIMESTAMP(),
    CONSTRAINT uq_item_batch UNIQUE (item_id, batch_number)
);

CREATE TABLE inventory.stock_balances (
    id UUID PRIMARY KEY, -- UUIDv7 generated by .NET 10 Guid.CreateVersion7()
    warehouse_id UUID NOT NULL REFERENCES inventory.warehouses(id),
    item_id UUID NOT NULL,
    batch_id UUID REFERENCES inventory.stock_batches(id),
    quantity_on_hand NUMERIC(14, 4) NOT NULL DEFAULT 0,
    quantity_allocated NUMERIC(14, 4) NOT NULL DEFAULT 0,
    valuation_rate_paise BIGINT NOT NULL DEFAULT 0,
    concurrency_version INT NOT NULL DEFAULT 1,
    -- Note: Negative stock constraint chk_positive_hand is enforced by default under Strict Online LAN;
    -- configurable via system policy / trigger if negative stock override or offline sync is ever enabled.
    CONSTRAINT chk_positive_hand CHECK (quantity_on_hand >= 0),
    CONSTRAINT uq_stock_warehouse_item_batch UNIQUE NULLS NOT DISTINCT (warehouse_id, item_id, batch_id)
);

-- Append-Only Stock Ledger
CREATE TABLE inventory.stock_ledger (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    transaction_timestamp TIMESTAMPTZ NOT NULL DEFAULT CLOCK_TIMESTAMP(),
    warehouse_id UUID NOT NULL REFERENCES inventory.warehouses(id),
    item_id UUID NOT NULL,
    batch_id UUID REFERENCES inventory.stock_batches(id),
    movement_type VARCHAR(30) NOT NULL CHECK (movement_type IN (
        'PURCHASE_RECEIPT', 'SALES_ISSUE', 'TRANSFER_IN', 'TRANSFER_OUT', 
        'ADJUSTMENT_ADD', 'ADJUSTMENT_SUBTRACT', 'SCRAP', 'RETURN_INWARD', 'RETURN_OUTWARD'
    )),
    reference_document_type VARCHAR(50) NOT NULL,
    reference_document_id UUID NOT NULL,
    inward_quantity NUMERIC(14, 4) NOT NULL DEFAULT 0,
    outward_quantity NUMERIC(14, 4) NOT NULL DEFAULT 0,
    unit_cost_paise BIGINT NOT NULL,
    total_cost_paise BIGINT NOT NULL,
    running_balance_quantity NUMERIC(14, 4) NOT NULL,
    created_by_user_id UUID NOT NULL
);

CREATE INDEX ix_stock_ledger_item_wh ON inventory.stock_ledger(item_id, warehouse_id, transaction_timestamp);
```

---

## 5. Concurrency Control and Transaction Guarantees

1. **Pessimistic Row-Level Locking (`SELECT FOR UPDATE`):**
   Used during high-contention checkout operations (POS sales, stock issue). When updating `inventory.stock_balances` or assigning sequential invoice voucher numbers, the target rows are selected `FOR UPDATE` inside the database transaction to prevent race conditions.
2. **Optimistic Concurrency (`concurrency_version`):**
   Used for master records (Customers, Items, Price Lists). EF Core maps `concurrency_version` as a concurrency token, throwing `DbUpdateConcurrencyException` if another transaction updated the record concurrently.
3. **Idempotency via Unique Constraints:**
   Every API mutation accepts a client-generated `Idempotency-Key` (UUIDv7). An `idempotency_keys` table stores keys and cached response payloads. Concurrent duplicate requests are absorbed safely without duplicate processing.

