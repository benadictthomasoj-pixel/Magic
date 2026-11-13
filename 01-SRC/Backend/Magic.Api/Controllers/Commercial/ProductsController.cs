using Magic.Application.Commercial.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Commercial;
using Magic.Contracts.Security;
using Magic.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Commercial;

[Authorize]
public sealed class ProductsController(
    IProductCatalogService catalogService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext)
    : CommercialBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("products")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProductSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductSummaryDto>>>> GetProducts(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? brandId,
        [FromQuery] ProductType? productType,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:products:view");
        var companyId = ResolveCompanyId();

        var (items, total) = await catalogService.GetProductsAsync(
            companyId, search, categoryId, brandId, productType, isActive, page, pageSize, cancellationToken);

        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("products/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> GetProductById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:products:view");
        var companyId = ResolveCompanyId();
        var product = await catalogService.GetProductByIdAsync(companyId, id, cancellationToken);
        return Envelope(product);
    }

    [HttpGet("products/lookup/barcode/{barcode}")]
    [ProducesResponseType(typeof(ApiResponse<BarcodeLookupResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BarcodeLookupResultDto>>> LookupBarcode(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:products:view");
        var companyId = ResolveCompanyId();
        var result = await catalogService.LookupBarcodeAsync(companyId, barcode, cancellationToken);

        if (result == null)
        {
            throw new DomainException(
                "BARCODE_NOT_FOUND",
                $"No product found registered with barcode '{barcode}'.",
                StatusCodes.Status404NotFound);
        }

        return Envelope(result);
    }

    [HttpPost("products")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:products:create");
        var companyId = ResolveCompanyId();
        var userId = ResolveAuthorizedUserId();

        var product = await catalogService.CreateProductAsync(companyId, userId, request, cancellationToken);
        return CreatedEnvelope(product);
    }

    [HttpPut("products/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:products:edit");
        var companyId = ResolveCompanyId();
        var userId = ResolveAuthorizedUserId();

        var product = await catalogService.UpdateProductAsync(companyId, userId, id, request, cancellationToken);
        return Envelope(product);
    }

    [HttpDelete("products/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> DeactivateProduct(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:products:archive");
        var companyId = ResolveCompanyId();
        var userId = ResolveAuthorizedUserId();

        await catalogService.DeactivateProductAsync(companyId, userId, id, cancellationToken);
        return Envelope($"Product {id} has been deactivated.");
    }
}
