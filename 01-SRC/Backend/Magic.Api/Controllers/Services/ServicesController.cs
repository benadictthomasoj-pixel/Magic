using Asp.Versioning;
using Magic.Application.Services.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Security;
using Magic.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Services;

[Authorize]
[Route("api/v{version:apiVersion}/services")]
public sealed class ServicesController(
    IServiceDeskService serviceDeskService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : OperationsBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<ServiceSummaryDto>>> GetSummary(
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("service:requests:view");
        var companyId = ResolveCompanyId();
        var bId = branchId ?? ResolveBranchId();
        var summary = await serviceDeskService.GetSummaryAsync(companyId, bId, cancellationToken);
        return Envelope(summary);
    }

    [HttpGet("catalog")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ServiceCatalogDto>>>> GetCatalog(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("service:requests:view", "service:catalog:manage");
        var companyId = ResolveCompanyId();
        var items = await serviceDeskService.GetCatalogAsync(companyId, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("catalog")]
    public async Task<ActionResult<ApiResponse<ServiceCatalogDto>>> CreateCatalogEntry(
        [FromBody] CreateServiceCatalogRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("service:catalog:manage");
        var companyId = ResolveCompanyId();
        var item = await serviceDeskService.CreateCatalogEntryAsync(companyId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpGet("requests")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ServiceRequestDto>>>> GetRequests(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? customerId,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("service:requests:view");
        var companyId = ResolveCompanyId();
        var bId = branchId ?? ResolveBranchId();
        var (items, total) = await serviceDeskService.GetRequestsAsync(companyId, bId, customerId, status, priority, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("requests/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ServiceRequestDto>>> GetRequestById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("service:requests:view");
        var companyId = ResolveCompanyId();
        var item = await serviceDeskService.GetRequestByIdAsync(companyId, id, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("requests")]
    public async Task<ActionResult<ApiResponse<ServiceRequestDto>>> CreateRequest(
        [FromBody] CreateServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("service:requests:manage");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var item = await serviceDeskService.CreateRequestAsync(companyId, branchId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPut("requests/{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<ServiceRequestDto>>> UpdateRequestStatus(
        Guid id,
        [FromBody] UpdateServiceRequestStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("service:requests:manage");
        var companyId = ResolveCompanyId();
        var item = await serviceDeskService.UpdateRequestStatusAsync(companyId, id, request, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("requests/{id:guid}/quotations")]
    public async Task<ActionResult<ApiResponse<ServiceQuotationDto>>> CreateQuotation(
        Guid id,
        [FromBody] CreateServiceQuotationRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("service:quotations:manage");
        var companyId = ResolveCompanyId();
        var item = await serviceDeskService.CreateQuotationAsync(companyId, id, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPost("quotations/{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<ServiceQuotationDto>>> ApproveQuotation(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("service:quotations:manage");
        var companyId = ResolveCompanyId();
        var item = await serviceDeskService.ApproveQuotationAsync(companyId, id, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("requests/{id:guid}/parts")]
    public async Task<ActionResult<ApiResponse<ServicePartConsumptionDto>>> ConsumePart(
        Guid id,
        [FromBody] ConsumeServicePartRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("service:parts:consume");
        var companyId = ResolveCompanyId();
        var item = await serviceDeskService.ConsumePartAsync(companyId, id, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPost("requests/{id:guid}/invoice")]
    public async Task<ActionResult<ApiResponse<Guid>>> InvoiceServiceRequest(
        Guid id,
        [FromBody] InvoiceServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("service:billing:create", "sales:invoice:create", "billing:sales:create");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var invoiceId = await serviceDeskService.InvoiceServiceRequestAsync(companyId, id, userId, request, cancellationToken);
        return Envelope(invoiceId);
    }
}
