# MAGIC ERP — PHASE F DATABASE MIGRATION REPORT
## Schemas, Tables, Constraints, and Execution Verification

**Project:** MAGIC — Universal Business Management ERP  
**Phase:** F — Extended Business Operations  
**Database:** PostgreSQL 18  
**Migration Name:** `20261113123956_AddPhaseFExtendedModules`  
**Execution Date:** 2026-11-13  
**Status:** MIGRATED & VERIFIED ON `magic_dev` & `magic_test`  

---

## 1. Migration Overview

Phase F introduces 6 distinct PostgreSQL database schemas, 24 relational tables, corresponding foreign keys, and performant B-tree indexes.

### Schemas Introduced
1. `expenses`: Expense claims, expense categories, petty cash accounts, and petty cash transaction audit logs.
2. `hr`: Departments, designations, employees, attendance records, leave types, leave balances, and leave applications.
3. `payroll`: Salary structures, salary structure components, employee salary assignments, payroll periods, and payroll item details.
4. `marketing`: Marketing campaigns, customer segments, promotion coupons, and coupon redemptions.
5. `orders`: Sales orders, sales order lines, order fulfillments, and fulfillment lines.
6. `services`: Service catalogs, service requests, service quotations, service quotation lines, service parts consumed, and service invoice links.

---

## 2. Table Catalog

| Schema | Table Name | Primary Key | Description |
| :--- | :--- | :--- | :--- |
| `expenses` | `expense_categories` | `id` (UUID) | Expense categorization rules & default GL account mappings |
| `expenses` | `expenses` | `id` (UUID) | Expense vouchers, claims, approval states, and GL vouchers |
| `expenses` | `petty_cash_accounts` | `id` (UUID) | Branch petty cash float accounts & custodian assignments |
| `expenses` | `petty_cash_transactions` | `id` (UUID) | Audit ledger of float allocations, replenishment & reconciliation |
| `hr` | `departments` | `id` (UUID) | Organizational departments |
| `hr` | `designations` | `id` (UUID) | Job titles and organizational hierarchy |
| `hr` | `employees` | `id` (UUID) | Employee master directory and HR profile data |
| `hr` | `attendance_records` | `id` (UUID) | Daily biometric clock-in/out records with supervisor audit |
| `hr` | `leave_types` | `id` (UUID) | Leave type configurations (CL, SL, EL) |
| `hr` | `leave_balances` | `id` (UUID) | Annual leave balances per fiscal year |
| `hr` | `leave_applications` | `id` (UUID) | Leave requests, date intervals, and manager approvals |
| `payroll` | `salary_structures` | `id` (UUID) | Master compensation package blueprints |
| `payroll` | `salary_structure_components` | `id` (UUID) | Earnings and deduction line components |
| `payroll` | `employee_salary_assignments` | `id` (UUID) | Active employee CTC and structure bindings |
| `payroll` | `payroll_periods` | `id` (UUID) | Monthly payroll processing runs & GL voucher links |
| `payroll` | `payroll_items` | `id` (UUID) | Per-employee payslip breakdowns with attendance adjustments |
| `marketing` | `campaigns` | `id` (UUID) | Growth and promotional campaigns |
| `marketing` | `customer_segments` | `id` (UUID) | Behavioral target audience definitions |
| `marketing` | `promotion_coupons` | `id` (UUID) | POS promo codes, discount basis points, and limits |
| `marketing` | `coupon_redemptions` | `id` (UUID) | POS coupon usage history linked to sales invoices |
| `orders` | `sales_orders` | `id` (UUID) | Customer sales orders and delivery schedules |
| `orders` | `sales_order_lines` | `id` (UUID) | Order items, prices, GST rates, and quantities |
| `orders` | `order_fulfillments` | `id` (UUID) | Shipment dispatch batches, carrier tracking info |
| `orders` | `order_fulfillment_lines` | `id` (UUID) | Dispatched item quantities against order lines |
| `services` | `service_catalogs` | `id` (UUID) | Service menu items, labor rates, and repair standard times |
| `services` | `service_requests` | `id` (UUID) | Bench service tickets and repair tracking |
| `services` | `service_quotations` | `id` (UUID) | Formal repair estimates sent to customers |
| `services` | `service_quotation_lines` | `id` (UUID) | Labor and spare parts line breakdown |
| `services` | `service_parts_consumed` | `id` (UUID) | Stock consumption records against inventory ledger |
| `services` | `service_invoice_links` | `id` (UUID) | Link between service ticket and final legal sales invoice |

---

## 3. Database Migration Execution Log

```text
Applying migration '20261113123956_AddPhaseFExtendedModules'.
Applying migration on database 'magic_dev'... Done in 2.14s.
Applying migration on database 'magic_test'... Done in 1.88s.

GRANT ALL ON SCHEMA expenses, hr, payroll, marketing, orders, services TO magic_dev, magic_app;
GRANT ALL ON ALL TABLES IN SCHEMA expenses, hr, payroll, marketing, orders, services TO magic_dev, magic_app;
GRANT ALL ON ALL SEQUENCES IN SCHEMA expenses, hr, payroll, marketing, orders, services TO magic_dev, magic_app;
GRANT Result: SUCCESS.
```

---

## 4. Verification & Integrity Confirmation

- All tables are configured with `ValueGeneratedNever()` on UUID primary keys to ensure deterministic Guid v7/v4 generation in application code.
- Foreign keys between Phase F tables and existing Phase A-E tables (`accounting.accounts`, `accounting.vouchers`, `accounting.fiscal_periods`, `catalog.products`, `commercial.customers`, `inventory.warehouses`, `sales.sales_invoices`) use `OnDelete(DeleteBehavior.Restrict)` to guarantee referential integrity and prevent accidental financial data loss.
- High-volume querying indexes:
  - `ix_attendance_emp_date`: `(employee_id, attendance_date)`
  - `ix_leave_balances_emp_fy`: `(employee_id, leave_type_id, fiscal_year_id)`
  - `ix_payroll_items_period_emp`: `(payroll_period_id, employee_id)`
  - `ix_coupon_redemptions_coupon_inv`: `(coupon_id, sales_invoice_id)`
  - `ix_service_invoice_links_req_inv`: `(service_request_id, sales_invoice_id)`

