using Magic.Application.Sales.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Sales;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Sales;

[Authorize]
[Route("api/v{version:apiVersion}/pos")]
public sealed class PosController(
    IPosService posService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : SalesBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("lookup")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PosProductLookupDto>>>> LookupProducts(
        [FromQuery] Guid warehouseId,
        [FromQuery] string? query,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("pos:terminal:operate", "billing:pos:access");
        var companyId = ResolveCompanyId();
        var items = await posService.LookupProductsAsync(companyId, warehouseId, query ?? string.Empty, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<ApiResponse<SalesInvoiceDto>>> Checkout(
        [FromBody] CreatePosSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("pos:terminal:operate", "sales:invoice:create", "billing:sales:create");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var userId = ResolveUserId();
        var result = await posService.CheckoutAsync(companyId, branchId, userId, request, cancellationToken);
        return CreatedEnvelope(result);
    }
}
