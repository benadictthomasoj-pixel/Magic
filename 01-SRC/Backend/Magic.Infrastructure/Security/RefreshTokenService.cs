using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Magic.Infrastructure.Security;

public sealed record RotateTokenResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public bool ReuseDetected { get; init; }
    public string? NewRawAccessToken { get; init; }
    public string? NewRawRefreshToken { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public UserRecord? User { get; init; }
}

public interface IRefreshTokenService
{
    Task<RefreshTokenRecord> CreateRefreshTokenAsync(
        Guid userId,
        Guid companyId,
        Guid? branchId,
        Guid? terminalId,
        string rawRefreshToken,
        DateTime expiresAtUtc,
        Guid? existingFamilyId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<RotateTokenResult> RotateRefreshTokenAsync(
        string rawRefreshToken,
        string ipAddress,
        CancellationToken cancellationToken = default);

    Task RevokeFamilyAsync(
        Guid tokenFamily,
        string reason,
        CancellationToken cancellationToken = default);

    Task RevokeUserSessionsAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default);
}

public sealed class RefreshTokenService(
    MagicDbContext dbContext,
    IJwtTokenService tokenService,
    ILogger<RefreshTokenService> logger) : IRefreshTokenService
{
    public async Task<RefreshTokenRecord> CreateRefreshTokenAsync(
        Guid userId,
        Guid companyId,
        Guid? branchId,
        Guid? terminalId,
        string rawRefreshToken,
        DateTime expiresAtUtc,
        Guid? existingFamilyId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = tokenService.HashToken(rawRefreshToken);
        var family = existingFamilyId ?? Guid.NewGuid();

        var record = new RefreshTokenRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyId = companyId,
            BranchId = branchId,
            TerminalId = terminalId,
            TokenHash = tokenHash,
            TokenFamily = family,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            IsRevoked = false
        };

        dbContext.RefreshTokens.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        return record;
    }

    public async Task<RotateTokenResult> RotateRefreshTokenAsync(
        string rawRefreshToken,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return new RotateTokenResult { Success = false, ErrorMessage = "Refresh token is missing." };
        }

        var tokenHash = tokenService.HashToken(rawRefreshToken);

        var tokenRecord = await dbContext.RefreshTokens
            .Include(t => t.User!)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role!)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (tokenRecord == null)
        {
            logger.LogWarning("Refresh token rotation failed: Token hash not found. IP: {IpAddress}", ipAddress);
            return new RotateTokenResult { Success = false, ErrorMessage = "Invalid refresh token." };
        }

        // REUSE DETECTION: If token was already revoked, immediately invalidate entire token family!
        if (tokenRecord.IsRevoked)
        {
            logger.LogCritical(
                "SECURITY ALERT: Refresh token reuse detected for TokenFamily {FamilyId}, UserId {UserId}, IP {IpAddress}. Revoking all family tokens.",
                tokenRecord.TokenFamily, tokenRecord.UserId, ipAddress);

            await RevokeFamilyAsync(
                tokenRecord.TokenFamily,
                $"Compromised: Refresh token reuse detected at {DateTime.UtcNow:O} from IP {ipAddress}",
                cancellationToken);

            // Record security audit event
            dbContext.SecurityAuditEvents.Add(new SecurityAuditEventRecord
            {
                Id = Guid.NewGuid(),
                CompanyId = tokenRecord.CompanyId,
                BranchId = tokenRecord.BranchId,
                TerminalId = tokenRecord.TerminalId,
                UserId = tokenRecord.UserId,
                Username = tokenRecord.User?.Username,
                EventType = "TOKEN_REUSE_DETECTED",
                IpAddress = ipAddress,
                DetailsJson = $"{{\"tokenFamily\":\"{tokenRecord.TokenFamily}\",\"reason\":\"Attempted use of revoked token\"}}",
                TimestampUtc = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync(cancellationToken);

            return new RotateTokenResult
            {
                Success = false,
                ErrorMessage = "Security violation: Refresh token reuse detected. All active sessions have been revoked.",
                ReuseDetected = true
            };
        }

        // Check expiration
        if (tokenRecord.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return new RotateTokenResult { Success = false, ErrorMessage = "Refresh token has expired. Please sign in again." };
        }

        var user = tokenRecord.User;
        if (user == null || !user.IsActive || user.IsLockedOut)
        {
            return new RotateTokenResult { Success = false, ErrorMessage = "User account is inactive or locked." };
        }

        // Issue new token pair
        var (newRawRefreshToken, newRefreshTokenHash, newExpiresAtUtc) = tokenService.GenerateRefreshToken();

        // Mark current token revoked with replacement pointer
        tokenRecord.IsRevoked = true;
        tokenRecord.RevokedAtUtc = DateTime.UtcNow;
        tokenRecord.RevocationReason = "Rotated";
        tokenRecord.ReplacedByTokenHash = newRefreshTokenHash;

        // Create new replacement token in same family
        var newRecord = new RefreshTokenRecord
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CompanyId = tokenRecord.CompanyId,
            BranchId = tokenRecord.BranchId,
            TerminalId = tokenRecord.TerminalId,
            TokenHash = newRefreshTokenHash,
            TokenFamily = tokenRecord.TokenFamily,
            ExpiresAtUtc = newExpiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            IsRevoked = false
        };

        dbContext.RefreshTokens.Add(newRecord);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Extract roles and permissions
        var roles = user.UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.Code)
            .Distinct()
            .ToList();

        var permissions = user.UserRoles
            .Where(ur => ur.Role != null)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .Where(rp => rp.Permission != null)
            .Select(rp => rp.Permission!.Code)
            .Distinct()
            .ToList();

        var newAccessToken = tokenService.IssueAccessToken(
            userId: user.Id,
            username: user.Username,
            email: user.Email,
            companyId: tokenRecord.CompanyId,
            branchId: tokenRecord.BranchId,
            terminalId: tokenRecord.TerminalId,
            roles: roles,
            permissions: permissions);

        return new RotateTokenResult
        {
            Success = true,
            NewRawAccessToken = newAccessToken,
            NewRawRefreshToken = newRawRefreshToken,
            ExpiresAtUtc = newExpiresAtUtc,
            User = user
        };
    }

    public async Task RevokeFamilyAsync(
        Guid tokenFamily,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(t => t.TokenFamily == tokenFamily && !t.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var t in tokens)
        {
            t.IsRevoked = true;
            t.RevokedAtUtc = DateTime.UtcNow;
            t.RevocationReason = reason;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeUserSessionsAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var t in tokens)
        {
            t.IsRevoked = true;
            t.RevokedAtUtc = DateTime.UtcNow;
            t.RevocationReason = reason;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

