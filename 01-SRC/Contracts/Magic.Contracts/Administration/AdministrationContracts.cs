namespace Magic.Contracts.Administration;

public sealed record AdminUserDto(
    Guid Id,
    Guid CompanyId,
    string Username,
    string Email,
    string DisplayName,
    bool IsActive,
    bool IsLockedOut,
    int FailedLoginAttempts,
    DateTime? LastLoginAtUtc,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> AssignedBranchIds,
    DateTime CreatedAtUtc);

public sealed record CreateAdminUserRequest(
    string Username,
    string Email,
    string DisplayName,
    string Password,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> BranchIds);

public sealed record UpdateAdminUserRequest(
    string DisplayName,
    string Email,
    bool IsActive,
    bool UnlockAccount,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> BranchIds);

public sealed record RoleWithPermissionsDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    IReadOnlyList<string> Permissions);

public sealed record UpdateRolePermissionsRequest(
    IReadOnlyList<string> PermissionCodes);

public sealed record ApprovalWorkflowDto(
    Guid Id,
    Guid CompanyId,
    string Module,
    string TransactionType,
    long MinimumAmountPaise,
    int RequiredApprovalsCount,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record CreateApprovalWorkflowRequest(
    string Module,
    string TransactionType,
    long MinimumAmountPaise,
    int RequiredApprovalsCount);

public sealed record ApprovalRequestDto(
    Guid Id,
    Guid CompanyId,
    Guid BranchId,
    string BranchName,
    Guid? WorkflowId,
    string SourceModule,
    Guid SourceDocumentId,
    string ReferenceNumber,
    long AmountPaise,
    string Status, // "Pending", "Approved", "Rejected"
    string RequestedByUserName,
    string? Notes,
    DateTime CreatedAtUtc,
    IReadOnlyList<ApprovalActionDto> Actions);

public sealed record ApprovalActionDto(
    Guid ApproverUserId,
    string ApproverName,
    string Action, // "Approved", "Rejected"
    string? Reason,
    DateTime ActedAtUtc);

public sealed record SubmitApprovalActionRequest(
    string Action, // "Approved", "Rejected"
    string? Reason);

public sealed record AuditLogQueryRequest(
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? EventType,
    string? UserId,
    int Page = 1,
    int PageSize = 50);

public sealed record AuditLogEntryDto(
    Guid Id,
    Guid CompanyId,
    string? Username,
    string EventType,
    string? Details,
    string? IpAddress,
    DateTime TimestampUtc);

public sealed record SecurityMetricsDto(
    int TotalActiveUsers,
    int LockedOutUsersCount,
    int FailedLoginsLast24Hours,
    int ActiveSessionsCount,
    int RevokedTokensCount,
    DateTime EvaluatedAtUtc);

