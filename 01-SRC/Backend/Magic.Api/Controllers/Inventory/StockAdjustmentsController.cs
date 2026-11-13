using Magic.Application.Inventory.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Inventory;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Inventory;

[Authorize]
[Route("api/v{version:apiVersion}/inventory/adjustments")]
public sealed class StockAdjustmentsController(
    IStockAdjustmentService adjustmentService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : InventoryBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StockAdjustmentDto>>>> GetAdjustments(
        [FromQuery] Guid? warehouseId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:stock:view");
        var companyId = ResolveCompanyId();
        var (items, total) = await adjustmentService.GetAdjustmentsAsync(companyId, warehouseId, status, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StockAdjustmentDto>>> GetAdjustmentById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:stock:view");
        var companyId = ResolveCompanyId();
        var item = await adjustmentService.GetAdjustmentByIdAsync(companyId, id, cancellationToken);
        return Envelope(item);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<StockAdjustmentDto>>> CreateAdjustment(
        [FromBody] CreateStockAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:adjust:create");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var item = await adjustmentService.CreateAdjustmentAsync(companyId, userId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<StockAdjustmentDto>>> ApproveAdjustment(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:adjust:approve");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var item = await adjustmentService.ApproveAdjustmentAsync(companyId, id, userId, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<StockAdjustmentDto>>> RejectAdjustment(
        Guid id,
        [FromBody] RejectAdjustmentRequest? body,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:adjust:approve");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var item = await adjustmentService.RejectAdjustmentAsync(companyId, id, userId, body?.Reason, cancellationToken);
        return Envelope(item);
    }
}

public sealed record RejectAdjustmentRequest(string? Reason);
