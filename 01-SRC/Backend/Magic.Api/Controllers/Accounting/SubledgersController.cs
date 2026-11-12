using Magic.Application.Accounting.Services;
using Magic.Contracts.Accounting;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Accounting;

public sealed class SubledgersController(
    IAccountingApplicationService accountingService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : AccountingBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("subledgers/ar")]
    [ProducesResponseType<ApiResponse<SubledgerReportDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SubledgerReportDto>>> GetArSubledger(CancellationToken cancellationToken)
    {
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var report = await accountingService.GetSubledgerReportAsync(companyId, branchId, "AR", cancellationToken);
        return Ok(Wrap(report));
    }

    [HttpGet("subledgers/ap")]
    [ProducesResponseType<ApiResponse<SubledgerReportDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SubledgerReportDto>>> GetApSubledger(CancellationToken cancellationToken)
    {
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var report = await accountingService.GetSubledgerReportAsync(companyId, branchId, "AP", cancellationToken);
        return Ok(Wrap(report));
    }
}
