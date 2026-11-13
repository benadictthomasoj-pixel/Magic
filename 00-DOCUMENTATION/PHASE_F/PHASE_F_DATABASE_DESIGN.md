# MAGIC ERP — PHASE F DATABASE DESIGN
## PostgreSQL 18 Schema & Relational Specifications

**Project:** MAGIC — Universal Business Management ERP  
**Phase:** F — Extended Business Modules  
**Database Engine:** PostgreSQL 18  
**ORM:** Entity Framework Core 10  
**Storage Convention:** Signed 64-bit integer paise (`bigint`) for currency, UUID primary keys.

---

## 1. Database Schema Organization

Phase F organizes its persistent records into six distinct, modular PostgreSQL schemas:

| Schema Name | Target Module | Domain Entity Group |
|---|---|---|
| `expenses` | Module 11 | Expense Categories, Expense Records, Petty Cash Accounts, Petty Cash Transactions |
| `hr` | Module 12 | Designations, Employees, Attendance, Leave Types, Leave Balances, Leave Applications, Lifecycle Events |
| `payroll` | Module 13 | Salary Structures, Salary Components, Employee Salary Assignments, Payroll Periods, Payroll Items |
| `marketing` | Module 14 | Customer Segments, Marketing Campaigns, Promotion Coupons, Coupon Redemptions, Loyalty Programs, Loyalty Transactions |
| `orders` | Module 15 | Sales Orders, Sales Order Lines, Order Fulfillments, Fulfillment Lines, Backorders |
| `services` | Module 16 | Service Catalog, Service Requests, Service Quotations, Quotation Lines, Part Consumptions, Service Invoice Links |

---

## 2. Table Schemas & Relational Specifications

### 2.1 Schema: `expenses`

#### `expenses.expense_categories`
- `id` (UUID, PK)
- `company_id` (UUID, FK -> `platform.companies`)
- `code` (VARCHAR(30), Unique per company)
- `name` (VARCHAR(100), NOT NULL)
- `default_gl_account_id` (UUID, FK -> `accounting.accounts`, NOT NULL)
- `description` (TEXT)
- `is_active` (BOOLEAN, DEFAULT TRUE)
- `created_at_utc` (TIMESTAMPTZ)

#### `expenses.expenses`
- `id` (UUID, PK)
- `company_id` (UUID, FK -> `platform.companies`, NOT NULL)
- `branch_id` (UUID, FK -> `platform.branches`, NOT NULL)
- `expense_number` (VARCHAR(40), UNIQUE, NOT NULL)
- `expense_category_id` (UUID, FK -> `expenses.expense_categories`, NOT NULL)
- `gl_account_id` (UUID, FK -> `accounting.accounts`, NOT NULL)
- `vendor_id` (UUID, FK -> `commercial.suppliers`, NULLABLE)
- `payment_method` (VARCHAR(20), NOT NULL: `Cash`, `Bank`, `PettyCash`, `Payable`)
- `bank_cash_account_id` (UUID, FK -> `finance.bank_cash_accounts`, NULLABLE)
- `expense_date` (DATE, NOT NULL)
- `amount_paise` (BIGINT, NOT NULL)
- `tax_amount_paise` (BIGINT, NOT NULL, DEFAULT 0)
- `total_amount_paise` (BIGINT, NOT NULL)
- `cost_center` (VARCHAR(50), NULLABLE)
- `reference_number` (VARCHAR(100), NULLABLE)
- `description` (TEXT, NOT NULL)
- `status` (VARCHAR(20), NOT NULL: `Draft`, `Submitted`, `UnderReview`, `Approved`, `Rejected`, `Paid`, `Cancelled`)
- `approved_by_user_id` (UUID, FK -> `security.users`, NULLABLE)
- `approved_at_utc` (TIMESTAMPTZ, NULLABLE)
- `journal_voucher_id` (UUID, FK -> `accounting.journal_vouchers`, NULLABLE)
- `reversal_voucher_id` (UUID, FK -> `accounting.journal_vouchers`, NULLABLE)
- `attachment_path` (TEXT, NULLABLE)
- `created_at_utc` (TIMESTAMPTZ, DEFAULT NOW())

