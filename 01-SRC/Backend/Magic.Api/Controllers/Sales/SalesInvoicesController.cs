using Magic.Application.Sales.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Sales;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Sales;

[Authorize]
[Route("api/v{version:apiVersion}/sales/invoices")]
public sealed class SalesInvoicesController(
    ISalesInvoiceService invoiceService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : SalesBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SalesInvoiceDto>>>> GetInvoices(
        [FromQuery] Guid? branchId,
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("sales:invoice:view", "billing:sales:view");
        var companyId = ResolveCompanyId();
        var targetBranch = branchId ?? ResolveBranchId();
        var (items, total) = await invoiceService.GetInvoicesAsync(companyId, targetBranch, search, status, fromDate, toDate, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SalesInvoiceDto>>> GetInvoiceById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("sales:invoice:view", "billing:sales:view");
        var companyId = ResolveCompanyId();
        var invoice = await invoiceService.GetInvoiceByIdAsync(companyId, id, cancellationToken);
        return Envelope(invoice);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<SalesInvoiceDto>>> CancelInvoice(
        Guid id,
        [FromBody] CancelInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("sales:invoice:cancel");
        var companyId = ResolveCompanyId();
        var userId = ResolveUserId();
        var result = await invoiceService.CancelInvoiceAsync(companyId, id, userId, request, cancellationToken);
        return Envelope(result);
    }
}
