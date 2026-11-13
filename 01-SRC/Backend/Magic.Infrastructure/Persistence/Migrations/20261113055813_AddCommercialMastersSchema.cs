using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Magic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialMastersSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "commercial");

            migrationBuilder.CreateTable(
                name: "brands",
                schema: "commercial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customer_groups",
                schema: "commercial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DefaultDiscountPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DefaultCreditLimitPaise = table.Column<long>(type: "bigint", nullable: false),
                    DefaultCreditDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                schema: "commercial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                schema: "commercial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GstType = table.Column<int>(type: "integer", nullable: false),
                    Gstin = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Pan = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ContactPerson = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PaymentTerms = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreditPeriodDays = table.Column<int>(type: "integer", nullable: false),
                    MsmeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UdyamRegistrationNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsMsmeRegistered = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    GlAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tax_codes",
                schema: "commercial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HsnSacCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RatePercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CgstPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    SgstPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    IgstPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CessPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tax_codes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "units_of_measure",
                schema: "commercial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Precision = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_units_of_measure", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "commercial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    CustomerType = table.Column<int>(type: "integer", nullable: false),
                    GstType = table.Column<int>(type: "integer", nullable: false),
                    Gstin = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Pan = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ContactPerson = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CustomerGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedBranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreditLimitPaise = table.Column<long>(type: "bigint", nullable: false),
                    CreditPeriodDays = table.Column<int>(type: "integer", nullable: false),
                    IsCreditHold = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    GlAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customers_customer_groups_CustomerGroupId",
                        column: x => x.CustomerGroupId,
                        principalSchema: "commercial",
                        principalTable: "customer_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "commercial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ParentCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    HsnCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DefaultGstRate = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_categories_categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalSchema: "commercial",
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_categories_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "commercial",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "supplier_addresses",
                schema: "commercial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddressType = table.Column<int>(type: "integer", nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    AddressLine2 = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    PinCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Country = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supplier_addresses_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "commercial",
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "supplier_bank_accounts",
                schema: "commercial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    BranchName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    AccountNumberEncrypted = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AccountNumberMasked = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IfscCode = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    AccountType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_bank_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supplier_bank_accounts_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "commercial",
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_addresses",
                schema: "commercial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddressType = table.Column<int>(type: "integer", nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    AddressLine2 = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    District = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    PinCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Country = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsDefaultBilling = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefaultShipping = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_addresses_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "commercial",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "products",
                schema: "commercial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProductType = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    HsnSacCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TaxCodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    GstRatePercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrimaryBarcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PurchasePricePaise = table.Column<long>(type: "bigint", nullable: false),
                    CostPricePaise = table.Column<long>(type: "bigint", nullable: false),
                    SellingPricePaise = table.Column<long>(type: "bigint", nullable: false),
                    MrpPaise = table.Column<long>(type: "bigint", nullable: false),
                    MinSellingPricePaise = table.Column<long>(type: "bigint", nullable: false),
                    WholesalePricePaise = table.Column<long>(type: "bigint", nullable: false),
                    ReorderLevel = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    MinStockLevel = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    MaxStockLevel = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HasVariants = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_products_brands_BrandId",
                        column: x => x.BrandId,
                        principalSchema: "commercial",
                        principalTable: "brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_products_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "commercial",
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_products_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "commercial",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_products_tax_codes_TaxCodeId",
                        column: x => x.TaxCodeId,
                        principalSchema: "commercial",
                        principalTable: "tax_codes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_products_units_of_measure_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalSchema: "commercial",
                        principalTable: "units_of_measure",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_price_history",
                schema: "commercial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OldPricePaise = table.Column<long>(type: "bigint", nullable: false),
                    NewPricePaise = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_price_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_price_history_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_variants",
                schema: "commercial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VariantName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AttributesJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SellingPricePaise = table.Column<long>(type: "bigint", nullable: false),
                    MrpPaise = table.Column<long>(type: "bigint", nullable: false),
                    PurchasePricePaise = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_variants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_variants_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_barcodes",
                schema: "commercial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BarcodeType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_barcodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_barcodes_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "commercial",
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_barcodes_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_prices",
                schema: "commercial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    PriceTier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MinQuantity = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    UnitPricePaise = table.Column<long>(type: "bigint", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    EffectiveFromUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveToUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_prices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_prices_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "commercial",
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_prices_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_brands_CompanyId",
                schema: "commercial",
                table: "brands",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_brands_CompanyId_Code",
                schema: "commercial",
                table: "brands",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_CompanyId",
                schema: "commercial",
                table: "categories",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_categories_CompanyId_Code",
                schema: "commercial",
                table: "categories",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_DepartmentId",
                schema: "commercial",
                table: "categories",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_categories_ParentCategoryId",
                schema: "commercial",
                table: "categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_addresses_CustomerId",
                schema: "commercial",
                table: "customer_addresses",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_groups_CompanyId",
                schema: "commercial",
                table: "customer_groups",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_groups_CompanyId_Code",
                schema: "commercial",
                table: "customer_groups",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_CompanyId_CustomerCode",
                schema: "commercial",
                table: "customers",
                columns: new[] { "CompanyId", "CustomerCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_CompanyId_Gstin",
                schema: "commercial",
                table: "customers",
                columns: new[] { "CompanyId", "Gstin" },
                filter: "\"Gstin\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customers_CompanyId_Name",
                schema: "commercial",
                table: "customers",
                columns: new[] { "CompanyId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_customers_CompanyId_Phone",
                schema: "commercial",
                table: "customers",
                columns: new[] { "CompanyId", "Phone" });

            migrationBuilder.CreateIndex(
                name: "IX_customers_CustomerGroupId",
                schema: "commercial",
                table: "customers",
                column: "CustomerGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_departments_CompanyId",
                schema: "commercial",
                table: "departments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_departments_CompanyId_Code",
                schema: "commercial",
                table: "departments",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_barcodes_CompanyId_Barcode",
                schema: "commercial",
                table: "product_barcodes",
                columns: new[] { "CompanyId", "Barcode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_barcodes_ProductId",
                schema: "commercial",
                table: "product_barcodes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_barcodes_VariantId",
                schema: "commercial",
                table: "product_barcodes",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_product_price_history_CompanyId_ChangedAtUtc",
                schema: "commercial",
                table: "product_price_history",
                columns: new[] { "CompanyId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_product_price_history_ProductId",
                schema: "commercial",
                table: "product_price_history",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_prices_CompanyId_ProductId_PriceTier",
                schema: "commercial",
                table: "product_prices",
                columns: new[] { "CompanyId", "ProductId", "PriceTier" });

            migrationBuilder.CreateIndex(
                name: "IX_product_prices_ProductId",
                schema: "commercial",
                table: "product_prices",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_prices_VariantId",
                schema: "commercial",
                table: "product_prices",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_ProductId",
                schema: "commercial",
                table: "product_variants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_Sku",
                schema: "commercial",
                table: "product_variants",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_BrandId",
                schema: "commercial",
                table: "products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_products_CategoryId",
                schema: "commercial",
                table: "products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_products_CompanyId_Name",
                schema: "commercial",
                table: "products",
                columns: new[] { "CompanyId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_products_CompanyId_PrimaryBarcode",
                schema: "commercial",
                table: "products",
                columns: new[] { "CompanyId", "PrimaryBarcode" },
                unique: true,
                filter: "\"PrimaryBarcode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_products_CompanyId_ProductCode",
                schema: "commercial",
                table: "products",
                columns: new[] { "CompanyId", "ProductCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_CompanyId_Sku",
                schema: "commercial",
                table: "products",
                columns: new[] { "CompanyId", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_DepartmentId",
                schema: "commercial",
                table: "products",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_products_TaxCodeId",
                schema: "commercial",
                table: "products",
                column: "TaxCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_products_UnitOfMeasureId",
                schema: "commercial",
                table: "products",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_addresses_SupplierId",
                schema: "commercial",
                table: "supplier_addresses",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_bank_accounts_SupplierId",
                schema: "commercial",
                table: "supplier_bank_accounts",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_CompanyId_Gstin",
                schema: "commercial",
                table: "suppliers",
                columns: new[] { "CompanyId", "Gstin" },
                filter: "\"Gstin\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_CompanyId_Name",
                schema: "commercial",
                table: "suppliers",
                columns: new[] { "CompanyId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_CompanyId_SupplierCode",
                schema: "commercial",
                table: "suppliers",
                columns: new[] { "CompanyId", "SupplierCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tax_codes_CompanyId",
                schema: "commercial",
                table: "tax_codes",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_tax_codes_CompanyId_Code",
                schema: "commercial",
                table: "tax_codes",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_units_of_measure_CompanyId",
                schema: "commercial",
                table: "units_of_measure",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_units_of_measure_CompanyId_Code",
                schema: "commercial",
                table: "units_of_measure",
                columns: new[] { "CompanyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_addresses",
                schema: "commercial");

            migrationBuilder.DropTable(
                name: "product_barcodes",
                schema: "commercial");

            migrationBuilder.DropTable(
                name: "product_price_history",
                schema: "commercial");

            migrationBuilder.DropTable(
                name: "product_prices",
                schema: "commercial");

            migrationBuilder.DropTable(
                name: "supplier_addresses",
                schema: "commercial");

            migrationBuilder.DropTable(
                name: "supplier_bank_accounts",
                schema: "commercial");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "commercial");

            migrationBuilder.DropTable(
                name: "product_variants",
                schema: "commercial");

            migrationBuilder.DropTable(
                name: "suppliers",
                schema: "commercial");

            migrationBuilder.DropTable(
                name: "customer_groups",
                schema: "commercial");

            migrationBuilder.DropTable(
                name: "products",
                schema: "commercial");

            migrationBuilder.DropTable(
                name: "brands",
                schema: "commercial");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "commercial");

            migrationBuilder.DropTable(
                name: "tax_codes",
                schema: "commercial");

            migrationBuilder.DropTable(
                name: "units_of_measure",
                schema: "commercial");

            migrationBuilder.DropTable(
                name: "departments",
                schema: "commercial");
        }
    }
}
