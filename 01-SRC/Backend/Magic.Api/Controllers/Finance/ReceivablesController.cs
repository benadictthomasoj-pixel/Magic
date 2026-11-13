using Asp.Versioning;
using Magic.Application.Finance.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Finance;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Finance;

[Route("api/v{version:apiVersion}/accounting/receivables")]
[Route("api/v{version:apiVersion}/finance/receivables")]
public sealed class ReceivablesController(
    IReceivablesService receivablesService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : FinanceBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<ReceivablesSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        EnsurePermission("finance:receivables:view");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var result = await receivablesService.GetSummaryAsync(companyId, branchId, cancellationToken);
        return Ok(Wrap(result));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerReceivableDto>>>> GetReceivables(
        [FromQuery] Guid? customerId,
        [FromQuery] string? status,
        [FromQuery] bool overdueOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:receivables:view");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var (items, totalCount) = await receivablesService.GetReceivablesAsync(
            companyId, branchId, customerId, status, overdueOnly, page, pageSize, cancellationToken);

        var meta = new PaginationMeta(page, pageSize, totalCount);

        return Ok(Wrap(items, meta));
    }

    [HttpGet("aging")]
    public async Task<ActionResult<ApiResponse<CustomerAgingReportDto>>> GetAgingReport(
        [FromQuery] DateOnly? reportDate,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:receivables:view");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var date = reportDate ?? DateOnly.FromDateTime(Clock.UtcNow.Date);

        var report = await receivablesService.GetAgingReportAsync(companyId, branchId, date, cancellationToken);
        return Ok(Wrap(report));
    }

    [HttpGet("statement/{customerId:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerStatementDto>>> GetStatement(
        Guid customerId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:receivables:view");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var from = fromDate ?? new DateOnly(2026, 4, 1);
        var to = toDate ?? DateOnly.FromDateTime(Clock.UtcNow.Date);

        var statement = await receivablesService.GetCustomerStatementAsync(companyId, branchId, customerId, from, to, cancellationToken);
        return Ok(Wrap(statement));
    }
}
