using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Magic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialManagementAndTreasurySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.CreateTable(
                name: "bank_cash_accounts",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    account_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Bank"),
                    bank_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    branch_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    account_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    masked_account_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ifsc_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    gl_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opening_balance_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    current_balance_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_cash_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_bank_cash_accounts_accounts_gl_account_id",
                        column: x => x.gl_account_id,
                        principalSchema: "accounting",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_invoices",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    system_invoice_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    invoice_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    po_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grn_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sub_total_paise = table.Column<long>(type: "bigint", nullable: false),
                    tax_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    total_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    settled_amount_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    outstanding_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "APPROVED"),
                    voucher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_invoices", x => x.id);
                    table.CheckConstraint("chk_supplier_invoice_amounts", "settled_amount_paise + outstanding_amount_paise = total_amount_paise");
                    table.CheckConstraint("chk_supplier_invoice_positive", "total_amount_paise > 0");
                    table.ForeignKey(
                        name: "FK_supplier_invoices_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "commercial",
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_invoices_vouchers_voucher_id",
                        column: x => x.voucher_id,
                        principalSchema: "accounting",
                        principalTable: "vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bank_statements",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    statement_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    statement_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    opening_balance_paise = table.Column<long>(type: "bigint", nullable: false),
                    closing_balance_paise = table.Column<long>(type: "bigint", nullable: false),
                    filename = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "OPEN"),
                    imported_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    imported_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_statements", x => x.id);
                    table.ForeignKey(
                        name: "FK_bank_statements_bank_cash_accounts_bank_account_id",
                        column: x => x.bank_account_id,
                        principalSchema: "finance",
                        principalTable: "bank_cash_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_receipts",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_date = table.Column<DateOnly>(type: "date", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bank_cash_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    allocated_amount_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    unallocated_amount_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "SUBMITTED"),
                    voucher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    narration = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_receipts", x => x.id);
                    table.CheckConstraint("chk_receipt_amounts", "allocated_amount_paise + unallocated_amount_paise = total_amount_paise");
                    table.CheckConstraint("chk_receipt_total_positive", "total_amount_paise > 0");
                    table.ForeignKey(
                        name: "FK_customer_receipts_bank_cash_accounts_bank_cash_account_id",
                        column: x => x.bank_cash_account_id,
                        principalSchema: "finance",
                        principalTable: "bank_cash_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_receipts_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "commercial",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_receipts_vouchers_voucher_id",
                        column: x => x.voucher_id,
                        principalSchema: "accounting",
                        principalTable: "vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_payments",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bank_cash_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    allocated_amount_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    unallocated_amount_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "SUBMITTED"),
                    voucher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    narration = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_payments", x => x.id);
                    table.CheckConstraint("chk_payment_amounts", "allocated_amount_paise + unallocated_amount_paise = total_amount_paise");
                    table.CheckConstraint("chk_payment_total_positive", "total_amount_paise > 0");
                    table.ForeignKey(
                        name: "FK_supplier_payments_bank_cash_accounts_bank_cash_account_id",
                        column: x => x.bank_cash_account_id,
                        principalSchema: "finance",
                        principalTable: "bank_cash_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_payments_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "commercial",
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_payments_vouchers_voucher_id",
                        column: x => x.voucher_id,
                        principalSchema: "accounting",
                        principalTable: "vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "treasury_transactions",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    source_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    narration = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "POSTED"),
                    voucher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_treasury_transactions", x => x.id);
                    table.CheckConstraint("chk_treasury_amount_positive", "amount_paise > 0");
                    table.ForeignKey(
                        name: "FK_treasury_transactions_bank_cash_accounts_destination_accoun~",
                        column: x => x.destination_account_id,
                        principalSchema: "finance",
                        principalTable: "bank_cash_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_treasury_transactions_bank_cash_accounts_source_account_id",
                        column: x => x.source_account_id,
                        principalSchema: "finance",
                        principalTable: "bank_cash_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_treasury_transactions_vouchers_voucher_id",
                        column: x => x.voucher_id,
                        principalSchema: "accounting",
                        principalTable: "vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_invoice_lines",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    unit_price_paise = table.Column<long>(type: "bigint", nullable: false),
                    tax_rate_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    tax_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    total_amount_paise = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_invoice_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_supplier_invoice_lines_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_invoice_lines_supplier_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "finance",
                        principalTable: "supplier_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bank_statement_lines",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    statement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    value_date = table.Column<DateOnly>(type: "date", nullable: true),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    withdrawal_debit_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    deposit_credit_paise = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    balance_paise = table.Column<long>(type: "bigint", nullable: false),
                    is_reconciled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    matched_voucher_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    matched_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    match_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_statement_lines", x => x.id);
                    table.CheckConstraint("chk_stmt_line_amounts", "withdrawal_debit_paise >= 0 AND deposit_credit_paise >= 0");
                    table.ForeignKey(
                        name: "FK_bank_statement_lines_bank_statements_statement_id",
                        column: x => x.statement_id,
                        principalSchema: "finance",
                        principalTable: "bank_statements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bank_statement_lines_journal_lines_matched_voucher_line_id",
                        column: x => x.matched_voucher_line_id,
                        principalSchema: "accounting",
                        principalTable: "journal_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "customer_receipt_allocations",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ar_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocated_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    allocation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_receipt_allocations", x => x.id);
                    table.CheckConstraint("chk_receipt_alloc_positive", "allocated_amount_paise > 0");
                    table.ForeignKey(
                        name: "FK_customer_receipt_allocations_ar_subledger_items_ar_item_id",
                        column: x => x.ar_item_id,
                        principalSchema: "accounting",
                        principalTable: "ar_subledger_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_receipt_allocations_customer_receipts_receipt_id",
                        column: x => x.receipt_id,
                        principalSchema: "finance",
                        principalTable: "customer_receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "supplier_payment_allocations",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ap_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocated_amount_paise = table.Column<long>(type: "bigint", nullable: false),
                    allocation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_payment_allocations", x => x.id);
                    table.CheckConstraint("chk_payment_alloc_positive", "allocated_amount_paise > 0");
                    table.ForeignKey(
                        name: "FK_supplier_payment_allocations_ap_subledger_items_ap_item_id",
                        column: x => x.ap_item_id,
                        principalSchema: "accounting",
                        principalTable: "ap_subledger_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_payment_allocations_supplier_payments_payment_id",
                        column: x => x.payment_id,
                        principalSchema: "finance",
                        principalTable: "supplier_payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bank_cash_accounts_company_branch",
                schema: "finance",
                table: "bank_cash_accounts",
                columns: new[] { "company_id", "branch_id", "account_name" });

            migrationBuilder.CreateIndex(
                name: "IX_bank_cash_accounts_gl_account_id",
                schema: "finance",
                table: "bank_cash_accounts",
                column: "gl_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_bank_statement_lines_matched_voucher_line_id",
                schema: "finance",
                table: "bank_statement_lines",
                column: "matched_voucher_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_bank_statement_lines_reconciled",
                schema: "finance",
                table: "bank_statement_lines",
                columns: new[] { "statement_id", "is_reconciled" });

            migrationBuilder.CreateIndex(
                name: "IX_bank_statements_bank_account_id",
                schema: "finance",
                table: "bank_statements",
                column: "bank_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_receipt_allocations_ar_item_id",
                schema: "finance",
                table: "customer_receipt_allocations",
                column: "ar_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_receipt_allocations_receipt_id",
                schema: "finance",
                table: "customer_receipt_allocations",
                column: "receipt_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_receipts_bank_cash_account_id",
                schema: "finance",
                table: "customer_receipts",
                column: "bank_cash_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_receipts_customer",
                schema: "finance",
                table: "customer_receipts",
                columns: new[] { "company_id", "branch_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_receipts_customer_id",
                schema: "finance",
                table: "customer_receipts",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_receipts_date",
                schema: "finance",
                table: "customer_receipts",
                columns: new[] { "company_id", "receipt_date" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_receipts_number",
                schema: "finance",
                table: "customer_receipts",
                column: "receipt_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_receipts_voucher_id",
                schema: "finance",
                table: "customer_receipts",
                column: "voucher_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoice_lines_invoice_id",
                schema: "finance",
                table: "supplier_invoice_lines",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoice_lines_product_id",
                schema: "finance",
                table: "supplier_invoice_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_invoices_due_date",
                schema: "finance",
                table: "supplier_invoices",
                columns: new[] { "company_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoices_supplier_id",
                schema: "finance",
                table: "supplier_invoices",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_invoices_supplier_number",
                schema: "finance",
                table: "supplier_invoices",
                columns: new[] { "company_id", "supplier_id", "invoice_number" });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_invoices_system_number",
                schema: "finance",
                table: "supplier_invoices",
                column: "system_invoice_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoices_voucher_id",
                schema: "finance",
                table: "supplier_invoices",
                column: "voucher_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_payment_allocations_ap_item_id",
                schema: "finance",
                table: "supplier_payment_allocations",
                column: "ap_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_payment_allocations_payment_id",
                schema: "finance",
                table: "supplier_payment_allocations",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_payments_bank_cash_account_id",
                schema: "finance",
                table: "supplier_payments",
                column: "bank_cash_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payments_date",
                schema: "finance",
                table: "supplier_payments",
                columns: new[] { "company_id", "payment_date" });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payments_number",
                schema: "finance",
                table: "supplier_payments",
                column: "payment_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payments_supplier",
                schema: "finance",
                table: "supplier_payments",
                columns: new[] { "company_id", "branch_id", "supplier_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_payments_supplier_id",
                schema: "finance",
                table: "supplier_payments",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_payments_voucher_id",
                schema: "finance",
                table: "supplier_payments",
                column: "voucher_id");

            migrationBuilder.CreateIndex(
                name: "ix_treasury_transactions_date",
                schema: "finance",
                table: "treasury_transactions",
                columns: new[] { "company_id", "transaction_date" });

            migrationBuilder.CreateIndex(
                name: "IX_treasury_transactions_destination_account_id",
                schema: "finance",
                table: "treasury_transactions",
                column: "destination_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_treasury_transactions_number",
                schema: "finance",
                table: "treasury_transactions",
                column: "transaction_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_treasury_transactions_source_account_id",
                schema: "finance",
                table: "treasury_transactions",
                column: "source_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_treasury_transactions_voucher_id",
                schema: "finance",
                table: "treasury_transactions",
                column: "voucher_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bank_statement_lines",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "customer_receipt_allocations",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "supplier_invoice_lines",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "supplier_payment_allocations",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "treasury_transactions",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "bank_statements",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "customer_receipts",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "supplier_invoices",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "supplier_payments",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "bank_cash_accounts",
                schema: "finance");
        }
    }
}
