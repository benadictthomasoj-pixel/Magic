using Magic.Application.Inventory.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Inventory;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Inventory;

[Authorize]
[Route("api/v{version:apiVersion}/inventory/counts")]
public sealed class StockCountsController(
    IStockCountService countService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : InventoryBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StockCountDto>>>> GetCounts(
        [FromQuery] Guid? warehouseId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:count:view");
        var companyId = ResolveCompanyId();
        var (items, total) = await countService.GetCountsAsync(companyId, warehouseId, status, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StockCountDto>>> GetCountById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:count:view");
        var companyId = ResolveCompanyId();
        var item = await countService.GetCountByIdAsync(companyId, id, cancellationToken);
        return Envelope(item);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<StockCountDto>>> CreateCount(
        [FromBody] CreateStockCountRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:count:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var item = await countService.CreateCountAsync(companyId, userId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPost("{id:guid}/record-lines")]
    public async Task<ActionResult<ApiResponse<StockCountDto>>> RecordLines(
        Guid id,
        [FromBody] RecordStockCountLinesRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:count:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var item = await countService.RecordCountLinesAsync(companyId, id, userId, request, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("{id:guid}/reconcile")]
    public async Task<ActionResult<ApiResponse<StockCountDto>>> Reconcile(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:count:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var item = await countService.ReconcileCountAsync(companyId, id, userId, cancellationToken);
        return Envelope(item);
    }
}
