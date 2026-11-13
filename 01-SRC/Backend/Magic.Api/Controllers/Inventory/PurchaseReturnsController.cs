using Magic.Application.Inventory.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Inventory;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Inventory;

[Authorize]
[Route("api/v{version:apiVersion}/procurement/purchase-returns")]
public sealed class PurchaseReturnsController(
    IPurchaseReturnService purchaseReturnService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : InventoryBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PurchaseReturnDto>>>> GetPurchaseReturns(
        [FromQuery] Guid? supplierId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("procurement:return:manage");
        var companyId = ResolveCompanyId();
        var (items, total) = await purchaseReturnService.GetPurchaseReturnsAsync(
            companyId, supplierId, status, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PurchaseReturnDto>>> GetPurchaseReturnById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("procurement:return:manage");
        var companyId = ResolveCompanyId();
        var item = await purchaseReturnService.GetPurchaseReturnByIdAsync(companyId, id, cancellationToken);
        return Envelope(item);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PurchaseReturnDto>>> CreatePurchaseReturn(
        [FromBody] CreatePurchaseReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("procurement:return:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var item = await purchaseReturnService.CreatePurchaseReturnAsync(companyId, userId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<PurchaseReturnDto>>> ConfirmPurchaseReturn(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("procurement:return:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var item = await purchaseReturnService.ConfirmPurchaseReturnAsync(companyId, id, userId, cancellationToken);
        return Envelope(item);
    }
}
