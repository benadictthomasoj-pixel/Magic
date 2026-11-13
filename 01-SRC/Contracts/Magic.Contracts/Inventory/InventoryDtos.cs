namespace Magic.Contracts.Inventory;

// ==========================================
// 1. Warehouses & Storage Locations
// ==========================================

public sealed record WarehouseDto
{
    public required Guid Id { get; init; }
    public required Guid CompanyId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string StateCode { get; init; }
    public required string PinCode { get; init; }
    public required string Country { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? ManagerName { get; init; }
    public decimal? CapacitySqFt { get; init; }
    public required bool IsActive { get; init; }
    public List<Guid> AssignedBranchIds { get; init; } = [];
    public int StorageLocationsCount { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}

public sealed record CreateWarehouseRequest
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string StateCode { get; init; }
    public required string PinCode { get; init; }
    public string Country { get; init; } = "India";
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? ManagerName { get; init; }
    public decimal? CapacitySqFt { get; init; }
    public List<Guid>? AssignedBranchIds { get; init; }
}

public sealed record UpdateWarehouseRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string StateCode { get; init; }
    public required string PinCode { get; init; }
    public string Country { get; init; } = "India";
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? ManagerName { get; init; }
    public decimal? CapacitySqFt { get; init; }
    public bool IsActive { get; init; } = true;
    public List<Guid>? AssignedBranchIds { get; init; }
}

public sealed record StorageLocationDto
{
    public required Guid Id { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string LocationType { get; init; } // Zone, Aisle, Rack, Shelf, Bin
    public Guid? ParentLocationId { get; init; }
    public string? Barcode { get; init; }
    public required bool IsActive { get; init; }
}

public sealed record CreateStorageLocationRequest
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string LocationType { get; init; }
    public Guid? ParentLocationId { get; init; }
    public string? Barcode { get; init; }
}

// ==========================================
// 2. Stock Balances & Movements
// ==========================================

