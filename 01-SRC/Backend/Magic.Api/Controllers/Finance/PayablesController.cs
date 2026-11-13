using Asp.Versioning;
using Magic.Application.Finance.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Finance;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Finance;

[Route("api/v{version:apiVersion}/accounting/payables")]
[Route("api/v{version:apiVersion}/finance/payables")]
public sealed class PayablesController(
    IPayablesService payablesService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : FinanceBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<PayablesSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        EnsurePermission("finance:payables:view");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var result = await payablesService.GetSummaryAsync(companyId, branchId, cancellationToken);
        return Ok(Wrap(result));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SupplierPayableDto>>>> GetPayables(
        [FromQuery] Guid? supplierId,
        [FromQuery] string? status,
        [FromQuery] bool overdueOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:payables:view");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var (items, totalCount) = await payablesService.GetPayablesAsync(
            companyId, branchId, supplierId, status, overdueOnly, page, pageSize, cancellationToken);

        var meta = new PaginationMeta(page, pageSize, totalCount);

        return Ok(Wrap(items, meta));
    }

    [HttpGet("aging")]
    public async Task<ActionResult<ApiResponse<SupplierAgingReportDto>>> GetAgingReport(
        [FromQuery] DateOnly? reportDate,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:payables:view");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var date = reportDate ?? DateOnly.FromDateTime(Clock.UtcNow.Date);

        var report = await payablesService.GetAgingReportAsync(companyId, branchId, date, cancellationToken);
        return Ok(Wrap(report));
    }

    [HttpGet("statement/{supplierId:guid}")]
    public async Task<ActionResult<ApiResponse<SupplierStatementDto>>> GetStatement(
        Guid supplierId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:payables:view");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var from = fromDate ?? new DateOnly(2026, 4, 1);
        var to = toDate ?? DateOnly.FromDateTime(Clock.UtcNow.Date);

        var statement = await payablesService.GetSupplierStatementAsync(companyId, branchId, supplierId, from, to, cancellationToken);
        return Ok(Wrap(statement));
    }

    [HttpPost("bills")]
    public async Task<ActionResult<ApiResponse<SupplierBillDto>>> CreateBill(
        [FromBody] CreateSupplierBillRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:bills:create");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var userId = ResolveCurrentUserId();

        var bill = await payablesService.CreateSupplierBillAsync(companyId, branchId, userId, request, cancellationToken);
        return CreatedAtAction(nameof(GetBillById), new { id = bill.Id }, Wrap(bill));
    }

    [HttpGet("bills/{id:guid}")]
    public async Task<ActionResult<ApiResponse<SupplierBillDto>>> GetBillById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:payables:view");
        var companyId = ResolveCompanyId();

        var bill = await payablesService.GetSupplierBillByIdAsync(companyId, id, cancellationToken);
        return Ok(Wrap(bill));
    }
}
