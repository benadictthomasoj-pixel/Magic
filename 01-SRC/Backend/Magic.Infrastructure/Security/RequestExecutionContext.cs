using Magic.Contracts.Security;

namespace Magic.Infrastructure.Security;

/// <summary>
/// Dedicated mutation interface isolating context modification to authorized middleware.
/// </summary>
public interface IRequestContextSetter
{
    void SetBranchAndTerminal(Guid? branchId, Guid? terminalId);
    void SetAuthenticatedCaller(
        Guid userId,
        string username,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions);
}

/// <summary>
/// Per-request scoped security, branch, and terminal context container populated by API middleware.
/// </summary>
public sealed class MutableRequestContext : ICurrentUserContext, IBranchContext, ITerminalContext, IRequestContextSetter
{
    public Guid? UserId { get; private set; }
    public string? Username { get; private set; }
    public bool IsAuthenticated { get; private set; }
    public IReadOnlyCollection<string> Roles { get; private set; } = [];
    public IReadOnlyCollection<string> Permissions { get; private set; } = [];

    public Guid? BranchId { get; private set; }
    public bool HasBranch => BranchId.HasValue;

    public Guid? TerminalId { get; private set; }
    public bool HasTerminal => TerminalId.HasValue;

    public void SetBranchAndTerminal(Guid? branchId, Guid? terminalId)
    {
        BranchId = branchId;
        TerminalId = terminalId;
    }

    public void SetAuthenticatedCaller(
        Guid userId,
        string username,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions)
    {
        UserId = userId;
        Username = username;
        IsAuthenticated = true;
        Roles = roles;
        Permissions = permissions;
    }
}

/// <summary>
/// Deferred token validation boundary pending explicit OPEN-003 cryptographic engine approval.
/// </summary>
public sealed class DeferredTokenService : ITokenService
{
    public bool IsConfigured => false;

    public Task<TokenValidationOutcome> ValidateAccessTokenAsync(
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TokenValidationOutcome
        {
            IsValid = false,
            FailureReason = "Authentication token verification is deferred pending OPEN-003 resolution."
        });
    }
}

/// <summary>
/// Fail-closed default permission evaluator until OPEN-003 identity and RBAC seeding are enabled.
/// </summary>
public sealed class FailClosedPermissionEvaluator(ICurrentUserContext currentUser) : IPermissionEvaluator
{
    public Task<bool> HasPermissionAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(permissionCode))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(currentUser.Permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Fail-closed default supervisor elevation service until OPEN-003 supervisor authentication is configured.
/// </summary>
public sealed class FailClosedSupervisorElevationService : ISupervisorElevationService
{
    public Task<SupervisorElevationOutcome> ValidateElevationAsync(
        string supervisorAuthToken,
        string requiredPermission,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SupervisorElevationOutcome
        {
            IsElevated = false,
            Reason = "Supervisor elevation authentication is deferred pending OPEN-003 resolution."
        });
    }
}

