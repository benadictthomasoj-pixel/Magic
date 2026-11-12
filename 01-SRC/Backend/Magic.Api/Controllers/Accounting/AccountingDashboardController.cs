using Magic.Application.Accounting.Services;
using Magic.Contracts.Accounting;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Accounting;

public sealed class AccountingDashboardController(
    IAccountingApplicationService accountingService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : AccountingBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("dashboard")]
    [ProducesResponseType<ApiResponse<AccountingDashboardDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AccountingDashboardDto>>> GetDashboard(CancellationToken cancellationToken)
    {
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var dashboard = await accountingService.GetDashboardAsync(companyId, branchId, cancellationToken);
        return Ok(Wrap(dashboard));
    }
}
