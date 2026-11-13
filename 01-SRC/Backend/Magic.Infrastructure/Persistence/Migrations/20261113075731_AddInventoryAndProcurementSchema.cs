using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Magic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryAndProcurementSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.CreateTable(
                name: "warehouses",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AddressLine1 = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    AddressLine2 = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StateCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PinCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ManagerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CapacitySqFt = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SubtotalPaise = table.Column<long>(type: "bigint", nullable: false),
                    DiscountPaise = table.Column<long>(type: "bigint", nullable: false),
                    TaxableAmountPaise = table.Column<long>(type: "bigint", nullable: false),
                    CgstPaise = table.Column<long>(type: "bigint", nullable: false),
                    SgstPaise = table.Column<long>(type: "bigint", nullable: false),
                    IgstPaise = table.Column<long>(type: "bigint", nullable: false),
                    CessPaise = table.Column<long>(type: "bigint", nullable: false),
                    RoundOffPaise = table.Column<long>(type: "bigint", nullable: false),
                    TotalAmountPaise = table.Column<long>(type: "bigint", nullable: false),
                    PaymentTerms = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_orders_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "commercial",
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reorder_rules",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    PreferredSupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    MinStockLevel = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    MaxStockLevel = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    ReorderPoint = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    SafetyStock = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    LeadTimeDays = table.Column<int>(type: "integer", nullable: false),
                    ReorderQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reorder_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reorder_rules_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "commercial",
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reorder_rules_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reorder_rules_suppliers_PreferredSupplierId",
                        column: x => x.PreferredSupplierId,
                        principalSchema: "commercial",
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_reorder_rules_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_adjustments",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdjustmentNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AdjustmentDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReasonCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    TotalItemsCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_adjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_adjustments_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_counts",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CountNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CountDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CountedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReconciledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_counts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_counts_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_transfers",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceBranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationBranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TransferDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DispatchedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DispatchedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReceivedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TrackingNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CarrierName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_transfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_transfers_warehouses_DestinationWarehouseId",
                        column: x => x.DestinationWarehouseId,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transfers_warehouses_SourceWarehouseId",
                        column: x => x.SourceWarehouseId,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "storage_locations",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LocationType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ParentLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storage_locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_storage_locations_storage_locations_ParentLocationId",
                        column: x => x.ParentLocationId,
                        principalSchema: "inventory",
                        principalTable: "storage_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_storage_locations_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "warehouse_branch_assignments",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouse_branch_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_warehouse_branch_assignments_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "goods_receipt_notes",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrnNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierInvoiceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SupplierInvoiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ReceiptDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods_receipt_notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_goods_receipt_notes_purchase_orders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "inventory",
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_goods_receipt_notes_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "commercial",
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipt_notes_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_lines",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderedQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    CancelledQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    UnitPricePaise = table.Column<long>(type: "bigint", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    DiscountPaise = table.Column<long>(type: "bigint", nullable: false),
                    TaxCodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    GstRatePercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    TaxableAmountPaise = table.Column<long>(type: "bigint", nullable: false),
                    CgstPaise = table.Column<long>(type: "bigint", nullable: false),
                    SgstPaise = table.Column<long>(type: "bigint", nullable: false),
                    IgstPaise = table.Column<long>(type: "bigint", nullable: false),
                    TotalLineAmountPaise = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_order_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "commercial",
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_purchase_orders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "inventory",
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_storage_locations",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsPrimaryLocation = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_storage_locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_storage_locations_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "commercial",
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_storage_locations_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_storage_locations_storage_locations_StorageLocation~",
                        column: x => x.StorageLocationId,
                        principalSchema: "inventory",
                        principalTable: "storage_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_storage_locations_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_adjustment_lines",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockAdjustmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    StorageLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SystemQuantityOnHand = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    AdjustmentQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    NewQuantityOnHand = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    UnitCostPaise = table.Column<long>(type: "bigint", nullable: false),
                    LineReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_adjustment_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_adjustment_lines_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "commercial",
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_adjustment_lines_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_adjustment_lines_stock_adjustments_StockAdjustmentId",
                        column: x => x.StockAdjustmentId,
                        principalSchema: "inventory",
                        principalTable: "stock_adjustments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stock_adjustment_lines_storage_locations_StorageLocationId",
                        column: x => x.StorageLocationId,
                        principalSchema: "inventory",
                        principalTable: "storage_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_balances",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    StorageLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    QuantityOnHand = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    QuantityReserved = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    QuantityAvailable = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    QuantityDamaged = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    QuantityExpired = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    QuantityBlocked = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    UnitCostPaise = table.Column<long>(type: "bigint", nullable: false),
                    LastMovementAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_balances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_balances_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "commercial",
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_balances_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_balances_storage_locations_StorageLocationId",
                        column: x => x.StorageLocationId,
                        principalSchema: "inventory",
                        principalTable: "storage_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_balances_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_count_lines",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockCountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    StorageLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SystemQuantityOnHand = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    DiscrepancyQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    UnitCostPaise = table.Column<long>(type: "bigint", nullable: false),
                    DiscrepancyValuePaise = table.Column<long>(type: "bigint", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_count_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_count_lines_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "commercial",
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_count_lines_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_count_lines_stock_counts_StockCountId",
                        column: x => x.StockCountId,
                        principalSchema: "inventory",
                        principalTable: "stock_counts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stock_count_lines_storage_locations_StorageLocationId",
                        column: x => x.StorageLocationId,
                        principalSchema: "inventory",
                        principalTable: "storage_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    StorageLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    MovementType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    UnitCostPaise = table.Column<long>(type: "bigint", nullable: false),
                    TotalCostPaise = table.Column<long>(type: "bigint", nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BalanceAfterOnHand = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MovementDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_movements_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "commercial",
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_movements_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_movements_storage_locations_StorageLocationId",
                        column: x => x.StorageLocationId,
                        principalSchema: "inventory",
                        principalTable: "storage_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_movements_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_transfer_lines",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockTransferId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceStorageLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationStorageLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    DispatchedQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    DamagedQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    MissingQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    UnitCostPaise = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_transfer_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_transfer_lines_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "commercial",
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transfer_lines_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transfer_lines_stock_transfers_StockTransferId",
                        column: x => x.StockTransferId,
                        principalSchema: "inventory",
                        principalTable: "stock_transfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stock_transfer_lines_storage_locations_DestinationStorageLo~",
                        column: x => x.DestinationStorageLocationId,
                        principalSchema: "inventory",
                        principalTable: "storage_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transfer_lines_storage_locations_SourceStorageLocatio~",
                        column: x => x.SourceStorageLocationId,
                        principalSchema: "inventory",
                        principalTable: "storage_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_returns",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GoodsReceiptNoteId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierCreditNoteNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReturnDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TotalAmountPaise = table.Column<long>(type: "bigint", nullable: false),
                    ReturnReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_returns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_returns_goods_receipt_notes_GoodsReceiptNoteId",
                        column: x => x.GoodsReceiptNoteId,
                        principalSchema: "inventory",
                        principalTable: "goods_receipt_notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_purchase_returns_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "commercial",
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_returns_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "goods_receipt_lines",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    StorageLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    AcceptedQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    RejectedQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    DamagedQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    UnitPricePaise = table.Column<long>(type: "bigint", nullable: false),
                    TaxableAmountPaise = table.Column<long>(type: "bigint", nullable: false),
                    CgstPaise = table.Column<long>(type: "bigint", nullable: false),
                    SgstPaise = table.Column<long>(type: "bigint", nullable: false),
                    IgstPaise = table.Column<long>(type: "bigint", nullable: false),
                    TotalLineAmountPaise = table.Column<long>(type: "bigint", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods_receipt_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_goods_receipt_lines_goods_receipt_notes_GoodsReceiptNoteId",
                        column: x => x.GoodsReceiptNoteId,
                        principalSchema: "inventory",
                        principalTable: "goods_receipt_notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_goods_receipt_lines_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "commercial",
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipt_lines_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipt_lines_purchase_order_lines_PurchaseOrderLineId",
                        column: x => x.PurchaseOrderLineId,
                        principalSchema: "inventory",
                        principalTable: "purchase_order_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_goods_receipt_lines_storage_locations_StorageLocationId",
                        column: x => x.StorageLocationId,
                        principalSchema: "inventory",
                        principalTable: "storage_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_return_lines",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    StorageLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReturnQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    UnitCostPaise = table.Column<long>(type: "bigint", nullable: false),
                    TotalLineAmountPaise = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_return_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_return_lines_goods_receipt_lines_GoodsReceiptLineId",
                        column: x => x.GoodsReceiptLineId,
                        principalSchema: "inventory",
                        principalTable: "goods_receipt_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_purchase_return_lines_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "commercial",
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_return_lines_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "commercial",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_return_lines_purchase_returns_PurchaseReturnId",
                        column: x => x.PurchaseReturnId,
                        principalSchema: "inventory",
                        principalTable: "purchase_returns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchase_return_lines_storage_locations_StorageLocationId",
                        column: x => x.StorageLocationId,
                        principalSchema: "inventory",
                        principalTable: "storage_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_GoodsReceiptNoteId",
                schema: "inventory",
                table: "goods_receipt_lines",
                column: "GoodsReceiptNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_ProductId",
                schema: "inventory",
                table: "goods_receipt_lines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_PurchaseOrderLineId",
                schema: "inventory",
                table: "goods_receipt_lines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_StorageLocationId",
                schema: "inventory",
                table: "goods_receipt_lines",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_VariantId",
                schema: "inventory",
                table: "goods_receipt_lines",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_notes_CompanyId_GrnNumber",
                schema: "inventory",
                table: "goods_receipt_notes",
                columns: new[] { "CompanyId", "GrnNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_notes_PurchaseOrderId",
                schema: "inventory",
                table: "goods_receipt_notes",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_notes_SupplierId",
                schema: "inventory",
                table: "goods_receipt_notes",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_notes_WarehouseId",
                schema: "inventory",
                table: "goods_receipt_notes",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_product_storage_locations_CompanyId_WarehouseId_ProductId_S~",
                schema: "inventory",
                table: "product_storage_locations",
                columns: new[] { "CompanyId", "WarehouseId", "ProductId", "StorageLocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_storage_locations_ProductId",
                schema: "inventory",
                table: "product_storage_locations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_storage_locations_StorageLocationId",
                schema: "inventory",
                table: "product_storage_locations",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_product_storage_locations_VariantId",
                schema: "inventory",
                table: "product_storage_locations",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_product_storage_locations_WarehouseId",
                schema: "inventory",
                table: "product_storage_locations",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_ProductId",
                schema: "inventory",
                table: "purchase_order_lines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_PurchaseOrderId",
                schema: "inventory",
                table: "purchase_order_lines",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_VariantId",
                schema: "inventory",
                table: "purchase_order_lines",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_CompanyId_OrderNumber",
                schema: "inventory",
                table: "purchase_orders",
                columns: new[] { "CompanyId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_SupplierId",
                schema: "inventory",
                table: "purchase_orders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_WarehouseId",
                schema: "inventory",
                table: "purchase_orders",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_return_lines_GoodsReceiptLineId",
                schema: "inventory",
                table: "purchase_return_lines",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_return_lines_ProductId",
                schema: "inventory",
                table: "purchase_return_lines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_return_lines_PurchaseReturnId",
                schema: "inventory",
                table: "purchase_return_lines",
                column: "PurchaseReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_return_lines_StorageLocationId",
                schema: "inventory",
                table: "purchase_return_lines",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_return_lines_VariantId",
                schema: "inventory",
                table: "purchase_return_lines",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_returns_CompanyId_ReturnNumber",
                schema: "inventory",
                table: "purchase_returns",
                columns: new[] { "CompanyId", "ReturnNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_returns_GoodsReceiptNoteId",
                schema: "inventory",
                table: "purchase_returns",
                column: "GoodsReceiptNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_returns_SupplierId",
                schema: "inventory",
                table: "purchase_returns",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_returns_WarehouseId",
                schema: "inventory",
                table: "purchase_returns",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_reorder_rules_CompanyId_WarehouseId_ProductId_VariantId",
                schema: "inventory",
                table: "reorder_rules",
                columns: new[] { "CompanyId", "WarehouseId", "ProductId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reorder_rules_PreferredSupplierId",
                schema: "inventory",
                table: "reorder_rules",
                column: "PreferredSupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_reorder_rules_ProductId",
                schema: "inventory",
                table: "reorder_rules",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_reorder_rules_VariantId",
                schema: "inventory",
                table: "reorder_rules",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_reorder_rules_WarehouseId",
                schema: "inventory",
                table: "reorder_rules",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_lines_ProductId",
                schema: "inventory",
                table: "stock_adjustment_lines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_lines_StockAdjustmentId",
                schema: "inventory",
                table: "stock_adjustment_lines",
                column: "StockAdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_lines_StorageLocationId",
                schema: "inventory",
                table: "stock_adjustment_lines",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_lines_VariantId",
                schema: "inventory",
                table: "stock_adjustment_lines",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustments_CompanyId_AdjustmentNumber",
                schema: "inventory",
                table: "stock_adjustments",
                columns: new[] { "CompanyId", "AdjustmentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustments_WarehouseId",
                schema: "inventory",
                table: "stock_adjustments",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_balances_CompanyId_WarehouseId_ProductId_VariantId_St~",
                schema: "inventory",
                table: "stock_balances",
                columns: new[] { "CompanyId", "WarehouseId", "ProductId", "VariantId", "StorageLocationId", "BatchNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_balances_ProductId",
                schema: "inventory",
                table: "stock_balances",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_balances_StorageLocationId",
                schema: "inventory",
                table: "stock_balances",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_balances_VariantId",
                schema: "inventory",
                table: "stock_balances",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_balances_WarehouseId",
                schema: "inventory",
                table: "stock_balances",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_count_lines_ProductId",
                schema: "inventory",
                table: "stock_count_lines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_count_lines_StockCountId",
                schema: "inventory",
                table: "stock_count_lines",
                column: "StockCountId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_count_lines_StorageLocationId",
                schema: "inventory",
                table: "stock_count_lines",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_count_lines_VariantId",
                schema: "inventory",
                table: "stock_count_lines",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_counts_CompanyId_CountNumber",
                schema: "inventory",
                table: "stock_counts",
                columns: new[] { "CompanyId", "CountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_counts_WarehouseId",
                schema: "inventory",
                table: "stock_counts",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_WarehouseId_ProductId_MovementDat~",
                schema: "inventory",
                table: "stock_movements",
                columns: new[] { "CompanyId", "WarehouseId", "ProductId", "MovementDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_ProductId",
                schema: "inventory",
                table: "stock_movements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_ReferenceId",
                schema: "inventory",
                table: "stock_movements",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_StorageLocationId",
                schema: "inventory",
                table: "stock_movements",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_VariantId",
                schema: "inventory",
                table: "stock_movements",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_WarehouseId",
                schema: "inventory",
                table: "stock_movements",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_lines_DestinationStorageLocationId",
                schema: "inventory",
                table: "stock_transfer_lines",
                column: "DestinationStorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_lines_ProductId",
                schema: "inventory",
                table: "stock_transfer_lines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_lines_SourceStorageLocationId",
                schema: "inventory",
                table: "stock_transfer_lines",
                column: "SourceStorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_lines_StockTransferId",
                schema: "inventory",
                table: "stock_transfer_lines",
                column: "StockTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_lines_VariantId",
                schema: "inventory",
                table: "stock_transfer_lines",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_CompanyId_TransferNumber",
                schema: "inventory",
                table: "stock_transfers",
                columns: new[] { "CompanyId", "TransferNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_DestinationWarehouseId",
                schema: "inventory",
                table: "stock_transfers",
                column: "DestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_SourceWarehouseId",
                schema: "inventory",
                table: "stock_transfers",
                column: "SourceWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_storage_locations_CompanyId",
                schema: "inventory",
                table: "storage_locations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_storage_locations_ParentLocationId",
                schema: "inventory",
                table: "storage_locations",
                column: "ParentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_storage_locations_WarehouseId_Code",
                schema: "inventory",
                table: "storage_locations",
                columns: new[] { "WarehouseId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_branch_assignments_BranchId",
                schema: "inventory",
                table: "warehouse_branch_assignments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_branch_assignments_CompanyId_WarehouseId_BranchId",
                schema: "inventory",
                table: "warehouse_branch_assignments",
                columns: new[] { "CompanyId", "WarehouseId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_branch_assignments_WarehouseId",
                schema: "inventory",
                table: "warehouse_branch_assignments",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_CompanyId",
                schema: "inventory",
                table: "warehouses",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_CompanyId_Code",
                schema: "inventory",
                table: "warehouses",
                columns: new[] { "CompanyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_storage_locations",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "purchase_return_lines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "reorder_rules",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "stock_adjustment_lines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "stock_balances",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "stock_count_lines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "stock_movements",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "stock_transfer_lines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "warehouse_branch_assignments",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "goods_receipt_lines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "purchase_returns",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "stock_adjustments",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "stock_counts",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "stock_transfers",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "purchase_order_lines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "storage_locations",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "goods_receipt_notes",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "purchase_orders",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "warehouses",
                schema: "inventory");
        }
    }
}
