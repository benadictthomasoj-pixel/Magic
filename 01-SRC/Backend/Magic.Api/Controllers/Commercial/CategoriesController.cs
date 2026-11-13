using Magic.Application.Commercial.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Commercial;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Commercial;

[Authorize]
public sealed class CategoriesController(
    IProductCatalogService catalogService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext)
    : CommercialBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryDto>>>> GetCategoriesTree(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:products:view");
        var companyId = ResolveCompanyId();
        var tree = await catalogService.GetCategoriesTreeAsync(companyId, cancellationToken);
        return Envelope(tree);
    }

    [HttpGet("categories/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategoryById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:products:view");
        var companyId = ResolveCompanyId();
        var cat = await catalogService.GetCategoryByIdAsync(companyId, id, cancellationToken);
        return Envelope(cat);
    }

    [HttpPost("categories")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:categories:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveAuthorizedUserId();

        var cat = await catalogService.CreateCategoryAsync(companyId, userId, request, cancellationToken);
        return CreatedEnvelope(cat);
    }

    [HttpPut("categories/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:categories:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveAuthorizedUserId();

        var cat = await catalogService.UpdateCategoryAsync(companyId, userId, id, request, cancellationToken);
        return Envelope(cat);
    }

    [HttpDelete("categories/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> DeleteCategory(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:categories:manage");
        var companyId = ResolveCompanyId();

        await catalogService.DeleteCategoryAsync(companyId, id, cancellationToken);
        return Envelope($"Category {id} has been deleted.");
    }
}
