# MAGIC ERP — PHASE E: DATABASE DESIGN & SCHEMA SPECIFICATION

**Project:** MAGIC – Universal Business Management ERP  
**Phase:** E — Financial Management & Treasury  
**Database:** PostgreSQL 18.x  
**Primary Schemas:** `finance` (Operational Entities) & `accounting` (Ledger Subsystem)  

---

## 1. Schema Overview

Phase E introduces the `finance` schema to house operational records for customer receipts, supplier bills, supplier disbursements, bank/cash masters, treasury contra movements, and bank reconciliation statements, while fully linking into the core `accounting` ledger schema.

```sql
CREATE SCHEMA IF NOT EXISTS finance;
```

---

## 2. Table Specifications

### 2.1. `finance.bank_cash_accounts`
Stores bank and cash accounts managed by the treasury, mapped 1:1 to General Ledger accounts.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `UUID` | `PRIMARY KEY` | Unique account identifier |
| `company_id` | `UUID` | `NOT NULL` | Multi-tenant company context |
| `branch_id` | `UUID` | `NOT NULL` | Branch ownership |
| `account_name` | `VARCHAR(150)` | `NOT NULL` | e.g., "HDFC Corporate Current" |
| `account_type` | `VARCHAR(30)` | `NOT NULL` | `'Bank'` or `'Cash'` |
| `bank_name` | `VARCHAR(100)` | `NULL` | Bank institution name |
| `branch_name` | `VARCHAR(100)` | `NULL` | Bank branch location |
| `account_number` | `VARCHAR(100)` | `NOT NULL` | Plain account number (authorized reveal only) |
| `masked_account_number` | `VARCHAR(100)` | `NOT NULL` | Masked representation (e.g. `••••••••5678`) |
| `ifsc_code` | `VARCHAR(20)` | `NULL` | Indian IFSC routing code |
| `gl_account_id` | `UUID` | `NOT NULL, FK -> accounting.accounts(id)` | Control account in General Ledger |
| `opening_balance_paise` | `BIGINT` | `NOT NULL DEFAULT 0` | Opening balance in paise |
| `current_balance_paise` | `BIGINT` | `NOT NULL DEFAULT 0` | Running ledger balance in paise |
| `is_active` | `BOOLEAN` | `NOT NULL DEFAULT TRUE` | Active status flag |
| `created_at_utc` | `TIMESTAMPTZ` | `NOT NULL` | Creation timestamp |
| `created_by_user_id` | `UUID` | `NOT NULL` | User creator |

---

### 2.2. `finance.customer_receipts`
Records customer payments and advance remittances.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `UUID` | `PRIMARY KEY` | Unique receipt identifier |
| `company_id` | `UUID` | `NOT NULL` | Multi-tenant company context |
| `branch_id` | `UUID` | `NOT NULL` | Branch context |
| `receipt_number` | `VARCHAR(50)` | `NOT NULL, UNIQUE` | Sequential receipt number |
| `customer_id` | `UUID` | `NOT NULL, FK -> commercial.customers(id)` | Customer entity |
| `receipt_date` | `DATE` | `NOT NULL` | Transaction date |
| `payment_method` | `VARCHAR(30)` | `NOT NULL` | `Cash`, `BankTransfer`, `UPI`, `Cheque` |
| `reference_number` | `VARCHAR(100)` | `NULL` | UTR / Cheque / Transaction reference |
| `bank_cash_account_id` | `UUID` | `NOT NULL, FK -> finance.bank_cash_accounts(id)` | Destination bank/cash account |
| `total_amount_paise` | `BIGINT` | `NOT NULL, CHECK (total_amount_paise > 0)` | Total payment received in paise |
| `allocated_amount_paise` | `BIGINT` | `NOT NULL DEFAULT 0` | Amount allocated to open invoices |
| `unallocated_amount_paise` | `BIGINT` | `NOT NULL DEFAULT 0` | Advance customer balance |
| `status` | `VARCHAR(30)` | `NOT NULL DEFAULT 'SUBMITTED'` | `'SUBMITTED'`, `'REVERSED'` |
| `voucher_id` | `UUID` | `NOT NULL, FK -> accounting.journal_vouchers(id)` | Double-entry receipt journal voucher |
| `narration` | `TEXT` | `NOT NULL DEFAULT ''` | Remarks or receipt narration |
| `created_at_utc` | `TIMESTAMPTZ` | `NOT NULL` | Timestamp |
| `created_by_user_id` | `UUID` | `NOT NULL` | Creator |

