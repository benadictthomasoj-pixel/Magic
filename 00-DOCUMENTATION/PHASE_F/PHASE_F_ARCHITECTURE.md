# MAGIC ERP — PHASE F ARCHITECTURE SPECIFICATION
## Enterprise Business Modules: Expenses, HR, Payroll, Marketing, Orders & Services

**Project:** MAGIC — Universal Business Management ERP  
**Phase:** F — Extended Business Operations  
**Architecture Style:** Modular Monolith / Clean Architecture / Domain-Driven Design (DDD)  
**Date:** 2026-11-13  
**Status:** Approved for Implementation (Stage F1)

---

## 1. Architectural Principles & Boundaries

MAGIC ERP adheres strictly to a **Modular Monolith** architecture with clean domain separation:

```
+-----------------------------------------------------------------------------------+
|                                  ANGULAR 22 FRONTEND                              |
|   /billing/expenses | /billing/hr | /billing/payroll | /billing/marketing         |
|                     /billing/orders | /billing/service                            |
+-----------------------------------------------------------------------------------+
                                        | (REST APIs with JWT & Tenant Context)
+-----------------------------------------------------------------------------------+
|                              ASP.NET CORE 10 WEB API                              |
|  ExpensesController | HrController | PayrollController | MarketingController      |
|  OrdersController   | ServiceController                                           |
+-----------------------------------------------------------------------------------+
                                        | (Contracts & DTOs)
+-----------------------------------------------------------------------------------+
|                                APPLICATION LAYER                                  |
|  IExpenseService    | IHrService   | IPayrollService   | IMarketingService        |
|  IOrderService      | IServiceDeskService                                         |
+-----------------------------------------------------------------------------------+
               |                               |                      |
               v                               v                      v
+-----------------------------+ +-----------------------------+ +-------------------+
|     DOMAIN SUB-SYSTEMS      | |    INTEGRATION SERVICES     | | PLATFORM SERVICES |
| - Expense & Petty Cash      | | - Existing PostingService   | | - MagicDbContext  |
| - HR & Employee Lifecycle   | | - Existing InventoryService | | - Outbox Worker   |
| - Payroll & Deductions      | | - Existing SalesService     | | - Security Audit  |
| - Marketing & Loyalty       | | - Existing TreasuryService  | | - Idempotency     |
| - Sales Order Lifecycle     | | - Existing AR/AP Subledger  | | - Money (Paise)   |
| - Service Desk & Workflows  | +-----------------------------+ +-------------------+
+-----------------------------+
```

### Invariant Preservation
1. **Single General Ledger:** All financial movements (expenses, petty cash, payroll accruals, salary disbursements) integrate directly with the existing `PostingService` and `accounting.*` schema. No competing GL is created.
2. **Single Stock Ledger:** Order fulfillment and service repair parts consumption interact directly with `inventory.stock_balances` and `inventory.stock_movements`. No double-counting or secondary stock ledgers.
3. **Paise Precision:** All monetary amounts are stored as signed 64-bit integers (`long` / `int64`) in paise. Floating-point currency is strictly forbidden.
4. **Tenant Isolation:** Every table carries `CompanyId` and `BranchId` foreign keys where applicable. Queries strictly filter by tenant context.
5. **Fail-Closed Security:** Endpoints enforce granular permission checks. Multi-branch access is governed server-side.
6. **Immutability & Auditability:** Posted accounting vouchers and closed periods are immutable; corrections execute via formal counter-reversals.

---

## 2. Module Boundary Specifications

### Module 11: Expense Management (`/billing/expenses`)
- **Domain:** Direct operational expenses, recurring bills, petty cash floats, and multi-tier approval.
- **Integrations:**
  - `PostingService`: Posts debit to Expense Account (Class 5) and credit to Cash/Bank/Payable (Class 1 or 2).
  - `TreasuryService`: Petty cash replenishments link to bank/cash accounts.
  - `Suppliers`: Optional association with registered commercial suppliers for recurring vendor expenses.

### Module 12: Human Resource Management (`/billing/hr`)
- **Domain:** Employee master directory, departments, designations, daily attendance tracking, leave applications, and employee lifecycle.
- **Integrations:**
  - `Security`: Employees can be mapped to platform `UserRecord` accounts for Employee Self-Service (ESS).
  - `Branches`: Strict branch allocation and cross-branch transfer tracking.

### Module 13: Payroll Management (`/billing/payroll`)
- **Domain:** Salary structure definitions, monthly attendance/leave-adjusted payroll calculation, approvals, salary slips, and disbursement reconciliation.
- **Integrations:**
  - `HR Module`: Consumes attendance and approved leave data for calendar day calculations.
  - `PostingService`: Accrues gross salaries and statutory deductions into `Salary Payable` (2130) and statutory liability accounts.
  - `TreasuryService`: Records bank disbursement vouchers debiting `Salary Payable` and crediting bank accounts.

### Module 14: Marketing & Customer Growth (`/billing/marketing`)
- **Domain:** Customer segmentation, campaign tracking, discount coupons, and loyalty points rewards.
- **Integrations:**
  - `Commercial (Customers)`: Queries customer spending and lifetime transaction history.
  - `POS & Sales Billing`: Evaluates coupon eligibility and point redemption without altering baseline tax/pricing invariants.

### Module 15: Advanced Order Management (`/billing/orders`)
- **Domain:** Sales orders, backorders, warehouse allocation, delivery dispatches, and sales quotation conversions.
- **Integrations:**
  - `Commercial (Products & Customers)`: Catalog pricing, customer credit limits, and addresses.
  - `Inventory`: Validates stock availability; generates delivery dispatches.
  - `Sales & Invoicing`: Invoices sales orders via existing `SalesService`, ensuring inventory is decremented exactly once upon invoicing/dispatch.

### Module 16: Service Management (`/billing/service`)
- **Domain:** Repair tickets, maintenance requests, labor catalog, quotations, warranty tracking, and parts consumption.
- **Integrations:**
  - `Inventory`: Tracks spare parts consumption directly via `StockMovements`.
  - `HR`: Assigns service jobs to qualified technicians/employees.
  - `Sales & Invoicing`: Converts approved completed service work (labor charges + parts) into sales tax invoices.

---

## 3. Technology Alignment & Compatibility
- **Backend Framework:** .NET 10.0 / ASP.NET Core Web API / EF Core 10.
- **Database Engine:** PostgreSQL 18.x with UUID primary keys and B-Tree indexes.
- **Frontend Framework:** Angular 22 (Standalone Components, Signals, Reactive Forms, CDK).
- **Design System:** MAGIC Enterprise Navy (`#0b192c`, `#1e3e62`), Pure Gold (`#e0a96d`), Crisp White (`#ffffff`), and Neutral Surface Gray (`#f8fafc`).

