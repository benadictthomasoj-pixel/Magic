using Magic.Application.Sales.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Sales;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Sales;

[Authorize]
[Route("api/v{version:apiVersion}/sales/reports")]
public sealed class SalesReportsController(
    ISalesReportingService reportingService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : SalesBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<SalesReportSummaryDto>>> GetSummary(
        [FromQuery] Guid? branchId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("sales:reports:view");
        var companyId = ResolveCompanyId();
        var from = fromDate ?? DateTime.UtcNow.Date.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;
        var summary = await reportingService.GetSummaryAsync(companyId, branchId, from, to, cancellationToken);
        return Envelope(summary);
    }

    [HttpGet("payment-methods")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SalesByPaymentMethodDto>>>> GetPaymentBreakdown(
        [FromQuery] Guid? branchId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("sales:reports:view");
        var companyId = ResolveCompanyId();
        var from = fromDate ?? DateTime.UtcNow.Date.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;
        var breakdown = await reportingService.GetPaymentBreakdownAsync(companyId, branchId, from, to, cancellationToken);
        return Envelope(breakdown);
    }

    [HttpGet("top-products")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SalesByProductDto>>>> GetTopProducts(
        [FromQuery] Guid? branchId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("sales:reports:view");
        var companyId = ResolveCompanyId();
        var from = fromDate ?? DateTime.UtcNow.Date.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;
        var products = await reportingService.GetTopSellingProductsAsync(companyId, branchId, from, to, limit, cancellationToken);
        return Envelope(products);
    }

    [HttpGet("hourly")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<HourlySalesDto>>>> GetHourlySales(
        [FromQuery] Guid? branchId,
        [FromQuery] DateTime? date,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("sales:reports:view");
        var companyId = ResolveCompanyId();
        var targetDate = date ?? DateTime.UtcNow;
        var hourly = await reportingService.GetHourlySalesAsync(companyId, branchId, targetDate, cancellationToken);
        return Envelope(hourly);
    }
}
