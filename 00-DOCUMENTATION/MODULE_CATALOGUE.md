# MAGIC — MODULE CATALOGUE & SPECIFICATION

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  

---

## Master Module Roster

This catalogue details the 21 architectural modules of **MAGIC**. Every module conforms to strict domain boundaries, isolated data ownership, and explicit contract-based integration. All 14 governance dimensions (Module ID, Purpose, Responsibilities, Feature Catalogue, Business Rules, Domain Entities, Database Ownership, API Boundaries, Dependencies, Permissions, Events, Validation, Testing Requirements, Acceptance Criteria) are enforced.

> [!NOTE]
> **Product Catalogue & Inventory Boundary:** Module 05 (`MOD-05`) encompasses Domain B's Product Catalogue (`products.*`: SKU, barcode, variants, prices, UoMs, HSN mapping) and Warehouse/Inventory (`inventory.*`: stock balances, stock ledger, batches, stock transfers). Physical tables are cleanly separated into `products` and `inventory` PostgreSQL schemas without duplicating entity ownership across modules.

---

### Module 01: Dashboard and Business Intelligence (`MOD-01` — Domain G)
- **Purpose:** Provide operational transparency and executive decision support through aggregated metrics and trend analytics.
- **Feature Catalogue:** Daily sales velocity, cash-flow summary, gross margin heatmaps, top-selling items, low-stock warnings, branch performance leaderboards.
- **Domain Entities:** `MetricSnapshot`, `UserWidget`, `KpiConfiguration`.
- **Data Ownership:** `bi.metric_snapshots`, `bi.user_widgets`, `bi.kpi_configurations`.
- **Dependencies:** Platform (`MOD-21`), Sales (`MOD-03`), Purchases (`MOD-04`), Accounting (`MOD-06`), Inventory (`MOD-05`).
- **API Contracts:** `GET /api/v1/bi/dashboard`, `GET /api/v1/bi/sales-velocity`, `GET /api/v1/bi/kpis`.
- **Permissions:** `bi:dashboard:view`, `bi:financial_kpi:view`, `bi:branch_comparison:view`.
- **Events:** Consumes `magic.sales.invoice_posted.v1`, `magic.purchases.invoice_verified.v1`, `magic.inventory.stock_deficit_detected.v1`.
- **Validation Rules:** Date range must not exceed 365 days in a single ad-hoc query; aggregation must run against read snapshots.
- **Testing Requirements:** Snapshot projection reconciliation tests vs. live GL/Sales aggregates, performance benchmarks under 500ms.
- **Acceptance Criteria:** Dashboard loads in < 500ms; cash vs. credit split matches GL control accounts exactly.

---

### Module 02: POS and Billing (`MOD-02` — Domain A)
- **Purpose:** Deliver lightning-fast, keyboard-driven retail checkout and counter cash reconciliation.
- **Feature Catalogue:** Fast barcode lookup, hold/recall carts, multi-tender split (Cash/Card/UPI), cashier shift management (X/Z reports), cash drawer trigger, thermal receipt generation.
- **Domain Entities:** `PosSession`, `HeldCart`, `HeldCartItem`, `CashDrop`, `TerminalConfiguration`.
- **Data Ownership:** `pos.pos_sessions`, `pos.held_carts`, `pos.held_cart_items`, `pos.cash_drops`, `pos.terminal_configurations`.
- **Dependencies:** Inventory & Products (`MOD-05`), Sales (`MOD-03`), Accounting (`MOD-06`), Tax (`MOD-08`), Marketing (`MOD-14`), Platform (`MOD-21`).
- **API Contracts:** `POST /api/v1/pos/sessions/open`, `POST /api/v1/pos/checkout`, `POST /api/v1/pos/carts/hold`, `POST /api/v1/pos/sessions/close`.
- **Permissions:** `pos:checkout:execute`, `pos:discount:apply`, `pos:session:manage`, `pos:reprint:execute`.
- **Events:** Emits `magic.pos.checkout_completed.v1`, `magic.pos.shift_closed.v1`.
- **Validation Rules:** Cash tendered must equal or exceed total due when cash-only; discounts exceeding threshold require supervisor PIN.
- **Testing Requirements:** Multi-terminal checkout concurrency tests, keyboard shortcut UI tests, sub-50ms barcode lookup benchmarks, shift X/Z balance tests.
- **Acceptance Criteria:** Barcode scan to line entry < 50ms; checkout generates atomic sales record, stock decrement, and accounting journal.

