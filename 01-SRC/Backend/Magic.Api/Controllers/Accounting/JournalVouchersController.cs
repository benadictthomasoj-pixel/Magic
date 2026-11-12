using Magic.Application.Accounting.Services;
using Magic.Contracts.Accounting;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Headers;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Accounting;

public sealed class JournalVouchersController(
    IAccountingApplicationService accountingService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : AccountingBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("vouchers")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<VoucherSummaryDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VoucherSummaryDto>>>> GetVouchers(
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        [FromQuery] string? voucherType,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var (items, totalCount) = await accountingService.GetVouchersAsync(
            companyId,
            branchId,
            dateFrom,
            dateTo,
            voucherType,
            status,
            search,
            page,
            pageSize,
            cancellationToken);

        var meta = new PaginationMeta(page, pageSize, totalCount);
        return Ok(Wrap(items, meta));
    }

    [HttpGet("vouchers/{id:guid}")]
    [ProducesResponseType<ApiResponse<VoucherDetailDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<VoucherDetailDto>>> GetVoucherById(Guid id, CancellationToken cancellationToken)
    {
        var voucher = await accountingService.GetVoucherByIdAsync(id, cancellationToken);
        return Ok(Wrap(voucher));
    }

    [HttpPost("vouchers/draft")]
    [ProducesResponseType<ApiResponse<VoucherDetailDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<VoucherDetailDto>>> CreateJournalDraft(
        [FromBody] CreateJournalDraftRequest request,
        CancellationToken cancellationToken)
    {
        var userId = ResolveAuthorizedUserId();
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var created = await accountingService.CreateJournalDraftAsync(companyId, branchId, request, userId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, Wrap(created));
    }

    [HttpPost("vouchers/validate")]
    [ProducesResponseType<ApiResponse<JournalValidationResultDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<JournalValidationResultDto>>> ValidateJournal(
        [FromBody] CreateJournalDraftRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var result = await accountingService.ValidateJournalAsync(companyId, branchId, request, cancellationToken);
        return Ok(Wrap(result));
    }

    [HttpPost("vouchers/{id:guid}/post")]
    [ProducesResponseType<ApiResponse<VoucherDetailDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<VoucherDetailDto>>> PostVoucher(
        Guid id,
        [FromBody] PostVoucherRequest request,
        CancellationToken cancellationToken)
    {
        var userId = ResolveAuthorizedUserId();
        var hasSupervisorElevation = Request.Headers.ContainsKey(MagicHttpHeaders.SupervisorAuthToken);

        var posted = await accountingService.PostVoucherAsync(id, request, userId, hasSupervisorElevation, cancellationToken);
        return Ok(Wrap(posted));
    }

    [HttpPost("vouchers/{id:guid}/reverse")]
    [ProducesResponseType<ApiResponse<VoucherDetailDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<VoucherDetailDto>>> ReverseVoucher(
        Guid id,
        [FromBody] ReverseVoucherRequest request,
        CancellationToken cancellationToken)
    {
        var userId = ResolveAuthorizedUserId();
        var reversal = await accountingService.ReverseVoucherAsync(id, request, userId, cancellationToken);
        return Ok(Wrap(reversal));
    }
}
