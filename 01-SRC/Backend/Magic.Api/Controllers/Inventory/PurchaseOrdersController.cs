using Magic.Application.Inventory.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Inventory;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Inventory;

[Authorize]
[Route("api/v{version:apiVersion}/procurement/purchase-orders")]
public sealed class PurchaseOrdersController(
    IPurchaseOrderService purchaseOrderService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : InventoryBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PurchaseOrderDto>>>> GetPurchaseOrders(
        [FromQuery] Guid? supplierId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("procurement:po:view");
        var companyId = ResolveCompanyId();
        var (items, total) = await purchaseOrderService.GetPurchaseOrdersAsync(
            companyId, supplierId, status, search, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> GetPurchaseOrderById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("procurement:po:view");
        var companyId = ResolveCompanyId();
        var item = await purchaseOrderService.GetPurchaseOrderByIdAsync(companyId, id, cancellationToken);
        return Envelope(item);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> CreatePurchaseOrder(
        [FromBody] CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("procurement:po:create");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var item = await purchaseOrderService.CreatePurchaseOrderAsync(companyId, userId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> ApprovePurchaseOrder(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("procurement:po:approve");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var item = await purchaseOrderService.ApprovePurchaseOrderAsync(companyId, id, userId, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> CancelPurchaseOrder(
        Guid id,
        [FromBody] CancelOrderRequest? body,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("procurement:po:create");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var item = await purchaseOrderService.CancelPurchaseOrderAsync(companyId, id, userId, body?.Reason, cancellationToken);
        return Envelope(item);
    }
}

public sealed record CancelOrderRequest(string? Reason);
