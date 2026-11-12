# MAGIC ERP — DATABASE ARCHITECTURE SPECIFICATION
## Authoritative PostgreSQL 18 Multi-Schema Architecture

**Document Version:** 2.0.0  
**Project:** MAGIC – Universal Business Management ERP  
**Database Engine:** PostgreSQL 18  
**ORM:** Entity Framework Core 10  
**Storage Invariant:** Strict 64-bit Signed Integer Paise (`BIGINT`) for all INR Monetary Values  
**Quantities & Rates:** Explicit Decimal Precision (`NUMERIC(14, 4)` and `NUMERIC(5, 2)`)  
**Last Updated:** 2026-11-12  

---

## 1. Domain Schemas Overview

To guarantee clean domain boundaries and prevent monolithic table sprawl, PostgreSQL is partitioned into isolated logical schemas:

```
                                  PostgreSQL 18 ("magic_dev")
                                                │
    ┌──────────────┬──────────────┬─────────────┴┬─────────────┬──────────────┬─────────────┐
    ▼              ▼              ▼              ▼             ▼              ▼             ▼
[platform]     [security]      [audit]     [accounting]    [products]    [inventory]     [sales]
    │              │              │              │             │              │             │
    ▼              ▼              ▼              ▼             ▼              ▼             ▼
[purchases]     [pos]          [tax]        [customers]   [suppliers]    [finance]     [expenses]
    │              │              │              │             │              │             │
    ▼              ▼              ▼              ▼             ▼              ▼             ▼
[employees]    [payroll]     [marketing]      [orders]     [services]        [bi]      [automation]
```

---

## 2. Implemented Schemas (Current Production State)

### 2.1. `platform` Schema
- **`platform.idempotency_records`**: Idempotency key tracking for financial mutations (`idempotency_key`, `resource_name`, `request_hash`, `status_code`, `response_payload`, `expires_at`).
- **`platform.outbox_messages`**: Transactional outbox pattern for asynchronous domain event dispatching (`id`, `occurred_at_utc`, `event_type`, `payload_json`, `processed_at_utc`, `retry_count`, `worker_lease_token`).

### 2.2. `audit` Schema
- **`audit.system_audit_logs`**: Append-only tamper-evident audit trail (`id`, `company_id`, `branch_id`, `terminal_id`, `user_id`, `action`, `resource_type`, `resource_id`, `changes_diff_json`, `created_at`).
  - **Engine Protection:** Protected by PostgreSQL trigger `trg_prevent_audit_log_modification` which raises SQLSTATE `55000` on any `UPDATE` or `DELETE`.

### 2.3. `accounting` Schema (12 Production Tables)
- **`accounting.account_groups`**: Hierarchical 5-class structure (1: Assets, 2: Liabilities, 3: Equity, 4: Inward, 5: Outward).
- **`accounting.accounts`**: Master ledger accounts with 5-digit string codes (`11100`, `11200`, `11300`, `21100`, etc.), company scoping, active flags, and system-locked control account tags.
- **`accounting.fiscal_years`**: Financial years (e.g. FY 2026-27: 2026-04-01 to 2027-03-31) with closed/locked state governance.
- **`accounting.fiscal_periods`**: 12 monthly periods per fiscal year with independent locking and supervisor reopening audit.
- **`accounting.vouchers`**: Journal voucher headers with gapless sequential numbers (`JV-YYYY-NNNNNN`), voucher types (`JV`, `SV`, `PV`, `RV`, `PMT`, `RCT`), posted status, posted timestamps, and total amount.
  - **Engine Invariant:** `chk_vouchers_balance` ensures $\text{total\_debit} = \text{total\_credit}$.
  - **Engine Invariant:** `trg_protect_posted_voucher_header` prevents mutation/deletion once `posted = true`.
- **`accounting.journal_lines`**: Journal voucher line items with debit/credit paise amounts, account foreign keys, tax breakdown, line descriptions, and matching pairs.
  - **Engine Invariant:** `chk_journal_lines_dr_cr` ensures positive debit OR credit paise, never both.
  - **Engine Invariant:** `trg_protect_posted_journal_lines` prevents mutation/deletion once parent voucher is posted.
- **`accounting.account_period_balances`**: Pre-aggregated trial balance snapshots (`opening_debit_paise`, `opening_credit_paise`, `period_debit_paise`, `period_credit_paise`, `closing_debit_paise`, `closing_credit_paise`).
- **`accounting.ar_subledger_items`** & **`accounting.ar_allocations`**: Customer Accounts Receivable subledger.
- **`accounting.ap_subledger_items`** & **`accounting.ap_allocations`**: Supplier Accounts Payable subledger.
- **`accounting.voucher_sequences`**: Atomic sequence generators ensuring gapless voucher numbering per company, year, and type.

---

## 3. Planned Domain Schemas (Phases A through G)

