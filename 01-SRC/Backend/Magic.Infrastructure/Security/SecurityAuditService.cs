using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.Extensions.Logging;

namespace Magic.Infrastructure.Security;

public interface ISecurityAuditService
{
    Task LogEventAsync(
        Guid companyId,
        string eventType,
        Guid? userId = null,
        string? username = null,
        Guid? branchId = null,
        Guid? terminalId = null,
        string? ipAddress = null,
        string? detailsJson = null,
        CancellationToken cancellationToken = default);
}

public sealed class SecurityAuditService(
    MagicDbContext dbContext,
    ILogger<SecurityAuditService> logger) : ISecurityAuditService
{
    public async Task LogEventAsync(
        Guid companyId,
        string eventType,
        Guid? userId = null,
        string? username = null,
        Guid? branchId = null,
        Guid? terminalId = null,
        string? ipAddress = null,
        string? detailsJson = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var record = new SecurityAuditEventRecord
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = branchId,
                TerminalId = terminalId,
                UserId = userId,
                Username = username,
                EventType = eventType,
                IpAddress = ipAddress,
                DetailsJson = detailsJson,
                TimestampUtc = DateTime.UtcNow
            };

            dbContext.SecurityAuditEvents.Add(record);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write security audit event {EventType} for user {Username}", eventType, username);
        }
    }
}
