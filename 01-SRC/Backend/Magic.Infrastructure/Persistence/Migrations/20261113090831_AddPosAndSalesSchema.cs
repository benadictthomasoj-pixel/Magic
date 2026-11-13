using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Magic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosAndSalesSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cashier");

            migrationBuilder.EnsureSchema(
                name: "sales");

            migrationBuilder.CreateTable(
                name: "cashier_sessions",
                schema: "cashier",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    TerminalId = table.Column<Guid>(type: "uuid", nullable: true),
                    CashierUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CashierUsername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OpenedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OpeningCashPaise = table.Column<long>(type: "bigint", nullable: false),
                    CashSalesPaise = table.Column<long>(type: "bigint", nullable: false),
                    CashRefundsPaise = table.Column<long>(type: "bigint", nullable: false),
                    CashDepositsPaise = table.Column<long>(type: "bigint", nullable: false),
                    CashWithdrawalsPaise = table.Column<long>(type: "bigint", nullable: false),
                    ExpectedCashPaise = table.Column<long>(type: "bigint", nullable: false),
                    ActualClosingCashPaise = table.Column<long>(type: "bigint", nullable: true),
                    VariancePaise = table.Column<long>(type: "bigint", nullable: true),
                    ClosingNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SupervisorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cashier_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cash_movements",
                schema: "cashier",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CashierSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MovementType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AmountPaise = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    MovementDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CashierUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cash_movements_cashier_sessions_CashierSessionId",
                        column: x => x.CashierSessionId,
                        principalSchema: "cashier",
                        principalTable: "cashier_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_invoices",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CashierSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CashierUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SubtotalPaise = table.Column<long>(type: "bigint", nullable: false),
                    DiscountPaise = table.Column<long>(type: "bigint", nullable: false),
                    CgstPaise = table.Column<long>(type: "bigint", nullable: false),
                    SgstPaise = table.Column<long>(type: "bigint", nullable: false),
                    IgstPaise = table.Column<long>(type: "bigint", nullable: false),
                    TotalTaxPaise = table.Column<long>(type: "bigint", nullable: false),
                    GrandTotalPaise = table.Column<long>(type: "bigint", nullable: false),
                    AmountPaidPaise = table.Column<long>(type: "bigint", nullable: false),
                    OutstandingBalancePaise = table.Column<long>(type: "bigint", nullable: false),
                    PlaceOfSupplyStateCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CustomerGstin = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    SellerGstin = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_invoices_cashier_sessions_CashierSessionId",
                        column: x => x.CashierSessionId,
                        principalSchema: "cashier",
                        principalTable: "cashier_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_sales_invoices_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "commercial",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "sales_invoice_lines",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HsnCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    UnitPricePaise = table.Column<long>(type: "bigint", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DiscountPaise = table.Column<long>(type: "bigint", nullable: false),
                    GstRatePercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CgstPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    SgstPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    IgstPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    TaxAmountPaise = table.Column<long>(type: "bigint", nullable: false),
                    LineTotalPaise = table.Column<long>(type: "bigint", nullable: false),
                    StorageLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_invoice_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_invoice_lines_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_invoice_lines_sales_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "sales",
                        principalTable: "sales_invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_payments",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CashierSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentMethod = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AmountPaise = table.Column<long>(type: "bigint", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaymentDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CashierUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_payments_cashier_sessions_CashierSessionId",
                        column: x => x.CashierSessionId,
                        principalSchema: "cashier",
                        principalTable: "cashier_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_sales_payments_sales_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "sales",
                        principalTable: "sales_invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_returns",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CashierSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CashierUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReasonCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalRefundPaise = table.Column<long>(type: "bigint", nullable: false),
                    CreditNoteNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_returns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_returns_cashier_sessions_CashierSessionId",
                        column: x => x.CashierSessionId,
                        principalSchema: "cashier",
                        principalTable: "cashier_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_sales_returns_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "commercial",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_sales_returns_sales_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "sales",
                        principalTable: "sales_invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credit_notes",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditNoteNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SalesReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    IssueDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AmountPaise = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_credit_notes_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "commercial",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_credit_notes_sales_returns_SalesReturnId",
                        column: x => x.SalesReturnId,
                        principalSchema: "sales",
                        principalTable: "sales_returns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_return_lines",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReturnQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    UnitPricePaise = table.Column<long>(type: "bigint", nullable: false),
                    GstRatePercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    TaxAmountPaise = table.Column<long>(type: "bigint", nullable: false),
                    TotalAmountPaise = table.Column<long>(type: "bigint", nullable: false),
                    IsResalable = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_return_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_return_lines_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_return_lines_sales_invoice_lines_InvoiceLineId",
                        column: x => x.InvoiceLineId,
                        principalSchema: "sales",
                        principalTable: "sales_invoice_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_return_lines_sales_returns_ReturnId",
                        column: x => x.ReturnId,
                        principalSchema: "sales",
                        principalTable: "sales_returns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cash_movements_CashierSessionId",
                schema: "cashier",
                table: "cash_movements",
                column: "CashierSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_movements_CompanyId_BranchId",
                schema: "cashier",
                table: "cash_movements",
                columns: new[] { "CompanyId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_cashier_sessions_CashierUserId",
                schema: "cashier",
                table: "cashier_sessions",
                column: "CashierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_cashier_sessions_CompanyId_BranchId",
                schema: "cashier",
                table: "cashier_sessions",
                columns: new[] { "CompanyId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_cashier_sessions_Status",
                schema: "cashier",
                table: "cashier_sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_credit_notes_CompanyId_BranchId",
                schema: "sales",
                table: "credit_notes",
                columns: new[] { "CompanyId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_credit_notes_CompanyId_CreditNoteNumber",
                schema: "sales",
                table: "credit_notes",
                columns: new[] { "CompanyId", "CreditNoteNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_credit_notes_CustomerId",
                schema: "sales",
                table: "credit_notes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_credit_notes_SalesReturnId",
                schema: "sales",
                table: "credit_notes",
                column: "SalesReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoice_lines_InvoiceId",
                schema: "sales",
                table: "sales_invoice_lines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoice_lines_ProductId",
                schema: "sales",
                table: "sales_invoice_lines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoices_CashierSessionId",
                schema: "sales",
                table: "sales_invoices",
                column: "CashierSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoices_CompanyId_BranchId",
                schema: "sales",
                table: "sales_invoices",
                columns: new[] { "CompanyId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoices_CompanyId_InvoiceNumber",
                schema: "sales",
                table: "sales_invoices",
                columns: new[] { "CompanyId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoices_CustomerId",
                schema: "sales",
                table: "sales_invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoices_InvoiceDateUtc",
                schema: "sales",
                table: "sales_invoices",
                column: "InvoiceDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payments_CashierSessionId",
                schema: "sales",
                table: "sales_payments",
                column: "CashierSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payments_CompanyId_BranchId",
                schema: "sales",
                table: "sales_payments",
                columns: new[] { "CompanyId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_payments_InvoiceId",
                schema: "sales",
                table: "sales_payments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payments_PaymentDateUtc",
                schema: "sales",
                table: "sales_payments",
                column: "PaymentDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_sales_return_lines_InvoiceLineId",
                schema: "sales",
                table: "sales_return_lines",
                column: "InvoiceLineId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_return_lines_ProductId",
                schema: "sales",
                table: "sales_return_lines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_return_lines_ReturnId",
                schema: "sales",
                table: "sales_return_lines",
                column: "ReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_returns_CashierSessionId",
                schema: "sales",
                table: "sales_returns",
                column: "CashierSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_returns_CompanyId_BranchId",
                schema: "sales",
                table: "sales_returns",
                columns: new[] { "CompanyId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_returns_CompanyId_ReturnNumber",
                schema: "sales",
                table: "sales_returns",
                columns: new[] { "CompanyId", "ReturnNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_returns_CustomerId",
                schema: "sales",
                table: "sales_returns",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_returns_InvoiceId",
                schema: "sales",
                table: "sales_returns",
                column: "InvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cash_movements",
                schema: "cashier");

            migrationBuilder.DropTable(
                name: "credit_notes",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_payments",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_return_lines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_invoice_lines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_returns",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_invoices",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "cashier_sessions",
                schema: "cashier");
        }
    }
}
