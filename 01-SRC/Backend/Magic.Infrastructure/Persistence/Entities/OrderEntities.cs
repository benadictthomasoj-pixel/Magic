namespace Magic.Infrastructure.Persistence.Entities;

/// <summary>
/// Commercial sales order entity with multi-stage fulfillment.
/// </summary>
public sealed class SalesOrderRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid WarehouseId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedDeliveryDate { get; set; }
    public long SubtotalPaise { get; set; }
    public long TaxAmountPaise { get; set; }
    public long TotalAmountPaise { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = "Submitted"; // Draft, Submitted, Confirmed, PartiallyFulfilled, Fulfilled, Invoiced, Cancelled
    public Guid? ConfirmedByUserId { get; set; }
    public DateTimeOffset? ConfirmedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public WarehouseRecord? Warehouse { get; set; }
    public CustomerRecord? Customer { get; set; }
    public UserRecord? ConfirmedByUser { get; set; }
    public ICollection<SalesOrderLineRecord> Lines { get; set; } = new List<SalesOrderLineRecord>();
    public ICollection<OrderFulfillmentRecord> Fulfillments { get; set; } = new List<OrderFulfillmentRecord>();
}

/// <summary>
/// Line item within a sales order.
/// </summary>
public sealed class SalesOrderLineRecord
{
    public Guid Id { get; set; }
    public Guid SalesOrderId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal FulfilledQuantity { get; set; }
    public decimal InvoicedQuantity { get; set; }
    public long UnitPricePaise { get; set; }
    public long DiscountAmountPaise { get; set; }
    public decimal TaxRatePercent { get; set; }
    public long TaxAmountPaise { get; set; }
    public long LineTotalPaise { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, PartiallyFulfilled, Fulfilled, Cancelled

    public SalesOrderRecord? SalesOrder { get; set; }
    public ProductRecord? Product { get; set; }
    public ProductVariantRecord? Variant { get; set; }
}

/// <summary>
/// Shipment dispatch fulfillment against sales order lines.
/// </summary>
public sealed class OrderFulfillmentRecord
{
    public Guid Id { get; set; }
    public Guid SalesOrderId { get; set; }
    public string FulfillmentNumber { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string? CarrierName { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTimeOffset? DispatchedAtUtc { get; set; }
    public DateTimeOffset? DeliveredAtUtc { get; set; }
    public string Status { get; set; } = "Preparing"; // Preparing, Dispatched, Delivered, Failed
    public DateTimeOffset CreatedAtUtc { get; set; }

    public SalesOrderRecord? SalesOrder { get; set; }
    public WarehouseRecord? Warehouse { get; set; }
    public ICollection<OrderFulfillmentLineRecord> Lines { get; set; } = new List<OrderFulfillmentLineRecord>();
}

/// <summary>
/// Itemized fulfillment dispatch quantity.
/// </summary>
public sealed class OrderFulfillmentLineRecord
{
    public Guid Id { get; set; }
    public Guid OrderFulfillmentId { get; set; }
    public Guid SalesOrderLineId { get; set; }
    public decimal QuantityDispatched { get; set; }

    public OrderFulfillmentRecord? OrderFulfillment { get; set; }
    public SalesOrderLineRecord? SalesOrderLine { get; set; }
}

/// <summary>
/// Backorder for pending customer demand when stock is unavailable.
/// </summary>
public sealed class BackorderRecord
{
    public Guid Id { get; set; }
    public Guid SalesOrderId { get; set; }
    public Guid SalesOrderLineId { get; set; }
    public Guid ProductId { get; set; }
    public decimal PendingQuantity { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Allocated, Fulfilled, Cancelled
    public DateOnly? ExpectedRestockDate { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public SalesOrderRecord? SalesOrder { get; set; }
    public SalesOrderLineRecord? SalesOrderLine { get; set; }
    public ProductRecord? Product { get; set; }
}

