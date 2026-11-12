using Magic.Contracts.Audit;
using Magic.Contracts.Common;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;

namespace Magic.Infrastructure.Audit;

/// <summary>
/// Persists append-only audit trail records into audit.system_audit_logs.
/// </summary>
public sealed class PostgresAuditLogger(
    MagicDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : IAuditLogger
{
    public Task WriteAsync(AuditRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        dbContext.SystemAuditLogs.Add(new SystemAuditLogRecord
        {
            EventTimestampUtc = dateTimeProvider.UtcNow,
            UserId = record.UserId,
            BranchId = record.BranchId,
            TerminalId = record.TerminalId,
            ClientIp = record.ClientIp,
            ActionName = record.ActionName,
            EntityName = record.EntityName,
            EntityId = record.EntityId,
            BeforeStateJson = record.BeforeStateJson,
            AfterStateJson = record.AfterStateJson,
            OverrideBySupervisorId = record.OverrideBySupervisorId,
            CorrelationId = record.CorrelationId
        });

        return Task.CompletedTask;
    }
}

