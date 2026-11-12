namespace Magic.Infrastructure.Persistence.Entities;

/// <summary>
/// Append-only audit trail persistence entity mapped to audit.system_audit_logs.
/// </summary>
public sealed class SystemAuditLogRecord
{
    public long Id { get; set; }
    public DateTimeOffset EventTimestampUtc { get; set; }
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? TerminalId { get; set; }
    public string ClientIp { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? BeforeStateJson { get; set; }
    public string? AfterStateJson { get; set; }
    public Guid? OverrideBySupervisorId { get; set; }
    public Guid CorrelationId { get; set; }
}

