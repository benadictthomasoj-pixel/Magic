using Asp.Versioning;
using Magic.Application.Hr.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Hr;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Hr;

[Authorize]
[Route("api/v{version:apiVersion}/hr")]
public sealed class HrController(
    IHrService hrService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : OperationsBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<HrSummaryDto>>> GetSummary(
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("hr:employees:view");
        var companyId = ResolveCompanyId();
        var bId = branchId ?? ResolveBranchId();
        var summary = await hrService.GetSummaryAsync(companyId, bId, cancellationToken);
        return Envelope(summary);
    }

    [HttpGet("designations")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DesignationDto>>>> GetDesignations(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("hr:employees:view");
        var companyId = ResolveCompanyId();
        var items = await hrService.GetDesignationsAsync(companyId, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("designations")]
    public async Task<ActionResult<ApiResponse<DesignationDto>>> CreateDesignation(
        [FromBody] CreateDesignationRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("hr:employees:manage");
        var companyId = ResolveCompanyId();
        var item = await hrService.CreateDesignationAsync(companyId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpGet("employees")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EmployeeDto>>>> GetEmployees(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? designationId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("hr:employees:view");
        var companyId = ResolveCompanyId();
        var bId = branchId ?? ResolveBranchId();
        var (items, total) = await hrService.GetEmployeesAsync(companyId, bId, departmentId, designationId, status, search, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("employees/{id:guid}")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetEmployeeById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("hr:employees:view");
        var companyId = ResolveCompanyId();
        var item = await hrService.GetEmployeeByIdAsync(companyId, id, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("employees")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> CreateEmployee(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("hr:employees:manage");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var item = await hrService.CreateEmployeeAsync(companyId, branchId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPut("employees/{id:guid}")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> UpdateEmployee(
        Guid id,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("hr:employees:manage");
        var companyId = ResolveCompanyId();
        var item = await hrService.UpdateEmployeeAsync(companyId, id, request, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("attendance")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AttendanceDto>>>> GetAttendance(
        [FromQuery] Guid? branchId,
        [FromQuery] DateOnly attendanceDate,
        [FromQuery] Guid? employeeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("hr:employees:view");
        var companyId = ResolveCompanyId();
        var bId = branchId ?? ResolveBranchId();
        var (items, total) = await hrService.GetAttendanceAsync(companyId, bId, attendanceDate, employeeId, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpPost("attendance/punch")]
    public async Task<ActionResult<ApiResponse<AttendanceDto>>> PunchAttendance(
        [FromBody] PunchAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("hr:attendance:record");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var item = await hrService.PunchAttendanceAsync(companyId, branchId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPost("attendance/correct")]
    public async Task<ActionResult<ApiResponse<AttendanceDto>>> CorrectAttendance(
        [FromBody] CorrectAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("hr:attendance:correct");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var userId = ResolveCurrentUserId();
        var item = await hrService.CorrectAttendanceAsync(companyId, branchId, userId, request, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("leaves/types")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveTypeDto>>>> GetLeaveTypes(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("hr:employees:view");
        var companyId = ResolveCompanyId();
        var items = await hrService.GetLeaveTypesAsync(companyId, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("leaves/types")]
    public async Task<ActionResult<ApiResponse<LeaveTypeDto>>> CreateLeaveType(
        [FromBody] CreateLeaveTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("hr:employees:manage");
        var companyId = ResolveCompanyId();
        var item = await hrService.CreateLeaveTypeAsync(companyId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpGet("leaves/balances/{employeeId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveBalanceDto>>>> GetLeaveBalances(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("hr:employees:view");
        var companyId = ResolveCompanyId();
        var items = await hrService.GetLeaveBalancesAsync(companyId, employeeId, cancellationToken);
        return Envelope(items);
    }

    [HttpGet("leaves/applications")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveApplicationDto>>>> GetLeaveApplications(
        [FromQuery] Guid? employeeId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("hr:employees:view");
        var companyId = ResolveCompanyId();
        var (items, total) = await hrService.GetLeaveApplicationsAsync(companyId, employeeId, status, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpPost("leaves/apply")]
    public async Task<ActionResult<ApiResponse<LeaveApplicationDto>>> SubmitLeaveApplication(
        [FromBody] SubmitLeaveApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("hr:leave:apply");
        var companyId = ResolveCompanyId();
        var item = await hrService.SubmitLeaveApplicationAsync(companyId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPost("leaves/applications/{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<LeaveApplicationDto>>> ApproveLeaveApplication(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("hr:leave:approve");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var item = await hrService.ApproveLeaveApplicationAsync(companyId, id, userId, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("leaves/applications/{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<LeaveApplicationDto>>> RejectLeaveApplication(
        Guid id,
        [FromBody] RejectLeaveApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("hr:leave:approve");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var item = await hrService.RejectLeaveApplicationAsync(companyId, id, userId, request, cancellationToken);
        return Envelope(item);
    }
}
