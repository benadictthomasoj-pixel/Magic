using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Magic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingSchemaAndLedgerTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "accounting");

            migrationBuilder.CreateTable(
                name: "account_groups",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    group_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    root_class = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    hierarchy_depth = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_groups", x => x.id);
                    table.CheckConstraint("chk_account_groups_hierarchy", "id <> parent_group_id");
                    table.CheckConstraint("chk_account_groups_root_class", "root_class IN ('Asset', 'Liability', 'Equity', 'Revenue', 'Expense', 'ASSET', 'LIABILITY', 'EQUITY', 'REVENUE', 'EXPENSE')");
                    table.ForeignKey(
                        name: "FK_account_groups_account_groups_parent_group_id",
                        column: x => x.parent_group_id,
                        principalSchema: "accounting",
                        principalTable: "account_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "account_period_balances",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opening_debit_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    opening_credit_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    movement_debit_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    movement_credit_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    closing_debit_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    closing_credit_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    last_updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_period_balances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ap_allocations",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    debit_voucher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    allocated_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ap_allocations", x => x.id);
                    table.CheckConstraint("chk_ap_alloc_positive", "allocated_amount_paise > 0");
                });

            migrationBuilder.CreateTable(
                name: "ap_subledger_items",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    voucher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bill_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    original_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    settled_amount_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    outstanding_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "OPEN"),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ap_subledger_items", x => x.id);
                    table.CheckConstraint("chk_ap_amounts", "settled_amount_paise + outstanding_amount_paise = original_amount_paise");
                    table.CheckConstraint("chk_ap_status", "status IN ('OPEN', 'PARTIALLY_PAID', 'PAID')");
                });

            migrationBuilder.CreateTable(
                name: "ar_allocations",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    debit_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_voucher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    allocated_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ar_allocations", x => x.id);
                    table.CheckConstraint("chk_ar_alloc_positive", "allocated_amount_paise > 0");
                });

            migrationBuilder.CreateTable(
                name: "ar_subledger_items",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    voucher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    original_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    settled_amount_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    outstanding_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "OPEN"),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ar_subledger_items", x => x.id);
                    table.CheckConstraint("chk_ar_amounts", "settled_amount_paise + outstanding_amount_paise = original_amount_paise");
                    table.CheckConstraint("chk_ar_status", "status IN ('OPEN', 'PARTIALLY_PAID', 'PAID')");
                });

            migrationBuilder.CreateTable(
                name: "fiscal_years",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    closed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiscal_years", x => x.id);
                    table.CheckConstraint("chk_fiscal_years_dates", "end_date > start_date");
                });

            migrationBuilder.CreateTable(
                name: "voucher_sequences",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    voucher_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    current_sequence = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_voucher_sequences", x => x.id);
                    table.CheckConstraint("chk_voucher_sequences_positive", "current_sequence >= 0");
                });

            migrationBuilder.CreateTable(
                name: "accounts",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    account_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    root_class = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    normal_balance = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_control_account = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    control_account_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    allow_direct_posting = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_system_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounts", x => x.id);
                    table.CheckConstraint("chk_accounts_normal_balance", "normal_balance IN ('Debit', 'Credit', 'DEBIT', 'CREDIT')");
                    table.CheckConstraint("chk_accounts_root_class", "root_class IN ('Asset', 'Liability', 'Equity', 'Revenue', 'Expense', 'ASSET', 'LIABILITY', 'EQUITY', 'REVENUE', 'EXPENSE')");
                    table.ForeignKey(
                        name: "FK_accounts_account_groups_account_group_id",
                        column: x => x.account_group_id,
                        principalSchema: "accounting",
                        principalTable: "account_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fiscal_periods",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_number = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    locked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    locked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiscal_periods", x => x.id);
                    table.CheckConstraint("chk_fiscal_periods_dates", "end_date >= start_date");
                    table.CheckConstraint("chk_fiscal_periods_state", "state IN ('Open', 'Closing', 'Locked', 'OPEN', 'CLOSING', 'LOCKED')");
                    table.ForeignKey(
                        name: "FK_fiscal_periods_fiscal_years_fiscal_year_id",
                        column: x => x.fiscal_year_id,
                        principalSchema: "accounting",
                        principalTable: "fiscal_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vouchers",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    voucher_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    voucher_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    posting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reference_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    narration = table.Column<string>(type: "text", nullable: false),
                    is_reversed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    reversal_voucher_id = table.Column<Guid>(type: "uuid", nullable: true),
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: false),
                    source_module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    source_transaction_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    source_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    posted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    supervisor_approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    trace_correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_debit_paise = table.Column<long>(type: "bigint", nullable: false),
                    total_credit_paise = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vouchers", x => x.id);
                    table.CheckConstraint("chk_vouchers_balance", "total_debit_paise = total_credit_paise");
                    table.CheckConstraint("chk_vouchers_positive_totals", "total_debit_paise > 0 AND total_credit_paise > 0");
                    table.CheckConstraint("chk_vouchers_status", "status IN ('Draft', 'Approved', 'Posted', 'Reversed', 'Cancelled', 'DRAFT', 'APPROVED', 'POSTED', 'REVERSED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_vouchers_fiscal_periods_fiscal_period_id",
                        column: x => x.fiscal_period_id,
                        principalSchema: "accounting",
                        principalTable: "fiscal_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vouchers_vouchers_reversal_voucher_id",
                        column: x => x.reversal_voucher_id,
                        principalSchema: "accounting",
                        principalTable: "vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journal_lines",
                schema: "accounting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    voucher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    debit_paise = table.Column<long>(type: "bigint", nullable: false),
                    credit_paise = table.Column<long>(type: "bigint", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    cost_center_id = table.Column<Guid>(type: "uuid", nullable: true),
                    party_id = table.Column<Guid>(type: "uuid", nullable: true),
                    party_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journal_lines", x => x.id);
                    table.CheckConstraint("chk_journal_lines_credit_non_negative", "credit_paise >= 0");
                    table.CheckConstraint("chk_journal_lines_debit_non_negative", "debit_paise >= 0");
                    table.CheckConstraint("chk_journal_lines_exclusive_side", "(debit_paise > 0 AND credit_paise = 0) OR (credit_paise > 0 AND debit_paise = 0)");
                    table.ForeignKey(
                        name: "FK_journal_lines_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "accounting",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_journal_lines_vouchers_voucher_id",
                        column: x => x.voucher_id,
                        principalSchema: "accounting",
                        principalTable: "vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_account_groups_parent",
                schema: "accounting",
                table: "account_groups",
                column: "parent_group_id");

            migrationBuilder.CreateIndex(
                name: "uq_account_groups_company_code",
                schema: "accounting",
                table: "account_groups",
                columns: new[] { "company_id", "group_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_account_period_balances_account_period_branch",
                schema: "accounting",
                table: "account_period_balances",
                columns: new[] { "account_id", "fiscal_period_id", "branch_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounts_control",
                schema: "accounting",
                table: "accounts",
                columns: new[] { "company_id", "is_control_account", "control_account_type" });

            migrationBuilder.CreateIndex(
                name: "ix_accounts_group",
                schema: "accounting",
                table: "accounts",
                column: "account_group_id");

            migrationBuilder.CreateIndex(
                name: "uq_accounts_company_code",
                schema: "accounting",
                table: "accounts",
                columns: new[] { "company_id", "account_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ap_aging",
                schema: "accounting",
                table: "ap_subledger_items",
                columns: new[] { "supplier_id", "due_date" },
                filter: "status <> 'PAID'");

            migrationBuilder.CreateIndex(
                name: "ix_ap_supplier_status",
                schema: "accounting",
                table: "ap_subledger_items",
                columns: new[] { "supplier_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_ar_aging",
                schema: "accounting",
                table: "ar_subledger_items",
                columns: new[] { "customer_id", "due_date" },
                filter: "status <> 'PAID'");

            migrationBuilder.CreateIndex(
                name: "ix_ar_customer_status",
                schema: "accounting",
                table: "ar_subledger_items",
                columns: new[] { "customer_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_fiscal_periods_dates",
                schema: "accounting",
                table: "fiscal_periods",
                columns: new[] { "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "uq_fiscal_periods_year_period",
                schema: "accounting",
                table: "fiscal_periods",
                columns: new[] { "fiscal_year_id", "period_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fiscal_years_company_dates",
                schema: "accounting",
                table: "fiscal_years",
                columns: new[] { "company_id", "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "uq_fiscal_years_company_code",
                schema: "accounting",
                table: "fiscal_years",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_journal_lines_account_branch",
                schema: "accounting",
                table: "journal_lines",
                columns: new[] { "account_id", "branch_id" });

            migrationBuilder.CreateIndex(
                name: "ix_journal_lines_party",
                schema: "accounting",
                table: "journal_lines",
                columns: new[] { "party_type", "party_id" },
                filter: "party_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_journal_lines_voucher",
                schema: "accounting",
                table: "journal_lines",
                column: "voucher_id");

            migrationBuilder.CreateIndex(
                name: "uq_journal_lines_voucher_line",
                schema: "accounting",
                table: "journal_lines",
                columns: new[] { "voucher_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_voucher_sequences_branch_fy_type",
                schema: "accounting",
                table: "voucher_sequences",
                columns: new[] { "branch_id", "fiscal_year_id", "voucher_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vouchers_period_date",
                schema: "accounting",
                table: "vouchers",
                columns: new[] { "fiscal_period_id", "posting_date" });

            migrationBuilder.CreateIndex(
                name: "ix_vouchers_reversal",
                schema: "accounting",
                table: "vouchers",
                column: "reversal_voucher_id",
                filter: "reversal_voucher_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_vouchers_source_document",
                schema: "accounting",
                table: "vouchers",
                columns: new[] { "source_module", "source_document_id" },
                filter: "source_module IS NOT NULL AND source_document_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_vouchers_branch_idempotency",
                schema: "accounting",
                table: "vouchers",
                columns: new[] { "branch_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_vouchers_branch_number",
                schema: "accounting",
                table: "vouchers",
                columns: new[] { "branch_id", "voucher_number" },
                unique: true);

            // Database Financial Integrity Trigger 1: Immutability of Posted/Reversed Journal Lines
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION accounting.fn_prevent_posted_journal_modification()
                RETURNS TRIGGER AS $$
                DECLARE
                    v_status VARCHAR(20);
                BEGIN
                    SELECT status INTO v_status FROM accounting.vouchers WHERE id = OLD.voucher_id;
                    IF v_status IN ('POSTED', 'REVERSED', 'Posted', 'Reversed') THEN
                        RAISE EXCEPTION 'DATABASE_FINANCIAL_INVARIANT_VIOLATION: Cannot modify or delete journal lines of a POSTED or REVERSED voucher (Voucher ID: %).', OLD.voucher_id
                            USING ERRCODE = '55000';
                    END IF;
                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    ELSE
                        RETURN NEW;
                    END IF;
                END;
                $$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS trg_protect_posted_journal_lines ON accounting.journal_lines;

                CREATE TRIGGER trg_protect_posted_journal_lines
                BEFORE UPDATE OR DELETE ON accounting.journal_lines
                FOR EACH ROW
                EXECUTE FUNCTION accounting.fn_prevent_posted_journal_modification();
                """);

            // Database Financial Integrity Trigger 2: Immutability of Posted Voucher Headers
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION accounting.fn_protect_posted_voucher_header()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        IF OLD.status IN ('POSTED', 'REVERSED', 'Posted', 'Reversed') THEN
                            RAISE EXCEPTION 'DATABASE_FINANCIAL_INVARIANT_VIOLATION: Cannot delete POSTED or REVERSED voucher (Voucher Number: %). Records are permanently immutable.', OLD.voucher_number
                                USING ERRCODE = '55000';
                        END IF;
                        RETURN OLD;
                    END IF;

                    IF TG_OP = 'UPDATE' THEN
                        IF OLD.status IN ('POSTED', 'REVERSED', 'Posted', 'Reversed') THEN
                            IF OLD.total_debit_paise <> NEW.total_debit_paise OR
                               OLD.total_credit_paise <> NEW.total_credit_paise OR
                               OLD.posting_date <> NEW.posting_date OR
                               OLD.fiscal_period_id <> NEW.fiscal_period_id OR
                               OLD.branch_id <> NEW.branch_id OR
                               OLD.company_id <> NEW.company_id OR
                               OLD.voucher_number <> NEW.voucher_number THEN
                                RAISE EXCEPTION 'DATABASE_FINANCIAL_INVARIANT_VIOLATION: Financial and governance fields of POSTED voucher % are permanently immutable.', OLD.voucher_number
                                    USING ERRCODE = '55000';
                            END IF;

                            IF OLD.status IN ('POSTED', 'Posted') AND NEW.status NOT IN ('POSTED', 'REVERSED', 'Posted', 'Reversed') THEN
                                RAISE EXCEPTION 'DATABASE_FINANCIAL_INVARIANT_VIOLATION: Cannot transition POSTED voucher to status %.', NEW.status
                                    USING ERRCODE = '55000';
                            END IF;

                            IF OLD.status IN ('REVERSED', 'Reversed') AND NEW.status NOT IN ('REVERSED', 'Reversed') THEN
                                RAISE EXCEPTION 'DATABASE_FINANCIAL_INVARIANT_VIOLATION: Status of REVERSED voucher cannot be modified.'
                                    USING ERRCODE = '55000';
                            END IF;
                        END IF;

                        RETURN NEW;
                    END IF;

                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS trg_protect_posted_voucher_header ON accounting.vouchers;

                CREATE TRIGGER trg_protect_posted_voucher_header
                BEFORE UPDATE OR DELETE ON accounting.vouchers
                FOR EACH ROW
                EXECUTE FUNCTION accounting.fn_protect_posted_voucher_header();
                """);

            // Database Financial Integrity Trigger 3: Cross-Row Double-Entry Aggregate Verification
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION accounting.fn_verify_posted_voucher_balance()
                RETURNS TRIGGER AS $$
                DECLARE
                    v_sum_debit BIGINT;
                    v_sum_credit BIGINT;
                    v_line_count INT;
                BEGIN
                    IF NEW.status IN ('POSTED', 'Posted', 'REVERSED', 'Reversed') THEN
                        SELECT COALESCE(SUM(debit_paise), 0), COALESCE(SUM(credit_paise), 0), COUNT(*)
                        INTO v_sum_debit, v_sum_credit, v_line_count
                        FROM accounting.journal_lines
                        WHERE voucher_id = NEW.id;

                        IF v_line_count < 2 THEN
                            RAISE EXCEPTION 'DATABASE_DOUBLE_ENTRY_VIOLATION: Posted voucher % requires at least 2 journal lines. Found: % lines.', NEW.voucher_number, v_line_count
                                USING ERRCODE = '23514';
                        END IF;

                        IF v_sum_debit <> v_sum_credit THEN
                            RAISE EXCEPTION 'DATABASE_DOUBLE_ENTRY_VIOLATION: Posted voucher % is unbalanced: SUM(Debit) = % paise, SUM(Credit) = % paise.', NEW.voucher_number, v_sum_debit, v_sum_credit
                                USING ERRCODE = '23514';
                        END IF;

                        IF v_sum_debit <= 0 THEN
                            RAISE EXCEPTION 'DATABASE_DOUBLE_ENTRY_VIOLATION: Posted voucher % has non-positive financial balance (Debit = %).', NEW.voucher_number, v_sum_debit
                                USING ERRCODE = '23514';
                        END IF;

                        IF v_sum_debit <> NEW.total_debit_paise OR v_sum_credit <> NEW.total_credit_paise THEN
                            RAISE EXCEPTION 'DATABASE_DOUBLE_ENTRY_VIOLATION: Voucher header totals (%/%) do not match sum of journal lines (%/%).', NEW.total_debit_paise, NEW.total_credit_paise, v_sum_debit, v_sum_credit
                                USING ERRCODE = '23514';
                        END IF;
                    END IF;

                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS trg_verify_posted_voucher_balance ON accounting.vouchers;

                CREATE CONSTRAINT TRIGGER trg_verify_posted_voucher_balance
                AFTER INSERT OR UPDATE ON accounting.vouchers
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW
                EXECUTE FUNCTION accounting.fn_verify_posted_voucher_balance();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS trg_verify_posted_voucher_balance ON accounting.vouchers;
                DROP FUNCTION IF EXISTS accounting.fn_verify_posted_voucher_balance();

                DROP TRIGGER IF EXISTS trg_protect_posted_voucher_header ON accounting.vouchers;
                DROP FUNCTION IF EXISTS accounting.fn_protect_posted_voucher_header();

                DROP TRIGGER IF EXISTS trg_protect_posted_journal_lines ON accounting.journal_lines;
                DROP FUNCTION IF EXISTS accounting.fn_prevent_posted_journal_modification();
                """);
            migrationBuilder.DropTable(
                name: "account_period_balances",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "ap_allocations",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "ap_subledger_items",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "ar_allocations",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "ar_subledger_items",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "journal_lines",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "voucher_sequences",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "accounts",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "vouchers",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "account_groups",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "fiscal_periods",
                schema: "accounting");

            migrationBuilder.DropTable(
                name: "fiscal_years",
                schema: "accounting");
        }
    }
}
