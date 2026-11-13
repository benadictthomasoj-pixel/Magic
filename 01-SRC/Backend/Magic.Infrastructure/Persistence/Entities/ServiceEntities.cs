namespace Magic.Infrastructure.Persistence.Entities;

/// <summary>
/// Service catalog item specifying labor charges and duration.
/// </summary>
public sealed class ServiceCatalogRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Repair";
    public string? Description { get; set; }
    public long StandardLaborChargePaise { get; set; }
    public int EstimatedDurationMinutes { get; set; } = 60;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>
/// Service desk ticket tracking customer repairs, complaints, and workflow states.
/// </summary>
public sealed class ServiceRequestRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid ServiceCatalogId { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string ReportedIssue { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Urgent
    public string Status { get; set; } = "NewRequest"; // NewRequest, Inspection, Quotation, Approved, InProgress, WaitingForParts, Completed, Delivered, Cancelled
    public Guid? AssignedEmployeeId { get; set; }
    public DateOnly ReceivedDate { get; set; }
    public DateOnly? TargetCompletionDate { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? DeliveredAtUtc { get; set; }
    public string? TechnicianNotes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public CustomerRecord? Customer { get; set; }
    public ServiceCatalogRecord? ServiceCatalog { get; set; }
    public EmployeeRecord? AssignedEmployee { get; set; }
    public ICollection<ServiceQuotationRecord> Quotations { get; set; } = new List<ServiceQuotationRecord>();
    public ICollection<ServicePartConsumptionRecord> ConsumedParts { get; set; } = new List<ServicePartConsumptionRecord>();
}

/// <summary>
/// Formal repair quotation for labor and replacement spare parts.
/// </summary>
public sealed class ServiceQuotationRecord
{
    public Guid Id { get; set; }
    public Guid ServiceRequestId { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public long LaborTotalPaise { get; set; }
    public long PartsTotalPaise { get; set; }
    public long DiscountPaise { get; set; }
    public long TaxAmountPaise { get; set; }
    public long TotalAmountPaise { get; set; }
    public DateOnly ValidUntil { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Sent, Approved, Rejected, Expired
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public ServiceRequestRecord? ServiceRequest { get; set; }
    public ICollection<ServiceQuotationLineRecord> Lines { get; set; } = new List<ServiceQuotationLineRecord>();
}

/// <summary>
/// Quotation line item representing labor fees or parts replacement.
/// </summary>
public sealed class ServiceQuotationLineRecord
{
    public Guid Id { get; set; }
    public Guid ServiceQuotationId { get; set; }
    public string LineType { get; set; } = "Labor"; // Labor, Part
    public Guid? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public long UnitRatePaise { get; set; }
    public long LineTotalPaise { get; set; }

    public ServiceQuotationRecord? ServiceQuotation { get; set; }
    public ProductRecord? Product { get; set; }
}

/// <summary>
/// Spare parts consumed from physical inventory for service execution.
/// </summary>
public sealed class ServicePartConsumptionRecord
{
    public Guid Id { get; set; }
    public Guid ServiceRequestId { get; set; }
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public long UnitCostPaise { get; set; }
    public Guid? StockMovementId { get; set; }
    public DateTimeOffset ConsumedAtUtc { get; set; }

    public ServiceRequestRecord? ServiceRequest { get; set; }
    public ProductRecord? Product { get; set; }
    public WarehouseRecord? Warehouse { get; set; }
    public StockMovementRecord? StockMovement { get; set; }
}

/// <summary>
/// Links a completed service request to a sales tax invoice.
/// </summary>
public sealed class ServiceInvoiceLinkRecord
{
    public Guid Id { get; set; }
    public Guid ServiceRequestId { get; set; }
    public Guid SalesInvoiceId { get; set; }
    public DateTimeOffset InvoicedAtUtc { get; set; }

    public ServiceRequestRecord? ServiceRequest { get; set; }
    public SalesInvoiceRecord? SalesInvoice { get; set; }
}

