using Magic.Application.Inventory.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Inventory;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Inventory;

[Authorize]
[Route("api/v{version:apiVersion}/inventory/transfers")]
public sealed class StockTransfersController(
    IStockTransferService transferService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : InventoryBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StockTransferDto>>>> GetTransfers(
        [FromQuery] Guid? sourceWarehouseId,
        [FromQuery] Guid? destinationWarehouseId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:stock:view");
        var companyId = ResolveCompanyId();
        var (items, total) = await transferService.GetTransfersAsync(
            companyId, sourceWarehouseId, destinationWarehouseId, status, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StockTransferDto>>> GetTransferById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:stock:view");
        var companyId = ResolveCompanyId();
        var item = await transferService.GetTransferByIdAsync(companyId, id, cancellationToken);
        return Envelope(item);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<StockTransferDto>>> CreateTransfer(
        [FromBody] CreateStockTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:transfer:create");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var item = await transferService.CreateTransferAsync(companyId, userId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPost("{id:guid}/dispatch")]
    public async Task<ActionResult<ApiResponse<StockTransferDto>>> DispatchTransfer(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:transfer:dispatch");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var item = await transferService.DispatchTransferAsync(companyId, id, userId, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("{id:guid}/receive")]
    public async Task<ActionResult<ApiResponse<StockTransferDto>>> ReceiveTransfer(
        Guid id,
        [FromBody] ReceiveStockTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:transfer:receive");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var item = await transferService.ReceiveTransferAsync(companyId, id, userId, request, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<StockTransferDto>>> CancelTransfer(
        Guid id,
        [FromBody] CancelTransferRequest? body,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:transfer:create");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var item = await transferService.CancelTransferAsync(companyId, id, userId, body?.Reason, cancellationToken);
        return Envelope(item);
    }
}

public sealed record CancelTransferRequest(string? Reason);
