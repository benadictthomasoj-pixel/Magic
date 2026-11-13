using Magic.Contracts.Inventory;

namespace Magic.Application.Inventory.Services;

public interface IWarehouseService
{
    Task<(IReadOnlyList<WarehouseDto> Items, int TotalCount)> GetWarehousesAsync(
        Guid companyId,
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<WarehouseDto> GetWarehouseByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
    Task<WarehouseDto> CreateWarehouseAsync(Guid companyId, Guid userId, CreateWarehouseRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseDto> UpdateWarehouseAsync(Guid companyId, Guid id, Guid userId, UpdateWarehouseRequest request, CancellationToken cancellationToken = default);
    Task ArchiveWarehouseAsync(Guid companyId, Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StorageLocationDto>> GetStorageLocationsAsync(Guid companyId, Guid warehouseId, CancellationToken cancellationToken = default);
    Task<StorageLocationDto> CreateStorageLocationAsync(Guid companyId, Guid warehouseId, CreateStorageLocationRequest request, CancellationToken cancellationToken = default);
}

public interface IInventoryLedgerService
{
    Task<(IReadOnlyList<StockBalanceDto> Items, int TotalCount)> GetStockBalancesAsync(
        Guid companyId,
        Guid? branchId,
        Guid? warehouseId,
        Guid? productId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<StockMovementDto> Items, int TotalCount)> GetStockMovementsAsync(
        Guid companyId,
        Guid? warehouseId,
        Guid? productId,
        string? movementType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task PostOpeningStockAsync(Guid companyId, Guid userId, PostOpeningStockRequest request, CancellationToken cancellationToken = default);
}

public interface IStockAdjustmentService
{
    Task<(IReadOnlyList<StockAdjustmentDto> Items, int TotalCount)> GetAdjustmentsAsync(
        Guid companyId,
        Guid? warehouseId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<StockAdjustmentDto> GetAdjustmentByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
    Task<StockAdjustmentDto> CreateAdjustmentAsync(Guid companyId, Guid userId, CreateStockAdjustmentRequest request, CancellationToken cancellationToken = default);
    Task<StockAdjustmentDto> ApproveAdjustmentAsync(Guid companyId, Guid id, Guid approverId, CancellationToken cancellationToken = default);
    Task<StockAdjustmentDto> RejectAdjustmentAsync(Guid companyId, Guid id, Guid rejectorId, string? reason, CancellationToken cancellationToken = default);
}

public interface IStockCountService
{
    Task<(IReadOnlyList<StockCountDto> Items, int TotalCount)> GetCountsAsync(
        Guid companyId,
        Guid? warehouseId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<StockCountDto> GetCountByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
    Task<StockCountDto> CreateCountAsync(Guid companyId, Guid userId, CreateStockCountRequest request, CancellationToken cancellationToken = default);
    Task<StockCountDto> RecordCountLinesAsync(Guid companyId, Guid id, Guid counterId, RecordStockCountLinesRequest request, CancellationToken cancellationToken = default);
    Task<StockCountDto> ReconcileCountAsync(Guid companyId, Guid id, Guid approverId, CancellationToken cancellationToken = default);
}

public interface IPurchaseOrderService
{
    Task<(IReadOnlyList<PurchaseOrderDto> Items, int TotalCount)> GetPurchaseOrdersAsync(
        Guid companyId,
        Guid? supplierId,
        string? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PurchaseOrderDto> GetPurchaseOrderByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(Guid companyId, Guid userId, CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> ApprovePurchaseOrderAsync(Guid companyId, Guid id, Guid approverId, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> CancelPurchaseOrderAsync(Guid companyId, Guid id, Guid userId, string? reason, CancellationToken cancellationToken = default);
}

public interface IGoodsReceiptService
{
    Task<(IReadOnlyList<GoodsReceiptNoteDto> Items, int TotalCount)> GetGoodsReceiptsAsync(
        Guid companyId,
        Guid? supplierId,
        Guid? purchaseOrderId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<GoodsReceiptNoteDto> GetGoodsReceiptByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
    Task<GoodsReceiptNoteDto> CreateGoodsReceiptAsync(Guid companyId, Guid userId, CreateGoodsReceiptRequest request, CancellationToken cancellationToken = default);
    Task<GoodsReceiptNoteDto> ConfirmGoodsReceiptAsync(Guid companyId, Guid id, Guid confirmerId, CancellationToken cancellationToken = default);
}

public interface IPurchaseReturnService
{
    Task<(IReadOnlyList<PurchaseReturnDto> Items, int TotalCount)> GetPurchaseReturnsAsync(
        Guid companyId,
        Guid? supplierId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PurchaseReturnDto> GetPurchaseReturnByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseReturnDto> CreatePurchaseReturnAsync(Guid companyId, Guid userId, CreatePurchaseReturnRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseReturnDto> ConfirmPurchaseReturnAsync(Guid companyId, Guid id, Guid confirmerId, CancellationToken cancellationToken = default);
}

public interface IStockTransferService
{
    Task<(IReadOnlyList<StockTransferDto> Items, int TotalCount)> GetTransfersAsync(
        Guid companyId,
        Guid? sourceWarehouseId,
        Guid? destinationWarehouseId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<StockTransferDto> GetTransferByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
    Task<StockTransferDto> CreateTransferAsync(Guid companyId, Guid userId, CreateStockTransferRequest request, CancellationToken cancellationToken = default);
    Task<StockTransferDto> DispatchTransferAsync(Guid companyId, Guid id, Guid dispatcherId, CancellationToken cancellationToken = default);
    Task<StockTransferDto> ReceiveTransferAsync(Guid companyId, Guid id, Guid receiverId, ReceiveStockTransferRequest request, CancellationToken cancellationToken = default);
    Task<StockTransferDto> CancelTransferAsync(Guid companyId, Guid id, Guid userId, string? reason, CancellationToken cancellationToken = default);
}

public interface IReorderService
{
    Task<(IReadOnlyList<ReorderRuleDto> Items, int TotalCount)> GetReorderRulesAsync(
        Guid companyId,
        Guid? warehouseId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ReorderRuleDto> UpsertReorderRuleAsync(Guid companyId, UpsertReorderRuleRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReorderSuggestionDto>> GetReorderSuggestionsAsync(Guid companyId, Guid? warehouseId, CancellationToken cancellationToken = default);
}