---

### Module 03: Sales Management (`MOD-03` — Domain A)
- **Purpose:** Manage commercial wholesale, B2B, distribution, and service sales lifecycles.
- **Feature Catalogue:** Quotations, Proforma Invoices, Sales Orders, Delivery Challans, Tax Invoices, Credit Notes, Customer Pricing Tiers, Salesperson commissions.
- **Domain Entities:** `SalesOrder`, `SalesInvoice`, `DeliveryChallan`, `CreditNote`, `CustomerPriceTier`.
- **Data Ownership:** `sales.sales_orders`, `sales.sales_order_items`, `sales.invoices`, `sales.invoice_items`, `sales.delivery_challans`, `sales.credit_notes`.
- **Dependencies:** Customers (`MOD-09`), Inventory & Products (`MOD-05`), Accounting (`MOD-06`), Tax (`MOD-08`), Marketing (`MOD-14`), Platform (`MOD-21`).
- **API Contracts:** `POST /api/v1/sales/orders`, `POST /api/v1/sales/invoices`, `POST /api/v1/sales/credit-notes`.
- **Permissions:** `sales:orders:create`, `sales:invoices:post`, `sales:credit_notes:approve`.
- **Events:** Emits `magic.sales.invoice_posted.v1`, `magic.sales.credit_note_posted.v1`.
- **Validation Rules:** Customer credit limit validation; HSN/SAC code mandatory per item; reverse inclusive or forward exclusive tax applied correctly.
- **Testing Requirements:** Credit limit enforcement unit tests, intra-state vs inter-state GST integration tests, atomic invoice + stock + journal transaction tests.
- **Acceptance Criteria:** Invoice posting synchronously executes stock deduction and accounting journal within one ACID transaction, emitting outbox event for BI/CRM.

---

### Module 04: Purchase and Procurement (`MOD-04` — Domain A / B)
- **Purpose:** Control supplier procurement, purchase order tracking, goods inward verification, and landed cost tracking.
- **Feature Catalogue:** Purchase requisitions, PO creation, Goods Receipt Notes (GRN), Purchase Invoices, Debit Notes, Landed cost apportionment.
- **Domain Entities:** `PurchaseOrder`, `GoodsReceiptNote`, `PurchaseInvoice`, `DebitNote`, `LandedCostAllocation`.
- **Data Ownership:** `purchases.purchase_orders`, `purchases.purchase_order_items`, `purchases.goods_receipt_notes`, `purchases.purchase_invoices`, `purchases.debit_notes`.
- **Dependencies:** Suppliers (`MOD-10`), Inventory & Products (`MOD-05`), Accounting (`MOD-06`), Tax (`MOD-08`), Platform (`MOD-21`).
- **API Contracts:** `POST /api/v1/purchases/orders`, `POST /api/v1/purchases/grn`, `POST /api/v1/purchases/invoices`.
- **Permissions:** `purchases:orders:create`, `purchases:grn:receive`, `purchases:invoices:post`.
- **Events:** Emits `magic.purchases.invoice_verified.v1`, `magic.purchases.grn_received.v1`.
- **Validation Rules:** Three-way matching (PO vs. GRN vs. Invoice) within acceptable price/quantity tolerance thresholds.
- **Testing Requirements:** Three-way match discrepancy tests, landed cost apportionment precision tests, ITC ledger reconciliation tests.
- **Acceptance Criteria:** Posted purchase invoice debits Inventory/Expense, debits Input Tax Credit (ITC), and credits Supplier AP subledger atomically.

---

