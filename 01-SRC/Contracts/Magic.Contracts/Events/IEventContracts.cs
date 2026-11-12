namespace Magic.Contracts.Events;

/// <summary>
/// Versioned outbox payload contract for secondary asynchronous domain notifications.
/// Core financial and inventory writes always execute synchronously in the primary ACID transaction.
/// </summary>
public sealed record IntegrationEventEnvelope
{
    public required Guid EventId { get; init; }
    public required string AggregateType { get; init; }
    public required Guid AggregateId { get; init; }
    public required string EventType { get; init; }
    public required string PayloadJson { get; init; }
    public required Guid CorrelationId { get; init; }
    public DateTimeOffset OccurredOnUtc { get; init; }
}

/// <summary>
/// Enqueues secondary asynchronous events into platform.outbox_messages within the current transaction.
/// </summary>
public interface IOutboxEventDispatcher
{
    Task EnqueueAsync(IntegrationEventEnvelope integrationEvent, CancellationToken cancellationToken = default);
}

