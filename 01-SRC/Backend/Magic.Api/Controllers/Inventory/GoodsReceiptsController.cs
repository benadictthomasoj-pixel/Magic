using Magic.Application.Inventory.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Inventory;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Inventory;

[Authorize]
[Route("api/v{version:apiVersion}/procurement/goods-receipts")]
public sealed class GoodsReceiptsController(
    IGoodsReceiptService goodsReceiptService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : InventoryBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GoodsReceiptNoteDto>>>> GetGoodsReceipts(
        [FromQuery] Guid? supplierId,
        [FromQuery] Guid? purchaseOrderId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("procurement:grn:view");
        var companyId = ResolveCompanyId();
        var (items, total) = await goodsReceiptService.GetGoodsReceiptsAsync(
            companyId, supplierId, purchaseOrderId, status, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<GoodsReceiptNoteDto>>> GetGoodsReceiptById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("procurement:grn:view");
        var companyId = ResolveCompanyId();
        var item = await goodsReceiptService.GetGoodsReceiptByIdAsync(companyId, id, cancellationToken);
        return Envelope(item);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GoodsReceiptNoteDto>>> CreateGoodsReceipt(
        [FromBody] CreateGoodsReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("procurement:grn:create");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var item = await goodsReceiptService.CreateGoodsReceiptAsync(companyId, userId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<GoodsReceiptNoteDto>>> ConfirmGoodsReceipt(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("procurement:grn:confirm");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var item = await goodsReceiptService.ConfirmGoodsReceiptAsync(companyId, id, userId, cancellationToken);
        return Envelope(item);
    }
}
