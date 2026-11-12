# MAGIC ERP — API ARCHITECTURE & CONTRACT SPECIFICATION
## Enterprise RESTful Web API Architecture & Communication Standards

**Document Version:** 2.0.0  
**Project:** MAGIC – Universal Business Management ERP  
**Protocol:** HTTPS / RESTful JSON  
**Specification Standard:** RFC 7807 (Problem Details for HTTP APIs)  
**Security Standard:** Bearer JWT + Strict Header Validation (OPEN-003)  
**Last Updated:** 2026-11-12  

---

## 1. Unified Request & Response Protocols

All MAGIC ERP APIs adhere to strict uniform REST standards across all 21 modules.

### 1.1. Standard Request Headers
| Header Name | Type | Mandatory? | Description |
|---|---|:---:|---|
| `Authorization` | String | Yes (Protected) | `Bearer <JWT_ACCESS_TOKEN>` containing user, company, role claims |
| `X-Correlation-ID` | UUID | Yes | Client-generated or middleware-injected distributed trace ID |
| `X-Company-ID` | UUID | Yes | Current active company tenant context |
| `X-Branch-ID` | UUID | Yes | Current active physical branch context |
| `X-Terminal-ID` | UUID / String | Yes (POS/Desk) | Physical terminal or cash register identifier |
| `Idempotency-Key` | String | Yes (Mutations) | Unique client key preventing duplicate postings on retries |

### 1.2. Standard HTTP Response Formats
- **Success Responses (200 OK, 201 Created):** Directly return the typed DTO or envelope containing the requested data and metadata.
- **Client Errors & Failures (400, 401, 403, 404, 409, 422, 500):** Conform strictly to **RFC 7807 Problem Details**:
```json
{
  "type": "https://errors.magic.erp/AUTH_REQUIRED_OPEN_003",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Authentication is required to perform this financial operation.",
  "instance": "/api/v1/accounting/vouchers/draft",
  "correlationId": "8f24a1b0-4f51-4e7a-9a43-021948ba269c"
}
```

---

## 2. API Roster Across All 21 Modules

### Module 01: Dashboard & Business Intelligence
- `GET /api/v1/bi/overview`: Consolidated KPI metrics (sales, purchases, cash, GP/NP).
- `GET /api/v1/bi/sales-trends`: Daily, weekly, monthly turnover series.
- `GET /api/v1/bi/top-products`: Top 10 revenue and quantity velocity items.

### Module 02: POS & Billing
- `POST /api/v1/pos/sessions/open`: Open cashier shift with opening float.
- `POST /api/v1/pos/sessions/close`: Close shift with physical cash count and variance report.
- `POST /api/v1/pos/checkout`: Complete high-speed retail checkout transaction.
- `POST /api/v1/pos/carts/hold`: Temporarily park an incomplete shopping cart.
- `GET /api/v1/pos/carts/held`: List active parked carts for recall.
- `POST /api/v1/pos/cash-drops`: Log mid-shift cash drop to safe.

### Module 03: Sales Management
- `GET /api/v1/sales/invoices`: Paginated invoice list with customer, date, status filters.
- `GET /api/v1/sales/invoices/{id}`: Detailed invoice view with items, tax, payment allocations.
- `POST /api/v1/sales/invoices`: Create and post statutory GST tax invoice.
- `POST /api/v1/sales/orders`: Create commercial sales order.
- `POST /api/v1/sales/credit-notes`: Issue sales return or discount credit note.

### Module 04: Purchase & Procurement
- `GET /api/v1/purchases/invoices`: Paginated purchase bills list.
- `POST /api/v1/purchases/invoices`: Create and verify vendor purchase invoice.
- `POST /api/v1/purchases/orders`: Create purchase order for supplier.
- `POST /api/v1/purchases/grn`: Record Goods Receipt Note with physical count.
- `POST /api/v1/purchases/debit-notes`: Issue purchase return or rate adjustment debit note.

### Module 05: Inventory & Warehouse
- `GET /api/v1/inventory/items`: Master product list with search and filters.
- `POST /api/v1/inventory/items`: Create new catalog item with pricing and HSN codes.
- `PUT /api/v1/inventory/items/{id}`: Update item details and reorder thresholds.
- `GET /api/v1/inventory/stock-balances`: Real-time stock levels per branch and warehouse.
- `POST /api/v1/inventory/transfers`: Create and approve inter-warehouse stock transfer.
- `POST /api/v1/inventory/adjustments`: Record physical inventory audit adjustment.

### Module 06: Accounting (Live Production Endpoints)
- `GET /api/v1/accounting/dashboard`: Financial health KPIs.
- `GET /api/v1/accounting/chart-of-accounts`: Full hierarchical tree of accounts.
- `POST /api/v1/accounting/accounts`: Create user detail account.
- `PUT /api/v1/accounting/accounts/{id}`: Update account properties.
- `GET /api/v1/accounting/vouchers`: Paginated voucher registry with filters.
- `GET /api/v1/accounting/vouchers/{id}`: Full journal voucher with lines.
- `POST /api/v1/accounting/vouchers/draft`: Save balanced draft voucher.
- `POST /api/v1/accounting/vouchers/{id}/post`: Post voucher to General Ledger.
- `POST /api/v1/accounting/vouchers/{id}/reverse`: Create counter-reversal voucher.
- `GET /api/v1/accounting/general-ledger`: Detailed account statement.
- `GET /api/v1/accounting/trial-balance`: Real-time balanced trial balance.
- `GET /api/v1/accounting/fiscal-periods`: Fiscal years and monthly period states.
- `POST /api/v1/accounting/fiscal-periods/{id}/lock`: Lock period against backdated entries.
- `POST /api/v1/accounting/fiscal-periods/{id}/close`: Permanently close period.
- `POST /api/v1/accounting/fiscal-periods/{id}/reopen`: Authorized supervisor reopening.
- `GET /api/v1/accounting/subledgers/ar`: Customer receivable subledger.
- `GET /api/v1/accounting/subledgers/ap`: Supplier payable subledger.