### Module 05: Inventory and Warehouse Management (`MOD-05` — Domain B)
- **Purpose:** Maintain total physical and financial control over product catalogue, stock, warehouses, batches, and valuations.
- **Feature Catalogue:** Multi-warehouse locations, SKU/Barcode master, variants, UoM conversion, batch & expiry tracking, serial numbers, stock transfers (IBT), physical stock audits, FIFO/Moving Average valuation.
- **Domain Entities:** `Product`, `UnitOfMeasure`, `Warehouse`, `StockBalance`, `StockLedgerEntry`, `StockBatch`, `StockTransfer`.
- **Data Ownership:** `products.items`, `products.units`, `products.barcodes`, `inventory.warehouses`, `inventory.stock_balances`, `inventory.stock_ledger`, `inventory.stock_batches`, `inventory.stock_transfers`.
- **Dependencies:** Platform (`MOD-21`), Multi-Branch (`MOD-18`), Tax (`MOD-08`), Accounting (`MOD-06`).
- **API Contracts:** `GET /api/v1/inventory/items/{id}/stock`, `POST /api/v1/inventory/transfers`, `POST /api/v1/inventory/adjustments`.
- **Permissions:** `inventory:items:manage`, `inventory:stock:transfer`, `inventory:adjustment:approve`.
- **Events:** Emits `magic.inventory.stock_adjusted.v1`, `magic.inventory.low_stock_reached.v1`.
- **Validation Rules:** Stock balance cannot be negative when `DisallowNegativeStock` is enabled; batch expiry must be after manufacturing date.
- **Testing Requirements:** FIFO lot depletion sequence tests, Moving Weighted Average precision tests, concurrency oversell prevention tests.
- **Acceptance Criteria:** Stock ledger is append-only; balance is reconstructed from movements; inventory asset account reconciles to total stock valuation.

---

### Module 06: Accounting (Financial Core) (`MOD-06` — Domain C)
- **Purpose:** Serve as the immutable, double-entry financial source of truth for the entire business.
- **Feature Catalogue:** Hierarchical Chart of Accounts, Journal Vouchers, General Ledger, Subledgers (AR/AP), Trial Balance, Profit & Loss, Balance Sheet, Period closing & locking.
- **Domain Entities:** `AccountGroup`, `Account`, `FiscalYear`, `AccountingPeriod`, `Voucher`, `JournalLine`.
- **Data Ownership:** `accounting.accounts`, `accounting.account_groups`, `accounting.fiscal_years`, `accounting.accounting_periods`, `accounting.vouchers`, `accounting.journal_lines`.
- **Dependencies:** Platform (`MOD-21`), Multi-Branch (`MOD-18`).
- **API Contracts:** `POST /api/v1/accounting/vouchers`, `GET /api/v1/accounting/general-ledger`, `GET /api/v1/accounting/trial-balance`, `POST /api/v1/accounting/periods/lock`.
- **Permissions:** `accounting:vouchers:post`, `accounting:vouchers:reverse`, `accounting:reports:view`, `accounting:period:close`.
- **Events:** Emits `magic.accounting.voucher_posted.v1`, `magic.accounting.period_locked.v1`.
- **Validation Rules:** Sum of debits must equal sum of credits to exact integer paise ($\sum Dr = \sum Cr$); posted vouchers are immutable.
- **Testing Requirements:** Double-entry zero-sum invariant tests, period lock enforcement tests, reversal voucher integrity tests, AR/AP reconciliation tests.
- **Acceptance Criteria:** Vouchers cannot be deleted or updated in place; reversals create counter-vouchers; Trial Balance net delta is zero.

---

### Module 07: Financial Management & Treasury (`MOD-07` — Domain C)
- **Purpose:** Manage cash balances, bank accounts, cheque registers, bank reconciliations, and fixed asset depreciation.
- **Feature Catalogue:** Bank account master, Cheque leaf registers, Bank Reconciliation Statement (BRS), Fixed Asset Register, Depreciation engine (SLM/WDV), Cash flow statements.
- **Domain Entities:** `BankAccount`, `BankTransaction`, `BankReconciliation`, `FixedAsset`, `DepreciationSchedule`.
- **Data Ownership:** `finance.bank_accounts`, `finance.bank_transactions`, `finance.bank_reconciliations`, `finance.fixed_assets`, `finance.depreciation_schedules`.
- **Dependencies:** Accounting (`MOD-06`), Platform (`MOD-21`).
- **API Contracts:** `POST /api/v1/finance/bank-reconciliations`, `POST /api/v1/finance/fixed-assets`, `POST /api/v1/finance/depreciation/run`.
- **Permissions:** `finance:banking:manage`, `finance:reconciliation:post`, `finance:assets:manage`.
- **Events:** Emits `magic.finance.depreciation_posted.v1`, `magic.finance.reconciliation_completed.v1`.
- **Validation Rules:** Bank statement closing balance plus uncredited cheques minus unpresented cheques must equal book balance.
- **Testing Requirements:** SLM & WDV depreciation calculation tests in integer paise, BRS tie-out verification tests.
- **Acceptance Criteria:** Depreciation run generates atomic journal vouchers; bank reconciliation flags cleared transactions without mutating the GL.

