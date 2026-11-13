using Asp.Versioning;
using Magic.Application.Branches.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Branches;
using Magic.Contracts.Common;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RegisterTerminalRequest = Magic.Contracts.Branches.RegisterTerminalRequest;

namespace Magic.Api.Controllers.Branches;

[Authorize]
[Route("api/v{version:apiVersion}/branches")]
public sealed class BranchesController(
    IBranchManagementService branchService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext userContext) : OperationsBaseController(dateTimeProvider, branchContext, userContext)
{
    [HttpGet("company/profile")]
    public async Task<ActionResult<ApiResponse<CompanyProfileDto>>> GetCompanyProfile(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("branches:view");
        var companyId = ResolveCompanyId();
        var item = await branchService.GetCompanyProfileAsync(companyId, cancellationToken);
        return Envelope(item);
    }

    [HttpPut("company/profile")]
    public async Task<ActionResult<ApiResponse<CompanyProfileDto>>> UpdateCompanyProfile(
        [FromBody] UpdateCompanyProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("branches:manage");
        var companyId = ResolveCompanyId();
        var item = await branchService.UpdateCompanyProfileAsync(companyId, request, cancellationToken);
        return Envelope(item);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BranchDto>>>> GetBranches(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("branches:view");
        var companyId = ResolveCompanyId();
        var items = await branchService.GetBranchesAsync(companyId, cancellationToken);
        return Envelope(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BranchDto>>> GetBranchById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("branches:view");
        var companyId = ResolveCompanyId();
        var item = await branchService.GetBranchByIdAsync(companyId, id, cancellationToken);
        return Envelope(item);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BranchDto>>> CreateBranch(
        [FromBody] CreateBranchRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("branches:manage");
        var companyId = ResolveCompanyId();
        var item = await branchService.CreateBranchAsync(companyId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BranchDto>>> UpdateBranch(
        Guid id,
        [FromBody] UpdateBranchRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("branches:manage");
        var companyId = ResolveCompanyId();
        var item = await branchService.UpdateBranchAsync(companyId, id, request, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("terminals")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TerminalDetailsDto>>>> GetTerminals(
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("branches:view");
        var companyId = ResolveCompanyId();
        var items = await branchService.GetTerminalsAsync(companyId, branchId, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("terminals")]
    public async Task<ActionResult<ApiResponse<TerminalDetailsDto>>> RegisterTerminal(
        [FromBody] RegisterTerminalRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("branches:manage");
        var companyId = ResolveCompanyId();
        var item = await branchService.RegisterTerminalAsync(companyId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPut("terminals/{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<TerminalDetailsDto>>> UpdateTerminalStatus(
        Guid id,
        [FromBody] UpdateTerminalStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("branches:manage");
        var companyId = ResolveCompanyId();
        var item = await branchService.UpdateTerminalStatusAsync(companyId, id, request, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("transfers")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InterBranchTransferRequestDto>>>> GetTransfers(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("branches:view");
        var companyId = ResolveCompanyId();
        var items = await branchService.GetInterBranchTransfersAsync(companyId, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("transfers")]
    public async Task<ActionResult<ApiResponse<InterBranchTransferRequestDto>>> CreateTransfer(
        [FromBody] CreateInterBranchTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("branches:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var item = await branchService.CreateInterBranchTransferAsync(companyId, userId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPost("transfers/{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<InterBranchTransferRequestDto>>> ApproveTransfer(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("branches:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var item = await branchService.ApproveInterBranchTransferAsync(companyId, id, userId, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("transfers/{id:guid}/dispatch")]
    public async Task<ActionResult<ApiResponse<InterBranchTransferRequestDto>>> DispatchTransfer(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("branches:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var item = await branchService.DispatchInterBranchTransferAsync(companyId, id, userId, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("transfers/{id:guid}/receive")]
    public async Task<ActionResult<ApiResponse<InterBranchTransferRequestDto>>> ReceiveTransfer(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("branches:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var item = await branchService.ReceiveInterBranchTransferAsync(companyId, id, userId, cancellationToken);
        return Envelope(item);
    }
}