### Module 07: Financial Management & Treasury
- `GET /api/v1/finance/bank-accounts`: List connected bank ledgers.
- `POST /api/v1/finance/reconciliations`: Post bank statement reconciliation (BRS).
- `GET /api/v1/finance/fixed-assets`: Fixed asset register.
- `POST /api/v1/finance/fixed-assets/depreciation-run`: Execute monthly depreciation batch.

### Module 08: Tax & Statutory Compliance
- `GET /api/v1/tax/rates`: List statutory GST tax slabs.
- `GET /api/v1/tax/hsn-codes`: Search official HSN/SAC directory.
- `GET /api/v1/tax/gstr-1`: Generate GSTR-1 outward supply JSON/tabular dataset.
- `GET /api/v1/tax/gstr-3b`: Generate GSTR-3B tax summary dataset.
- `POST /api/v1/tax/settlement`: Post monthly input/output GST set-off voucher.

### Module 09: Customer CRM
- `GET /api/v1/customers`: Paginated customer directory with search.
- `POST /api/v1/customers`: Create customer record with GSTIN validation.
- `PUT /api/v1/customers/{id}`: Update customer profile and credit limits.
- `GET /api/v1/customers/{id}/statement`: Customer ledger statement of account.

### Module 10: Supplier Management
- `GET /api/v1/suppliers`: Paginated supplier directory.
- `POST /api/v1/suppliers`: Onboard vendor with MSME and bank payment details.
- `PUT /api/v1/suppliers/{id}`: Update vendor profile.
- `GET /api/v1/suppliers/{id}/aging`: Supplier AP aging schedule.

### Module 11: Expense Management
- `GET /api/v1/expenses/claims`: List operational expense claims.
- `POST /api/v1/expenses/claims`: Submit new expense claim with receipts.
- `POST /api/v1/expenses/claims/{id}/approve`: Supervisor approval and GL posting.
- `POST /api/v1/expenses/petty-cash/voucher`: Disburse petty cash payment.

### Module 12: Human Resources (HR)
- `GET /api/v1/hr/employees`: Employee directory.
- `POST /api/v1/hr/employees`: Onboard employee with departmental details.
- `POST /api/v1/hr/attendance/log`: Record or ingest biometric attendance logs.
- `POST /api/v1/hr/leaves/apply`: Submit employee leave request.

### Module 13: Payroll
- `GET /api/v1/payroll/structures`: Salary component templates.
- `POST /api/v1/payroll/runs/generate`: Compute monthly payrun for all active staff.
- `POST /api/v1/payroll/runs/{id}/approve`: Approve payrun and post payroll journal.
- `GET /api/v1/payroll/payslips/{id}`: Retrieve employee payslip PDF data.

### Module 14: Marketing & Growth
- `GET /api/v1/marketing/promotions`: Active promotional campaigns.
- `POST /api/v1/marketing/promotions`: Create volume discount or festive promotion.
- `POST /api/v1/marketing/coupons/validate`: Real-time coupon code validation during checkout.

### Module 15: Order Management
- `GET /api/v1/orders/fulfillment`: Pending order fulfillment staging.
- `POST /api/v1/orders/{id}/pick`: Generate pick list and reserve items.
- `POST /api/v1/orders/{id}/dispatch`: Dispatch shipment and generate delivery challan.

### Module 16: Service Management
- `GET /api/v1/services/job-cards`: Active workshop repair job cards.
- `POST /api/v1/services/job-cards`: Open repair job card with customer device details.
- `POST /api/v1/services/job-cards/{id}/consume-parts`: Issue spare parts to job.
- `POST /api/v1/services/job-cards/{id}/complete`: Close job card and generate invoice.

### Module 17: Reports & Analytics
- `POST /api/v1/reports/generate`: Request asynchronous or synchronous report generation.
- `GET /api/v1/reports/export/{format}`: Export report in `.xlsx`, `.pdf`, or `.csv`.

### Module 18: Multi-Branch & Terminal
- `GET /api/v1/branches`: List authorized physical branches.
- `POST /api/v1/branches`: Configure new store branch or warehouse.
- `POST /api/v1/terminals/register`: Register POS checkout terminal.

### Module 19: Administration & Security (OPEN-003)
- `POST /api/v1/auth/login`: Authenticate credentials, return JWT access & refresh tokens.
- `POST /api/v1/auth/refresh`: Rotate refresh token and issue new 15-minute access token.
- `POST /api/v1/auth/logout`: Revoke active refresh token and invalidate terminal session.
- `GET /api/v1/users`: User management list.
- `POST /api/v1/users`: Create user with assigned roles and branch permissions.
- `GET /api/v1/audit/logs`: Filtered tamper-evident system audit log query.

### Module 20: Automation & AI (Auto Stock Update)
- `POST /api/v1/automation/auto-stock/parse`: Extract structured line items from invoice text/JSON.
- `PUT /api/v1/automation/auto-stock/{id}`: Human verification & reconciliation grid edit.
- `POST /api/v1/automation/auto-stock/{id}/commit`: Atomic commit to PO, Inventory & GL.

### Module 21: System Platform Foundation (Live Production Endpoints)
- `GET /api/v1/system/status`: Platform health, PostgreSQL connectivity, memory, and outbox state.
- `POST /api/v1/system/backups/trigger`: On-demand PostgreSQL database backup.
- `POST /api/v1/system/backups/verify`: Checksum and integrity verification drill.