#### `expenses.petty_cash_accounts`
- `id` (UUID, PK)
- `company_id` (UUID, FK -> `platform.companies`, NOT NULL)
- `branch_id` (UUID, FK -> `platform.branches`, NOT NULL)
- `account_name` (VARCHAR(100), NOT NULL)
- `gl_account_id` (UUID, FK -> `accounting.accounts`, NOT NULL)
- `custodian_user_id` (UUID, FK -> `security.users`, NOT NULL)
- `float_limit_paise` (BIGINT, NOT NULL)
- `current_balance_paise` (BIGINT, NOT NULL)
- `is_active` (BOOLEAN, DEFAULT TRUE)
- `created_at_utc` (TIMESTAMPTZ, DEFAULT NOW())

#### `expenses.petty_cash_transactions`
- `id` (UUID, PK)
- `petty_cash_account_id` (UUID, FK -> `expenses.petty_cash_accounts`, NOT NULL)
- `transaction_type` (VARCHAR(20), NOT NULL: `Allocation`, `Expense`, `Replenishment`, `Shortage`, `Excess`)
- `amount_paise` (BIGINT, NOT NULL)
- `balance_after_paise` (BIGINT, NOT NULL)
- `expense_id` (UUID, FK -> `expenses.expenses`, NULLABLE)
- `journal_voucher_id` (UUID, FK -> `accounting.journal_vouchers`, NULLABLE)
- `performed_by_user_id` (UUID, FK -> `security.users`, NOT NULL)
- `notes` (TEXT)
- `transaction_date_utc` (TIMESTAMPTZ, DEFAULT NOW())

---

### 2.2 Schema: `hr`

#### `hr.designations`
- `id` (UUID, PK)
- `company_id` (UUID, FK -> `platform.companies`, NOT NULL)
- `code` (VARCHAR(30), NOT NULL)
- `title` (VARCHAR(100), NOT NULL)
- `department_id` (UUID, FK -> `commercial.departments`, NULLABLE)
- `description` (TEXT)
- `is_active` (BOOLEAN, DEFAULT TRUE)

#### `hr.employees`
- `id` (UUID, PK)
- `company_id` (UUID, FK -> `platform.companies`, NOT NULL)
- `branch_id` (UUID, FK -> `platform.branches`, NOT NULL)
- `employee_code` (VARCHAR(30), UNIQUE, NOT NULL)
- `first_name` (VARCHAR(60), NOT NULL)
- `last_name` (VARCHAR(60), NOT NULL)
- `email` (VARCHAR(120), NOT NULL)
- `phone` (VARCHAR(30), NOT NULL)
- `address` (TEXT)
- `joining_date` (DATE, NOT NULL)
- `employment_type` (VARCHAR(30), NOT NULL: `FullTime`, `PartTime`, `Contract`, `Probation`, `Intern`)
- `department_id` (UUID, FK -> `commercial.departments`, NULLABLE)
- `designation_id` (UUID, FK -> `hr.designations`, NULLABLE)
- `reporting_manager_id` (UUID, FK -> `hr.employees`, NULLABLE)
- `status` (VARCHAR(20), NOT NULL: `Active`, `OnLeave`, `Resigned`, `Terminated`, `Deactivated`)
- `bank_account_number` (VARCHAR(50), NULLABLE)
- `bank_ifsc` (VARCHAR(20), NULLABLE)
- `bank_name` (VARCHAR(100), NULLABLE)
- `pan_number` (VARCHAR(20), NULLABLE)
- `linked_user_id` (UUID, FK -> `security.users`, NULLABLE)
- `created_at_utc` (TIMESTAMPTZ, DEFAULT NOW())

