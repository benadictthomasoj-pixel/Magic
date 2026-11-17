namespace Magic.Contracts.Branches;

public sealed record CompanyProfileDto(
    Guid Id,
    string LegalName,
    string TradeName,
    string? Gstin,
    string? Pan,
    string Address,
    string? Phone,
    string? Email,
    string CurrencyCode,
    int FiscalYearStartMonth,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record UpdateCompanyProfileRequest(
    string LegalName,
    string TradeName,
    string? Gstin,
    string? Pan,
    string Address,
    string? Phone,
    string? Email,
    int FiscalYearStartMonth);

public sealed record BranchDto(
    Guid Id,
    Guid CompanyId,
    string BranchCode,
    string BranchName,
    string Address,
    string? Phone,
    string? Email,
    string? Gstin,
    string? BranchManager,
    bool IsActive,
    int WarehouseCount,
    int TerminalCount,
    DateTime CreatedAtUtc);

public sealed record CreateBranchRequest(
    string BranchCode,
    string BranchName,
    string Address,
    string? Phone,
    string? Email,
    string? Gstin,
    string? BranchManager,
    IReadOnlyList<Guid>? WarehouseIds);

public sealed record UpdateBranchRequest(
    string BranchName,
    string Address,
    string? Phone,
    string? Email,
    string? Gstin,
    string? BranchManager,
    bool IsActive,
    IReadOnlyList<Guid>? WarehouseIds);

public sealed record TerminalDetailsDto(
    Guid Id,
    Guid CompanyId,
    Guid BranchId,
    string BranchName,
    string TerminalCode,
    string MachineName,
    string? OperatingSystem,
    string? AssignedCashierName,
    bool IsAuthorized,
    DateTime RegisteredAtUtc,
    DateTime? LastHeartbeatAtUtc,
    string? PosPrinterModel,
    int ReceiptWidthMm);

public sealed record RegisterTerminalRequest(
    Guid BranchId,
    string TerminalCode,
    string MachineName,
    string? OperatingSystem,
    string? PosPrinterModel,
    int ReceiptWidthMm);

public sealed record UpdateTerminalStatusRequest(
    bool IsAuthorized,
    Guid? AssignedCashierUserId);

public sealed record InterBranchTransferRequestDto(
    Guid Id,
    Guid CompanyId,
    string TransferNumber,
    Guid SourceBranchId,
    string SourceBranchName,
    Guid SourceWarehouseId,
    string SourceWarehouseName,
    Guid DestinationBranchId,
    string DestinationBranchName,
    Guid DestinationWarehouseId,
    string DestinationWarehouseName,
    string Status, // "Requested", "Approved", "Dispatched", "Received", "Rejected"
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record CreateInterBranchTransferRequest(
    Guid SourceBranchId,
    Guid SourceWarehouseId,
    Guid DestinationBranchId,
    Guid DestinationWarehouseId,
    string? Notes,
    IReadOnlyList<InterBranchTransferLineRequest> Lines);

public sealed record InterBranchTransferLineRequest(
    Guid ProductId,
    Guid? VariantId,
    decimal Quantity);

