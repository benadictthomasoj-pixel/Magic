using Asp.Versioning;
using Magic.Application.Payroll.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Payroll;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Payroll;

[Authorize]
[Route("api/v{version:apiVersion}/payroll")]
public sealed class PayrollController(
    IPayrollService payrollService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : OperationsBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<PayrollSummaryDto>>> GetSummary(
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("payroll:process", "payroll:approve");
        var companyId = ResolveCompanyId();
        var bId = branchId ?? ResolveBranchId();
        var summary = await payrollService.GetSummaryAsync(companyId, bId, cancellationToken);
        return Envelope(summary);
    }

    [HttpGet("structures")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SalaryStructureDto>>>> GetSalaryStructures(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("payroll:structures:manage", "payroll:process");
        var companyId = ResolveCompanyId();
        var items = await payrollService.GetSalaryStructuresAsync(companyId, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("structures")]
    public async Task<ActionResult<ApiResponse<SalaryStructureDto>>> CreateSalaryStructure(
        [FromBody] CreateSalaryStructureRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("payroll:structures:manage");
        var companyId = ResolveCompanyId();
        var item = await payrollService.CreateSalaryStructureAsync(companyId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPost("assign")]
    public async Task<ActionResult<ApiResponse<EmployeeSalaryAssignmentDto>>> AssignSalaryStructure(
        [FromBody] AssignEmployeeSalaryRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("payroll:structures:manage");
        var companyId = ResolveCompanyId();
        var item = await payrollService.AssignSalaryStructureAsync(companyId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpGet("assignments/{employeeId:guid}")]
    public async Task<ActionResult<ApiResponse<EmployeeSalaryAssignmentDto?>>> GetEmployeeAssignment(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("payroll:structures:manage", "payroll:process");
        var companyId = ResolveCompanyId();
        var item = await payrollService.GetEmployeeAssignmentAsync(companyId, employeeId, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("periods")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PayrollPeriodDto>>>> GetPayrollPeriods(
        [FromQuery] Guid? branchId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("payroll:process", "payroll:approve");
        var companyId = ResolveCompanyId();
        var bId = branchId ?? ResolveBranchId();
        var (items, total) = await payrollService.GetPayrollPeriodsAsync(companyId, bId, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("periods/{id:guid}")]
    public async Task<ActionResult<ApiResponse<PayrollPeriodDto>>> GetPayrollPeriodById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("payroll:process", "payroll:approve");
        var companyId = ResolveCompanyId();
        var item = await payrollService.GetPayrollPeriodByIdAsync(companyId, id, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<ApiResponse<PayrollPeriodDto>>> CalculatePayroll(
        [FromBody] CalculatePayrollRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("payroll:process");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var userId = ResolveCurrentUserId();
        var item = await payrollService.CalculatePayrollAsync(companyId, branchId, userId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPost("periods/{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<PayrollPeriodDto>>> ApprovePayroll(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("payroll:approve");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var item = await payrollService.ApprovePayrollAsync(companyId, id, userId, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("periods/{id:guid}/finalize")]
    public async Task<ActionResult<ApiResponse<PayrollPeriodDto>>> FinalizePayroll(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("payroll:finalize");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var item = await payrollService.FinalizePayrollAsync(companyId, id, userId, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("periods/{id:guid}/disburse")]
    public async Task<ActionResult<ApiResponse<PayrollPeriodDto>>> DisbursePayroll(
        Guid id,
        [FromBody] DisbursePayrollRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("payroll:disburse");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var item = await payrollService.DisbursePayrollAsync(companyId, id, userId, request, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("payslip/{payrollItemId:guid}")]
    public async Task<ActionResult<ApiResponse<PayrollItemDto>>> GetPayslip(
        Guid payrollItemId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("payroll:payslip:view");
        var companyId = ResolveCompanyId();
        var item = await payrollService.GetPayslipAsync(companyId, payrollItemId, cancellationToken);
        return Envelope(item);
    }
}
