using Magic.Application.Commercial.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Commercial;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Commercial;

[Authorize]
public sealed class SuppliersController(
    ISupplierPartyService supplierService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext)
    : CommercialBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("suppliers")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SupplierSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SupplierSummaryDto>>>> GetSuppliers(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] bool? isMsme,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:suppliers:view");
        var companyId = ResolveCompanyId();

        var (items, total) = await supplierService.GetSuppliersAsync(
            companyId, search, category, isMsme, isActive, page, pageSize, cancellationToken);

        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("suppliers/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SupplierDetailDto>>> GetSupplierById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:suppliers:view");
        var companyId = ResolveCompanyId();
        var supplier = await supplierService.GetSupplierByIdAsync(companyId, id, cancellationToken);
        return Envelope(supplier);
    }

    [HttpPost("suppliers")]
    [ProducesResponseType(typeof(ApiResponse<SupplierDetailDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<SupplierDetailDto>>> CreateSupplier(
        [FromBody] CreateSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:suppliers:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveAuthorizedUserId();

        var supplier = await supplierService.CreateSupplierAsync(companyId, userId, request, cancellationToken);
        return CreatedEnvelope(supplier);
    }

    [HttpPut("suppliers/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SupplierDetailDto>>> UpdateSupplier(
        Guid id,
        [FromBody] UpdateSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:suppliers:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveAuthorizedUserId();

        var supplier = await supplierService.UpdateSupplierAsync(companyId, userId, id, request, cancellationToken);
        return Envelope(supplier);
    }

    [HttpGet("suppliers/{id:guid}/bank-accounts/{bankAccountId:guid}/unmasked")]
    [ProducesResponseType(typeof(ApiResponse<UnmaskedBankAccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UnmaskedBankAccountDto>>> GetUnmaskedBankAccount(
        Guid id,
        Guid bankAccountId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:suppliers:bank-details");
        var companyId = ResolveCompanyId();
        var userId = ResolveAuthorizedUserId();

        var unmasked = await supplierService.GetUnmaskedBankAccountAsync(companyId, userId, id, bankAccountId, cancellationToken);
        return Envelope(unmasked);
    }
}
