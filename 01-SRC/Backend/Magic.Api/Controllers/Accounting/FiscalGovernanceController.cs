using Magic.Application.Accounting.Services;
using Magic.Contracts.Accounting;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Accounting;

public sealed class FiscalGovernanceController(
    IAccountingApplicationService accountingService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : AccountingBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("fiscal-years")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<FiscalYearDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FiscalYearDto>>>> GetFiscalYears(CancellationToken cancellationToken)
    {
        var companyId = ResolveCompanyId();
        var years = await accountingService.GetFiscalYearsAsync(companyId, cancellationToken);
        return Ok(Wrap(years));
    }

    [HttpPost("fiscal-periods/{id:guid}/lock")]
    [ProducesResponseType<ApiResponse<FiscalPeriodDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FiscalPeriodDto>>> LockPeriod(Guid id, CancellationToken cancellationToken)
    {
        var userId = ResolveAuthorizedUserId();
        var period = await accountingService.LockFiscalPeriodAsync(id, userId, cancellationToken);
        return Ok(Wrap(period));
    }

    [HttpPost("fiscal-periods/{id:guid}/close")]
    [ProducesResponseType<ApiResponse<FiscalPeriodDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FiscalPeriodDto>>> ClosePeriod(Guid id, CancellationToken cancellationToken)
    {
        var userId = ResolveAuthorizedUserId();
        var period = await accountingService.CloseFiscalPeriodAsync(id, userId, cancellationToken);
        return Ok(Wrap(period));
    }

    [HttpPost("fiscal-periods/{id:guid}/reopen")]
    [ProducesResponseType<ApiResponse<FiscalPeriodDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FiscalPeriodDto>>> ReopenPeriod(Guid id, CancellationToken cancellationToken)
    {
        var userId = ResolveAuthorizedUserId();
        var period = await accountingService.ReopenFiscalPeriodAsync(id, userId, cancellationToken);
        return Ok(Wrap(period));
    }
}
