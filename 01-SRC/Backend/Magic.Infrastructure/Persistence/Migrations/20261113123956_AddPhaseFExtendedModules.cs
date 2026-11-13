using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Magic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhaseFExtendedModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "hr");

            migrationBuilder.EnsureSchema(
                name: "orders");

            migrationBuilder.EnsureSchema(
                name: "marketing");

            migrationBuilder.EnsureSchema(
                name: "payroll");

            migrationBuilder.EnsureSchema(
                name: "expenses");

            migrationBuilder.EnsureSchema(
                name: "services");

            migrationBuilder.CreateTable(
                name: "customer_segments",
                schema: "marketing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    segment_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    min_lifetime_spend_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    min_orders_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_segments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "designations",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_designations", x => x.id);
                    table.ForeignKey(
                        name: "FK_designations_departments_department_id",
                        column: x => x.department_id,
                        principalSchema: "commercial",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "expense_categories",
                schema: "expenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    default_gl_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expense_categories", x => x.id);
                    table.ForeignKey(
                        name: "FK_expense_categories_accounts_default_gl_account_id",
                        column: x => x.default_gl_account_id,
                        principalSchema: "accounting",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "leave_types",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    annual_days = table.Column<int>(type: "integer", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leave_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "loyalty_programs",
                schema: "marketing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    points_per_hundred_spent = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    currency_value_per_point_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 100L),
                    min_points_to_redeem = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loyalty_programs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "loyalty_transactions",
                schema: "marketing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    balance_after_points = table.Column<int>(type: "integer", nullable: false),
                    sales_invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loyalty_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_loyalty_transactions_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "commercial",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_loyalty_transactions_sales_invoices_sales_invoice_id",
                        column: x => x.sales_invoice_id,
                        principalSchema: "sales",
                        principalTable: "sales_invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payroll_periods",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Calculated"),
                    total_gross_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    total_deductions_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    total_net_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    total_employer_contributions_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    journal_voucher_id = table.Column<Guid>(type: "uuid", nullable: true),
                    disbursement_voucher_id = table.Column<Guid>(type: "uuid", nullable: true),
                    processed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    finalized_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payroll_periods", x => x.id);
                    table.CheckConstraint("chk_payroll_net_calc", "total_net_paise = total_gross_paise - total_deductions_paise");
                    table.ForeignKey(
                        name: "FK_payroll_periods_fiscal_years_fiscal_year_id",
                        column: x => x.fiscal_year_id,
                        principalSchema: "accounting",
                        principalTable: "fiscal_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payroll_periods_users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payroll_periods_users_processed_by_user_id",
                        column: x => x.processed_by_user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payroll_periods_vouchers_disbursement_voucher_id",
                        column: x => x.disbursement_voucher_id,
                        principalSchema: "accounting",
                        principalTable: "vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payroll_periods_vouchers_journal_voucher_id",
                        column: x => x.journal_voucher_id,
                        principalSchema: "accounting",
                        principalTable: "vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "petty_cash_accounts",
                schema: "expenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    gl_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    custodian_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    float_limit_paise = table.Column<long>(type: "bigint", nullable: false),
                    current_balance_paise = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_petty_cash_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_petty_cash_accounts_accounts_gl_account_id",
                        column: x => x.gl_account_id,
                        principalSchema: "accounting",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_petty_cash_accounts_users_custodian_user_id",
                        column: x => x.custodian_user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "salary_structures",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_salary_structures", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sales_orders",
                schema: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expected_delivery_date = table.Column<DateOnly>(type: "date", nullable: true),
                    subtotal_paise = table.Column<long>(type: "bigint", nullable: false),
                    tax_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    total_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    delivery_address = table.Column<string>(type: "text", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false, defaultValue: "Submitted"),
                    confirmed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_orders", x => x.id);
                    table.CheckConstraint("chk_order_total", "total_amount_paise = subtotal_paise + tax_amount_paise");
                    table.ForeignKey(
                        name: "FK_sales_orders_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "commercial",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_orders_users_confirmed_by_user_id",
                        column: x => x.confirmed_by_user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_orders_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_catalog",
                schema: "services",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    category = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false, defaultValue: "Repair"),
                    description = table.Column<string>(type: "text", nullable: true),
                    standard_labor_charge_paise = table.Column<long>(type: "bigint", nullable: false),
                    estimated_duration_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_catalog", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "campaigns",
                schema: "marketing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    target_segment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    budget_paise = table.Column<long>(type: "bigint", nullable: false),
                    actual_spend_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    attributed_revenue_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaigns", x => x.id);
                    table.ForeignKey(
                        name: "FK_campaigns_customer_segments_target_segment_id",
                        column: x => x.target_segment_id,
                        principalSchema: "marketing",
                        principalTable: "customer_segments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    first_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    last_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    address = table.Column<string>(type: "text", nullable: true),
                    joining_date = table.Column<DateOnly>(type: "date", nullable: false),
                    employment_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "FullTime"),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    designation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reporting_manager_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    emergency_contact_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    emergency_contact_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    bank_account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    bank_ifsc = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    bank_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pan_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    linked_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employees", x => x.id);
                    table.ForeignKey(
                        name: "FK_employees_departments_department_id",
                        column: x => x.department_id,
                        principalSchema: "commercial",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employees_designations_designation_id",
                        column: x => x.designation_id,
                        principalSchema: "hr",
                        principalTable: "designations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employees_employees_reporting_manager_id",
                        column: x => x.reporting_manager_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employees_users_linked_user_id",
                        column: x => x.linked_user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "expenses",
                schema: "expenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expense_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    expense_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gl_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    bank_cash_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expense_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    tax_amount_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    total_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    cost_center = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Submitted"),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    journal_voucher_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reversal_voucher_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attachment_path = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expenses", x => x.id);
                    table.CheckConstraint("chk_expense_amounts", "amount_paise >= 0 AND tax_amount_paise >= 0 AND total_amount_paise = amount_paise + tax_amount_paise");
                    table.ForeignKey(
                        name: "FK_expenses_accounts_gl_account_id",
                        column: x => x.gl_account_id,
                        principalSchema: "accounting",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_expenses_bank_cash_accounts_bank_cash_account_id",
                        column: x => x.bank_cash_account_id,
                        principalSchema: "finance",
                        principalTable: "bank_cash_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_expenses_expense_categories_expense_category_id",
                        column: x => x.expense_category_id,
                        principalSchema: "expenses",
                        principalTable: "expense_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_expenses_suppliers_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "commercial",
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_expenses_vouchers_journal_voucher_id",
                        column: x => x.journal_voucher_id,
                        principalSchema: "accounting",
                        principalTable: "vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_expenses_vouchers_reversal_voucher_id",
                        column: x => x.reversal_voucher_id,
                        principalSchema: "accounting",
                        principalTable: "vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "salary_structure_components",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    salary_structure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    component_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    calculation_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Fixed"),
                    default_value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    is_deduction = table.Column<bool>(type: "boolean", nullable: false),
                    is_employer_contribution = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_salary_structure_components", x => x.id);
                    table.ForeignKey(
                        name: "FK_salary_structure_components_salary_structures_salary_struct~",
                        column: x => x.salary_structure_id,
                        principalSchema: "payroll",
                        principalTable: "salary_structures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_fulfillments",
                schema: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fulfillment_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    carrier_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tracking_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    dispatched_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    delivered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Preparing"),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_fulfillments", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_fulfillments_sales_orders_sales_order_id",
                        column: x => x.sales_order_id,
                        principalSchema: "orders",
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_fulfillments_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_lines",
                schema: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    fulfilled_quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false, defaultValue: 0m),
                    invoiced_quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false, defaultValue: 0m),
                    unit_price_paise = table.Column<long>(type: "bigint", nullable: false),
                    discount_amount_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    tax_rate_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    tax_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    line_total_paise = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_order_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_sales_order_lines_product_variants_variant_id",
                        column: x => x.variant_id,
                        principalSchema: "commercial",
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_order_lines_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_order_lines_sales_orders_sales_order_id",
                        column: x => x.sales_order_id,
                        principalSchema: "orders",
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promotion_coupons",
                schema: "marketing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: true),
                    discount_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    discount_value = table.Column<long>(type: "bigint", nullable: false),
                    min_order_amount_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    max_discount_amount_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    usage_limit_total = table.Column<int>(type: "integer", nullable: false, defaultValue: 1000),
                    usage_limit_per_customer = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    current_usage_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_coupons", x => x.id);
                    table.ForeignKey(
                        name: "FK_promotion_coupons_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalSchema: "marketing",
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attendance",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attendance_date = table.Column<DateOnly>(type: "date", nullable: false),
                    check_in_time_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    check_out_time_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Present"),
                    work_duration_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_corrected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    correction_notes = table.Column<string>(type: "text", nullable: true),
                    corrected_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance", x => x.id);
                    table.ForeignKey(
                        name: "FK_attendance_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_attendance_users_corrected_by_user_id",
                        column: x => x.corrected_by_user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_salary_assignments",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    salary_structure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    monthly_gross_paise = table.Column<long>(type: "bigint", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_salary_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_salary_assignments_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_salary_assignments_salary_structures_salary_struct~",
                        column: x => x.salary_structure_id,
                        principalSchema: "payroll",
                        principalTable: "salary_structures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "leave_applications",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_days = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Applied"),
                    decision_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decision_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leave_applications", x => x.id);
                    table.ForeignKey(
                        name: "FK_leave_applications_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leave_applications_leave_types_leave_type_id",
                        column: x => x.leave_type_id,
                        principalSchema: "hr",
                        principalTable: "leave_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_leave_applications_users_decision_by_user_id",
                        column: x => x.decision_by_user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "leave_balances",
                schema: "hr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_days = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    consumed_days = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false, defaultValue: 0m),
                    pending_days = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leave_balances", x => x.id);
                    table.ForeignKey(
                        name: "FK_leave_balances_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leave_balances_leave_types_leave_type_id",
                        column: x => x.leave_type_id,
                        principalSchema: "hr",
                        principalTable: "leave_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payroll_items",
                schema: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payroll_period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    present_days = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    paid_leave_days = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    unpaid_leave_days = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    basic_paise = table.Column<long>(type: "bigint", nullable: false),
                    hra_paise = table.Column<long>(type: "bigint", nullable: false),
                    allowances_paise = table.Column<long>(type: "bigint", nullable: false),
                    gross_salary_paise = table.Column<long>(type: "bigint", nullable: false),
                    pf_deduction_paise = table.Column<long>(type: "bigint", nullable: false),
                    esi_deduction_paise = table.Column<long>(type: "bigint", nullable: false),
                    pt_deduction_paise = table.Column<long>(type: "bigint", nullable: false),
                    tds_deduction_paise = table.Column<long>(type: "bigint", nullable: false),
                    total_deductions_paise = table.Column<long>(type: "bigint", nullable: false),
                    net_salary_paise = table.Column<long>(type: "bigint", nullable: false),
                    employer_pf_paise = table.Column<long>(type: "bigint", nullable: false),
                    employer_esi_paise = table.Column<long>(type: "bigint", nullable: false),
                    payment_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    payment_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payroll_items", x => x.id);
                    table.CheckConstraint("chk_item_net_calc", "net_salary_paise = gross_salary_paise - total_deductions_paise");
                    table.ForeignKey(
                        name: "FK_payroll_items_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payroll_items_payroll_periods_payroll_period_id",
                        column: x => x.payroll_period_id,
                        principalSchema: "payroll",
                        principalTable: "payroll_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_requests",
                schema: "services",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_catalog_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_description = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reported_issue = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Medium"),
                    status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false, defaultValue: "NewRequest"),
                    assigned_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    received_date = table.Column<DateOnly>(type: "date", nullable: false),
                    target_completion_date = table.Column<DateOnly>(type: "date", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    delivered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    technician_notes = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_requests_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "commercial",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_employees_assigned_employee_id",
                        column: x => x.assigned_employee_id,
                        principalSchema: "hr",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_service_catalog_service_catalog_id",
                        column: x => x.service_catalog_id,
                        principalSchema: "services",
                        principalTable: "service_catalog",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "petty_cash_transactions",
                schema: "expenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    petty_cash_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    balance_after_paise = table.Column<long>(type: "bigint", nullable: false),
                    expense_id = table.Column<Guid>(type: "uuid", nullable: true),
                    journal_voucher_id = table.Column<Guid>(type: "uuid", nullable: true),
                    performed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    transaction_date_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_petty_cash_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_petty_cash_transactions_expenses_expense_id",
                        column: x => x.expense_id,
                        principalSchema: "expenses",
                        principalTable: "expenses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_petty_cash_transactions_petty_cash_accounts_petty_cash_acco~",
                        column: x => x.petty_cash_account_id,
                        principalSchema: "expenses",
                        principalTable: "petty_cash_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_petty_cash_transactions_users_performed_by_user_id",
                        column: x => x.performed_by_user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_petty_cash_transactions_vouchers_journal_voucher_id",
                        column: x => x.journal_voucher_id,
                        principalSchema: "accounting",
                        principalTable: "vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "backorders",
                schema: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pending_quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    expected_restock_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_backorders", x => x.id);
                    table.ForeignKey(
                        name: "FK_backorders_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_backorders_sales_order_lines_sales_order_line_id",
                        column: x => x.sales_order_line_id,
                        principalSchema: "orders",
                        principalTable: "sales_order_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_backorders_sales_orders_sales_order_id",
                        column: x => x.sales_order_id,
                        principalSchema: "orders",
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_fulfillment_lines",
                schema: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_fulfillment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_dispatched = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_fulfillment_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_fulfillment_lines_order_fulfillments_order_fulfillmen~",
                        column: x => x.order_fulfillment_id,
                        principalSchema: "orders",
                        principalTable: "order_fulfillments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_fulfillment_lines_sales_order_lines_sales_order_line_~",
                        column: x => x.sales_order_line_id,
                        principalSchema: "orders",
                        principalTable: "sales_order_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "coupon_redemptions",
                schema: "marketing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    coupon_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discount_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    redeemed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coupon_redemptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_coupon_redemptions_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "commercial",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_coupon_redemptions_promotion_coupons_coupon_id",
                        column: x => x.coupon_id,
                        principalSchema: "marketing",
                        principalTable: "promotion_coupons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_coupon_redemptions_sales_invoices_sales_invoice_id",
                        column: x => x.sales_invoice_id,
                        principalSchema: "sales",
                        principalTable: "sales_invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_invoice_links",
                schema: "services",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoiced_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_invoice_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_invoice_links_sales_invoices_sales_invoice_id",
                        column: x => x.sales_invoice_id,
                        principalSchema: "sales",
                        principalTable: "sales_invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_invoice_links_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalSchema: "services",
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_part_consumptions",
                schema: "services",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    unit_cost_paise = table.Column<long>(type: "bigint", nullable: false),
                    stock_movement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    consumed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_part_consumptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_part_consumptions_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_part_consumptions_service_requests_service_request_~",
                        column: x => x.service_request_id,
                        principalSchema: "services",
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_service_part_consumptions_stock_movements_stock_movement_id",
                        column: x => x.stock_movement_id,
                        principalSchema: "inventory",
                        principalTable: "stock_movements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_part_consumptions_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_quotations",
                schema: "services",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quotation_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    labor_total_paise = table.Column<long>(type: "bigint", nullable: false),
                    parts_total_paise = table.Column<long>(type: "bigint", nullable: false),
                    discount_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    tax_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    total_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    approved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_quotations", x => x.id);
                    table.CheckConstraint("chk_quote_total", "total_amount_paise = labor_total_paise + parts_total_paise - discount_paise + tax_amount_paise");
                    table.ForeignKey(
                        name: "FK_service_quotations_service_requests_service_request_id",
                        column: x => x.service_request_id,
                        principalSchema: "services",
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_quotation_lines",
                schema: "services",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_quotation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Labor"),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    unit_rate_paise = table.Column<long>(type: "bigint", nullable: false),
                    line_total_paise = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_quotation_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_quotation_lines_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_quotation_lines_service_quotations_service_quotatio~",
                        column: x => x.service_quotation_id,
                        principalSchema: "services",
                        principalTable: "service_quotations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoices_AccountingVoucherId",
                schema: "sales",
                table: "sales_invoices",
                column: "AccountingVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoices_WarehouseId",
                schema: "sales",
                table: "sales_invoices",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_corrected_by_user_id",
                schema: "hr",
                table: "attendance",
                column: "corrected_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_employee_date",
                schema: "hr",
                table: "attendance",
                columns: new[] { "employee_id", "attendance_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_backorders_product_id",
                schema: "orders",
                table: "backorders",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_backorders_sales_order_id",
                schema: "orders",
                table: "backorders",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_backorders_sales_order_line_id",
                schema: "orders",
                table: "backorders",
                column: "sales_order_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_campaigns_target_segment_id",
                schema: "marketing",
                table: "campaigns",
                column: "target_segment_id");

            migrationBuilder.CreateIndex(
                name: "ix_coupon_redemptions_coupon_invoice",
                schema: "marketing",
                table: "coupon_redemptions",
                columns: new[] { "coupon_id", "sales_invoice_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_coupon_redemptions_customer_id",
                schema: "marketing",
                table: "coupon_redemptions",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_coupon_redemptions_sales_invoice_id",
                schema: "marketing",
                table: "coupon_redemptions",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_designations_company_code",
                schema: "hr",
                table: "designations",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_designations_department_id",
                schema: "hr",
                table: "designations",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_salary_assignments_employee_id",
                schema: "payroll",
                table: "employee_salary_assignments",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_salary_assignments_salary_structure_id",
                schema: "payroll",
                table: "employee_salary_assignments",
                column: "salary_structure_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_company_code",
                schema: "hr",
                table: "employees",
                columns: new[] { "company_id", "employee_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_department_id",
                schema: "hr",
                table: "employees",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_designation_id",
                schema: "hr",
                table: "employees",
                column: "designation_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_linked_user_id",
                schema: "hr",
                table: "employees",
                column: "linked_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_reporting_manager_id",
                schema: "hr",
                table: "employees",
                column: "reporting_manager_id");

            migrationBuilder.CreateIndex(
                name: "ix_expense_categories_company_code",
                schema: "expenses",
                table: "expense_categories",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_expense_categories_default_gl_account_id",
                schema: "expenses",
                table: "expense_categories",
                column: "default_gl_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_bank_cash_account_id",
                schema: "expenses",
                table: "expenses",
                column: "bank_cash_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_expenses_company_branch_date",
                schema: "expenses",
                table: "expenses",
                columns: new[] { "company_id", "branch_id", "expense_date" });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_company_number",
                schema: "expenses",
                table: "expenses",
                columns: new[] { "company_id", "expense_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_expenses_expense_category_id",
                schema: "expenses",
                table: "expenses",
                column: "expense_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_gl_account_id",
                schema: "expenses",
                table: "expenses",
                column: "gl_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_journal_voucher_id",
                schema: "expenses",
                table: "expenses",
                column: "journal_voucher_id");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_reversal_voucher_id",
                schema: "expenses",
                table: "expenses",
                column: "reversal_voucher_id");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_vendor_id",
                schema: "expenses",
                table: "expenses",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_applications_decision_by_user_id",
                schema: "hr",
                table: "leave_applications",
                column: "decision_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_applications_employee_id",
                schema: "hr",
                table: "leave_applications",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_applications_leave_type_id",
                schema: "hr",
                table: "leave_applications",
                column: "leave_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_leave_applications_number",
                schema: "hr",
                table: "leave_applications",
                column: "application_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_leave_balances_employee_type_year",
                schema: "hr",
                table: "leave_balances",
                columns: new[] { "employee_id", "leave_type_id", "fiscal_year_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leave_balances_leave_type_id",
                schema: "hr",
                table: "leave_balances",
                column: "leave_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_leave_types_company_code",
                schema: "hr",
                table: "leave_types",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_loyalty_transactions_customer_id",
                schema: "marketing",
                table: "loyalty_transactions",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_loyalty_transactions_sales_invoice_id",
                schema: "marketing",
                table: "loyalty_transactions",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_fulfillment_lines_order_fulfillment_id",
                schema: "orders",
                table: "order_fulfillment_lines",
                column: "order_fulfillment_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_fulfillment_lines_sales_order_line_id",
                schema: "orders",
                table: "order_fulfillment_lines",
                column: "sales_order_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_fulfillments_number",
                schema: "orders",
                table: "order_fulfillments",
                column: "fulfillment_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_fulfillments_sales_order_id",
                schema: "orders",
                table: "order_fulfillments",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_fulfillments_warehouse_id",
                schema: "orders",
                table: "order_fulfillments",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_items_employee_id",
                schema: "payroll",
                table: "payroll_items",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_items_period_employee",
                schema: "payroll",
                table: "payroll_items",
                columns: new[] { "payroll_period_id", "employee_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payroll_periods_approved_by_user_id",
                schema: "payroll",
                table: "payroll_periods",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_periods_company_branch_month_year",
                schema: "payroll",
                table: "payroll_periods",
                columns: new[] { "company_id", "branch_id", "year", "month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payroll_periods_disbursement_voucher_id",
                schema: "payroll",
                table: "payroll_periods",
                column: "disbursement_voucher_id");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_periods_fiscal_year_id",
                schema: "payroll",
                table: "payroll_periods",
                column: "fiscal_year_id");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_periods_journal_voucher_id",
                schema: "payroll",
                table: "payroll_periods",
                column: "journal_voucher_id");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_periods_processed_by_user_id",
                schema: "payroll",
                table: "payroll_periods",
                column: "processed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_petty_cash_accounts_custodian_user_id",
                schema: "expenses",
                table: "petty_cash_accounts",
                column: "custodian_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_petty_cash_accounts_gl_account_id",
                schema: "expenses",
                table: "petty_cash_accounts",
                column: "gl_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_petty_cash_company_branch_name",
                schema: "expenses",
                table: "petty_cash_accounts",
                columns: new[] { "company_id", "branch_id", "account_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_petty_cash_transactions_expense_id",
                schema: "expenses",
                table: "petty_cash_transactions",
                column: "expense_id");

            migrationBuilder.CreateIndex(
                name: "IX_petty_cash_transactions_journal_voucher_id",
                schema: "expenses",
                table: "petty_cash_transactions",
                column: "journal_voucher_id");

            migrationBuilder.CreateIndex(
                name: "IX_petty_cash_transactions_performed_by_user_id",
                schema: "expenses",
                table: "petty_cash_transactions",
                column: "performed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_petty_cash_transactions_petty_cash_account_id",
                schema: "expenses",
                table: "petty_cash_transactions",
                column: "petty_cash_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_coupons_campaign_id",
                schema: "marketing",
                table: "promotion_coupons",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_promotion_coupons_company_code",
                schema: "marketing",
                table: "promotion_coupons",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salary_structure_components_salary_structure_id",
                schema: "payroll",
                table: "salary_structure_components",
                column: "salary_structure_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_lines_product_id",
                schema: "orders",
                table: "sales_order_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_lines_sales_order_id",
                schema: "orders",
                table: "sales_order_lines",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_lines_variant_id",
                schema: "orders",
                table: "sales_order_lines",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_company_number",
                schema: "orders",
                table: "sales_orders",
                columns: new[] { "company_id", "order_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_confirmed_by_user_id",
                schema: "orders",
                table: "sales_orders",
                column: "confirmed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_customer_id",
                schema: "orders",
                table: "sales_orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_warehouse_id",
                schema: "orders",
                table: "sales_orders",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_catalog_company_code",
                schema: "services",
                table: "service_catalog",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_invoice_links_req_inv",
                schema: "services",
                table: "service_invoice_links",
                columns: new[] { "service_request_id", "sales_invoice_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_invoice_links_sales_invoice_id",
                schema: "services",
                table: "service_invoice_links",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_part_consumptions_product_id",
                schema: "services",
                table: "service_part_consumptions",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_part_consumptions_service_request_id",
                schema: "services",
                table: "service_part_consumptions",
                column: "service_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_part_consumptions_stock_movement_id",
                schema: "services",
                table: "service_part_consumptions",
                column: "stock_movement_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_part_consumptions_warehouse_id",
                schema: "services",
                table: "service_part_consumptions",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_quotation_lines_product_id",
                schema: "services",
                table: "service_quotation_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_quotation_lines_service_quotation_id",
                schema: "services",
                table: "service_quotation_lines",
                column: "service_quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_quotations_number",
                schema: "services",
                table: "service_quotations",
                column: "quotation_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_quotations_service_request_id",
                schema: "services",
                table: "service_quotations",
                column: "service_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_assigned_employee_id",
                schema: "services",
                table: "service_requests",
                column: "assigned_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_company_number",
                schema: "services",
                table: "service_requests",
                columns: new[] { "company_id", "request_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_customer_id",
                schema: "services",
                table: "service_requests",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_service_catalog_id",
                schema: "services",
                table: "service_requests",
                column: "service_catalog_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "backorders",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "coupon_redemptions",
                schema: "marketing");

            migrationBuilder.DropTable(
                name: "employee_salary_assignments",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "leave_applications",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "leave_balances",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "loyalty_programs",
                schema: "marketing");

            migrationBuilder.DropTable(
                name: "loyalty_transactions",
                schema: "marketing");

            migrationBuilder.DropTable(
                name: "order_fulfillment_lines",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "payroll_items",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "petty_cash_transactions",
                schema: "expenses");

            migrationBuilder.DropTable(
                name: "salary_structure_components",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "service_invoice_links",
                schema: "services");

            migrationBuilder.DropTable(
                name: "service_part_consumptions",
                schema: "services");

            migrationBuilder.DropTable(
                name: "service_quotation_lines",
                schema: "services");

            migrationBuilder.DropTable(
                name: "promotion_coupons",
                schema: "marketing");

            migrationBuilder.DropTable(
                name: "leave_types",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "order_fulfillments",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "sales_order_lines",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "payroll_periods",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "expenses",
                schema: "expenses");

            migrationBuilder.DropTable(
                name: "petty_cash_accounts",
                schema: "expenses");

            migrationBuilder.DropTable(
                name: "salary_structures",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "service_quotations",
                schema: "services");

            migrationBuilder.DropTable(
                name: "campaigns",
                schema: "marketing");

            migrationBuilder.DropTable(
                name: "sales_orders",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "expense_categories",
                schema: "expenses");

            migrationBuilder.DropTable(
                name: "service_requests",
                schema: "services");

            migrationBuilder.DropTable(
                name: "customer_segments",
                schema: "marketing");

            migrationBuilder.DropTable(
                name: "employees",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "service_catalog",
                schema: "services");

            migrationBuilder.DropTable(
                name: "designations",
                schema: "hr");

            migrationBuilder.DropIndex(
                name: "IX_sales_invoices_AccountingVoucherId",
                schema: "sales",
                table: "sales_invoices");

            migrationBuilder.DropIndex(
                name: "IX_sales_invoices_WarehouseId",
                schema: "sales",
                table: "sales_invoices");
        }
    }
}