---

### Module 08: Tax and Statutory Compliance (`MOD-08` — Domain D)
- **Purpose:** Calculate, classify, and report statutory taxes (Indian GST, TDS, TCS).
- **Feature Catalogue:** HSN/SAC master with slab rates, Reverse inclusive GST calculation, Forward exclusive GST, CGST/SGST/IGST automation, GSTR-1, GSTR-3B, GSTR-2B reporting datasets.
- **Domain Entities:** `TaxRate`, `HsnSacCode`, `TaxTransaction`, `TaxSettlement`.
- **Data Ownership:** `tax.tax_rates`, `tax.hsn_sac_codes`, `tax.tax_transactions`, `tax.tax_settlements`.
- **Dependencies:** Accounting (`MOD-06`), Platform (`MOD-21`).
- **API Contracts:** `POST /api/v1/tax/calculate`, `GET /api/v1/tax/gstr1-summary`, `POST /api/v1/tax/settle`.
- **Permissions:** `tax:configuration:manage`, `tax:returns:export`, `tax:settlement:post`.
- **Events:** Emits `magic.tax.settlement_posted.v1`.
- **Validation Rules:** Place of supply determines CGST+SGST vs. IGST; reverse inclusive gross amounts must match sum of taxable base and tax heads.
- **Testing Requirements:** 100,000-case synthetic reverse inclusive GST fuzz test, odd-paise CGST/SGST split test, Section 170 rounding test.
- **Acceptance Criteria:** Zero tax discrepancy between invoice line-item calculations and accounting tax liability accounts.

---

### Module 09: Customer CRM (`MOD-09` — Domain E)
- **Purpose:** Maintain customer profiles, credit policies, loyalty programs, and relationship histories.
- **Feature Catalogue:** Customer profiles, multiple shipping addresses, GSTIN validation, credit limit/period controls, loyalty points ledger, statement of accounts (SOA).
- **Domain Entities:** `Customer`, `CustomerAddress`, `LoyaltyAccount`, `LoyaltyTransaction`, `CustomerInteraction`.
- **Data Ownership:** `customers.customers`, `customers.customer_addresses`, `customers.loyalty_accounts`, `customers.loyalty_transactions`, `customers.customer_interactions`.
- **Dependencies:** Accounting (`MOD-06`), Platform (`MOD-21`).
- **API Contracts:** `GET /api/v1/customers`, `POST /api/v1/customers`, `GET /api/v1/customers/{id}/statement`.
- **Permissions:** `customers:view`, `customers:create`, `customers:loyalty:adjust`.
- **Events:** Consumes `magic.sales.invoice_posted.v1` (loyalty accrual); emits `magic.customers.credit_limit_breached.v1`.
- **Validation Rules:** GSTIN format must pass 15-character statutory checksum regex; credit limit must be non-negative.
- **Testing Requirements:** GSTIN checksum validator tests, customer SOA balance vs. GL Account `1130` tie-out tests.
- **Acceptance Criteria:** Customer balance matches AR subledger balance for that customer's ledger account.

---

