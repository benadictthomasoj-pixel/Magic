using Asp.Versioning;
using Magic.Application.Finance.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Finance;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Finance;

[Route("api/v{version:apiVersion}/accounting/bank-accounts")]
[Route("api/v{version:apiVersion}/finance/bank-accounts")]
public sealed class BankAccountsController(
    IBankCashAccountService accountService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : FinanceBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BankCashAccountDto>>>> GetAccounts(
        [FromQuery] string? accountType,
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:bank-accounts:view");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var accounts = await accountService.GetAccountsAsync(companyId, branchId, accountType, activeOnly, cancellationToken);
        return Ok(Wrap(accounts));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BankCashAccountDto>>> GetAccountById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:bank-accounts:view");
        var companyId = ResolveCompanyId();

        var account = await accountService.GetAccountByIdAsync(companyId, id, cancellationToken);
        return Ok(Wrap(account));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BankCashAccountDto>>> CreateAccount(
        [FromBody] CreateBankCashAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:bank-accounts:manage");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var userId = ResolveCurrentUserId();

        var account = await accountService.CreateAccountAsync(companyId, branchId, userId, request, cancellationToken);
        return CreatedAtAction(nameof(GetAccountById), new { id = account.Id }, Wrap(account));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BankCashAccountDto>>> UpdateAccount(
        Guid id,
        [FromBody] UpdateBankCashAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:bank-accounts:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();

        var account = await accountService.UpdateAccountAsync(companyId, id, userId, request, cancellationToken);
        return Ok(Wrap(account));
    }

    [HttpPost("{id:guid}/reveal")]
    public async Task<ActionResult<ApiResponse<RevealAccountNumberResponse>>> RevealAccountNumber(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:bank-accounts:reveal");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var response = await accountService.RevealAccountNumberAsync(companyId, id, userId, clientIp, cancellationToken);
        return Ok(Wrap(response));
    }
}
