# MAGIC ERP — Phase D Authorization Matrix

**Document:** `00-DOCUMENTATION/PHASE_D_POS_SALES/PHASE_D_AUTHORIZATION_MATRIX.md`  

---

| Permission Code | Module | Description | Administrator | Store Manager | Cashier | Senior Accountant |
|---|---|---|:---:|:---:|:---:|:---:|
| `pos:terminal:access` | POS | Access POS checkout workspace | ✓ | ✓ | ✓ | — |
| `sales:checkout:create` | Sales | Complete retail sales and create invoices | ✓ | ✓ | ✓ | — |
| `sales:invoice:view` | Sales | View sales invoices and customer receipts | ✓ | ✓ | ✓ | ✓ |
| `sales:invoice:cancel` | Sales | Cancel sales invoices (requires audit reason) | ✓ | ✓ | — | — |
| `sales:return:create` | Sales | Initiate sales return against original invoice | ✓ | ✓ | ✓ | — |
| `sales:return:approve` | Sales | Approve return, issue refund and credit note | ✓ | ✓ | — | ✓ |
| `cashier:session:manage` | Cashier | Open, move cash, and submit shift closing | ✓ | ✓ | ✓ | — |
| `cashier:session:reconcile`| Cashier | Supervisor review and approval of cash variance | ✓ | ✓ | — | ✓ |
| `sales:reports:view` | Reports | View sales performance reports and turnover KPIs | ✓ | ✓ | — | ✓ |
| `sales:credit:override` | Sales | Authorize sale on customer credit beyond limit | ✓ | ✓ | — | ✓ |
