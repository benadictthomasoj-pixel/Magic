# MAGIC ERP — Database Migration & DDL Schema Report
## Phase D & Phase E Remediation

**Databases Updated:**
1. Development: `magic_dev` on `127.0.0.1:5432`
2. Test / Integration: `magic_test` on `127.0.0.1:5432`

---

## 1. Schema Modifications Summary

### Table: `sales.sales_invoices`
To support originating warehouse tracking (R5 / DEF-DE-005) and GL voucher linkage (R1 / DEF-DE-001):
```sql
ALTER TABLE sales.sales_invoices 
    ADD COLUMN IF NOT EXISTS "WarehouseId" uuid NULL;

ALTER TABLE sales.sales_invoices 
    ADD COLUMN IF NOT EXISTS "AccountingVoucherId" uuid NULL;

CREATE INDEX IF NOT EXISTS "ix_sales_invoices_warehouse_id" 
    ON sales.sales_invoices ("WarehouseId");

CREATE INDEX IF NOT EXISTS "ix_sales_invoices_voucher_id" 
    ON sales.sales_invoices ("AccountingVoucherId");
```

### Table: `accounting.accounts` (Chart of Accounts Seeding)
To support statutory Indian GST tax accounting for inter-state procurements (R7 / DEF-DE-007) and sales:
```sql
-- Input IGST Credit (Asset)
INSERT INTO accounting.accounts 
    (id, company_id, code, name, root_class, account_group_id, normal_balance, is_reconciliation, is_active, created_at_utc)
VALUES
    (
        '01a1d3f9-7f30-749e-b154-113300000001',
        '11111111-1111-1111-1111-111111111111',
        '1133',
        'Input IGST Credit',
        1, -- Asset
        '01a1cefa-db7f-7120-8339-ff727f711003', -- Taxes Receivable Group
        1, -- Debit
        FALSE,
        TRUE,
        NOW()
    )
ON CONFLICT (company_id, code) DO NOTHING;

-- Output IGST Payable (Liability)
INSERT INTO accounting.accounts 
    (id, company_id, code, name, root_class, account_group_id, normal_balance, is_reconciliation, is_active, created_at_utc)
VALUES
    (
        '01a1d3f9-7f30-749e-b154-212300000001',
        '11111111-1111-1111-1111-111111111111',
        '2123',
        'Output IGST Payable',
        2, -- Liability
        '01a1cefa-db7f-7120-8339-ff727f711005', -- Current Liabilities Group
        2, -- Credit
        FALSE,
        TRUE,
        NOW()
    )
ON CONFLICT (company_id, code) DO NOTHING;
```

## 2. Integrity Verification
All check constraints on `accounting.ar_subledger_items`:
- `chk_ar_amounts` (`settled_amount_paise + outstanding_amount_paise = original_amount_paise`)
- `chk_ar_status` (`status IN ('OPEN', 'PARTIALLY_PAID', 'PAID')`)
remain fully satisfied and enforced across all records.
