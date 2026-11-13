namespace Magic.Infrastructure.Persistence.Entities;

public sealed class ApprovalWorkflowRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Module { get; set; } // "Expenses", "Purchases", "StockAdjustment", "Payroll", "Payments"
    public required string TransactionType { get; set; }
    public long MinimumAmountPaise { get; set; }
    public int RequiredApprovalsCount { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ApprovalRequestRecord> Requests { get; set; } = [];
}

public sealed class ApprovalRequestRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? WorkflowId { get; set; }
    public required string SourceModule { get; set; }
    public Guid SourceDocumentId { get; set; }
    public required string ReferenceNumber { get; set; }
    public long AmountPaise { get; set; }
    public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"
    public Guid RequestedByUserId { get; set; }
    public required string RequestedByUserName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAtUtc { get; set; }

    public ApprovalWorkflowRecord? Workflow { get; set; }
    public ICollection<ApprovalActionRecord> Actions { get; set; } = [];
}

public sealed class ApprovalActionRecord
{
    public Guid Id { get; set; }
    public Guid ApprovalRequestId { get; set; }
    public Guid ApproverUserId { get; set; }
    public required string ApproverName { get; set; }
    public required string Action { get; set; } // "Approved", "Rejected"
    public string? Reason { get; set; }
    public DateTime ActedAtUtc { get; set; } = DateTime.UtcNow;

    public ApprovalRequestRecord? ApprovalRequest { get; set; }
}