---

### 2.3. `finance.customer_receipt_allocations`
Captures multi-invoice settlement distribution of receipts.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `UUID` | `PRIMARY KEY` | Unique allocation identifier |
| `receipt_id` | `UUID` | `NOT NULL, FK -> finance.customer_receipts(id) ON DELETE CASCADE` | Parent receipt |
| `ar_item_id` | `UUID` | `NOT NULL, FK -> accounting.ar_subledger_items(id)` | Open receivable item |
| `allocated_amount_paise` | `BIGINT` | `NOT NULL, CHECK (allocated_amount_paise > 0)` | Allocated amount in paise |
| `allocation_date` | `DATE` | `NOT NULL` | Allocation date |
| `created_at_utc` | `TIMESTAMPTZ` | `NOT NULL` | Creation timestamp |

---

### 2.4. `finance.supplier_invoices` & `lines`
Recognizes authorized commercial purchase liability.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `UUID` | `PRIMARY KEY` | Unique bill identifier |
| `company_id` | `UUID` | `NOT NULL` | Tenant context |
| `branch_id` | `UUID` | `NOT NULL` | Branch context |
| `supplier_id` | `UUID` | `NOT NULL, FK -> commercial.suppliers(id)` | Supplier |
| `invoice_number` | `VARCHAR(100)` | `NOT NULL` | Supplier's tax invoice / bill reference |
| `system_invoice_number` | `VARCHAR(50)` | `NOT NULL, UNIQUE` | Internal sequential PINV reference |
| `invoice_date` | `DATE` | `NOT NULL` | Bill date |
| `due_date` | `DATE` | `NOT NULL` | Payment due date |
| `po_id` | `UUID` | `NULL, FK -> inventory.purchase_orders(id)` | Optional PO reference |
| `grn_id` | `UUID` | `NULL, FK -> inventory.goods_receipt_notes(id)` | Optional GRN reference |
| `sub_total_paise` | `BIGINT` | `NOT NULL` | Taxable subtotal |
| `tax_amount_paise` | `BIGINT` | `NOT NULL` | Input GST |
| `total_amount_paise` | `BIGINT` | `NOT NULL, CHECK (total_amount_paise > 0)` | Net payable liability |
| `settled_amount_paise` | `BIGINT` | `NOT NULL DEFAULT 0` | Amount settled via payments |
| `outstanding_amount_paise`| `BIGINT` | `NOT NULL` | Remaining unpaid liability |
| `status` | `VARCHAR(30)` | `NOT NULL DEFAULT 'APPROVED'` | `'APPROVED'`, `'PAID'`, `'PARTIALLY_PAID'`, `'CANCELLED'` |
| `voucher_id` | `UUID` | `NOT NULL, FK -> accounting.journal_vouchers(id)` | General ledger purchase voucher |
| `notes` | `TEXT` | `NOT NULL DEFAULT ''` | Remarks |
| `created_at_utc` | `TIMESTAMPTZ` | `NOT NULL` | Created at |
| `created_by_user_id` | `UUID` | `NOT NULL` | Creator |

---

### 2.5. `finance.supplier_payments` & `allocations`
Disbursement records and multi-bill settlement allocations.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `UUID` | `PRIMARY KEY` | Unique payment identifier |
| `company_id` | `UUID` | `NOT NULL` | Tenant context |
| `branch_id` | `UUID` | `NOT NULL` | Branch context |
| `payment_number` | `VARCHAR(50)` | `NOT NULL, UNIQUE` | Sequential payment number |
| `supplier_id` | `UUID` | `NOT NULL, FK -> commercial.suppliers(id)` | Vendor entity |
| `payment_date` | `DATE` | `NOT NULL` | Disbursement date |
| `payment_method` | `VARCHAR(30)` | `NOT NULL` | `Cash`, `BankTransfer`, `UPI`, `Cheque` |
| `reference_number` | `VARCHAR(100)` | `NULL` | Transaction reference / UTR |
| `bank_cash_account_id` | `UUID` | `NOT NULL, FK -> finance.bank_cash_accounts(id)` | Funding bank/cash account |
| `total_amount_paise` | `BIGINT` | `NOT NULL, CHECK (total_amount_paise > 0)` | Amount paid in paise |
| `allocated_amount_paise` | `BIGINT` | `NOT NULL DEFAULT 0` | Amount allocated to open bills |
| `unallocated_amount_paise` | `BIGINT` | `NOT NULL DEFAULT 0` | Vendor advance balance |
| `status` | `VARCHAR(30)` | `NOT NULL DEFAULT 'SUBMITTED'` | `'SUBMITTED'`, `'REVERSED'` |
| `voucher_id` | `UUID` | `NOT NULL, FK -> accounting.journal_vouchers(id)` | Double-entry payment journal voucher |
| `narration` | `TEXT` | `NOT NULL DEFAULT ''` | Remarks |
| `created_at_utc` | `TIMESTAMPTZ` | `NOT NULL` | Timestamp |
| `created_by_user_id` | `UUID` | `NOT NULL` | Creator |

