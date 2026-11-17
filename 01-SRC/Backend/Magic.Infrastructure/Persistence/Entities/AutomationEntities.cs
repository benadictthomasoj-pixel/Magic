namespace Magic.Infrastructure.Persistence.Entities;

public sealed class AutomationRuleRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public required string TriggerType { get; set; } // "LowStock", "OverduePayment", "PendingApproval", "DailySummary"
    public string ConditionJson { get; set; } = "{}";
    public required string ActionType { get; set; } // "CreateNotification", "SendTask", "ReorderSuggestion"
    public string ActionPayloadJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastTriggeredAtUtc { get; set; }
    public int ExecutionCount { get; set; }

    public ICollection<AutomationExecutionRecord> Executions { get; set; } = [];
}

public sealed class AutomationExecutionRecord
{
    public Guid Id { get; set; }
    public Guid RuleId { get; set; }
    public DateTime TriggeredAtUtc { get; set; } = DateTime.UtcNow;
    public required string Status { get; set; } // "Success", "Failed", "Skipped"
    public string? ResultMessage { get; set; }
    public string? ErrorDetails { get; set; }
    public string? TraceCorrelationId { get; set; }

    public AutomationRuleRecord? Rule { get; set; }
}

public sealed class NotificationRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? TargetUserId { get; set; }
    public required string Category { get; set; } // "System", "Inventory", "Finance", "Approvals", "Automation"
    public required string Priority { get; set; } // "Low", "Medium", "High", "Critical"
    public required string Title { get; set; }
    public required string Message { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