public sealed record StockBalanceDto
{
    public required Guid Id { get; init; }
    public required Guid CompanyId { get; init; }
    public required Guid BranchId { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public required Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? ProductCode { get; init; }
    public string? Sku { get; init; }
    public Guid? VariantId { get; init; }
    public string? VariantName { get; init; }
    public Guid? StorageLocationId { get; init; }
    public string? LocationCode { get; init; }
    public string? BatchNumber { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public required decimal QuantityOnHand { get; init; }
    public required decimal QuantityReserved { get; init; }
    public required decimal QuantityAvailable { get; init; }
    public required decimal QuantityDamaged { get; init; }
    public required decimal QuantityExpired { get; init; }
    public required decimal QuantityBlocked { get; init; }
    public required long UnitCostPaise { get; init; }
    public DateTimeOffset LastMovementAtUtc { get; init; }
}

public sealed record StockMovementDto
{
    public required Guid Id { get; init; }
    public required Guid CompanyId { get; init; }
    public required Guid BranchId { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public required Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? Sku { get; init; }
    public Guid? VariantId { get; init; }
    public Guid? StorageLocationId { get; init; }
    public required string MovementType { get; init; }
    public required string Direction { get; init; } // In, Out
    public required decimal Quantity { get; init; }
    public string? BatchNumber { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public required long UnitCostPaise { get; init; }
    public required long TotalCostPaise { get; init; }
    public required string ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? Reason { get; init; }
    public required decimal BalanceAfterOnHand { get; init; }
    public required Guid PerformedByUserId { get; init; }
    public DateTimeOffset MovementDateUtc { get; init; }
}

public sealed record PostOpeningStockRequest
{
    public required Guid WarehouseId { get; init; }
    public required Guid BranchId { get; init; }
    public required List<OpeningStockItemRequest> Items { get; init; }
}

public sealed record OpeningStockItemRequest
{
    public required Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public Guid? StorageLocationId { get; init; }
    public required decimal Quantity { get; init; }
    public required long UnitCostPaise { get; init; }
    public string? BatchNumber { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public string? Reason { get; init; }
}

// ==========================================
// 3. Stock Adjustments
// ==========================================

public sealed record StockAdjustmentDto
{
    public required Guid Id { get; init; }
    public required Guid CompanyId { get; init; }
    public required Guid BranchId { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public required string AdjustmentNumber { get; init; }
    public DateTimeOffset AdjustmentDateUtc { get; init; }
    public required string ReasonCode { get; init; } // Damaged, Expired, Missing, Found, Correction, WriteOff
    public string? Description { get; init; }
    public required string Status { get; init; } // Draft, PendingApproval, Approved, Rejected, Cancelled
    public required bool RequiresApproval { get; init; }
    public required int TotalItemsCount { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public Guid? ApprovedByUserId { get; init; }
    public DateTimeOffset? ApprovedAtUtc { get; init; }
    public string? RejectionReason { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public List<StockAdjustmentLineDto> Lines { get; init; } = [];
}

public sealed record StockAdjustmentLineDto
{
    public required Guid Id { get; init; }
    public required Guid StockAdjustmentId { get; init; }
    public required Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? Sku { get; init; }
    public Guid? VariantId { get; init; }
    public Guid? StorageLocationId { get; init; }
    public string? BatchNumber { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public required decimal SystemQuantityOnHand { get; init; }
    public required decimal AdjustmentQuantity { get; init; }
    public required decimal NewQuantityOnHand { get; init; }
    public required long UnitCostPaise { get; init; }
    public string? LineReason { get; init; }
}

public sealed record CreateStockAdjustmentRequest
{
    public required Guid BranchId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string ReasonCode { get; init; }
    public string? Description { get; init; }
    public required List<CreateStockAdjustmentLineRequest> Lines { get; init; }
}

public sealed record CreateStockAdjustmentLineRequest
{
    public required Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public Guid? StorageLocationId { get; init; }
    public string? BatchNumber { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public required decimal AdjustmentQuantity { get; init; }
    public required long UnitCostPaise { get; init; }
    public string? LineReason { get; init; }
}

// ==========================================
// 4. Stock Counting
// ==========================================

public sealed record StockCountDto
{
    public required Guid Id { get; init; }
    public required Guid CompanyId { get; init; }
    public required Guid BranchId { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public required string CountNumber { get; init; }
    public DateTimeOffset CountDateUtc { get; init; }
    public required string Status { get; init; } // Planned, InProgress, Submitted, Approved, Reconciled, Cancelled
    public string? Notes { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public Guid? CountedByUserId { get; init; }
    public Guid? ApprovedByUserId { get; init; }
    public DateTimeOffset? ApprovedAtUtc { get; init; }
    public DateTimeOffset? ReconciledAtUtc { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public List<StockCountLineDto> Lines { get; init; } = [];
}

public sealed record StockCountLineDto
{
    public required Guid Id { get; init; }
    public required Guid StockCountId { get; init; }
    public required Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? Sku { get; init; }
    public Guid? VariantId { get; init; }
    public Guid? StorageLocationId { get; init; }
    public required decimal SystemQuantityOnHand { get; init; }
    public required decimal CountedQuantity { get; init; }
    public required decimal DiscrepancyQuantity { get; init; }
    public required long UnitCostPaise { get; init; }
    public required long DiscrepancyValuePaise { get; init; }
    public string? Notes { get; init; }
}

public sealed record CreateStockCountRequest
{
    public required Guid BranchId { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? Notes { get; init; }
    public List<Guid>? ProductIds { get; init; }
}

public sealed record RecordStockCountLinesRequest
{
    public required List<StockCountLineEntryRequest> Lines { get; init; }
}

public sealed record StockCountLineEntryRequest
{
    public required Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public Guid? StorageLocationId { get; init; }
    public required decimal CountedQuantity { get; init; }
    public string? Notes { get; init; }
}

// ==========================================
// 5. Purchase Orders
// ==========================================

public sealed record PurchaseOrderDto
{
    public required Guid Id { get; init; }
    public required Guid CompanyId { get; init; }
    public required Guid BranchId { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public required string OrderNumber { get; init; }
    public required Guid SupplierId { get; init; }
    public string? SupplierName { get; init; }
    public DateTimeOffset OrderDateUtc { get; init; }
    public DateOnly? ExpectedDeliveryDate { get; init; }
    public required string Status { get; init; } // Draft, Submitted, Approved, PartiallyReceived, Completed, Cancelled
    public required long SubtotalPaise { get; init; }
    public required long DiscountPaise { get; init; }
    public required long TaxableAmountPaise { get; init; }
    public required long CgstPaise { get; init; }
    public required long SgstPaise { get; init; }
    public required long IgstPaise { get; init; }
    public required long CessPaise { get; init; }
    public required long RoundOffPaise { get; init; }
    public required long TotalAmountPaise { get; init; }
    public required string PaymentTerms { get; init; }
    public string? Notes { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public Guid? ApprovedByUserId { get; init; }
    public DateTimeOffset? ApprovedAtUtc { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public List<PurchaseOrderLineDto> Lines { get; init; } = [];
}

public sealed record PurchaseOrderLineDto
{
    public required Guid Id { get; init; }
    public required Guid PurchaseOrderId { get; init; }
    public required Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? Sku { get; init; }
    public Guid? VariantId { get; init; }
    public required decimal OrderedQuantity { get; init; }
    public required decimal ReceivedQuantity { get; init; }
    public required decimal CancelledQuantity { get; init; }
    public required long UnitPricePaise { get; init; }
    public required decimal DiscountPercentage { get; init; }
    public required long DiscountPaise { get; init; }
    public Guid? TaxCodeId { get; init; }
    public required decimal GstRatePercentage { get; init; }
    public required long TaxableAmountPaise { get; init; }
    public required long CgstPaise { get; init; }
    public required long SgstPaise { get; init; }
    public required long IgstPaise { get; init; }
    public required long TotalLineAmountPaise { get; init; }
}

public sealed record CreatePurchaseOrderRequest
{
    public required Guid BranchId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required Guid SupplierId { get; init; }
    public DateOnly? ExpectedDeliveryDate { get; init; }
    public string PaymentTerms { get; init; } = "Net 30";
    public string? Notes { get; init; }
    public required List<CreatePurchaseOrderLineRequest> Lines { get; init; }
}

public sealed record CreatePurchaseOrderLineRequest
{
    public required Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public required decimal Quantity { get; init; }
    public required long UnitPricePaise { get; init; }
    public decimal DiscountPercentage { get; init; }
    public Guid? TaxCodeId { get; init; }
    public decimal GstRatePercentage { get; init; }
}

// ==========================================
// 6. Goods Receipt Notes (GRN)
// ==========================================

public sealed record GoodsReceiptNoteDto
{
    public required Guid Id { get; init; }
    public required Guid CompanyId { get; init; }
    public required Guid BranchId { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public required string GrnNumber { get; init; }
    public Guid? PurchaseOrderId { get; init; }
    public string? PurchaseOrderNumber { get; init; }
    public required Guid SupplierId { get; init; }
    public string? SupplierName { get; init; }
    public string? SupplierInvoiceNumber { get; init; }
    public DateOnly? SupplierInvoiceDate { get; init; }
    public DateTimeOffset ReceiptDateUtc { get; init; }
    public required string Status { get; init; } // Draft, Confirmed, Cancelled
    public string? Notes { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public Guid? ConfirmedByUserId { get; init; }
    public DateTimeOffset? ConfirmedAtUtc { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public List<GoodsReceiptLineDto> Lines { get; init; } = [];
}

public sealed record GoodsReceiptLineDto
{
    public required Guid Id { get; init; }
    public required Guid GoodsReceiptNoteId { get; init; }
    public Guid? PurchaseOrderLineId { get; init; }
    public required Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? Sku { get; init; }
    public Guid? VariantId { get; init; }
    public Guid? StorageLocationId { get; init; }
    public required decimal ReceivedQuantity { get; init; }
    public required decimal AcceptedQuantity { get; init; }
    public required decimal RejectedQuantity { get; init; }
    public required decimal DamagedQuantity { get; init; }
    public string? BatchNumber { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public required long UnitPricePaise { get; init; }
    public required long TaxableAmountPaise { get; init; }
    public required long CgstPaise { get; init; }
    public required long SgstPaise { get; init; }
    public required long IgstPaise { get; init; }
    public required long TotalLineAmountPaise { get; init; }
    public string? RejectionReason { get; init; }
}

public sealed record CreateGoodsReceiptRequest
{
    public required Guid BranchId { get; init; }
    public required Guid WarehouseId { get; init; }
    public Guid? PurchaseOrderId { get; init; }
    public required Guid SupplierId { get; init; }
    public string? SupplierInvoiceNumber { get; init; }
    public DateOnly? SupplierInvoiceDate { get; init; }
    public string? Notes { get; init; }
    public required List<CreateGoodsReceiptLineRequest> Lines { get; init; }
}

public sealed record CreateGoodsReceiptLineRequest
{
    public Guid? PurchaseOrderLineId { get; init; }
    public required Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public Guid? StorageLocationId { get; init; }
    public required decimal ReceivedQuantity { get; init; }
    public required decimal AcceptedQuantity { get; init; }
    public decimal RejectedQuantity { get; init; }
    public decimal DamagedQuantity { get; init; }
    public string? BatchNumber { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public required long UnitPricePaise { get; init; }
    public decimal GstRatePercentage { get; init; }
    public string? RejectionReason { get; init; }
}

// ==========================================
// 7. Purchase Returns
// ==========================================

public sealed record PurchaseReturnDto
{
    public required Guid Id { get; init; }
    public required Guid CompanyId { get; init; }
    public required Guid BranchId { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public required string ReturnNumber { get; init; }
    public Guid? GoodsReceiptNoteId { get; init; }
    public string? GrnNumber { get; init; }
    public required Guid SupplierId { get; init; }
    public string? SupplierName { get; init; }
    public string? SupplierCreditNoteNumber { get; init; }
    public DateTimeOffset ReturnDateUtc { get; init; }
    public required string Status { get; init; } // Draft, Approved, Confirmed, Cancelled
    public required long TotalAmountPaise { get; init; }
    public string? ReturnReason { get; init; }
    public string? Notes { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public Guid? ConfirmedByUserId { get; init; }
    public DateTimeOffset? ConfirmedAtUtc { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public List<PurchaseReturnLineDto> Lines { get; init; } = [];
}

public sealed record PurchaseReturnLineDto
{
    public required Guid Id { get; init; }
    public required Guid PurchaseReturnId { get; init; }
    public Guid? GoodsReceiptLineId { get; init; }
    public required Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? Sku { get; init; }
    public Guid? VariantId { get; init; }
    public Guid? StorageLocationId { get; init; }
    public required decimal ReturnQuantity { get; init; }
    public required long UnitCostPaise { get; init; }
    public required long TotalLineAmountPaise { get; init; }
    public string? Reason { get; init; }
}

public sealed record CreatePurchaseReturnRequest
{
    public required Guid BranchId { get; init; }
    public required Guid WarehouseId { get; init; }
    public Guid? GoodsReceiptNoteId { get; init; }
    public required Guid SupplierId { get; init; }
    public string? SupplierCreditNoteNumber { get; init; }
    public string? ReturnReason { get; init; }
    public string? Notes { get; init; }
    public required List<CreatePurchaseReturnLineRequest> Lines { get; init; }
}

public sealed record CreatePurchaseReturnLineRequest
{
    public Guid? GoodsReceiptLineId { get; init; }
    public required Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public Guid? StorageLocationId { get; init; }
    public required decimal ReturnQuantity { get; init; }
    public required long UnitCostPaise { get; init; }
    public string? Reason { get; init; }
}

// ==========================================
// 8. Stock Transfers
// ==========================================

public sealed record StockTransferDto
{
    public required Guid Id { get; init; }
    public required Guid CompanyId { get; init; }
    public required string TransferNumber { get; init; }
    public required Guid SourceBranchId { get; init; }
    public required Guid SourceWarehouseId { get; init; }
    public string? SourceWarehouseName { get; init; }
    public required Guid DestinationBranchId { get; init; }
    public required Guid DestinationWarehouseId { get; init; }
    public string? DestinationWarehouseName { get; init; }
    public required string Status { get; init; } // Draft, Approved, Dispatched, PartiallyReceived, Received, Cancelled
    public DateTimeOffset TransferDateUtc { get; init; }
    public DateTimeOffset? DispatchedAtUtc { get; init; }
    public Guid? DispatchedByUserId { get; init; }
    public DateTimeOffset? ReceivedAtUtc { get; init; }
    public Guid? ReceivedByUserId { get; init; }
    public string? TrackingNumber { get; init; }
    public string? CarrierName { get; init; }
    public string? Notes { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public List<StockTransferLineDto> Lines { get; init; } = [];
}

public sealed record StockTransferLineDto
{
    public required Guid Id { get; init; }
    public required Guid StockTransferId { get; init; }
    public required Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? Sku { get; init; }
    public Guid? VariantId { get; init; }
    public Guid? SourceStorageLocationId { get; init; }
    public Guid? DestinationStorageLocationId { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal DispatchedQuantity { get; init; }
    public required decimal ReceivedQuantity { get; init; }
    public required decimal DamagedQuantity { get; init; }
    public required decimal MissingQuantity { get; init; }
    public required long UnitCostPaise { get; init; }
}

public sealed record CreateStockTransferRequest
{
    public required Guid SourceBranchId { get; init; }
    public required Guid SourceWarehouseId { get; init; }
    public required Guid DestinationBranchId { get; init; }
    public required Guid DestinationWarehouseId { get; init; }
    public string? TrackingNumber { get; init; }
    public string? CarrierName { get; init; }
    public string? Notes { get; init; }
    public required List<CreateStockTransferLineRequest> Lines { get; init; }
}

public sealed record CreateStockTransferLineRequest
{
    public required Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public Guid? SourceStorageLocationId { get; init; }
    public Guid? DestinationStorageLocationId { get; init; }
    public required decimal RequestedQuantity { get; init; }
}

public sealed record ReceiveStockTransferRequest
{
    public required List<ReceiveStockTransferLineRequest> Lines { get; init; }
}

public sealed record ReceiveStockTransferLineRequest
{
    public required Guid LineId { get; init; }
    public required decimal ReceivedQuantity { get; init; }
    public decimal DamagedQuantity { get; init; }
    public decimal MissingQuantity { get; init; }
    public Guid? DestinationStorageLocationId { get; init; }
}

// ==========================================
// 9. Reorder Management
// ==========================================

public sealed record ReorderRuleDto
{
    public required Guid Id { get; init; }
    public required Guid CompanyId { get; init; }
    public Guid? BranchId { get; init; }
    public Guid? WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public required Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? Sku { get; init; }
    public Guid? VariantId { get; init; }
    public Guid? PreferredSupplierId { get; init; }
    public string? PreferredSupplierName { get; init; }
    public required decimal MinStockLevel { get; init; }
    public required decimal MaxStockLevel { get; init; }
    public required decimal ReorderPoint { get; init; }
    public required decimal SafetyStock { get; init; }
    public required int LeadTimeDays { get; init; }
    public required decimal ReorderQuantity { get; init; }
    public required bool IsActive { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}

public sealed record UpsertReorderRuleRequest
{
    public Guid? BranchId { get; init; }
    public Guid? WarehouseId { get; init; }
    public required Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public Guid? PreferredSupplierId { get; init; }
    public required decimal MinStockLevel { get; init; }
    public required decimal MaxStockLevel { get; init; }
    public required decimal ReorderPoint { get; init; }
    public decimal SafetyStock { get; init; }
    public int LeadTimeDays { get; init; } = 7;
    public required decimal ReorderQuantity { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record ReorderSuggestionDto
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required string Sku { get; init; }
    public Guid? WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public required decimal CurrentStockOnHand { get; init; }
    public required decimal CurrentStockAvailable { get; init; }
    public required decimal ReorderPoint { get; init; }
    public required decimal SuggestedOrderQuantity { get; init; }
    public Guid? PreferredSupplierId { get; init; }
    public string? PreferredSupplierName { get; init; }
    public int LeadTimeDays { get; init; }
    public required string UrgencyLevel { get; init; } // Critical, Low, Normal
}

