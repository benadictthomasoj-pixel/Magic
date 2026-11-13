using Magic.Application.Commercial.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Commercial;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Commercial;

[Authorize]
public sealed class BrandsAndDepartmentsController(
    IProductCatalogService catalogService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext)
    : CommercialBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    // Brands
    [HttpGet("brands")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BrandDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BrandDto>>>> GetBrands(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:products:view");
        var companyId = ResolveCompanyId();
        var brands = await catalogService.GetBrandsAsync(companyId, cancellationToken);
        return Envelope(brands);
    }

    [HttpPost("brands")]
    [ProducesResponseType(typeof(ApiResponse<BrandDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<BrandDto>>> CreateBrand(
        [FromBody] CreateBrandRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:brands:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveAuthorizedUserId();

        var brand = await catalogService.CreateBrandAsync(companyId, userId, request, cancellationToken);
        return CreatedEnvelope(brand);
    }

    [HttpPut("brands/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BrandDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BrandDto>>> UpdateBrand(
        Guid id,
        [FromBody] UpdateBrandRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:brands:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveAuthorizedUserId();

        var brand = await catalogService.UpdateBrandAsync(companyId, userId, id, request, cancellationToken);
        return Envelope(brand);
    }

    // Departments
    [HttpGet("departments")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DepartmentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DepartmentDto>>>> GetDepartments(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:products:view");
        var companyId = ResolveCompanyId();
        var depts = await catalogService.GetDepartmentsAsync(companyId, cancellationToken);
        return Envelope(depts);
    }

    [HttpPost("departments")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> CreateDepartment(
        [FromBody] CreateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:categories:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveAuthorizedUserId();

        var dept = await catalogService.CreateDepartmentAsync(companyId, userId, request, cancellationToken);
        return CreatedEnvelope(dept);
    }

    [HttpPut("departments/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> UpdateDepartment(
        Guid id,
        [FromBody] UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:categories:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveAuthorizedUserId();

        var dept = await catalogService.UpdateDepartmentAsync(companyId, userId, id, request, cancellationToken);
        return Envelope(dept);
    }

    // Units of Measure
    [HttpGet("units")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UnitOfMeasureDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UnitOfMeasureDto>>>> GetUnits(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:products:view");
        var companyId = ResolveCompanyId();
        var units = await catalogService.GetUnitsOfMeasureAsync(companyId, cancellationToken);
        return Envelope(units);
    }

    // Tax Codes
    [HttpGet("tax-codes")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TaxCodeDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TaxCodeDto>>>> GetTaxCodes(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:products:view");
        var companyId = ResolveCompanyId();
        var codes = await catalogService.GetTaxCodesAsync(companyId, cancellationToken);
        return Envelope(codes);
    }
}