### Module 10: Supplier Management (`MOD-10` — Domain B)
- **Purpose:** Oversee supplier onboarding, bank records, MSME status, and procurement tracking.
- **Feature Catalogue:** Supplier directory, bank account details, MSME classification (Micro, Small, Medium), payment terms, purchase history, AP aging.
- **Domain Entities:** `Supplier`, `SupplierBankDetail`, `SupplierRating`.
- **Data Ownership:** `suppliers.suppliers`, `suppliers.supplier_bank_details`, `suppliers.supplier_ratings`.
- **Dependencies:** Accounting (`MOD-06`), Platform (`MOD-21`).
- **API Contracts:** `GET /api/v1/suppliers`, `POST /api/v1/suppliers`, `GET /api/v1/suppliers/{id}/aging`.
- **Permissions:** `suppliers:manage`, `suppliers:view_financials`.
- **Events:** Emits `magic.suppliers.onboarded.v1`.
- **Validation Rules:** Bank IFSC and account number mandatory if payment mode is Electronic; PAN/GSTIN format validation.
- **Testing Requirements:** IFSC/PAN regex tests, supplier AP aging vs. GL Account `2110` tie-out tests.
- **Acceptance Criteria:** Supplier outstanding balance matches AP subledger balance for that supplier's ledger account.

---

### Module 11: Expense Management (`MOD-11` — Domain C)
- **Purpose:** Track operational expenditures, petty cash floats, and vendor operational billings.
- **Feature Catalogue:** Expense categories, petty cash vouchers, claim requests, multi-level approval workflows, GST Input Tax Credit (ITC) claim tagging.
- **Domain Entities:** `ExpenseCategory`, `ExpenseClaim`, `ExpenseItem`, `PettyCashFloat`.
- **Data Ownership:** `expenses.expense_categories`, `expenses.expense_claims`, `expenses.expense_items`, `expenses.petty_cash_floats`.
- **Dependencies:** Accounting (`MOD-06`), Tax (`MOD-08`), Platform (`MOD-21`).
- **API Contracts:** `POST /api/v1/expenses/claims`, `POST /api/v1/expenses/claims/{id}/approve`, `POST /api/v1/expenses/petty-cash/voucher`.
- **Permissions:** `expenses:create`, `expenses:approve`, `expenses:petty_cash:manage`.
- **Events:** Emits `magic.expenses.claim_approved.v1`.
- **Validation Rules:** Expenses exceeding branch limit require supervisor approval; non-business expenses flagged as ineligible for ITC.
- **Testing Requirements:** Approval hierarchy enforcement tests, petty cash float balance non-negativity tests.
- **Acceptance Criteria:** Approved expense claim posts immediately to General Ledger (Expense Dr, Cash/Bank/Payable Cr).

---

### Module 12: Human Resources (HR) (`MOD-12` — Domain F)
- **Purpose:** Manage employee records, departmental structures, and biometric attendance ingestion.
- **Feature Catalogue:** Employee directory, department/designation hierarchy, shift configurations, attendance registers, leave balances.
- **Domain Entities:** `Employee`, `Department`, `Designation`, `Shift`, `AttendanceLog`, `LeaveApplication`.
- **Data Ownership:** `employees.employees`, `employees.departments`, `employees.designations`, `employees.shifts`, `employees.attendance_logs`, `employees.leave_applications`.
- **Dependencies:** Platform (`MOD-21`), Multi-Branch (`MOD-18`).
- **API Contracts:** `GET /api/v1/hr/employees`, `POST /api/v1/hr/attendance/ingest`, `POST /api/v1/hr/leave/apply`.
- **Permissions:** `hr:employees:manage`, `hr:attendance:manage`, `hr:leave:approve`.
- **Events:** Emits `magic.hr.attendance_ingested.v1`, `magic.hr.leave_approved.v1`.
- **Validation Rules:** An employee cannot have overlapping active shifts; leave cannot be approved beyond allocated quota unless unpaid.
- **Testing Requirements:** Overlapping shift rejection tests, biometric CSV/log parser tests, leave balance accrual tests.
- **Acceptance Criteria:** Attendance logs feed directly into the Payroll processing engine without manual re-entry.

---