#### `hr.attendance`
- `id` (UUID, PK)
- `employee_id` (UUID, FK -> `hr.employees`, NOT NULL)
- `company_id` (UUID, FK -> `platform.companies`, NOT NULL)
- `branch_id` (UUID, FK -> `platform.branches`, NOT NULL)
- `attendance_date` (DATE, NOT NULL)
- `check_in_time_utc` (TIMESTAMPTZ, NULLABLE)
- `check_out_time_utc` (TIMESTAMPTZ, NULLABLE)
- `status` (VARCHAR(20), NOT NULL: `Present`, `Absent`, `HalfDay`, `OnLeave`, `Holiday`)
- `work_duration_minutes` (INT, DEFAULT 0)
- `is_corrected` (BOOLEAN, DEFAULT FALSE)
- `correction_notes` (TEXT)
- `corrected_by_user_id` (UUID, FK -> `security.users`, NULLABLE)

#### `hr.leave_types`
- `id` (UUID, PK)
- `company_id` (UUID, FK -> `platform.companies`, NOT NULL)
- `code` (VARCHAR(20), NOT NULL)
- `name` (VARCHAR(60), NOT NULL)
- `annual_days` (INT, NOT NULL)
- `is_paid` (BOOLEAN, DEFAULT TRUE)
- `is_active` (BOOLEAN, DEFAULT TRUE)

#### `hr.leave_balances`
- `id` (UUID, PK)
- `employee_id` (UUID, FK -> `hr.employees`, NOT NULL)
- `leave_type_id` (UUID, FK -> `hr.leave_types`, NOT NULL)
- `fiscal_year_id` (UUID, FK -> `accounting.fiscal_years`, NOT NULL)
- `total_days` (DECIMAL(5,1), NOT NULL)
- `consumed_days` (DECIMAL(5,1), DEFAULT 0)
- `pending_days` (DECIMAL(5,1), DEFAULT 0)

#### `hr.leave_applications`
- `id` (UUID, PK)
- `application_number` (VARCHAR(40), UNIQUE, NOT NULL)
- `employee_id` (UUID, FK -> `hr.employees`, NOT NULL)
- `leave_type_id` (UUID, FK -> `hr.leave_types`, NOT NULL)
- `start_date` (DATE, NOT NULL)
- `end_date` (DATE, NOT NULL)
- `total_days` (DECIMAL(5,1), NOT NULL)
- `reason` (TEXT, NOT NULL)
- `status` (VARCHAR(20), NOT NULL: `Applied`, `Approved`, `Rejected`, `Cancelled`)
- `decision_by_user_id` (UUID, FK -> `security.users`, NULLABLE)
- `decision_at_utc` (TIMESTAMPTZ, NULLABLE)
- `rejection_reason` (TEXT)

---

### 2.3 Schema: `payroll`

#### `payroll.salary_structures`
- `id` (UUID, PK)
- `company_id` (UUID, FK -> `platform.companies`, NOT NULL)
- `name` (VARCHAR(100), NOT NULL)
- `description` (TEXT)
- `is_active` (BOOLEAN, DEFAULT TRUE)

#### `payroll.salary_structure_components`
- `id` (UUID, PK)
- `salary_structure_id` (UUID, FK -> `payroll.salary_structures`, NOT NULL)
- `name` (VARCHAR(60), NOT NULL)
- `component_type` (VARCHAR(30), NOT NULL: `Basic`, `HRA`, `Allowance`, `Bonus`, `PF_Employee`, `ESI_Employee`, `PT`, `TDS`, `PF_Employer`, `ESI_Employer`)
- `calculation_type` (VARCHAR(20), NOT NULL: `Fixed`, `PercentageOfBasic`)
- `default_value` (DECIMAL(10,2), NOT NULL)
- `is_deduction` (BOOLEAN, NOT NULL)
- `is_employer_contribution` (BOOLEAN, NOT NULL)

#### `payroll.employee_salary_assignments`
- `id` (UUID, PK)
- `employee_id` (UUID, FK -> `hr.employees`, NOT NULL)
- `salary_structure_id` (UUID, FK -> `payroll.salary_structures`, NOT NULL)
- `monthly_gross_paise` (BIGINT, NOT NULL)
- `effective_from` (DATE, NOT NULL)
- `is_active` (BOOLEAN, DEFAULT TRUE)