### 3.1. `security` Schema (Phase A)
- **`security.users`**: Identity users with PBKDF2 / Argon2id password hashes, lockouts, two-factor secrets, and tenant scoping.
- **`security.roles`**: System and custom roles (Administrator, Store Manager, Cashier, Accountant, Auditor).
- **`security.permissions`**: Granular claim permissions (e.g. `pos:checkout`, `sales:invoice:create`, `accounting:period:lock`).
- **`security.user_roles`** & **`security.role_permissions`**: Many-to-many junction tables.
- **`security.refresh_tokens`**: Cryptographically secure 7-day tokens, hashed with SHA-256, enforcing one-time rotation and token family invalidation.
- **`security.terminal_sessions`**: Active device pairings linked to branch and physical terminal MAC / machine IDs.

### 3.2. `products` & `inventory` Schemas (Phases B & C)
- **`products.categories`** & **`products.brands`**: Catalog hierarchy.
- **`products.units`**: Base and conversion units of measure.
- **`products.items`**: Master SKU, item name, HSN code, tax slab ID, standard cost paise, retail MRP paise, wholesale rate paise, min selling price paise, reorder level.
- **`products.barcodes`**: 1D/2D barcode aliases per SKU with packaging quantity multipliers.
- **`inventory.warehouses`**: Physical warehouses mapped to branches.
- **`inventory.stock_balances`**: Real-time on-hand, reserved, and available stock quantities per item and warehouse.
- **`inventory.stock_ledger`**: Append-only stock transaction history (Date, MovementType, DocumentRef, QtyIn, QtyOut, UnitCostPaise, TotalValuationPaise).
- **`inventory.stock_batches`**: Lot/Batch tracking with manufacturing and expiry timestamps.
- **`inventory.stock_transfers`**: Inter-branch and inter-warehouse stock transfer orders with transit tracking.

### 3.3. `purchases` & `suppliers` Schemas (Phases B & C)
- **`suppliers.suppliers`**: Vendor master, legal name, trade name, GSTIN, MSME type, payment terms, credit limit paise.
- **`suppliers.supplier_bank_details`**: IFSC, bank account number, account holder name for electronic disbursements.
- **`purchases.purchase_orders`** & **`purchases.purchase_order_items`**: Commercial POs with ordered quantities and approved rates.
- **`purchases.goods_receipt_notes`**: Physical warehouse receiving logs with discrepancy tracking.
- **`purchases.purchase_invoices`**: Vendor bills with 3-way matching status, landed costs, input tax credit flags, and AP booking.
- **`purchases.debit_notes`**: Purchase returns and rate adjustments.

### 3.4. `sales`, `pos` & `customers` Schemas (Phases B & D)
- **`customers.customers`**: Customer profiles, B2B/B2C flags, GSTIN, credit limits, payment terms.
- **`customers.customer_addresses`**: Multiple billing and shipping locations with state tax jurisdiction codes.
- **`sales.sales_orders`** & **`sales.sales_order_items`**: Quotations and confirmed commercial orders.
- **`sales.invoices`** & **`sales.invoice_items`**: Statutory GST tax invoices with reverse inclusive / forward exclusive tax amounts.
- **`sales.credit_notes`**: Sales returns, discount credit notes, and rate adjustments.
- **`pos.pos_sessions`**: Cashier shifts with opening float paise, opening timestamp, closing timestamp, closing cash, and variance.
- **`pos.held_carts`** & **`pos.held_cart_items`**: Temporarily parked customer checkout carts.
- **`pos.cash_drops`**: Mid-shift cash drawer transfers to store vault.

### 3.5. `tax` Schema (Phases B & E)
- **`tax.tax_rates`**: Standard Indian GST slabs (0%, 0.25%, 3%, 5%, 12%, 18%, 28%) with CGST, SGST, IGST splits.
- **`tax.hsn_sac_codes`**: 4-, 6-, and 8-digit HSN/SAC master with description and default tax slab reference.
- **`tax.tax_transactions`**: Denormalized tax ledger capturing every taxable sale and purchase line for rapid GSTR-1, GSTR-3B, and GSTR-2B reporting.
- **`tax.tax_settlements`**: Monthly ITC vs. Output liability set-off entries.

### 3.6. `finance`, `expenses`, `employees`, `payroll`, `marketing`, `orders`, `services`, `bi`, `automation`
- Dedicated schemas conforming strictly to the module catalogue specifications in `00-DOCUMENTATION/MODULE_CATALOGUE.md`.

---

## 4. Integrity and Engine-Level Invariants

1. **Paise Enforcement:** All table columns representing currency are typed `BIGINT` and named with a `_paise` suffix (e.g. `amount_paise`, `taxable_paise`, `cgst_paise`, `balance_paise`).
2. **Tenancy Enforcement:** Every operational table contains a `company_id` and optional `branch_id` with composite indexes (`company_id, ...`) and EF Core Global Query Filters.
3. **Optimistic Concurrency:** High-concurrency mutable records (stock balances, accounts, sessions) use `xmin` (PostgreSQL system row version) or explicit `concurrency_token` to prevent lost updates.
4. **Append-Only Immutability:** Audit logs, stock ledger entries, and posted accounting vouchers/lines are immutable. Direct updates are prevented at the database trigger level.

