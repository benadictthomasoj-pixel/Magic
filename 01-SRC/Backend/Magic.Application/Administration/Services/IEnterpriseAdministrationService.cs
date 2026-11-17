using Magic.Contracts.Administration;

namespace Magic.Application.Administration.Services;

public interface IEnterpriseAdministrationService
{
    Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<AdminUserDto> GetUserByIdAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<AdminUserDto> CreateUserAsync(Guid companyId, CreateAdminUserRequest request, CancellationToken cancellationToken = default);
    Task<AdminUserDto> UpdateUserAsync(Guid companyId, Guid userId, UpdateAdminUserRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleWithPermissionsDto>> GetRolesAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<RoleWithPermissionsDto> UpdateRolePermissionsAsync(Guid companyId, Guid roleId, UpdateRolePermissionsRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalWorkflowDto>> GetWorkflowsAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<ApprovalWorkflowDto> CreateWorkflowAsync(Guid companyId, CreateApprovalWorkflowRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalRequestDto>> GetApprovalRequestsAsync(Guid companyId, string? status, CancellationToken cancellationToken = default);
    Task<ApprovalRequestDto> SubmitApprovalActionAsync(Guid companyId, Guid requestId, Guid userId, string userName, SubmitApprovalActionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogEntryDto>> QueryAuditLogsAsync(Guid companyId, AuditLogQueryRequest request, CancellationToken cancellationToken = default);
    Task<SecurityMetricsDto> GetSecurityMetricsAsync(Guid companyId, CancellationToken cancellationToken = default);
}

