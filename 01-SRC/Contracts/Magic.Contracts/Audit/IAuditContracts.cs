namespace Magic.Contracts.Audit;

/// <summary>
/// Immutable audit entry structure aligned with audit.system_audit_logs.
/// </summary>
public sealed record AuditRecord
{
    public required Guid UserId { get; init; }
    public required Guid BranchId { get; init; }
    public Guid? TerminalId { get; init; }
    public required string ClientIp { get; init; }
    public required string ActionName { get; init; }
    public required string EntityName { get; init; }
    public required Guid EntityId { get; init; }
    public string? BeforeStateJson { get; init; }
    public string? AfterStateJson { get; init; }
    public Guid? OverrideBySupervisorId { get; init; }
    public required Guid CorrelationId { get; init; }
}

/// <summary>
/// Persists append-only audit entries inside the active transactional boundary.
/// </summary>
public interface IAuditLogger
{
    Task WriteAsync(AuditRecord record, CancellationToken cancellationToken = default);
}

