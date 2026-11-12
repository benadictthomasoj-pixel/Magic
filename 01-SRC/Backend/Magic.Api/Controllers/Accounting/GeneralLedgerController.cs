using Magic.Application.Accounting.Services;
using Magic.Contracts.Accounting;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Accounting;

public sealed class GeneralLedgerController(
    IAccountingApplicationService accountingService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : AccountingBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("general-ledger")]
    [ProducesResponseType<ApiResponse<GeneralLedgerReportDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<GeneralLedgerReportDto>>> GetGeneralLedger(
        [FromQuery] Guid accountId,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        CancellationToken cancellationToken)
    {
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var report = await accountingService.GetGeneralLedgerAsync(companyId, branchId, accountId, dateFrom, dateTo, cancellationToken);
        return Ok(Wrap(report));
    }

    [HttpGet("trial-balance")]
    [ProducesResponseType<ApiResponse<TrialBalanceReportDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TrialBalanceReportDto>>> GetTrialBalance(
        [FromQuery] Guid? fiscalYearId,
        [FromQuery] Guid? fiscalPeriodId,
        [FromQuery] DateOnly? asOfDate,
        CancellationToken cancellationToken)
    {
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var report = await accountingService.GetTrialBalanceAsync(companyId, branchId, fiscalYearId, fiscalPeriodId, asOfDate, cancellationToken);
        return Ok(Wrap(report));
    }
}
