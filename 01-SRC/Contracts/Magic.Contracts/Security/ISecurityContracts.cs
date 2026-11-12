namespace Magic.Contracts.Security;

/// <summary>
/// Represents the resolved caller identity for the current API request.
/// Concrete token validation remains deferred pending OPEN-003 approval.
/// </summary>
public interface ICurrentUserContext
{
    Guid? UserId { get; }
    string? Username { get; }
    bool IsAuthenticated { get; }
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Permissions { get; }
}

/// <summary>
/// Active operational branch context resolved from verified headers or claims.
/// </summary>
public interface IBranchContext
{
    Guid? BranchId { get; }
    bool HasBranch { get; }
}

/// <summary>
/// Active client hardware terminal context resolved from request headers.
/// </summary>
public interface ITerminalContext
{
    Guid? TerminalId { get; }
    bool HasTerminal { get; }
}

/// <summary>
/// Evaluates module action permissions in &lt;domain&gt;:&lt;resource&gt;:&lt;action&gt; format.
/// </summary>
public interface IPermissionEvaluator
{
    Task<bool> HasPermissionAsync(string permissionCode, CancellationToken cancellationToken = default);
}

/// <summary>
/// Deferred token lifecycle contract boundary (OPEN-003).
/// Phase 1 defines the integration seam only without custom cryptographic implementations.
/// </summary>
public interface ITokenService
{
    bool IsConfigured { get; }
    Task<TokenValidationOutcome> ValidateAccessTokenAsync(string rawToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome produced by a configured ITokenService implementation.
/// </summary>
public sealed record TokenValidationOutcome
{
    public required bool IsValid { get; init; }
    public Guid? UserId { get; init; }
    public string? Username { get; init; }
    public string? FailureReason { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = [];
    public IReadOnlyCollection<string> Permissions { get; init; } = [];
}

