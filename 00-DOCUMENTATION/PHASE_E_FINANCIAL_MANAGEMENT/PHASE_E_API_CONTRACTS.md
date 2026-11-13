# MAGIC ERP — PHASE E: API CONTRACT SPECIFICATION

**Project:** MAGIC – Universal Business Management ERP  
**Phase:** E — Financial Management & Treasury  
**Base URL:** `/api/v1/accounting` or `/api/v1/finance`  

---

## 1. Endpoints Summary

### Receivables & Receipts
- `GET /api/v1/finance/receivables` — Customer outstanding receivables summary & invoice-wise list
- `GET /api/v1/finance/receivables/aging` — Customer AR aging report (Current, 1-30, 31-60, 61-90, 90+)
- `GET /api/v1/finance/receivables/statement/{customerId}` — Detailed customer statement of accounts
- `GET /api/v1/finance/customer-receipts` — Customer receipts history & register
- `GET /api/v1/finance/customer-receipts/{id}` — Customer receipt detail with allocations
- `POST /api/v1/finance/customer-receipts` — Create customer receipt with multi-invoice allocations or unallocated advance
- `POST /api/v1/finance/customer-receipts/{id}/cancel` — Reverse customer receipt with reversal voucher

### Payables & Supplier Payments
- `GET /api/v1/finance/payables` — Supplier outstanding payables & bill-wise register
- `GET /api/v1/finance/payables/aging` — Supplier AP aging report
- `GET /api/v1/finance/payables/statement/{supplierId}` — Detailed supplier statement of accounts
- `POST /api/v1/finance/payables/bills` — Register and approve supplier commercial bill / purchase invoice
- `GET /api/v1/finance/supplier-payments` — Supplier payments history & register
- `GET /api/v1/finance/supplier-payments/{id}` — Supplier payment detail with allocations
- `POST /api/v1/finance/supplier-payments` — Create supplier payment with multi-bill settlement
- `POST /api/v1/finance/supplier-payments/{id}/cancel` — Reverse supplier payment with reversal voucher

### Bank & Cash Accounts Master
- `GET /api/v1/finance/bank-accounts` — List bank and cash accounts with masked account numbers
- `GET /api/v1/finance/bank-accounts/{id}` — Get single bank/cash account
- `POST /api/v1/finance/bank-accounts` — Create new bank or cash account
- `PUT /api/v1/finance/bank-accounts/{id}` — Update bank/cash account
- `POST /api/v1/finance/bank-accounts/{id}/reveal` — Authorized reveal of unmasked account number (audited)

### Treasury Transactions
- `GET /api/v1/finance/treasury` — List treasury movements (deposits, withdrawals, transfers, charges)
- `POST /api/v1/finance/treasury/contra` — Create Contra transaction (Cash deposit, withdrawal, bank transfer)
- `POST /api/v1/finance/treasury/adjustment` — Create Bank Charge or Interest entry

### Bank Reconciliation (BRS)
- `GET /api/v1/finance/bank-reconciliation/statements` — List imported bank statements
- `POST /api/v1/finance/bank-reconciliation/import-csv` — Upload and parse bank statement CSV
- `GET /api/v1/finance/bank-reconciliation/match-workspace/{statementId}` — Get reconciliation workspace (statement lines vs internal GL transactions)
- `POST /api/v1/finance/bank-reconciliation/match` — Match statement line with internal GL journal line
- `POST /api/v1/finance/bank-reconciliation/unmatch` — Unmatch statement line
- `POST /api/v1/finance/bank-reconciliation/complete/{statementId}` — Approve and finalize bank reconciliation

### Financial Reports
- `GET /api/v1/finance/reports/cash-book` — Chronological Cash Book
- `GET /api/v1/finance/reports/bank-book` — Chronological Bank Book
- `GET /api/v1/finance/reports/summary` — High-level financial overview KPIs (Total AR, Total AP, Cash on Hand, Bank Balances)
