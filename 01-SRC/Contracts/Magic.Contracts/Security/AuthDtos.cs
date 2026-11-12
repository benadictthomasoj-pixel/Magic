namespace Magic.Contracts.Security;

public sealed record LoginRequest
{
    public required string UsernameOrEmail { get; init; }
    public required string Password { get; init; }
    public Guid? CompanyId { get; init; }
    public Guid? BranchId { get; init; }
    public Guid? TerminalId { get; init; }
}

public sealed record LoginResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime ExpiresAtUtc { get; init; }
    public required UserSummaryDto User { get; init; }
    public required CompanyContextDto Company { get; init; }
    public BranchContextDto? Branch { get; init; }
    public TerminalContextDto? Terminal { get; init; }
}

public sealed record RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}

public sealed record ChangePasswordRequest
{
    public required string CurrentPassword { get; init; }
    public required string NewPassword { get; init; }
    public required string ConfirmPassword { get; init; }
}

public sealed record PasswordResetRequest
{
    public required string UsernameOrEmail { get; init; }
}

public sealed record RegisterTerminalRequest
{
    public required string TerminalCode { get; init; }
    public required string MachineName { get; init; }
    public string? OperatingSystem { get; init; }
    public required Guid BranchId { get; init; }
}

public sealed record TerminalRegistrationResponse
{
    public required Guid TerminalId { get; init; }
    public required string TerminalCode { get; init; }
    public required string MachineName { get; init; }
    public required Guid BranchId { get; init; }
    public required bool IsAuthorized { get; init; }
}

public sealed record UserSummaryDto
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; }
    public required IReadOnlyCollection<string> Permissions { get; init; }
}

public sealed record CompanyContextDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
}

public sealed record BranchContextDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
}

public sealed record TerminalContextDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string MachineName { get; init; }
}

