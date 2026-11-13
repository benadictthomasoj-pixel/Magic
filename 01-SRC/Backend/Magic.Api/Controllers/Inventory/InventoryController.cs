using Magic.Application.Inventory.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Inventory;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Inventory;

[Authorize]
[Route("api/v{version:apiVersion}/inventory/stock")]
public sealed class InventoryController(
    IInventoryLedgerService inventoryLedgerService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : InventoryBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("balances")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StockBalanceDto>>>> GetStockBalances(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? productId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:stock:view");
        var companyId = ResolveCompanyId();
        var (items, total) = await inventoryLedgerService.GetStockBalancesAsync(
            companyId, branchId, warehouseId, productId, search, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("movements")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StockMovementDto>>>> GetStockMovements(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? productId,
        [FromQuery] string? movementType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:stock:view");
        var companyId = ResolveCompanyId();
        var (items, total) = await inventoryLedgerService.GetStockMovementsAsync(
            companyId, warehouseId, productId, movementType, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpPost("opening-stock")]
    public async Task<ActionResult<ApiResponse<bool>>> PostOpeningStock(
        [FromBody] PostOpeningStockRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:stock:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        await inventoryLedgerService.PostOpeningStockAsync(companyId, userId, request, cancellationToken);
        return Envelope(true);
    }
}
