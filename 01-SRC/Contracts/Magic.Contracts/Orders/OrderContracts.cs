namespace Magic.Contracts.Orders;

public sealed record SalesOrderLineDto(
    Guid Id,
    Guid SalesOrderId,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    Guid? VariantId,
    decimal Quantity,
    decimal FulfilledQuantity,
    decimal InvoicedQuantity,
    long UnitPricePaise,
    long DiscountAmountPaise,
    decimal TaxRatePercent,
    long TaxAmountPaise,
    long LineTotalPaise,
    string Status);

public sealed record SalesOrderDto(
    Guid Id,
    Guid CompanyId,
    Guid BranchId,
    Guid WarehouseId,
    string WarehouseName,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    long SubtotalPaise,
    long TaxAmountPaise,
    long TotalAmountPaise,
    string DeliveryAddress,
    string? Notes,
    string Status,
    Guid? ConfirmedByUserId,
    DateTimeOffset? ConfirmedAtUtc,
    IReadOnlyList<SalesOrderLineDto> Lines);

public sealed record CreateSalesOrderLineRequest(
    Guid ProductId,
    Guid? VariantId,
    decimal Quantity,
    long UnitPricePaise,
    long DiscountAmountPaise,
    decimal TaxRatePercent);

public sealed record CreateSalesOrderRequest(
    Guid CustomerId,
    Guid WarehouseId,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    string DeliveryAddress,
    string? Notes,
    IReadOnlyList<CreateSalesOrderLineRequest> Lines);

public sealed record OrderFulfillmentLineDto(
    Guid Id,
    Guid SalesOrderLineId,
    string ProductName,
    decimal QuantityDispatched);

public sealed record OrderFulfillmentDto(
    Guid Id,
    Guid SalesOrderId,
    string FulfillmentNumber,
    Guid WarehouseId,
    string WarehouseName,
    string? CarrierName,
    string? TrackingNumber,
    DateTimeOffset? DispatchedAtUtc,
    DateTimeOffset? DeliveredAtUtc,
    string Status,
    IReadOnlyList<OrderFulfillmentLineDto> Lines);

public sealed record FulfillOrderLineRequest(
    Guid SalesOrderLineId,
    decimal QuantityDispatched);

public sealed record FulfillOrderRequest(
    string? CarrierName,
    string? TrackingNumber,
    IReadOnlyList<FulfillOrderLineRequest> Lines);

public sealed record BackorderDto(
    Guid Id,
    Guid SalesOrderId,
    string OrderNumber,
    Guid SalesOrderLineId,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    decimal PendingQuantity,
    string Status,
    DateOnly? ExpectedRestockDate,
    DateTimeOffset CreatedAtUtc);

public sealed record OrderSummaryDto(
    int TotalOrdersCount,
    int PendingFulfillmentCount,
    int BackordersCount,
    long TotalOpenOrdersValuePaise);