#### `payroll.payroll_periods`
- `id` (UUID, PK)
- `company_id` (UUID, FK -> `platform.companies`, NOT NULL)
- `branch_id` (UUID, FK -> `platform.branches`, NOT NULL)
- `fiscal_year_id` (UUID, FK -> `accounting.fiscal_years`, NOT NULL)
- `period_name` (VARCHAR(50), NOT NULL)
- `month` (INT, NOT NULL)
- `year` (INT, NOT NULL)
- `start_date` (DATE, NOT NULL)
- `end_date` (DATE, NOT NULL)
- `status` (VARCHAR(20), NOT NULL: `Draft`, `Calculated`, `Approved`, `Finalized`, `Paid`, `Cancelled`)
- `total_gross_paise` (BIGINT, NOT NULL, DEFAULT 0)
- `total_deductions_paise` (BIGINT, NOT NULL, DEFAULT 0)
- `total_net_paise` (BIGINT, NOT NULL, DEFAULT 0)
- `total_employer_contributions_paise` (BIGINT, NOT NULL, DEFAULT 0)
- `journal_voucher_id` (UUID, FK -> `accounting.journal_vouchers`, NULLABLE)
- `disbursement_voucher_id` (UUID, FK -> `accounting.journal_vouchers`, NULLABLE)
- `processed_by_user_id` (UUID, FK -> `security.users`, NOT NULL)
- `approved_by_user_id` (UUID, FK -> `security.users`, NULLABLE)
- `finalized_at_utc` (TIMESTAMPTZ, NULLABLE)

#### `payroll.payroll_items`
- `id` (UUID, PK)
- `payroll_period_id` (UUID, FK -> `payroll.payroll_periods`, NOT NULL)
- `employee_id` (UUID, FK -> `hr.employees`, NOT NULL)
- `present_days` (DECIMAL(5,1), NOT NULL)
- `paid_leave_days` (DECIMAL(5,1), NOT NULL)
- `unpaid_leave_days` (DECIMAL(5,1), NOT NULL)
- `basic_paise` (BIGINT, NOT NULL)
- `hra_paise` (BIGINT, NOT NULL)
- `allowances_paise` (BIGINT, NOT NULL)
- `gross_salary_paise` (BIGINT, NOT NULL)
- `pf_deduction_paise` (BIGINT, NOT NULL)
- `esi_deduction_paise` (BIGINT, NOT NULL)
- `pt_deduction_paise` (BIGINT, NOT NULL)
- `tds_deduction_paise` (BIGINT, NOT NULL)
- `total_deductions_paise` (BIGINT, NOT NULL)
- `net_salary_paise` (BIGINT, NOT NULL)
- `employer_pf_paise` (BIGINT, NOT NULL)
- `employer_esi_paise` (BIGINT, NOT NULL)
- `payment_status` (VARCHAR(20), NOT NULL: `Pending`, `Paid`)
- `payment_reference` (VARCHAR(100), NULLABLE)

---

### 2.4 Schema: `marketing`

#### `marketing.customer_segments`
- `id` (UUID, PK)
- `company_id` (UUID, FK -> `platform.companies`, NOT NULL)
- `name` (VARCHAR(100), NOT NULL)
- `segment_type` (VARCHAR(30), NOT NULL: `VIP`, `Frequent`, `Inactive`, `HighSpender`, `NewCustomers`)
- `description` (TEXT)
- `min_lifetime_spend_paise` (BIGINT, DEFAULT 0)
- `min_orders_count` (INT, DEFAULT 0)
- `is_active` (BOOLEAN, DEFAULT TRUE)

