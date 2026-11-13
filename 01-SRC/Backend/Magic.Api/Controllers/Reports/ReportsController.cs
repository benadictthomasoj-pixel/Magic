using Asp.Versioning;
using Magic.Application.Reports.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Reports;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Reports;

[Authorize]
[Route("api/v{version:apiVersion}/reports")]
public sealed class ReportsController(
    IReportService reportService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext userContext) : OperationsBaseController(dateTimeProvider, branchContext, userContext)
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<ExecutiveDashboardDto>>> GetExecutiveDashboard(
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("reports:view");
        var companyId = ResolveCompanyId();
        var item = await reportService.GetExecutiveDashboardAsync(companyId, branchId, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("sales")]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetSalesReport(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] Guid? branchId,
        [FromQuery] string? groupByType,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("reports:view");
        var companyId = ResolveCompanyId();
        var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
        var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var item = await reportService.GetSalesReportAsync(companyId, start, end, branchId, groupByType, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("purchases")]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetPurchaseReport(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] Guid? supplierId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("reports:view");
        var companyId = ResolveCompanyId();
        var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
        var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var item = await reportService.GetPurchaseReportAsync(companyId, start, end, supplierId, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("inventory")]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetInventoryReport(
        [FromQuery] Guid? warehouseId,
        [FromQuery] bool? lowStockOnly,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("reports:view");
        var companyId = ResolveCompanyId();
        var item = await reportService.GetInventoryReportAsync(companyId, warehouseId, lowStockOnly, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("financial")]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetFinancialSummaryReport(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("reports:view");
        var companyId = ResolveCompanyId();
        var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
        var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var item = await reportService.GetFinancialSummaryReportAsync(companyId, start, end, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("hr-payroll")]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetHrPayrollReport(
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("reports:view");
        var companyId = ResolveCompanyId();
        var item = await reportService.GetHrPayrollReportAsync(companyId, month, year, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("operations")]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetMarketingOrdersServicesReport(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("reports:view");
        var companyId = ResolveCompanyId();
        var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
        var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var item = await reportService.GetMarketingOrdersServicesReportAsync(companyId, start, end, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("export")]
    public async Task<IActionResult> ExportReport(
        [FromBody] ReportExportRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("reports:export");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var userName = CurrentUser.Username ?? "Administrator";
        var result = await reportService.ExportReportAsync(companyId, request, userId, userName, cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }
}
