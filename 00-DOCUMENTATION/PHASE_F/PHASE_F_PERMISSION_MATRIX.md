# MAGIC ERP — PHASE F PERMISSION MATRIX
## Role-Based Access Control (RBAC) & Security Specifications

**Project:** MAGIC — Universal Business Management ERP  
**Phase:** F — Extended Business Modules  
**Security Standard:** Fail-Closed, Tenant/Branch Isolation, Cryptographic JWT Validation

---

## 1. Granular Permission Definitions

The following permissions extend MAGIC's security schema to govern all Phase F business operations:

| Permission Code | Module | Display Name | Functional Scope |
|---|---|---|---|
| `expenses:view` | Expenses | View Expense Registry | View expense records, categories, and payment statuses |
| `expenses:create` | Expenses | Record Operational Expenses | Create draft and submitted expense vouchers |
| `expenses:approve` | Expenses | Authorize Expenses | Approve or reject pending operational expenses |
| `expenses:pay` | Expenses | Disburse Expense Payments | Mark approved expenses as paid and post accounting vouchers |
| `expenses:pettycash:manage` | Expenses | Manage Petty Cash | Manage petty cash accounts, allocations, replenishments & reconciliations |
| `hr:employees:view` | HR | View Employee Directory | View staff roster, designations, and professional contact info |
| `hr:employees:manage` | HR | Manage Employee Records | Register, update, and manage employee profiles and lifecycle |
| `hr:attendance:record` | HR | Record Attendance | Daily punch-in/out, employee presence, and work hours |
| `hr:attendance:correct` | HR | Authorize Attendance Corrections | Adjust attendance records with mandatory supervisor audit notes |
| `hr:leave:apply` | HR | Submit Leave Applications | Self-service leave requests against available leave quotas |
| `hr:leave:approve` | HR | Approve Leave Applications | Supervisor authorization or rejection of employee leave |
| `payroll:structures:manage`| Payroll | Manage Salary Structures | Configure basic, allowances, and statutory deduction rules |
| `payroll:process` | Payroll | Calculate & Preview Payroll | Generate monthly salary calculations based on attendance and leave |
| `payroll:approve` | Payroll | Authorize Payroll Run | Approve monthly payroll periods and lock calculations |
| `payroll:finalize` | Payroll | Finalize & Post Accruals | Finalize payroll and post accounting vouchers to General Ledger |
| `payroll:disburse` | Payroll | Disburse Salaries | Record bank transfer references and post disbursement vouchers |
| `payroll:payslip:view` | Payroll | View Payslips | Access individual employee salary slips (Self-Service or HR) |
| `marketing:segments:manage`| Marketing| Manage Customer Segments | Define dynamic and static customer purchasing segments |
| `marketing:campaigns:manage`| Marketing| Manage Campaigns & Offers | Create promotional campaigns, budget limits, and expiry dates |
| `marketing:coupons:manage` | Marketing| Issue Discount Coupons | Configure coupon codes, usage limits, and discount thresholds |
| `marketing:loyalty:manage` | Marketing| Configure Loyalty Programs | Set points accrual rates, redemption rules, and balance adjustments |
| `orders:view` | Orders | View Sales Orders | View customer orders, backlog, and fulfillment status |
| `orders:create` | Orders | Create Sales Orders | Enter new sales orders with pricing, discounts, and addresses |
| `orders:approve` | Orders | Confirm Sales Orders | Authorize orders, commit stock, and trigger fulfillment |
| `orders:fulfill` | Orders | Dispatch & Fulfill Orders | Prepare shipments, pick items, record dispatches, and track tracking IDs |
| `orders:cancel` | Orders | Cancel Orders & Backorders | Cancel open orders and backorders with inventory restoration |
| `service:catalog:manage` | Services | Manage Service Catalog | Define repair services, standard labor rates, and estimated times |
| `service:requests:view` | Services | View Service Desk | View service tickets, status, technician assignments, and timeline |
| `service:requests:manage` | Services | Manage Service Requests | Create, inspect, update status, and assign technician to jobs |
| `service:quotations:manage`| Services | Authorize Quotations | Draft and approve service quotes for labor and replacement parts |
| `service:parts:consume` | Services | Record Parts Consumption | Consume physical inventory parts from warehouse for repair work |
| `service:billing:create` | Services | Convert Service to Invoice | Generate final sales tax invoices for completed service work |

---

## 2. Standard Role Mappings

| Role Name | Default Assigned Phase F Permissions |
|---|---|
| **System Administrator** | All Phase F permissions (Company & Branch wide) |
| **Branch Manager** | `expenses:*`, `hr:*`, `payroll:approve`, `marketing:*`, `orders:*`, `service:*` |
| **Accounts Manager** | `expenses:view`, `expenses:approve`, `expenses:pay`, `expenses:pettycash:manage`, `payroll:*`, `orders:view`, `service:billing:create` |
| **HR / Payroll Officer** | `hr:employees:*`, `hr:attendance:*`, `hr:leave:*`, `payroll:structures:manage`, `payroll:process`, `payroll:approve`, `payroll:payslip:view` |
| **Cashier / POS Operator** | `marketing:coupons:manage` (redemption verification only), `orders:create`, `service:requests:view` |
| **Store / Warehouse Keeper** | `orders:fulfill`, `service:parts:consume` |
| **Service Technician** | `service:requests:view`, `service:requests:manage`, `service:parts:consume` |
| **Standard Employee (ESS)** | `hr:leave:apply`, `payroll:payslip:view`, `hr:attendance:record` |