#### `marketing.campaigns`
- `id` (UUID, PK)
- `company_id` (UUID, FK -> `platform.companies`, NOT NULL)
- `name` (VARCHAR(120), NOT NULL)
- `description` (TEXT)
- `target_segment_id` (UUID, FK -> `marketing.customer_segments`, NULLABLE)
- `start_date` (DATE, NOT NULL)
- `end_date` (DATE, NOT NULL)
- `budget_paise` (BIGINT, NOT NULL)
- `actual_spend_paise` (BIGINT, DEFAULT 0)
- `attributed_revenue_paise` (BIGINT, DEFAULT 0)
- `status` (VARCHAR(20), NOT NULL: `Draft`, `Active`, `Paused`, `Completed`, `Cancelled`)

#### `marketing.promotion_coupons`
- `id` (UUID, PK)
- `company_id` (UUID, FK -> `platform.companies`, NOT NULL)
- `code` (VARCHAR(30), NOT NULL)
- `campaign_id` (UUID, FK -> `marketing.campaigns`, NULLABLE)
- `discount_type` (VARCHAR(20), NOT NULL: `Percentage`, `FixedAmount`)
- `discount_value` (BIGINT, NOT NULL) -- bps for percentage, paise for fixed amount
- `min_order_amount_paise` (BIGINT, DEFAULT 0)
- `max_discount_amount_paise` (BIGINT, DEFAULT 0)
- `valid_from` (TIMESTAMPTZ, NOT NULL)
- `valid_to` (TIMESTAMPTZ, NOT NULL)
- `usage_limit_total` (INT, NOT NULL, DEFAULT 1000)
- `usage_limit_per_customer` (INT, NOT NULL, DEFAULT 1)
- `current_usage_count` (INT, NOT NULL, DEFAULT 0)
- `is_active` (BOOLEAN, DEFAULT TRUE)

#### `marketing.coupon_redemptions`
- `id` (UUID, PK)
- `coupon_id` (UUID, FK -> `marketing.promotion_coupons`, NOT NULL)
- `customer_id` (UUID, FK -> `commercial.customers`, NOT NULL)
- `sales_invoice_id` (UUID, FK -> `pos.sales_invoices`, NOT NULL)
- `discount_amount_paise` (BIGINT, NOT NULL)
- `redeemed_at_utc` (TIMESTAMPTZ, DEFAULT NOW())

#### `marketing.loyalty_programs`
- `id` (UUID, PK)
- `company_id` (UUID, FK -> `platform.companies`, NOT NULL)
- `name` (VARCHAR(100), NOT NULL)
- `points_per_hundred_spent` (INT, NOT NULL, DEFAULT 1)
- `currency_value_per_point_paise` (BIGINT, NOT NULL, DEFAULT 100) -- e.g. 1 point = 100 paise (1 INR)
- `min_points_to_redeem` (INT, NOT NULL, DEFAULT 100)
- `is_active` (BOOLEAN, DEFAULT TRUE)

#### `marketing.loyalty_transactions`
- `id` (UUID, PK)
- `customer_id` (UUID, FK -> `commercial.customers`, NOT NULL)
- `company_id` (UUID, FK -> `platform.companies`, NOT NULL)
- `transaction_type` (VARCHAR(20), NOT NULL: `Earned`, `Redeemed`, `Adjusted`)
- `points` (INT, NOT NULL)
- `balance_after_points` (INT, NOT NULL)
- `sales_invoice_id` (UUID, FK -> `pos.sales_invoices`, NULLABLE)
- `notes` (TEXT)
- `created_at_utc` (TIMESTAMPTZ, DEFAULT NOW())

---

### 2.5 Schema: `orders`

