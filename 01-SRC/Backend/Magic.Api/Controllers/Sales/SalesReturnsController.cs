using Magic.Application.Sales.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Sales;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Sales;

[Authorize]
[Route("api/v{version:apiVersion}/sales")]
public sealed class SalesReturnsController(
    ISalesReturnService returnService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : SalesBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("returns")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SalesReturnDto>>>> GetReturns(
        [FromQuery] Guid? branchId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("sales:returns:view");
        var companyId = ResolveCompanyId();
        var targetBranch = branchId ?? ResolveBranchId();
        var (items, total) = await returnService.GetReturnsAsync(companyId, targetBranch, search, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("returns/{id:guid}")]
    public async Task<ActionResult<ApiResponse<SalesReturnDto>>> GetReturnById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("sales:returns:view");
        var companyId = ResolveCompanyId();
        var item = await returnService.GetReturnByIdAsync(companyId, id, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("returns")]
    public async Task<ActionResult<ApiResponse<SalesReturnDto>>> CreateReturn(
        [FromBody] CreateSalesReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("sales:returns:create");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var userId = ResolveUserId();
        var result = await returnService.CreateReturnAsync(companyId, branchId, userId, request, cancellationToken);
        return CreatedEnvelope(result);
    }

    [HttpGet("credit-notes")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CreditNoteDto>>>> GetCreditNotes(
        [FromQuery] Guid? customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("sales:credit-note:view");
        var companyId = ResolveCompanyId();
        var (items, total) = await returnService.GetCreditNotesAsync(companyId, customerId, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("credit-notes/{id:guid}")]
    public async Task<ActionResult<ApiResponse<CreditNoteDto>>> GetCreditNoteById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("sales:credit-note:view");
        var companyId = ResolveCompanyId();
        var item = await returnService.GetCreditNoteByIdAsync(companyId, id, cancellationToken);
        return Envelope(item);
    }
}
