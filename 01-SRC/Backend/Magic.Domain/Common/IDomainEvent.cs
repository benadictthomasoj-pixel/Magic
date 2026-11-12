namespace Magic.Domain.Common;

/// <summary>
/// Marker interface for strongly typed domain events raised inside aggregates.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOnUtc { get; }
}

/// <summary>
/// Base implementation providing sequential UUIDv7 identity for domain events.
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