#### `orders.sales_orders`
- `id` (UUID, PK)
- `company_id` (UUID, FK -> `platform.companies`, NOT NULL)
- `branch_id` (UUID, FK -> `platform.branches`, NOT NULL)
- `warehouse_id` (UUID, FK -> `inventory.warehouses`, NOT NULL)
- `order_number` (VARCHAR(40), UNIQUE, NOT NULL)
- `customer_id` (UUID, FK -> `commercial.customers`, NOT NULL)
- `order_date` (DATE, NOT NULL)
- `expected_delivery_date` (DATE, NULLABLE)
- `subtotal_paise` (BIGINT, NOT NULL)
- `tax_amount_paise` (BIGINT, NOT NULL)
- `total_amount_paise` (BIGINT, NOT NULL)
- `delivery_address` (TEXT, NOT NULL)
- `notes` (TEXT)
- `status` (VARCHAR(25), NOT NULL: `Draft`, `Submitted`, `Confirmed`, `PartiallyFulfilled`, `Fulfilled`, `Invoiced`, `Cancelled`)
- `confirmed_by_user_id` (UUID, FK -> `security.users`, NULLABLE)
- `confirmed_at_utc` (TIMESTAMPTZ, NULLABLE)
- `created_at_utc` (TIMESTAMPTZ, DEFAULT NOW())

#### `orders.sales_order_lines`
- `id` (UUID, PK)
- `sales_order_id` (UUID, FK -> `orders.sales_orders`, NOT NULL)
- `product_id` (UUID, FK -> `commercial.products`, NOT NULL)
- `variant_id` (UUID, FK -> `commercial.product_variants`, NULLABLE)
- `quantity` (DECIMAL(12,3), NOT NULL)
- `fulfilled_quantity` (DECIMAL(12,3), NOT NULL, DEFAULT 0)
- `invoiced_quantity` (DECIMAL(12,3), NOT NULL, DEFAULT 0)
- `unit_price_paise` (BIGINT, NOT NULL)
- `discount_amount_paise` (BIGINT, NOT NULL, DEFAULT 0)
- `tax_rate_percent` (DECIMAL(5,2), NOT NULL)
- `tax_amount_paise` (BIGINT, NOT NULL)
- `line_total_paise` (BIGINT, NOT NULL)
- `status` (VARCHAR(20), NOT NULL: `Pending`, `PartiallyFulfilled`, `Fulfilled`, `Cancelled`)

#### `orders.order_fulfillments`
- `id` (UUID, PK)
- `sales_order_id` (UUID, FK -> `orders.sales_orders`, NOT NULL)
- `fulfillment_number` (VARCHAR(40), UNIQUE, NOT NULL)
- `warehouse_id` (UUID, FK -> `inventory.warehouses`, NOT NULL)
- `carrier_name` (VARCHAR(100), NULLABLE)
- `tracking_number` (VARCHAR(100), NULLABLE)
- `dispatched_at_utc` (TIMESTAMPTZ, NULLABLE)
- `delivered_at_utc` (TIMESTAMPTZ, NULLABLE)
- `status` (VARCHAR(20), NOT NULL: `Preparing`, `Dispatched`, `Delivered`, `Failed`)

#### `orders.order_fulfillment_lines`
- `id` (UUID, PK)
- `order_fulfillment_id` (UUID, FK -> `orders.order_fulfillments`, NOT NULL)
- `sales_order_line_id` (UUID, FK -> `orders.sales_order_lines`, NOT NULL)
- `quantity_dispatched` (DECIMAL(12,3), NOT NULL)

#### `orders.backorders`
- `id` (UUID, PK)
- `sales_order_id` (UUID, FK -> `orders.sales_orders`, NOT NULL)
- `sales_order_line_id` (UUID, FK -> `orders.sales_order_lines`, NOT NULL)
- `product_id` (UUID, FK -> `commercial.products`, NOT NULL)
- `pending_quantity` (DECIMAL(12,3), NOT NULL)
- `status` (VARCHAR(20), NOT NULL: `Pending`, `Allocated`, `Fulfilled`, `Cancelled`)
- `expected_restock_date` (DATE, NULLABLE)

---

### 2.6 Schema: `services`

#### `services.service_catalog`
- `id` (UUID, PK)
- `company_id` (UUID, FK -> `platform.companies`, NOT NULL)
- `code` (VARCHAR(30), NOT NULL)
- `name` (VARCHAR(120), NOT NULL)
- `category` (VARCHAR(60), NOT NULL)
- `description` (TEXT)
- `standard_labor_charge_paise` (BIGINT, NOT NULL)
- `estimated_duration_minutes` (INT, NOT NULL, DEFAULT 60)
- `is_active` (BOOLEAN, DEFAULT TRUE)

