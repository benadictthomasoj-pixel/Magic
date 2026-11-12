using Magic.Contracts.Common;
using Magic.Contracts.Events;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;

namespace Magic.Infrastructure.Outbox;

/// <summary>
/// Appends versioned integration events to platform.outbox_messages within the caller's ACID transaction.
/// </summary>
public sealed class OutboxEventDispatcher(
    MagicDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : IOutboxEventDispatcher
{
    public Task EnqueueAsync(IntegrationEventEnvelope integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        dbContext.OutboxMessages.Add(new OutboxMessageRecord
        {
            Id = integrationEvent.EventId == Guid.Empty ? Guid.CreateVersion7() : integrationEvent.EventId,
            OccurredOnUtc = integrationEvent.OccurredOnUtc == default ? dateTimeProvider.UtcNow : integrationEvent.OccurredOnUtc,
            AggregateType = integrationEvent.AggregateType,
            AggregateId = integrationEvent.AggregateId,
            EventType = integrationEvent.EventType,
            PayloadJson = integrationEvent.PayloadJson,
            TraceCorrelationId = integrationEvent.CorrelationId
        });

        return Task.CompletedTask;
    }
}

