namespace Magic.Infrastructure.Persistence.Entities;

// ==========================================
// 1. Warehouses & Storage Locations
// ==========================================

public sealed class WarehouseRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }
    public required string StateCode { get; set; }
    public required string PinCode { get; set; }
    public string Country { get; set; } = "India";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ManagerName { get; set; }
    public decimal? CapacitySqFt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public ICollection<WarehouseBranchAssignmentRecord> BranchAssignments { get; set; } = [];
    public ICollection<StorageLocationRecord> StorageLocations { get; set; } = [];
    public ICollection<StockBalanceRecord> StockBalances { get; set; } = [];
}

public sealed class WarehouseBranchAssignmentRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid WarehouseId { get; set; }
    public WarehouseRecord? Warehouse { get; set; }
    public required Guid BranchId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; }
}

public sealed class StorageLocationRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid WarehouseId { get; set; }
    public WarehouseRecord? Warehouse { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string LocationType { get; set; } // Zone, Aisle, Rack, Shelf, Bin
    public Guid? ParentLocationId { get; set; }
    public StorageLocationRecord? ParentLocation { get; set; }
    public ICollection<StorageLocationRecord> Children { get; set; } = [];
    public string? Barcode { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class ProductStorageLocationRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid WarehouseId { get; set; }
    public WarehouseRecord? Warehouse { get; set; }
    public required Guid StorageLocationId { get; set; }
    public StorageLocationRecord? StorageLocation { get; set; }
    public required Guid ProductId { get; set; }
    public ProductRecord? Product { get; set; }
    public Guid? VariantId { get; set; }
    public ProductVariantRecord? Variant { get; set; }
    public bool IsPrimaryLocation { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

// ==========================================
// 2. Stock Balances & Movements
// ==========================================

public sealed class StockBalanceRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid BranchId { get; set; }
    public required Guid WarehouseId { get; set; }
    public WarehouseRecord? Warehouse { get; set; }
    public required Guid ProductId { get; set; }
    public ProductRecord? Product { get; set; }
    public Guid? VariantId { get; set; }
    public ProductVariantRecord? Variant { get; set; }
    public Guid? StorageLocationId { get; set; }
    public StorageLocationRecord? StorageLocation { get; set; }
    public string? BatchNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal QuantityAvailable { get; set; }
    public decimal QuantityDamaged { get; set; }
    public decimal QuantityExpired { get; set; }
    public decimal QuantityBlocked { get; set; }
    public long UnitCostPaise { get; set; }
    public DateTimeOffset LastMovementAtUtc { get; set; }
    public int ConcurrencyVersion { get; set; } = 1;
}

public sealed class StockMovementRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid BranchId { get; set; }
    public required Guid WarehouseId { get; set; }
    public WarehouseRecord? Warehouse { get; set; }
    public required Guid ProductId { get; set; }
    public ProductRecord? Product { get; set; }
    public Guid? VariantId { get; set; }
    public ProductVariantRecord? Variant { get; set; }
    public Guid? StorageLocationId { get; set; }
    public StorageLocationRecord? StorageLocation { get; set; }
    public required string MovementType { get; set; } // OpeningStock, PurchaseReceipt, PurchaseReturn, SalesIssue, CustomerReturn, TransferOut, TransferIn, AdjustmentPositive, AdjustmentNegative, CountReconcile
    public required string Direction { get; set; } // In, Out
    public decimal Quantity { get; set; }
    public string? BatchNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public long UnitCostPaise { get; set; }
    public long TotalCostPaise { get; set; }
    public required string ReferenceType { get; set; } // PO, GRN, PR, ST, SA, SC, SO, Manual
    public Guid? ReferenceId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Reason { get; set; }
    public decimal BalanceAfterOnHand { get; set; }
    public Guid PerformedByUserId { get; set; }
    public DateTimeOffset MovementDateUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

// ==========================================
// 3. Stock Adjustments
// ==========================================

public sealed class StockAdjustmentRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid BranchId { get; set; }
    public required Guid WarehouseId { get; set; }
    public WarehouseRecord? Warehouse { get; set; }
    public required string AdjustmentNumber { get; set; }
    public DateTimeOffset AdjustmentDateUtc { get; set; }
    public required string ReasonCode { get; set; } // Damaged, Expired, Missing, Found, Correction, WriteOff
    public string? Description { get; set; }
    public required string Status { get; set; } // Draft, PendingApproval, Approved, Rejected, Cancelled
    public bool RequiresApproval { get; set; }
    public int TotalItemsCount { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public ICollection<StockAdjustmentLineRecord> Lines { get; set; } = [];
}

public sealed class StockAdjustmentLineRecord
{
    public required Guid Id { get; set; }
    public required Guid StockAdjustmentId { get; set; }
    public StockAdjustmentRecord? StockAdjustment { get; set; }
    public required Guid ProductId { get; set; }
    public ProductRecord? Product { get; set; }
    public Guid? VariantId { get; set; }
    public ProductVariantRecord? Variant { get; set; }
    public Guid? StorageLocationId { get; set; }
    public StorageLocationRecord? StorageLocation { get; set; }
    public string? BatchNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public decimal SystemQuantityOnHand { get; set; }
    public decimal AdjustmentQuantity { get; set; }
    public decimal NewQuantityOnHand { get; set; }
    public long UnitCostPaise { get; set; }
    public string? LineReason { get; set; }
}

// ==========================================
// 4. Stock Counting
// ==========================================

public sealed class StockCountRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid BranchId { get; set; }
    public required Guid WarehouseId { get; set; }
    public WarehouseRecord? Warehouse { get; set; }
    public required string CountNumber { get; set; }
    public DateTimeOffset CountDateUtc { get; set; }
    public required string Status { get; set; } // Planned, InProgress, Submitted, Approved, Reconciled, Cancelled
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? CountedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public DateTimeOffset? ReconciledAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public ICollection<StockCountLineRecord> Lines { get; set; } = [];
}

public sealed class StockCountLineRecord
{
    public required Guid Id { get; set; }
    public required Guid StockCountId { get; set; }
    public StockCountRecord? StockCount { get; set; }
    public required Guid ProductId { get; set; }
    public ProductRecord? Product { get; set; }
    public Guid? VariantId { get; set; }
    public ProductVariantRecord? Variant { get; set; }
    public Guid? StorageLocationId { get; set; }
    public StorageLocationRecord? StorageLocation { get; set; }
    public decimal SystemQuantityOnHand { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal DiscrepancyQuantity { get; set; }
    public long UnitCostPaise { get; set; }
    public long DiscrepancyValuePaise { get; set; }
    public string? Notes { get; set; }
}

// ==========================================
// 5. Purchase Orders
// ==========================================

public sealed class PurchaseOrderRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid BranchId { get; set; }
    public required Guid WarehouseId { get; set; }
    public WarehouseRecord? Warehouse { get; set; }
    public required string OrderNumber { get; set; }
    public required Guid SupplierId { get; set; }
    public SupplierRecord? Supplier { get; set; }
    public DateTimeOffset OrderDateUtc { get; set; }
    public DateOnly? ExpectedDeliveryDate { get; set; }
    public required string Status { get; set; } // Draft, Submitted, Approved, PartiallyReceived, Completed, Cancelled
    public long SubtotalPaise { get; set; }
    public long DiscountPaise { get; set; }
    public long TaxableAmountPaise { get; set; }
    public long CgstPaise { get; set; }
    public long SgstPaise { get; set; }
    public long IgstPaise { get; set; }
    public long CessPaise { get; set; }
    public long RoundOffPaise { get; set; }
    public long TotalAmountPaise { get; set; }
    public string PaymentTerms { get; set; } = "Net 30";
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public ICollection<PurchaseOrderLineRecord> Lines { get; set; } = [];
}

public sealed class PurchaseOrderLineRecord
{
    public required Guid Id { get; set; }
    public required Guid PurchaseOrderId { get; set; }
    public PurchaseOrderRecord? PurchaseOrder { get; set; }
    public required Guid ProductId { get; set; }
    public ProductRecord? Product { get; set; }
    public Guid? VariantId { get; set; }
    public ProductVariantRecord? Variant { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal CancelledQuantity { get; set; }
    public long UnitPricePaise { get; set; }
    public decimal DiscountPercentage { get; set; }
    public long DiscountPaise { get; set; }
    public Guid? TaxCodeId { get; set; }
    public decimal GstRatePercentage { get; set; }
    public long TaxableAmountPaise { get; set; }
    public long CgstPaise { get; set; }
    public long SgstPaise { get; set; }
    public long IgstPaise { get; set; }
    public long TotalLineAmountPaise { get; set; }
}

// ==========================================
// 6. Goods Receipt Notes (GRN)
// ==========================================

public sealed class GoodsReceiptNoteRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid BranchId { get; set; }
    public required Guid WarehouseId { get; set; }
    public WarehouseRecord? Warehouse { get; set; }
    public required string GrnNumber { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrderRecord? PurchaseOrder { get; set; }
    public required Guid SupplierId { get; set; }
    public SupplierRecord? Supplier { get; set; }
    public string? SupplierInvoiceNumber { get; set; }
    public DateOnly? SupplierInvoiceDate { get; set; }
    public DateTimeOffset ReceiptDateUtc { get; set; }
    public required string Status { get; set; } // Draft, Confirmed, Cancelled
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? ConfirmedByUserId { get; set; }
    public DateTimeOffset? ConfirmedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public ICollection<GoodsReceiptLineRecord> Lines { get; set; } = [];
}

public sealed class GoodsReceiptLineRecord
{
    public required Guid Id { get; set; }
    public required Guid GoodsReceiptNoteId { get; set; }
    public GoodsReceiptNoteRecord? GoodsReceiptNote { get; set; }
    public Guid? PurchaseOrderLineId { get; set; }
    public PurchaseOrderLineRecord? PurchaseOrderLine { get; set; }
    public required Guid ProductId { get; set; }
    public ProductRecord? Product { get; set; }
    public Guid? VariantId { get; set; }
    public ProductVariantRecord? Variant { get; set; }
    public Guid? StorageLocationId { get; set; }
    public StorageLocationRecord? StorageLocation { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
    public decimal DamagedQuantity { get; set; }
    public string? BatchNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public long UnitPricePaise { get; set; }
    public long TaxableAmountPaise { get; set; }
    public long CgstPaise { get; set; }
    public long SgstPaise { get; set; }
    public long IgstPaise { get; set; }
    public long TotalLineAmountPaise { get; set; }
    public string? RejectionReason { get; set; }
}

// ==========================================
// 7. Purchase Returns
// ==========================================

public sealed class PurchaseReturnRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid BranchId { get; set; }
    public required Guid WarehouseId { get; set; }
    public WarehouseRecord? Warehouse { get; set; }
    public required string ReturnNumber { get; set; }
    public Guid? GoodsReceiptNoteId { get; set; }
    public GoodsReceiptNoteRecord? GoodsReceiptNote { get; set; }
    public required Guid SupplierId { get; set; }
    public SupplierRecord? Supplier { get; set; }
    public string? SupplierCreditNoteNumber { get; set; }
    public DateTimeOffset ReturnDateUtc { get; set; }
    public required string Status { get; set; } // Draft, Approved, Confirmed, Cancelled
    public long TotalAmountPaise { get; set; }
    public string? ReturnReason { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? ConfirmedByUserId { get; set; }
    public DateTimeOffset? ConfirmedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public ICollection<PurchaseReturnLineRecord> Lines { get; set; } = [];
}

public sealed class PurchaseReturnLineRecord
{
    public required Guid Id { get; set; }
    public required Guid PurchaseReturnId { get; set; }
    public PurchaseReturnRecord? PurchaseReturn { get; set; }
    public Guid? GoodsReceiptLineId { get; set; }
    public GoodsReceiptLineRecord? GoodsReceiptLine { get; set; }
    public required Guid ProductId { get; set; }
    public ProductRecord? Product { get; set; }
    public Guid? VariantId { get; set; }
    public ProductVariantRecord? Variant { get; set; }
    public Guid? StorageLocationId { get; set; }
    public StorageLocationRecord? StorageLocation { get; set; }
    public decimal ReturnQuantity { get; set; }
    public long UnitCostPaise { get; set; }
    public long TotalLineAmountPaise { get; set; }
    public string? Reason { get; set; }
}

// ==========================================
// 8. Stock Transfers
// ==========================================

public sealed class StockTransferRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required string TransferNumber { get; set; }
    public required Guid SourceBranchId { get; set; }
    public required Guid SourceWarehouseId { get; set; }
    public WarehouseRecord? SourceWarehouse { get; set; }
    public required Guid DestinationBranchId { get; set; }
    public required Guid DestinationWarehouseId { get; set; }
    public WarehouseRecord? DestinationWarehouse { get; set; }
    public required string Status { get; set; } // Draft, Approved, Dispatched, PartiallyReceived, Received, Cancelled
    public DateTimeOffset TransferDateUtc { get; set; }
    public DateTimeOffset? DispatchedAtUtc { get; set; }
    public Guid? DispatchedByUserId { get; set; }
    public DateTimeOffset? ReceivedAtUtc { get; set; }
    public Guid? ReceivedByUserId { get; set; }
    public string? TrackingNumber { get; set; }
    public string? CarrierName { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public ICollection<StockTransferLineRecord> Lines { get; set; } = [];
}

public sealed class StockTransferLineRecord
{
    public required Guid Id { get; set; }
    public required Guid StockTransferId { get; set; }
    public StockTransferRecord? StockTransfer { get; set; }
    public required Guid ProductId { get; set; }
    public ProductRecord? Product { get; set; }
    public Guid? VariantId { get; set; }
    public ProductVariantRecord? Variant { get; set; }
    public Guid? SourceStorageLocationId { get; set; }
    public StorageLocationRecord? SourceStorageLocation { get; set; }
    public Guid? DestinationStorageLocationId { get; set; }
    public StorageLocationRecord? DestinationStorageLocation { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal DispatchedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal DamagedQuantity { get; set; }
    public decimal MissingQuantity { get; set; }
    public long UnitCostPaise { get; set; }
}

// ==========================================
// 9. Reorder Management
// ==========================================

public sealed class ReorderRuleRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public WarehouseRecord? Warehouse { get; set; }
    public required Guid ProductId { get; set; }
    public ProductRecord? Product { get; set; }
    public Guid? VariantId { get; set; }
    public ProductVariantRecord? Variant { get; set; }
    public Guid? PreferredSupplierId { get; set; }
    public SupplierRecord? PreferredSupplier { get; set; }
    public decimal MinStockLevel { get; set; }
    public decimal MaxStockLevel { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal SafetyStock { get; set; }
    public int LeadTimeDays { get; set; } = 7;
    public decimal ReorderQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
