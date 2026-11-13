using Magic.Contracts.Automation;

namespace Magic.Application.Automation.Services;

public interface IAutomationService
{
    Task<IReadOnlyList<AutomationRuleDto>> GetRulesAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<AutomationRuleDto> CreateRuleAsync(Guid companyId, CreateAutomationRuleRequest request, CancellationToken cancellationToken = default);
    Task<AutomationRuleDto> UpdateRuleAsync(Guid companyId, Guid ruleId, UpdateAutomationRuleRequest request, CancellationToken cancellationToken = default);
    Task<AutomationExecutionDto> TriggerRuleEvaluationAsync(Guid companyId, Guid ruleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(Guid companyId, Guid? userId, bool? unreadOnly, CancellationToken cancellationToken = default);
    Task MarkNotificationReadAsync(Guid companyId, Guid notificationId, CancellationToken cancellationToken = default);
    Task<AiAssistantResponseDto> QueryAssistantAsync(Guid companyId, Guid? branchId, AiAssistantQueryRequest request, CancellationToken cancellationToken = default);
}
