using Asp.Versioning;
using Magic.Application.Finance.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Finance;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Finance;

[Route("api/v{version:apiVersion}/accounting/customer-receipts")]
[Route("api/v{version:apiVersion}/finance/customer-receipts")]
public sealed class CustomerReceiptsController(
    ICustomerReceiptService receiptService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : FinanceBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerReceiptDto>>>> GetReceipts(
        [FromQuery] Guid? customerId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:receivables:view");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var (items, totalCount) = await receiptService.GetReceiptsAsync(
            companyId, branchId, customerId, fromDate, toDate, status, page, pageSize, cancellationToken);

        var meta = new PaginationMeta(page, pageSize, totalCount);

        return Ok(Wrap(items, meta));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerReceiptDto>>> GetReceiptById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:receivables:view");
        var companyId = ResolveCompanyId();

        var item = await receiptService.GetReceiptByIdAsync(companyId, id, cancellationToken);
        return Ok(Wrap(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerReceiptDto>>> CreateReceipt(
        [FromBody] CreateCustomerReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:receipts:create");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var userId = ResolveCurrentUserId();

        var receipt = await receiptService.CreateReceiptAsync(companyId, branchId, userId, request, cancellationToken);
        return CreatedAtAction(nameof(GetReceiptById), new { id = receipt.Id }, Wrap(receipt));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<CustomerReceiptDto>>> CancelReceipt(
        Guid id,
        [FromBody] CancelReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:receipts:cancel");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var userId = ResolveCurrentUserId();

        var receipt = await receiptService.CancelReceiptAsync(companyId, branchId, id, userId, request, cancellationToken);
        return Ok(Wrap(receipt));
    }
}
