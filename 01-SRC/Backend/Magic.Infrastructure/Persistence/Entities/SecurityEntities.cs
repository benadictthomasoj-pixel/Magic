namespace Magic.Infrastructure.Persistence.Entities;

public sealed class UserRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Username { get; set; }
    public required string NormalizedUsername { get; set; }
    public required string Email { get; set; }
    public required string NormalizedEmail { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    public required string PasswordSalt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsLockedOut { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public int FailedLoginAttempts { get; set; }
    public Guid? DefaultBranchId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }

    public ICollection<UserRoleRecord> UserRoles { get; set; } = [];
    public ICollection<UserBranchAssignmentRecord> BranchAssignments { get; set; } = [];
    public ICollection<RefreshTokenRecord> RefreshTokens { get; set; } = [];
}

public sealed class RoleRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<UserRoleRecord> UserRoles { get; set; } = [];
    public ICollection<RolePermissionRecord> RolePermissions { get; set; } = [];
}

public sealed class PermissionRecord
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Module { get; set; }
    public required string Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<RolePermissionRecord> RolePermissions { get; set; } = [];
}

public sealed class UserRoleRecord
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime AssignedAtUtc { get; set; }

    public UserRecord? User { get; set; }
    public RoleRecord? Role { get; set; }
}

public sealed class RolePermissionRecord
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public DateTime AssignedAtUtc { get; set; }

    public RoleRecord? Role { get; set; }
    public PermissionRecord? Permission { get; set; }
}

public sealed class UserBranchAssignmentRecord
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
    public Guid CompanyId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime AssignedAtUtc { get; set; }

    public UserRecord? User { get; set; }
}

public sealed class RefreshTokenRecord
{
    public Guid Id { get; set; }
    public required string TokenHash { get; set; }
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? TerminalId { get; set; }
    public Guid TokenFamily { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevocationReason { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedByIp { get; set; }

    public UserRecord? User { get; set; }
}

public sealed class TerminalRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public required string TerminalCode { get; set; }
    public required string MachineName { get; set; }
    public string? OperatingSystem { get; set; }
    public bool IsAuthorized { get; set; } = true;
    public DateTime RegisteredAtUtc { get; set; }
    public DateTime? LastHeartbeatAtUtc { get; set; }
}

public sealed class SecurityAuditEventRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? TerminalId { get; set; }
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public required string EventType { get; set; }
    public string? IpAddress { get; set; }
    public string? DetailsJson { get; set; }
    public DateTime TimestampUtc { get; set; }
}