### Module 13: Payroll (`MOD-13` — Domain F)
- **Purpose:** Execute statutory-compliant compensation runs and generate payroll journal vouchers.
- **Feature Catalogue:** Salary component templates (Basic, HRA, DA, PF, ESI, PT, TDS), monthly payroll batch processing, payslip generation, payroll disbursement vouchers.
- **Domain Entities:** `SalaryComponent`, `SalaryStructure`, `Payrun`, `PayrunDetail`, `EmployeePayslip`.
- **Data Ownership:** `payroll.salary_components`, `payroll.salary_structures`, `payroll.payruns`, `payroll.payrun_details`, `payroll.employee_payslips`.
- **Dependencies:** HR (`MOD-12`), Accounting (`MOD-06`), Tax (`MOD-08`), Platform (`MOD-21`).
- **API Contracts:** `POST /api/v1/payroll/payruns/generate`, `POST /api/v1/payroll/payruns/{id}/approve`, `GET /api/v1/payroll/payslips/{id}`.
- **Permissions:** `payroll:run:execute`, `payroll:run:approve`, `payroll:payslip:view`.
- **Events:** Emits `magic.payroll.payrun_approved.v1`.
- **Validation Rules:** Net salary = Gross Earnings - Statutory Deductions - Voluntary Deductions; cannot be negative.
- **Testing Requirements:** PF/ESI/PT statutory ceiling calculation tests, payroll journal balance tests (`TX-20` & `TX-21`).
- **Acceptance Criteria:** Approving a payrun writes an atomic journal voucher to Accounting with correct liability splits.

---

### Module 14: Marketing and Customer Growth (`MOD-14` — Domain E)
- **Purpose:** Drive revenue growth through structured promotional rules, coupon campaigns, and customer retention mechanisms.
- **Feature Catalogue:** Promotional discount matrices (BOGO, volume discounts, festive discounts), coupon code management, campaign performance analytics.
- **Domain Entities:** `PromotionRule`, `Coupon`, `Campaign`, `CouponRedemption`.
- **Data Ownership:** `marketing.promotions`, `marketing.coupons`, `marketing.campaigns`, `marketing.coupon_redemptions`.
- **Dependencies:** Customer CRM (`MOD-09`), Inventory & Products (`MOD-05`), Platform (`MOD-21`). *(Zero synchronous dependency on Sales or POS; subscribes to `magic.sales.invoice_posted.v1` Outbox events for campaign attribution).*
- **API Contracts:** `POST /api/v1/marketing/promotions`, `POST /api/v1/marketing/coupons/validate`.
- **Permissions:** `marketing:promotions:manage`, `marketing:campaigns:view`.
- **Events:** Consumes `magic.sales.invoice_posted.v1`; emits `magic.marketing.coupon_redeemed.v1`.
- **Validation Rules:** Coupon redemption limits enforced; promotion active date ranges checked against transaction timestamp.
- **Testing Requirements:** Promotion precedence tests, expired coupon rejection tests, concurrency tests on single-use coupons.
- **Acceptance Criteria:** Validated coupons apply deterministic discount amounts in POS and Sales checkout pipelines without circular module coupling.

---

### Module 15: Order Management (`MOD-15` — Domain A)
- **Purpose:** Coordinate sales order allocation, backorder fulfillment, and stage-by-stage picking and packing.
- **Feature Catalogue:** Order fulfillment staging, inventory allocation/reservation, pick list generation, pack verification, shipment dispatch tracking.
- **Domain Entities:** `OrderFulfillment`, `PickList`, `PickListItem`, `Backorder`.
- **Data Ownership:** `orders.order_fulfillments`, `orders.pick_lists`, `orders.pick_list_items`, `orders.backorders`.
- **Dependencies:** Sales (`MOD-03`), Inventory (`MOD-05`), Customers (`MOD-09`), Platform (`MOD-21`).
- **API Contracts:** `POST /api/v1/orders/{id}/allocate`, `POST /api/v1/orders/{id}/pick`, `POST /api/v1/orders/{id}/dispatch`.
- **Permissions:** `orders:fulfill:manage`, `orders:dispatch:execute`.
- **Events:** Emits `magic.orders.dispatched.v1`.
- **Validation Rules:** Allocated stock cannot exceed available-to-promise (ATP) inventory.
- **Testing Requirements:** Concurrent allocation conflict tests, partial shipment backorder split tests.
- **Acceptance Criteria:** Dispatched orders trigger stock issue and update sales order status to Fulfilled.

---

