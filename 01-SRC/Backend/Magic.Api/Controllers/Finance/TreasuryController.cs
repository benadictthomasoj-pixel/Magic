using Asp.Versioning;
using Magic.Application.Finance.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Finance;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Finance;

[Route("api/v{version:apiVersion}/accounting/treasury")]
[Route("api/v{version:apiVersion}/finance/treasury")]
public sealed class TreasuryController(
    ITreasuryService treasuryService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : FinanceBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TreasuryTransactionDto>>>> GetTransactions(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? transactionType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:reports:view");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var (items, totalCount) = await treasuryService.GetTransactionsAsync(
            companyId, branchId, fromDate, toDate, transactionType, page, pageSize, cancellationToken);

        var meta = new PaginationMeta(page, pageSize, totalCount);

        return Ok(Wrap(items, meta));
    }

    [HttpPost("contra")]
    public async Task<ActionResult<ApiResponse<TreasuryTransactionDto>>> CreateContra(
        [FromBody] CreateContraTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:treasury:create");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var userId = ResolveCurrentUserId();

        var tx = await treasuryService.CreateContraTransactionAsync(companyId, branchId, userId, request, cancellationToken);
        return Ok(Wrap(tx));
    }

    [HttpPost("adjustment")]
    public async Task<ActionResult<ApiResponse<TreasuryTransactionDto>>> CreateAdjustment(
        [FromBody] CreateTreasuryAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:treasury:create");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var userId = ResolveCurrentUserId();

        var tx = await treasuryService.CreateAdjustmentTransactionAsync(companyId, branchId, userId, request, cancellationToken);
        return Ok(Wrap(tx));
    }
}
