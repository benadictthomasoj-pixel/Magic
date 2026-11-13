using Asp.Versioning;
using Magic.Application.Finance.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Finance;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Finance;

[Route("api/v{version:apiVersion}/accounting/supplier-payments")]
[Route("api/v{version:apiVersion}/finance/supplier-payments")]
public sealed class SupplierPaymentsController(
    ISupplierPaymentService paymentService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : FinanceBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SupplierPaymentDto>>>> GetPayments(
        [FromQuery] Guid? supplierId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:payables:view");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var (items, totalCount) = await paymentService.GetPaymentsAsync(
            companyId, branchId, supplierId, fromDate, toDate, status, page, pageSize, cancellationToken);

        var meta = new PaginationMeta(page, pageSize, totalCount);

        return Ok(Wrap(items, meta));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SupplierPaymentDto>>> GetPaymentById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:payables:view");
        var companyId = ResolveCompanyId();

        var item = await paymentService.GetPaymentByIdAsync(companyId, id, cancellationToken);
        return Ok(Wrap(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SupplierPaymentDto>>> CreatePayment(
        [FromBody] CreateSupplierPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:payments:create");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var userId = ResolveCurrentUserId();

        var payment = await paymentService.CreatePaymentAsync(companyId, branchId, userId, request, cancellationToken);
        return CreatedAtAction(nameof(GetPaymentById), new { id = payment.Id }, Wrap(payment));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<SupplierPaymentDto>>> CancelPayment(
        Guid id,
        [FromBody] CancelPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:payments:cancel");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var userId = ResolveCurrentUserId();

        var payment = await paymentService.CancelPaymentAsync(companyId, branchId, id, userId, request, cancellationToken);
        return Ok(Wrap(payment));
    }
}
