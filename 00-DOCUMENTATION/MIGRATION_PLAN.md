# MAGIC — DATA MIGRATION & SCHEMA EVOLUTION PLAN

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  

---

## 1. Database Schema Migration Protocol

MAGIC uses **Entity Framework Core 10 Code-First Migrations** coupled with strict SQL migration script generation and validation before database deployment.

> [!NOTE]
> **Greenfield Separation:** Legacy external data ingestion (e.g., CSV/Excel opening balances for new business onboarding) is strictly an optional customer-data import workflow — MAGIC never imports schemas, code, or database structures from any previous application.

### Migration Rules
1. **Never Silently Mutate Database Structures:**
   All schema modifications (adding tables, altering columns, adding foreign keys, indexing) must be captured as version-controlled migration files in `02-DATABASE/Migrations/`.
2. **Idempotent SQL Generation:**
   Every migration must be verifiable via `dotnet ef migrations script --idempotent` to ensure it can execute safely on existing databases without failing on re-run.
3. **Rollback Strategy:**
   Every forward migration must have a tested, verified `Down()` method. In production environments, database backups are taken immediately prior to migration execution.
4. **Zero-Destructive DDL on Upgrades:**
   Dropping existing columns containing business or historical data is prohibited. Columns are first marked as deprecated, unmapped from domain models, and archived before structural removal.

---

## 2. Legacy Enterprise Data Ingestion Plan

To onboard businesses migrating their own accounting opening balances from external bookkeeping spreadsheets or commercial packages (e.g., CSV / Excel / standardized XML), MAGIC provides a standardized **Four-Stage Opening Data Pipeline**:

```
[Legacy Export (XML / CSV / Excel)]
                |
                v
[Stage 1: Ingestion & Schema Normalization]
                |
                v
[Stage 2: Validation, Deduplication & HSN Classification]
                |
                v
[Stage 3: Review Grid & Discrepancy Reconciliation]
                |
                v
[Stage 4: Atomic Commit to Masters & Opening Balance Journal]
```

---

## 3. Opening Balance Financial Cutover Protocol

A clean financial cutover requires balancing the **Opening Trial Balance** at the start of the go-live fiscal period:

1. **Opening Ledger Accounts:**
   - Ingest master Chart of Accounts.
   - Post Opening Balance Journal Voucher:
     - Debits: Cash, Bank, Customer Outstanding Subledgers, Inventory Asset, Fixed Assets.
     - Credits: Supplier Outstanding Subledgers, Loans, Statutory Dues, Owner's Equity.
     - Difference (if any) temporary parked in `3199: Opening Balance Suspense Account` until reconciled.
2. **Opening Inventory Reconciliation:**
   - Ingest physical stock count by warehouse and batch (Qty, Batch No, Expiry, Purchase Cost in paise).
   - The sum of $(\text{BatchQty} \times \text{CostPaise})$ must equal the balance in GL Account `1140: Inventory Asset`.
3. **Accounts Receivable / Payable Itemized Cutover:**
   - Ingest open unpaid invoices for each customer and supplier to populate the Aging Register, matching the total AR/AP GL control accounts.