---

### 2.6. `finance.treasury_transactions`
Records internal cash/bank transfers, deposits, withdrawals, interest, and charges.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `UUID` | `PRIMARY KEY` | Transaction identifier |
| `company_id` | `UUID` | `NOT NULL` | Multi-tenant context |
| `branch_id` | `UUID` | `NOT NULL` | Branch context |
| `transaction_number` | `VARCHAR(50)` | `NOT NULL, UNIQUE` | Sequential transaction number |
| `transaction_type` | `VARCHAR(40)` | `NOT NULL` | `CashDeposit`, `CashWithdrawal`, `BankTransfer`, `CashTransfer`, `BankCharges`, `BankInterest` |
| `transaction_date` | `DATE` | `NOT NULL` | Date |
| `source_account_id` | `UUID` | `NOT NULL, FK -> finance.bank_cash_accounts(id)` | Source account |
| `destination_account_id` | `UUID` | `NULL, FK -> finance.bank_cash_accounts(id)` | Destination account (if transfer) |
| `amount_paise` | `BIGINT` | `NOT NULL, CHECK (amount_paise > 0)` | Movement amount in paise |
| `reference_number` | `VARCHAR(100)` | `NULL` | Reference number |
| `narration` | `TEXT` | `NOT NULL DEFAULT ''` | Narration |
| `status` | `VARCHAR(30)` | `NOT NULL DEFAULT 'POSTED'` | `'POSTED'`, `'REVERSED'` |
| `voucher_id` | `UUID` | `NOT NULL, FK -> accounting.journal_vouchers(id)` | Balanced double-entry contra/journal voucher |
| `created_at_utc` | `TIMESTAMPTZ` | `NOT NULL` | Timestamp |
| `created_by_user_id` | `UUID` | `NOT NULL` | Creator |

---

### 2.7. `finance.bank_statements` & `lines`
Bank statement imports for reconciliation.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `id` | `UUID` | `PRIMARY KEY` | Statement import identifier |
| `bank_account_id` | `UUID` | `NOT NULL, FK -> finance.bank_cash_accounts(id)` | Bank account |
| `statement_start_date` | `DATE` | `NOT NULL` | Period start date |
| `statement_end_date` | `DATE` | `NOT NULL` | Period end date |
| `opening_balance_paise` | `BIGINT` | `NOT NULL` | Opening balance |
| `closing_balance_paise` | `BIGINT` | `NOT NULL` | Closing balance |
| `filename` | `VARCHAR(200)` | `NOT NULL` | Original filename |
| `status` | `VARCHAR(30)` | `NOT NULL DEFAULT 'OPEN'` | `'OPEN'`, `'RECONCILED'` |
| `imported_at_utc` | `TIMESTAMPTZ` | `NOT NULL` | Import timestamp |
| `imported_by_user_id` | `UUID` | `NOT NULL` | User |

Lines:
- `withdrawal_debit_paise`: Withdrawal / Debit from bank (BIGINT >= 0)
- `deposit_credit_paise`: Deposit / Credit into bank (BIGINT >= 0)
- `balance_paise`: Statement running balance (BIGINT)
- `is_reconciled`: Boolean (true when matched)
- `matched_voucher_line_id`: Nullable FK to `accounting.journal_lines(id)`
- `match_type`: `'MANUAL'`, `'AUTO_EXACT'`, `'AUTO_SUGGESTED'`
