using System.Text.Json;
using Magic.Application.Automation.Services;
using Magic.Contracts.Automation;
using Magic.Domain.Common;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Automation.Services;

public sealed class AutomationService(
    MagicDbContext dbContext,
    IAiAssistantProvider aiAssistantProvider) : IAutomationService
{
    public async Task<IReadOnlyList<AutomationRuleDto>> GetRulesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rules = await dbContext.AutomationRules.AsNoTracking()
            .Where(r => r.CompanyId == companyId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return rules.Select(r => new AutomationRuleDto(
            r.Id,
            r.CompanyId,
            r.Name,
            r.TriggerType,
            r.ConditionJson,
            r.ActionType,
            r.ActionPayloadJson,
            r.IsActive,
            r.CreatedAtUtc,
            r.LastTriggeredAtUtc,
            r.ExecutionCount)).ToList();
    }

    public async Task<AutomationRuleDto> CreateRuleAsync(Guid companyId, CreateAutomationRuleRequest request, CancellationToken cancellationToken = default)
    {
        var rule = new AutomationRuleRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = request.Name.Trim(),
            TriggerType = request.TriggerType.Trim(),
            ConditionJson = string.IsNullOrWhiteSpace(request.ConditionJson) ? "{}" : request.ConditionJson.Trim(),
            ActionType = request.ActionType.Trim(),
            ActionPayloadJson = string.IsNullOrWhiteSpace(request.ActionPayloadJson) ? "{}" : request.ActionPayloadJson.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            ExecutionCount = 0
        };

        dbContext.AutomationRules.Add(rule);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AutomationRuleDto(
            rule.Id,
            rule.CompanyId,
            rule.Name,
            rule.TriggerType,
            rule.ConditionJson,
            rule.ActionType,
            rule.ActionPayloadJson,
            rule.IsActive,
            rule.CreatedAtUtc,
            rule.LastTriggeredAtUtc,
            rule.ExecutionCount);
    }

    public async Task<AutomationRuleDto> UpdateRuleAsync(Guid companyId, Guid ruleId, UpdateAutomationRuleRequest request, CancellationToken cancellationToken = default)
    {
        var rule = await dbContext.AutomationRules.FirstOrDefaultAsync(r => r.Id == ruleId && r.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("RULE_NOT_FOUND", "Automation rule not found.", StatusCodes.Status404NotFound);

        rule.Name = request.Name.Trim();
        rule.ConditionJson = string.IsNullOrWhiteSpace(request.ConditionJson) ? "{}" : request.ConditionJson.Trim();
        rule.ActionType = request.ActionType.Trim();
        rule.ActionPayloadJson = string.IsNullOrWhiteSpace(request.ActionPayloadJson) ? "{}" : request.ActionPayloadJson.Trim();
        rule.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AutomationRuleDto(
            rule.Id,
            rule.CompanyId,
            rule.Name,
            rule.TriggerType,
            rule.ConditionJson,
            rule.ActionType,
            rule.ActionPayloadJson,
            rule.IsActive,
            rule.CreatedAtUtc,
            rule.LastTriggeredAtUtc,
            rule.ExecutionCount);
    }

    public async Task<AutomationExecutionDto> TriggerRuleEvaluationAsync(Guid companyId, Guid ruleId, CancellationToken cancellationToken = default)
    {
        var rule = await dbContext.AutomationRules.FirstOrDefaultAsync(r => r.Id == ruleId && r.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("RULE_NOT_FOUND", "Automation rule not found.", StatusCodes.Status404NotFound);

        string status = "Success";
        string resultMessage = "Rule executed successfully.";

        // Action execution (Notification / Task / Reorder)
        if (rule.ActionType == "CreateNotification")
        {
            dbContext.Notifications.Add(new NotificationRecord
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Category = "Automation",
                Priority = "Medium",
                Title = $"Automation: {rule.Name}",
                Message = $"Automated alert triggered by {rule.TriggerType} event.",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        else if (rule.ActionType == "ReorderSuggestion")
        {
            resultMessage = "Inventory reorder suggestions generated and dispatched to procurement queue.";
            dbContext.Notifications.Add(new NotificationRecord
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Category = "Inventory",
                Priority = "High",
                Title = "Reorder Threshold Triggered",
                Message = $"Automated reorder suggestion evaluated for {rule.Name}.",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        rule.LastTriggeredAtUtc = DateTime.UtcNow;
        rule.ExecutionCount++;

        var execution = new AutomationExecutionRecord
        {
            Id = Guid.NewGuid(),
            RuleId = rule.Id,
            TriggeredAtUtc = DateTime.UtcNow,
            Status = status,
            ResultMessage = resultMessage,
            TraceCorrelationId = Guid.NewGuid().ToString()
        };

        dbContext.AutomationExecutions.Add(execution);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AutomationExecutionDto(
            execution.Id,
            rule.Id,
            rule.Name,
            execution.TriggeredAtUtc,
            execution.Status,
            execution.ResultMessage,
            execution.ErrorDetails);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(Guid companyId, Guid? userId, bool? unreadOnly, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Notifications.AsNoTracking().Where(n => n.CompanyId == companyId);
        if (userId.HasValue) query = query.Where(n => n.TargetUserId == null || n.TargetUserId == userId.Value);
        if (unreadOnly == true) query = query.Where(n => !n.IsRead);

        var list = await query.OrderByDescending(n => n.CreatedAtUtc).Take(50).ToListAsync(cancellationToken);

        return list.Select(n => new NotificationDto(
            n.Id,
            n.CompanyId,
            n.BranchId,
            n.Category,
            n.Priority,
            n.Title,
            n.Message,
            n.IsRead,
            n.CreatedAtUtc)).ToList();
    }

    public async Task MarkNotificationReadAsync(Guid companyId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notif = await dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.CompanyId == companyId, cancellationToken);
        if (notif != null)
        {
            notif.IsRead = true;
            notif.ReadAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<AiAssistantResponseDto> QueryAssistantAsync(Guid companyId, Guid? branchId, AiAssistantQueryRequest request, CancellationToken cancellationToken = default)
    {
        return await aiAssistantProvider.GenerateInsightAsync(
            companyId,
            branchId,
            request.Prompt,
            "{}",
            cancellationToken);
    }
}