### Module 16: Service Management (`MOD-16` — Domain A)
- **Purpose:** Support service businesses, repair workshops, and AMC contract administration.
- **Feature Catalogue:** Service job cards, technician task assignment, spare parts requisitioning, warranty verification, AMC contract tracking.
- **Domain Entities:** `JobCard`, `ServiceItem`, `JobSparePart`, `AmcContract`.
- **Data Ownership:** `services.job_cards`, `services.service_items`, `services.job_spare_parts`, `services.amc_contracts`.
- **Dependencies:** Customers (`MOD-09`), Inventory (`MOD-05`), Sales (`MOD-03`), Accounting (`MOD-06`), Platform (`MOD-21`).
- **API Contracts:** `POST /api/v1/services/job-cards`, `POST /api/v1/services/job-cards/{id}/consume-parts`, `POST /api/v1/services/amc`.
- **Permissions:** `services:jobcards:manage`, `services:technicians:assign`.
- **Events:** Emits `magic.services.jobcard_closed.v1`.
- **Validation Rules:** Spare parts consumed must be deducted from inventory via an issued job card; job cards cannot close with pending unbilled parts.
- **Testing Requirements:** Parts consumption stock deduction tests, job-card-to-invoice closure tests.
- **Acceptance Criteria:** Closing a job card generates a consolidated Service & Parts Tax Invoice.

---

### Module 17: Reports and Analytics (`MOD-17` — Domain G)
- **Purpose:** Centralize high-performance reporting, regulatory filings, and business intelligence exports.
- **Feature Catalogue:** Financial statements (Balance Sheet, P&L, Trial Balance), stock registers, GST filing tables, sales analysis, customer ledgers, PDF/Excel/CSV generation.
- **Domain Entities:** `ReportDefinition`, `ScheduledReport`, `ExportJob`.
- **Data Ownership:** `reports.report_definitions`, `reports.scheduled_reports`, `reports.export_jobs`.
- **Dependencies:** Read-only consumer across all domain modules (`Contracts/` read views).
- **API Contracts:** `POST /api/v1/reports/generate`, `GET /api/v1/reports/jobs/{id}/download`.
- **Permissions:** `reports:financial:view`, `reports:operational:view`, `reports:export:execute`.
- **Events:** Emits `magic.reports.export_completed.v1`.
- **Validation Rules:** Report queries must execute with `READ COMMITTED` snapshot isolation or against materialized read tables.
- **Testing Requirements:** Financial statement tie-out tests (Assets == Liabilities + Equity), query performance benchmarks (< 1s for 100k rows).
- **Acceptance Criteria:** Trial balance report generates in < 1s for 100,000 ledger records.

---

### Module 18: Multi-Branch and Multi-Terminal (`MOD-18` — Domain F)
- **Purpose:** Manage distributed physical branches, warehouses, and client checkout terminals.
- **Feature Catalogue:** Branch organization setup, terminal registration, hardware-to-terminal pairing, inter-branch stock transfer (IBT) workflows, centralized vs. decentralized accounting consolidation.
- **Domain Entities:** `Branch`, `Terminal`, `TerminalToken`, `BranchSetting`.
- **Data Ownership:** `organization.branches`, `organization.terminals`, `organization.terminal_tokens`, `organization.branch_settings`.
- **Dependencies:** Platform (`MOD-21`), Accounting (`MOD-06`), Inventory (`MOD-05`).
- **API Contracts:** `POST /api/v1/branches`, `POST /api/v1/terminals/register`, `POST /api/v1/terminals/{id}/authorize`.
- **Permissions:** `org:branches:manage`, `org:terminals:manage`.
- **Events:** Emits `magic.organization.terminal_authorized.v1`, `magic.organization.terminal_revoked.v1`.
- **Validation Rules:** Every commercial transaction must carry an authenticated `BranchId` and `TerminalId` header; terminal tokens must be cryptographically signed.
- **Testing Requirements:** Cross-branch data isolation security tests, terminal revocation immediate rejection tests.
- **Acceptance Criteria:** Client terminals cannot access data belonging to unauthorized branches.

---

