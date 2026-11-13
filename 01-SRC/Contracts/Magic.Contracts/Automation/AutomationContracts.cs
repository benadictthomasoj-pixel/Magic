namespace Magic.Contracts.Automation;

public sealed record AutomationRuleDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string TriggerType, // "LowStock", "OverduePayment", "PendingApproval", "DailySummary"
    string ConditionJson,
    string ActionType, // "CreateNotification", "SendTask", "ReorderSuggestion"
    string ActionPayloadJson,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastTriggeredAtUtc,
    int ExecutionCount);

public sealed record CreateAutomationRuleRequest(
    string Name,
    string TriggerType,
    string ConditionJson,
    string ActionType,
    string ActionPayloadJson);

public sealed record UpdateAutomationRuleRequest(
    string Name,
    string ConditionJson,
    string ActionType,
    string ActionPayloadJson,
    bool IsActive);

public sealed record AutomationExecutionDto(
    Guid Id,
    Guid RuleId,
    string RuleName,
    DateTime TriggeredAtUtc,
    string Status, // "Success", "Failed", "Skipped"
    string? ResultMessage,
    string? ErrorDetails);

public sealed record NotificationDto(
    Guid Id,
    Guid CompanyId,
    Guid? BranchId,
    string Category,
    string Priority, // "Low", "Medium", "High", "Critical"
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAtUtc);

public sealed record AiAssistantQueryRequest(
    string Prompt,
    Guid? BranchId);

public sealed record AiAssistantResponseDto(
    string Answer,
    string SourceModule,
    string ReportingPeriod,
    IReadOnlyList<AiDataDrilldownDto> Drilldowns,
    IReadOnlyList<string> RecommendedActions,
    DateTime GeneratedAtUtc);

public sealed record AiDataDrilldownDto(
    string Label,
    string Value,
    string? RouteUrl);
