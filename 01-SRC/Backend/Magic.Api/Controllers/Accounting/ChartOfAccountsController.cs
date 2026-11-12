using Magic.Application.Accounting.Services;
using Magic.Contracts.Accounting;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Accounting;

public sealed class ChartOfAccountsController(
    IAccountingApplicationService accountingService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : AccountingBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("account-groups")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<AccountGroupDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AccountGroupDto>>>> GetAccountGroups(CancellationToken cancellationToken)
    {
        var companyId = ResolveCompanyId();
        var groups = await accountingService.GetAccountGroupsAsync(companyId, cancellationToken);
        return Ok(Wrap(groups));
    }

    [HttpGet("accounts")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<AccountDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AccountDto>>>> GetAccounts(
        [FromQuery] string? rootClass,
        [FromQuery] bool? activeOnly,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var companyId = ResolveCompanyId();
        var accounts = await accountingService.GetAccountsAsync(companyId, rootClass, activeOnly, search, cancellationToken);
        return Ok(Wrap(accounts));
    }

    [HttpGet("accounts/{id:guid}")]
    [ProducesResponseType<ApiResponse<AccountDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AccountDto>>> GetAccountById(Guid id, CancellationToken cancellationToken)
    {
        var account = await accountingService.GetAccountByIdAsync(id, cancellationToken);
        return Ok(Wrap(account));
    }

    [HttpPost("accounts")]
    [ProducesResponseType<ApiResponse<AccountDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AccountDto>>> CreateAccount(
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var userId = ResolveAuthorizedUserId();
        EnsurePermission("accounting:accounts:manage");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var created = await accountingService.CreateAccountAsync(companyId, branchId, request, userId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, Wrap(created));
    }

    [HttpPut("accounts/{id:guid}")]
    [ProducesResponseType<ApiResponse<AccountDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AccountDto>>> UpdateAccount(
        Guid id,
        [FromBody] UpdateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var userId = ResolveAuthorizedUserId();
        EnsurePermission("accounting:accounts:manage");
        var updated = await accountingService.UpdateAccountAsync(id, request, userId, cancellationToken);
        return Ok(Wrap(updated));
    }
}
