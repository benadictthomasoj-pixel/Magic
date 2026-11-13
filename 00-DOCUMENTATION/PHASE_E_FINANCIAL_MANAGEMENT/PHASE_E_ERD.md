# MAGIC ERP — PHASE E: ENTITY RELATIONSHIP DIAGRAM (ERD)

**Project:** MAGIC – Universal Business Management ERP  
**Phase:** E — Financial Management & Treasury  

```mermaid
erDiagram
    COMMERCIAL_CUSTOMERS ||--o{ FINANCE_CUSTOMER_RECEIPTS : "pays"
    COMMERCIAL_SUPPLIERS ||--o{ FINANCE_SUPPLIER_INVOICES : "issues bill"
    COMMERCIAL_SUPPLIERS ||--o{ FINANCE_SUPPLIER_PAYMENTS : "receives payment"
    
    ACCOUNTING_ACCOUNTS ||--o{ FINANCE_BANK_CASH_ACCOUNTS : "mapped GL control"
    
    FINANCE_BANK_CASH_ACCOUNTS ||--o{ FINANCE_CUSTOMER_RECEIPTS : "funds deposited to"
    FINANCE_BANK_CASH_ACCOUNTS ||--o{ FINANCE_SUPPLIER_PAYMENTS : "disbursed from"
    FINANCE_BANK_CASH_ACCOUNTS ||--o{ FINANCE_TREASURY_TRANSACTIONS : "source account"
    FINANCE_BANK_CASH_ACCOUNTS ||--o{ FINANCE_TREASURY_TRANSACTIONS : "dest account"
    FINANCE_BANK_CASH_ACCOUNTS ||--o{ FINANCE_BANK_STATEMENTS : "reconciles"

    ACCOUNTING_AR_SUBLEDGER_ITEMS ||--o{ FINANCE_CUSTOMER_RECEIPT_ALLOCATIONS : "settled by"
    FINANCE_CUSTOMER_RECEIPTS ||--o{ FINANCE_CUSTOMER_RECEIPT_ALLOCATIONS : "allocates"
    
    ACCOUNTING_AP_SUBLEDGER_ITEMS ||--o{ FINANCE_SUPPLIER_PAYMENT_ALLOCATIONS : "settled by"
    FINANCE_SUPPLIER_PAYMENTS ||--o{ FINANCE_SUPPLIER_PAYMENT_ALLOCATIONS : "allocates"

    FINANCE_SUPPLIER_INVOICES ||--o{ FINANCE_SUPPLIER_INVOICE_LINES : "contains"
    FINANCE_SUPPLIER_INVOICES ||--|| ACCOUNTING_AP_SUBLEDGER_ITEMS : "generates open item"

    FINANCE_CUSTOMER_RECEIPTS ||--|| ACCOUNTING_JOURNAL_VOUCHERS : "posts receipt voucher"
    FINANCE_SUPPLIER_INVOICES ||--|| ACCOUNTING_JOURNAL_VOUCHERS : "posts purchase voucher"
    FINANCE_SUPPLIER_PAYMENTS ||--|| ACCOUNTING_JOURNAL_VOUCHERS : "posts payment voucher"
    FINANCE_TREASURY_TRANSACTIONS ||--|| ACCOUNTING_JOURNAL_VOUCHERS : "posts contra voucher"

    FINANCE_BANK_STATEMENTS ||--o{ FINANCE_BANK_STATEMENT_LINES : "contains"
    ACCOUNTING_JOURNAL_LINES ||--o| FINANCE_BANK_STATEMENT_LINES : "matched with"

    ACCOUNTING_JOURNAL_VOUCHERS ||--|{ ACCOUNTING_JOURNAL_LINES : "contains balanced lines"
```

---

## 2. Cross-Schema Foreign Key Integrity

1. `finance.customer_receipts.voucher_id` ──► `accounting.journal_vouchers.id` (RESTRICT)
2. `finance.customer_receipt_allocations.ar_item_id` ──► `accounting.ar_subledger_items.id` (RESTRICT)
3. `finance.supplier_invoices.voucher_id` ──► `accounting.journal_vouchers.id` (RESTRICT)
4. `finance.supplier_payments.voucher_id` ──► `accounting.journal_vouchers.id` (RESTRICT)
5. `finance.supplier_payment_allocations.ap_item_id` ──► `accounting.ap_subledger_items.id` (RESTRICT)
6. `finance.bank_cash_accounts.gl_account_id` ──► `accounting.accounts.id` (RESTRICT)
7. `finance.treasury_transactions.voucher_id` ──► `accounting.journal_vouchers.id` (RESTRICT)
8. `finance.bank_statement_lines.matched_voucher_line_id` ──► `accounting.journal_lines.id` (SET NULL)
