namespace Magic.Infrastructure.Persistence.Entities;

public sealed class CompanyRecord
{
    public Guid Id { get; set; }
    public required string LegalName { get; set; }
    public required string TradeName { get; set; }
    public string? Gstin { get; set; }
    public string? Pan { get; set; }
    public required string Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public int FiscalYearStartMonth { get; set; } = 4; // April for India
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<BranchRecord> Branches { get; set; } = [];
}

public sealed class BranchRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string BranchCode { get; set; }
    public required string BranchName { get; set; }
    public required string Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Gstin { get; set; }
    public string? BranchManager { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public CompanyRecord? Company { get; set; }
    public ICollection<TerminalRegistrationRecord> Terminals { get; set; } = [];
}

public sealed class TerminalRegistrationRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public required string TerminalCode { get; set; }
    public required string MachineName { get; set; }
    public string? OperatingSystem { get; set; }
    public Guid? AssignedCashierUserId { get; set; }
    public bool IsAuthorized { get; set; } = true;
    public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastHeartbeatAtUtc { get; set; }
    public string? PosPrinterModel { get; set; }
    public int ReceiptWidthMm { get; set; } = 80;

    public BranchRecord? Branch { get; set; }
}

public sealed class InterBranchTransferRequestRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string TransferNumber { get; set; }
    public Guid SourceBranchId { get; set; }
    public Guid SourceWarehouseId { get; set; }
    public Guid DestinationBranchId { get; set; }
    public Guid DestinationWarehouseId { get; set; }
    public string Status { get; set; } = "Requested"; // "Requested", "Approved", "Dispatched", "Received", "Rejected"
    public string? Notes { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<InterBranchTransferLineRecord> Lines { get; set; } = [];
}

public sealed class InterBranchTransferLineRecord
{
    public Guid Id { get; set; }
    public Guid TransferRequestId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal DispatchedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }

    public InterBranchTransferRequestRecord? TransferRequest { get; set; }
}