### Module 19: Administration and Security (`MOD-19` — Domain G)
- **Purpose:** Enforce authentication, role-based authorization, supervisor approvals, and comprehensive audit logs.
- **Feature Catalogue:** User management, custom roles, permission matrix, session management, supervisor override PINs, immutable audit trail.
- **Domain Entities:** `ApplicationUser`, `Role`, `Permission`, `SupervisorOverride`, `SystemAuditLog`.
- **Data Ownership:** `auth.users`, `auth.roles`, `auth.permissions`, `auth.role_permissions`, `auth.user_roles`, `audit.system_audit_logs`.
- **Dependencies:** Platform (`MOD-21`).
- **API Contracts:** `POST /api/v1/auth/login`, `POST /api/v1/auth/refresh`, `POST /api/v1/auth/override-approval`, `GET /api/v1/audit/logs`.
- **Permissions:** `admin:users:manage`, `admin:roles:manage`, `admin:audit:view`.
- **Events:** Emits `magic.auth.login_failed.v1`, `magic.auth.supervisor_override_used.v1`.
- **Validation Rules:** Passwords hashed with Argon2id; session revoked on timeout; audit logs append-only.
- **Testing Requirements:** Privilege escalation tests, brute-force lockout tests, audit log append-only immutability tests.
- **Acceptance Criteria:** Every financial mutation logs the actor, timestamp, client IP, terminal ID, entity type, and before/after state diff.

---

### Module 20: Automation and AI (Auto Stock Update) (`MOD-20` — Domain G)
- **Purpose:** Accelerate purchase invoice data entry through staged, AI-extracted invoice parsing with complete human-in-the-loop verification.
- **Feature Catalogue:** Invoice text/JSON paste workspace, structured parser, supplier matching engine, SKU/Barcode reconciliation, tax total cross-verification, staging correction grid.
- **Domain Entities:** `StagedPurchaseInvoice`, `StagedInvoiceItem`, `AiParsingLog`.
- **Data Ownership:** `automation.staged_purchase_invoices`, `automation.staged_invoice_items`, `automation.ai_parsing_logs`.
- **Dependencies:** Purchases (`MOD-04`), Suppliers (`MOD-10`), Inventory (`MOD-05`), Tax (`MOD-08`), Accounting (`MOD-06`), Platform (`MOD-21`).
- **API Contracts:** `POST /api/v1/automation/auto-stock/stage`, `PUT /api/v1/automation/auto-stock/{id}`, `POST /api/v1/automation/auto-stock/{id}/commit`.
- **Permissions:** `automation:autostock:stage`, `automation:autostock:commit`.
- **Events:** Emits `magic.automation.autostock_committed.v1`.
- **Validation Rules:** Must match existing supplier or explicitly confirm new supplier; SKU/Barcode verification required; line items must sum to invoice grand total; no inventory or accounting mutations until explicit human commit.
- **Testing Requirements:** Duplicate invoice blocking tests, line-item vs. header mismatch detection tests, unapproved stage isolation tests.
- **Acceptance Criteria:** Committing a staged invoice creates a verified Purchase Invoice, increments Inventory lots, and writes the Accounting voucher in a single atomic database transaction.

---

### Module 21: System Management (Platform Foundation) (`MOD-21` — Domain G)
- **Purpose:** Manage system health, automated database backups, restore drills, software versioning, and environment configuration.
- **Feature Catalogue:** Automated PostgreSQL backup engine (`pg_dump` wrapper), backup archive integrity check, point-in-time restore testing, system telemetry, error logs.
- **Domain Entities:** `SystemConfiguration`, `BackupRecord`, `SystemTelemetryEntry`, `OutboxMessage`.
- **Data Ownership:** `platform.system_configurations`, `platform.backup_history`, `platform.system_telemetry`, `platform.outbox_messages`, `platform.database_versions`.
- **Dependencies:** None (Base platform foundation).
- **API Contracts:** `GET /api/v1/system/status`, `POST /api/v1/system/backups/trigger`, `POST /api/v1/system/backups/verify`.
- **Permissions:** `system:config:manage`, `system:backups:manage`.
- **Events:** Emits `magic.system.backup_completed.v1`, `magic.system.backup_failed.v1`.
- **Validation Rules:** Backups must be checksummed with SHA-256; restore test runs in an isolated temporary database.
- **Testing Requirements:** Automated backup and temporary DB restore verification tests, outbox retry backoff tests.
- **Acceptance Criteria:** Backup executes without interrupting active POS transactions; verification proves file integrity.

