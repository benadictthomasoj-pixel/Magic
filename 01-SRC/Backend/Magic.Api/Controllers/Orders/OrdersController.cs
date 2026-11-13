using Asp.Versioning;
using Magic.Application.Orders.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Orders;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Orders;

[Authorize]
[Route("api/v{version:apiVersion}/orders")]
public sealed class OrdersController(
    IOrderService orderService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : OperationsBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<OrderSummaryDto>>> GetSummary(
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("orders:view");
        var companyId = ResolveCompanyId();
        var bId = branchId ?? ResolveBranchId();
        var summary = await orderService.GetSummaryAsync(companyId, bId, cancellationToken);
        return Envelope(summary);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SalesOrderDto>>>> GetOrders(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? customerId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("orders:view");
        var companyId = ResolveCompanyId();
        var bId = branchId ?? ResolveBranchId();
        var (items, total) = await orderService.GetOrdersAsync(companyId, bId, customerId, status, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> GetOrderById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("orders:view");
        var companyId = ResolveCompanyId();
        var item = await orderService.GetOrderByIdAsync(companyId, id, cancellationToken);
        return Envelope(item);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> CreateOrder(
        [FromBody] CreateSalesOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("orders:create");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var item = await orderService.CreateOrderAsync(companyId, branchId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> ConfirmOrder(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("orders:approve");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var item = await orderService.ConfirmOrderAsync(companyId, id, userId, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("{id:guid}/fulfill")]
    public async Task<ActionResult<ApiResponse<OrderFulfillmentDto>>> FulfillOrder(
        Guid id,
        [FromBody] FulfillOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("orders:fulfill");
        var companyId = ResolveCompanyId();
        var item = await orderService.FulfillOrderAsync(companyId, id, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> CancelOrder(
        Guid id,
        [FromBody] CancelOrderApiRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("orders:cancel");
        var companyId = ResolveCompanyId();
        var item = await orderService.CancelOrderAsync(companyId, id, request.Reason, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("backorders")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BackorderDto>>>> GetBackorders(
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("orders:view");
        var companyId = ResolveCompanyId();
        var bId = branchId ?? ResolveBranchId();
        var items = await orderService.GetBackordersAsync(companyId, bId, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("{id:guid}/invoice")]
    public async Task<ActionResult<ApiResponse<Guid>>> ConvertOrderToInvoice(
        Guid id,
        [FromBody] ConvertOrderToInvoiceApiRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("orders:fulfill", "sales:invoice:create", "billing:sales:create");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var invoiceId = await orderService.ConvertOrderToInvoiceAsync(companyId, id, userId, request.PaymentMethod, cancellationToken);
        return Envelope(invoiceId);
    }
}

public sealed record CancelOrderApiRequest(string Reason);
public sealed record ConvertOrderToInvoiceApiRequest(string PaymentMethod);
