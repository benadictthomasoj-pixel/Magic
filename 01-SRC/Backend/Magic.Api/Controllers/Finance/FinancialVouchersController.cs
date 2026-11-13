using Asp.Versioning;
using Magic.Application.Accounting.Services;
using Magic.Contracts.Accounting;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Finance;

[Route("api/v{version:apiVersion}/accounting/financial-vouchers")]
[Route("api/v{version:apiVersion}/finance/financial-vouchers")]
public sealed class FinancialVouchersController(
    IAccountingApplicationService accountingService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : FinanceBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VoucherSummaryDto>>>> GetVouchers(
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        [FromQuery] string? voucherType,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("accounting:vouchers:view");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var (items, totalCount) = await accountingService.GetVouchersAsync(
            companyId, branchId, dateFrom, dateTo, voucherType, status, search, page, pageSize, cancellationToken);

        var meta = new PaginationMeta(page, pageSize, totalCount);

        return Ok(Wrap(items, meta));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VoucherDetailDto>>> GetVoucherById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("accounting:vouchers:view");
        var voucher = await accountingService.GetVoucherByIdAsync(id, cancellationToken);
        return Ok(Wrap(voucher));
    }

    [HttpPost("draft")]
    public async Task<ActionResult<ApiResponse<VoucherDetailDto>>> CreateDraft(
        [FromBody] CreateJournalDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("accounting:vouchers:create");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var userId = ResolveCurrentUserId();

        var voucher = await accountingService.CreateJournalDraftAsync(companyId, branchId, request, userId, cancellationToken);
        return CreatedAtAction(nameof(GetVoucherById), new { id = voucher.Id }, Wrap(voucher));
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<ApiResponse<VoucherDetailDto>>> PostVoucher(
        Guid id,
        [FromBody] PostVoucherRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("accounting:vouchers:post");
        var userId = ResolveCurrentUserId();

        var voucher = await accountingService.PostVoucherAsync(id, request, userId, hasSupervisorElevation: true, cancellationToken);
        return Ok(Wrap(voucher));
    }

    [HttpPost("{id:guid}/reverse")]
    public async Task<ActionResult<ApiResponse<VoucherDetailDto>>> ReverseVoucher(
        Guid id,
        [FromBody] ReverseVoucherRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("accounting:vouchers:reverse");
        var userId = ResolveCurrentUserId();

        var voucher = await accountingService.ReverseVoucherAsync(id, request, userId, cancellationToken);
        return Ok(Wrap(voucher));
    }
}
