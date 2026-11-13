# MAGIC ERP — PHASE E: AUTHORIZATION MATRIX

**Project:** MAGIC – Universal Business Management ERP  
**Phase:** E — Financial Management & Treasury  

---

## 1. Permission Taxonomy

All endpoints enforce fail-closed role-based access control. Unauthorized requests return `403 Forbidden`.

| Permission Code | Module | Description | Admin | Accountant | Cashier | Auditor |
|---|---|---|:---:|:---:|:---:|:---:|
| `finance:receivables:view` | Finance | View customer receivables, outstanding aging, and customer statements | Yes | Yes | Read-Only | Read-Only |
| `finance:receipts:create` | Finance | Create and allocate customer receipts and advances | Yes | Yes | Yes | No |
| `finance:receipts:cancel` | Finance | Reverse customer receipts with non-destructive reversal vouchers | Yes | Yes | No | No |
| `finance:payables:view` | Finance | View supplier payables register, AP aging, and vendor statements | Yes | Yes | No | Read-Only |
| `finance:bills:create` | Finance | Register and approve supplier commercial purchase bills | Yes | Yes | No | No |
| `finance:payments:create` | Finance | Create and allocate supplier disbursements and advances | Yes | Yes | No | No |
| `finance:payments:cancel` | Finance | Reverse supplier payments with companion reversal vouchers | Yes | Yes | No | No |
| `finance:bank-accounts:view` | Finance | View bank and cash account masters with masked account numbers | Yes | Yes | No | Read-Only |
| `finance:bank-accounts:manage` | Finance | Create, configure, and update bank/cash account masters | Yes | Yes | No | No |
| `finance:bank-accounts:reveal` | Finance | Reveal unmasked bank account numbers (Audited in platform log) | Yes | No | No | No |
| `finance:treasury:create` | Finance | Record contra cash/bank transfers, deposits, and bank adjustments | Yes | Yes | No | No |
| `finance:reconciliation:manage`| Finance | Upload bank statements, execute matches, post adjustments & approve | Yes | Yes | No | Read-Only |
| `finance:reports:view` | Finance | Generate and view Cash Book, Bank Book, AR/AP aging & statements | Yes | Yes | Read-Only | Read-Only |
