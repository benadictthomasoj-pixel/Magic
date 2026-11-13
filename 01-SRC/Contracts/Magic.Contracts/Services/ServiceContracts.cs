namespace Magic.Contracts.Services;

public sealed record ServiceCatalogDto(
    Guid Id,
    Guid CompanyId,
    string Code,
    string Name,
    string Category,
    string? Description,
    long StandardLaborChargePaise,
    int EstimatedDurationMinutes,
    bool IsActive);

public sealed record CreateServiceCatalogRequest(
    string Code,
    string Name,
    string Category,
    string? Description,
    long StandardLaborChargePaise,
    int EstimatedDurationMinutes);

public sealed record ServiceQuotationLineDto(
    Guid Id,
    Guid ServiceQuotationId,
    string LineType,
    Guid? ProductId,
    string? ProductCode,
    string Description,
    decimal Quantity,
    long UnitRatePaise,
    long LineTotalPaise);

public sealed record ServiceQuotationDto(
    Guid Id,
    Guid ServiceRequestId,
    string QuotationNumber,
    long LaborTotalPaise,
    long PartsTotalPaise,
    long DiscountPaise,
    long TaxAmountPaise,
    long TotalAmountPaise,
    DateOnly ValidUntil,
    string Status,
    DateTimeOffset? ApprovedAtUtc,
    IReadOnlyList<ServiceQuotationLineDto> Lines);

public sealed record CreateServiceQuotationLineRequest(
    string LineType,
    Guid? ProductId,
    string Description,
    decimal Quantity,
    long UnitRatePaise);

public sealed record CreateServiceQuotationRequest(
    DateOnly ValidUntil,
    long DiscountPaise,
    decimal TaxRatePercent,
    IReadOnlyList<CreateServiceQuotationLineRequest> Lines);

public sealed record ServicePartConsumptionDto(
    Guid Id,
    Guid ServiceRequestId,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    Guid WarehouseId,
    string WarehouseName,
    decimal Quantity,
    long UnitCostPaise,
    Guid? StockMovementId,
    DateTimeOffset ConsumedAtUtc);

public sealed record ConsumeServicePartRequest(
    Guid ProductId,
    Guid WarehouseId,
    decimal Quantity);

public sealed record ServiceRequestDto(
    Guid Id,
    Guid CompanyId,
    Guid BranchId,
    string RequestNumber,
    Guid CustomerId,
    string CustomerName,
    Guid ServiceCatalogId,
    string ServiceCatalogName,
    string ItemDescription,
    string? SerialNumber,
    string ReportedIssue,
    string Priority,
    string Status,
    Guid? AssignedEmployeeId,
    string? AssignedEmployeeName,
    DateOnly ReceivedDate,
    DateOnly? TargetCompletionDate,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? DeliveredAtUtc,
    string? TechnicianNotes,
    Guid? LinkedSalesInvoiceId,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<ServiceQuotationDto>? Quotations,
    IReadOnlyList<ServicePartConsumptionDto>? ConsumedParts);

public sealed record CreateServiceRequest(
    Guid CustomerId,
    Guid ServiceCatalogId,
    string ItemDescription,
    string? SerialNumber,
    string ReportedIssue,
    string Priority,
    DateOnly ReceivedDate,
    DateOnly? TargetCompletionDate);

public sealed record UpdateServiceRequestStatusRequest(
    string Status,
    Guid? AssignedEmployeeId,
    string? TechnicianNotes);

public sealed record InvoiceServiceRequest(
    string PaymentMethod,
    Guid? BankCashAccountId);

public sealed record ServiceSummaryDto(
    int OpenRequestsCount,
    int InProgressCount,
    int WaitingForPartsCount,
    int CompletedCount);

