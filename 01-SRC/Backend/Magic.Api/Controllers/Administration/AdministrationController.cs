using Asp.Versioning;
using Magic.Application.Administration.Services;
using Magic.Contracts.Administration;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Administration;

[Authorize]
[Route("api/v{version:apiVersion}/administration")]
public sealed class AdministrationController(
    IEnterpriseAdministrationService adminService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext userContext) : OperationsBaseController(dateTimeProvider, branchContext, userContext)
{
    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminUserDto>>>> GetUsers(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("administration:users:manage");
        var companyId = ResolveCompanyId();
        var items = await adminService.GetUsersAsync(companyId, cancellationToken);
        return Envelope(items);
    }

    [HttpGet("users/{id:guid}")]
    public async Task<ActionResult<ApiResponse<AdminUserDto>>> GetUserById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("administration:users:manage");
        var companyId = ResolveCompanyId();
        var item = await adminService.GetUserByIdAsync(companyId, id, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("users")]
    public async Task<ActionResult<ApiResponse<AdminUserDto>>> CreateUser(
        [FromBody] CreateAdminUserRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("administration:users:manage");
        var companyId = ResolveCompanyId();
        var item = await adminService.CreateUserAsync(companyId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPut("users/{id:guid}")]
    public async Task<ActionResult<ApiResponse<AdminUserDto>>> UpdateUser(
        Guid id,
        [FromBody] UpdateAdminUserRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("administration:users:manage");
        var companyId = ResolveCompanyId();
        var item = await adminService.UpdateUserAsync(companyId, id, request, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("roles")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleWithPermissionsDto>>>> GetRoles(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("administration:users:manage");
        var companyId = ResolveCompanyId();
        var items = await adminService.GetRolesAsync(companyId, cancellationToken);
        return Envelope(items);
    }

    [HttpPut("roles/{id:guid}/permissions")]
    public async Task<ActionResult<ApiResponse<RoleWithPermissionsDto>>> UpdateRolePermissions(
        Guid id,
        [FromBody] UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("administration:users:manage");
        var companyId = ResolveCompanyId();
        var item = await adminService.UpdateRolePermissionsAsync(companyId, id, request, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("workflows")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ApprovalWorkflowDto>>>> GetWorkflows(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("administration:approvals:manage");
        var companyId = ResolveCompanyId();
        var items = await adminService.GetWorkflowsAsync(companyId, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("workflows")]
    public async Task<ActionResult<ApiResponse<ApprovalWorkflowDto>>> CreateWorkflow(
        [FromBody] CreateApprovalWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("administration:approvals:manage");
        var companyId = ResolveCompanyId();
        var item = await adminService.CreateWorkflowAsync(companyId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpGet("approvals")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ApprovalRequestDto>>>> GetApprovalRequests(
        [FromQuery] string? status,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("administration:approvals:manage");
        var companyId = ResolveCompanyId();
        var items = await adminService.GetApprovalRequestsAsync(companyId, status, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("approvals/{id:guid}/action")]
    public async Task<ActionResult<ApiResponse<ApprovalRequestDto>>> SubmitApprovalAction(
        Guid id,
        [FromBody] SubmitApprovalActionRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("administration:approvals:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var userName = CurrentUser.Username ?? "Administrator";
        var item = await adminService.SubmitApprovalActionAsync(companyId, id, userId, userName, request, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AuditLogEntryDto>>>> QueryAuditLogs(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] string? eventType,
        [FromQuery] string? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("administration:audit:view");
        var companyId = ResolveCompanyId();
        var query = new AuditLogQueryRequest(startDate, endDate, eventType, userId, page, pageSize);
        var items = await adminService.QueryAuditLogsAsync(companyId, query, cancellationToken);
        return Envelope(items);
    }

    [HttpGet("security/metrics")]
    public async Task<ActionResult<ApiResponse<SecurityMetricsDto>>> GetSecurityMetrics(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("administration:audit:view");
        var companyId = ResolveCompanyId();
        var item = await adminService.GetSecurityMetricsAsync(companyId, cancellationToken);
        return Envelope(item);
    }
}
