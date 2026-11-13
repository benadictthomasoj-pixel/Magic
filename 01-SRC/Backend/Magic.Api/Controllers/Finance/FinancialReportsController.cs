using Asp.Versioning;
using Magic.Application.Finance.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Finance;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Finance;

[Route("api/v{version:apiVersion}/accounting/financial-reports")]
[Route("api/v{version:apiVersion}/finance/reports")]
public sealed class FinancialReportsController(
    IFinancialReportingService reportingService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : FinanceBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<FinanceSummaryKpiDto>>> GetSummary(CancellationToken cancellationToken)
    {
        EnsurePermission("finance:reports:view");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var kpis = await reportingService.GetSummaryKpisAsync(companyId, branchId, cancellationToken);
        return Ok(Wrap(kpis));
    }

    [HttpGet("cash-book")]
    public async Task<ActionResult<ApiResponse<CashBookReportDto>>> GetCashBook(
        [FromQuery] Guid? accountId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:reports:view");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var from = fromDate ?? new DateOnly(2026, 4, 1);
        var to = toDate ?? DateOnly.FromDateTime(Clock.UtcNow.Date);

        var report = await reportingService.GetCashBookAsync(companyId, branchId, accountId, from, to, cancellationToken);
        return Ok(Wrap(report));
    }

    [HttpGet("bank-book")]
    public async Task<ActionResult<ApiResponse<BankBookReportDto>>> GetBankBook(
        [FromQuery] Guid? bankAccountId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:reports:view");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var from = fromDate ?? new DateOnly(2026, 4, 1);
        var to = toDate ?? DateOnly.FromDateTime(Clock.UtcNow.Date);

        var report = await reportingService.GetBankBookAsync(companyId, branchId, bankAccountId, from, to, cancellationToken);
        return Ok(Wrap(report));
    }
}
