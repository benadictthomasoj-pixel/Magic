using Magic.Application.Inventory.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Inventory;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Inventory;

[Authorize]
[Route("api/v{version:apiVersion}/inventory/reorder")]
public sealed class ReorderController(
    IReorderService reorderService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : InventoryBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("rules")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReorderRuleDto>>>> GetRules(
        [FromQuery] Guid? warehouseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:reorder:view");
        var companyId = ResolveCompanyId();
        var (items, total) = await reorderService.GetReorderRulesAsync(companyId, warehouseId, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpPost("rules")]
    public async Task<ActionResult<ApiResponse<ReorderRuleDto>>> UpsertRule(
        [FromBody] UpsertReorderRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:reorder:manage");
        var companyId = ResolveCompanyId();
        var item = await reorderService.UpsertReorderRuleAsync(companyId, request, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("suggestions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReorderSuggestionDto>>>> GetSuggestions(
        [FromQuery] Guid? warehouseId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("inventory:reorder:view");
        var companyId = ResolveCompanyId();
        var items = await reorderService.GetReorderSuggestionsAsync(companyId, warehouseId, cancellationToken);
        return Envelope(items);
    }
}
