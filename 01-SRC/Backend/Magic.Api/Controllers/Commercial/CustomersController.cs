using Magic.Application.Commercial.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Commercial;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Commercial;

[Authorize]
public sealed class CustomersController(
    ICustomerPartyService customerService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext)
    : CommercialBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("customers")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CustomerSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerSummaryDto>>>> GetCustomers(
        [FromQuery] string? search,
        [FromQuery] CustomerType? customerType,
        [FromQuery] GstRegistrationType? gstType,
        [FromQuery] Guid? customerGroupId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:customers:view");
        var companyId = ResolveCompanyId();

        var (items, total) = await customerService.GetCustomersAsync(
            companyId, search, customerType, gstType, customerGroupId, isActive, page, pageSize, cancellationToken);

        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("customers/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CustomerDetailDto>>> GetCustomerById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:customers:view");
        var companyId = ResolveCompanyId();
        var customer = await customerService.GetCustomerByIdAsync(companyId, id, cancellationToken);
        return Envelope(customer);
    }

    [HttpPost("customers")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDetailDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CustomerDetailDto>>> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:customers:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveAuthorizedUserId();

        var customer = await customerService.CreateCustomerAsync(companyId, userId, request, cancellationToken);
        return CreatedEnvelope(customer);
    }

    [HttpPut("customers/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CustomerDetailDto>>> UpdateCustomer(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:customers:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveAuthorizedUserId();

        var customer = await customerService.UpdateCustomerAsync(companyId, userId, id, request, cancellationToken);
        return Envelope(customer);
    }

    [HttpPut("customers/{id:guid}/credit-limit")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> UpdateCreditLimit(
        Guid id,
        [FromBody] UpdateCreditLimitRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:credit:approve");
        var companyId = ResolveCompanyId();
        var userId = ResolveAuthorizedUserId();

        await customerService.UpdateCreditLimitAsync(companyId, userId, id, request, cancellationToken);
        return Envelope($"Credit limit updated for customer {id}.");
    }

    [HttpGet("customers/groups")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CustomerGroupDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerGroupDto>>>> GetCustomerGroups(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:customers:view");
        var companyId = ResolveCompanyId();
        var groups = await customerService.GetCustomerGroupsAsync(companyId, cancellationToken);
        return Envelope(groups);
    }

    [HttpPost("customers/groups")]
    [ProducesResponseType(typeof(ApiResponse<CustomerGroupDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CustomerGroupDto>>> CreateCustomerGroup(
        [FromBody] CreateCustomerGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("commercial:customers:manage");
        var companyId = ResolveCompanyId();

        var group = await customerService.CreateCustomerGroupAsync(companyId, request, cancellationToken);
        return CreatedEnvelope(group);
    }
}
