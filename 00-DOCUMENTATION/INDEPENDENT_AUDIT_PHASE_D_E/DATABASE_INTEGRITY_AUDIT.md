# MAGIC ERP — INDEPENDENT TECHNICAL AUDIT
## Database Architecture, Schema Constraints & Data Integrity Audit

**Project:** MAGIC – Universal Business Management ERP  
**Scope:** Section 6 (Database & Data Integrity)  
**Database Server:** PostgreSQL 18.1  
**Databases Inspected:** `magic_dev` and `magic_test`  
**Date:** 2026-11-13  

---

## 1. Schema Isolation and Ownership

The database architecture is cleanly partitioned into domain-specific schemas:
1. `security`: User identity, password hashes, roles, permissions, session tokens.
2. `commercial`: Products, categories, brands, price tiers, customers, suppliers.
3. `inventory`: Warehouses, storage locations, stock balances, stock movements, purchase orders, goods receipts, purchase returns.
4. `sales`: POS sales invoices, sales invoice lines, sales payments, sales returns, sales return lines, credit notes.
5. `cashier`: Cashier sessions, cash movements.
6. `accounting`: Fiscal years, periods, chart of accounts, journal vouchers, journal lines, AR subledgers, AP subledgers, voucher sequences.
7. `finance`: Bank and cash accounts, customer receipts, receipt allocations, supplier commercial bills, bill lines, supplier payments, payment allocations, treasury transactions, bank statements, bank statement lines.
8. `platform`: System audit logs, idempotency records, outbox messages.

---

## 2. Foreign Key & Constraint Verification

### Relational Integrity Findings:
1. **Cascade Deletes Disallowed:** All foreign keys across `sales` and `finance` use `ON DELETE RESTRICT` or `ON DELETE NO ACTION`. Deleting master entities (Customers, Suppliers, Accounts, Products) with existing financial history is strictly blocked by the database engine.
2. **Tenant Isolation Foreign Keys:** Every transaction table includes `company_id` and `branch_id` indexed for fast tenant-scoped queries.
3. **Database Check Constraints:**
   - `finance.customer_receipts`: `chk_receipt_amounts` (`total_amount_paise > 0 AND allocated_amount_paise >= 0 AND unallocated_amount_paise >= 0 AND (allocated_amount_paise + unallocated_amount_paise = total_amount_paise)`)
   - `finance.supplier_invoices`: `chk_supplier_invoice_amounts` (`total_amount_paise >= 0 AND outstanding_amount_paise >= 0 AND (settled_amount_paise + outstanding_amount_paise = total_amount_paise)`)
   - `finance.supplier_payments`: `chk_payment_amounts` (`total_amount_paise > 0 AND allocated_amount_paise >= 0 AND unallocated_amount_paise >= 0 AND (allocated_amount_paise + unallocated_amount_paise = total_amount_paise)`)
   - `finance.treasury_transactions`: `chk_treasury_amount` (`amount_paise > 0`)

### Schema Deficiencies Identified:
1. **Missing Warehouse Foreign Key on Sales Invoices (DEF-DE-005):**
   - Table `sales.sales_invoices` has `company_id`, `branch_id`, `customer_id`, `cashier_session_id`, but **lacks `warehouse_id`**.
   - Table `sales.sales_invoice_lines` has `storage_location_id`, but lacks `warehouse_id`.
   - As a result, invoice cancellation cannot deterministically identify which warehouse issued the inventory.

---

## 3. Orphaned Records and Data Consistency Check

Using read-only SQL queries against the local databases:
- **Orphaned Invoice Lines:** 0 found (`sales_invoice_lines` without parent `sales_invoices`).
- **Orphaned Payment Allocations:** 0 found.
- **Orphaned Stock Movements:** 0 found.
- **Unbalanced Journal Vouchers:** 0 found in `accounting.journal_vouchers`. All posted vouchers have `total_debit_paise == total_credit_paise`.
- **Negative Stock Balances:** 0 unauthorized negative stock balances found.

---

## 4. Migration History & Idempotency

- All migrations (`20261112081209_AddAccountingSchemaAndLedgerTables` through `20261113095326_AddFinancialManagementAndTreasurySchema`) are registered in `__EFMigrationsHistory` in chronological sequence.
- Model snapshot `MagicDbContextModelSnapshot.cs` is synchronized with the PostgreSQL schema.
- Schema creation scripts are strictly additive and preserve historical accounting data.

