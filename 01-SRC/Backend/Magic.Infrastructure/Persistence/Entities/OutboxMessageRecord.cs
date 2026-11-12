namespace Magic.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence record for reliable secondary asynchronous event dispatch (platform.outbox_messages).
/// </summary>
public sealed class OutboxMessageRecord
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredOnUtc { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public Guid TraceCorrelationId { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public Guid? LockToken { get; set; }
    public DateTimeOffset? LockExpiresAtUtc { get; set; }
}

