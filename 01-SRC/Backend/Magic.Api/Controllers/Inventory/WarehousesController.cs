using Magic.Application.Inventory.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Inventory;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Inventory;

[Authorize]
[Route("api/v{version:apiVersion}/inventory/warehouses")]
public sealed class WarehousesController(
    IWarehouseService warehouseService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : InventoryBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WarehouseDto>>>> GetWarehouses(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:warehouse:view");
        var companyId = ResolveCompanyId();
        var (items, total) = await warehouseService.GetWarehousesAsync(companyId, search, isActive, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> GetWarehouseById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:warehouse:view");
        var companyId = ResolveCompanyId();
        var item = await warehouseService.GetWarehouseByIdAsync(companyId, id, cancellationToken);
        return Envelope(item);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> CreateWarehouse(
        [FromBody] CreateWarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:warehouse:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var item = await warehouseService.CreateWarehouseAsync(companyId, userId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WarehouseDto>>> UpdateWarehouse(
        Guid id,
        [FromBody] UpdateWarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:warehouse:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var item = await warehouseService.UpdateWarehouseAsync(companyId, id, userId, request, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<ActionResult<ApiResponse<bool>>> ArchiveWarehouse(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:warehouse:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        await warehouseService.ArchiveWarehouseAsync(companyId, id, userId, cancellationToken);
        return Envelope(true);
    }

    [HttpGet("{id:guid}/locations")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StorageLocationDto>>>> GetLocations(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:warehouse:view");
        var companyId = ResolveCompanyId();
        var items = await warehouseService.GetStorageLocationsAsync(companyId, id, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("{id:guid}/locations")]
    public async Task<ActionResult<ApiResponse<StorageLocationDto>>> CreateLocation(
        Guid id,
        [FromBody] CreateStorageLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:warehouse:manage");
        var companyId = ResolveCompanyId();
        var item = await warehouseService.CreateStorageLocationAsync(companyId, id, request, cancellationToken);
        return CreatedEnvelope(item);
    }
}