#### `services.service_requests`
- `id` (UUID, PK)
- `company_id` (UUID, FK -> `platform.companies`, NOT NULL)
- `branch_id` (UUID, FK -> `platform.branches`, NOT NULL)
- `request_number` (VARCHAR(40), UNIQUE, NOT NULL)
- `customer_id` (UUID, FK -> `commercial.customers`, NOT NULL)
- `service_catalog_id` (UUID, FK -> `services.service_catalog`, NOT NULL)
- `item_description` (VARCHAR(150), NOT NULL)
- `serial_number` (VARCHAR(100), NULLABLE)
- `reported_issue` (TEXT, NOT NULL)
- `priority` (VARCHAR(20), NOT NULL: `Low`, `Medium`, `High`, `Urgent`)
- `status` (VARCHAR(25), NOT NULL: `NewRequest`, `Inspection`, `Quotation`, `Approved`, `InProgress`, `WaitingForParts`, `Completed`, `Delivered`, `Cancelled`)
- `assigned_employee_id` (UUID, FK -> `hr.employees`, NULLABLE)
- `received_date` (DATE, NOT NULL)
- `target_completion_date` (DATE, NULLABLE)
- `completed_at_utc` (TIMESTAMPTZ, NULLABLE)
- `delivered_at_utc` (TIMESTAMPTZ, NULLABLE)
- `technician_notes` (TEXT)

#### `services.service_quotations`
- `id` (UUID, PK)
- `service_request_id` (UUID, FK -> `services.service_requests`, NOT NULL)
- `quotation_number` (VARCHAR(40), UNIQUE, NOT NULL)
- `labor_total_paise` (BIGINT, NOT NULL)
- `parts_total_paise` (BIGINT, NOT NULL)
- `discount_paise` (BIGINT, NOT NULL, DEFAULT 0)
- `tax_amount_paise` (BIGINT, NOT NULL)
- `total_amount_paise` (BIGINT, NOT NULL)
- `valid_until` (DATE, NOT NULL)
- `status` (VARCHAR(20), NOT NULL: `Draft`, `Sent`, `Approved`, `Rejected`, `Expired`)
- `approved_at_utc` (TIMESTAMPTZ, NULLABLE)

#### `services.service_quotation_lines`
- `id` (UUID, PK)
- `service_quotation_id` (UUID, FK -> `services.service_quotations`, NOT NULL)
- `line_type` (VARCHAR(20), NOT NULL: `Labor`, `Part`)
- `product_id` (UUID, FK -> `commercial.products`, NULLABLE)
- `description` (VARCHAR(150), NOT NULL)
- `quantity` (DECIMAL(12,3), NOT NULL)
- `unit_rate_paise` (BIGINT, NOT NULL)
- `line_total_paise` (BIGINT, NOT NULL)

#### `services.service_part_consumptions`
- `id` (UUID, PK)
- `service_request_id` (UUID, FK -> `services.service_requests`, NOT NULL)
- `product_id` (UUID, FK -> `commercial.products`, NOT NULL)
- `warehouse_id` (UUID, FK -> `inventory.warehouses`, NOT NULL)
- `quantity` (DECIMAL(12,3), NOT NULL)
- `unit_cost_paise` (BIGINT, NOT NULL)
- `stock_movement_id` (UUID, FK -> `inventory.stock_movements`, NULLABLE)
- `consumed_at_utc` (TIMESTAMPTZ, DEFAULT NOW())

#### `services.service_invoice_links`
- `id` (UUID, PK)
- `service_request_id` (UUID, FK -> `services.service_requests`, NOT NULL)
- `sales_invoice_id` (UUID, FK -> `pos.sales_invoices`, NOT NULL)
- `invoiced_at_utc` (TIMESTAMPTZ, DEFAULT NOW())

