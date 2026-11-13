using Magic.Application.Sales.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Sales;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Sales;

[Authorize]
[Route("api/v{version:apiVersion}/cashier/sessions")]
public sealed class CashierSessionsController(
    ICashierSessionService cashierSessionService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : SalesBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<CashierSessionDto?>>> GetActiveSession(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("pos:terminal:operate", "pos:terminal:open-close-shift");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var userId = ResolveUserId();
        var session = await cashierSessionService.GetActiveSessionAsync(companyId, branchId, userId, cancellationToken);
        return Envelope(session);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CashierSessionDto>>>> GetSessions(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? userId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("pos:terminal:open-close-shift");
        var companyId = ResolveCompanyId();
        var (items, total) = await cashierSessionService.GetSessionsAsync(companyId, branchId, userId, status, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CashierSessionDto>>> GetSessionById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("pos:terminal:open-close-shift");
        var companyId = ResolveCompanyId();
        var session = await cashierSessionService.GetSessionByIdAsync(companyId, id, cancellationToken);
        return Envelope(session);
    }

    [HttpPost("open")]
    public async Task<ActionResult<ApiResponse<CashierSessionDto>>> OpenSession(
        [FromBody] OpenCashierSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("pos:terminal:open-close-shift");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var userId = ResolveUserId();
        var session = await cashierSessionService.OpenSessionAsync(companyId, branchId, userId, request, cancellationToken);
        return CreatedEnvelope(session);
    }

    [HttpPost("{id:guid}/movements")]
    public async Task<ActionResult<ApiResponse<CashMovementDto>>> RecordMovement(
        Guid id,
        [FromBody] RecordCashMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("pos:cash:movement");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var movement = await cashierSessionService.RecordMovementAsync(companyId, id, userId, request, cancellationToken);
        return CreatedEnvelope(movement);
    }

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<ApiResponse<CashierSessionDto>>> CloseSession(
        Guid id,
        [FromBody] CloseCashierSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("pos:terminal:open-close-shift");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var session = await cashierSessionService.CloseSessionAsync(companyId, id, userId, request, cancellationToken);
        return Envelope(session);
    }
}
