using Asp.Versioning;
using Magic.Application.Automation.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Automation;
using Magic.Contracts.Common;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Automation;

[Authorize]
[Route("api/v{version:apiVersion}/automation")]
public sealed class AutomationController(
    IAutomationService automationService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext userContext) : OperationsBaseController(dateTimeProvider, branchContext, userContext)
{
    [HttpGet("rules")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AutomationRuleDto>>>> GetRules(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("automation:rules:manage");
        var companyId = ResolveCompanyId();
        var items = await automationService.GetRulesAsync(companyId, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("rules")]
    public async Task<ActionResult<ApiResponse<AutomationRuleDto>>> CreateRule(
        [FromBody] CreateAutomationRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("automation:rules:manage");
        var companyId = ResolveCompanyId();
        var item = await automationService.CreateRuleAsync(companyId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPut("rules/{id:guid}")]
    public async Task<ActionResult<ApiResponse<AutomationRuleDto>>> UpdateRule(
        Guid id,
        [FromBody] UpdateAutomationRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("automation:rules:manage");
        var companyId = ResolveCompanyId();
        var item = await automationService.UpdateRuleAsync(companyId, id, request, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("rules/{id:guid}/trigger")]
    public async Task<ActionResult<ApiResponse<AutomationExecutionDto>>> TriggerRule(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("automation:rules:manage");
        var companyId = ResolveCompanyId();
        var item = await automationService.TriggerRuleEvaluationAsync(companyId, id, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationDto>>>> GetNotifications(
        [FromQuery] bool? unreadOnly,
        CancellationToken cancellationToken = default)
    {
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var items = await automationService.GetNotificationsAsync(companyId, userId, unreadOnly, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("notifications/{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<string>>> MarkNotificationRead(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = ResolveCompanyId();
        await automationService.MarkNotificationReadAsync(companyId, id, cancellationToken);
        return Envelope("Notification marked as read.");
    }

    [HttpPost("assistant/query")]
    public async Task<ActionResult<ApiResponse<AiAssistantResponseDto>>> QueryAssistant(
        [FromBody] AiAssistantQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("automation:assistant:use");
        var companyId = ResolveCompanyId();
        var item = await automationService.QueryAssistantAsync(companyId, request.BranchId, request, cancellationToken);
        return Envelope(item);
    }
}
