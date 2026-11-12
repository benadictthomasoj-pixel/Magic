namespace Magic.Domain.Common;

/// <summary>
/// Marker interface identifying aggregate roots that own transactional consistency boundaries.
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

/// <summary>
/// Audit and multi-branch scoping metadata contract applied to persistent domain entities.
/// </summary>
public interface IAuditableEntity
{
    Guid BranchId { get; }
    DateTimeOffset CreatedAtUtc { get; }
    Guid CreatedByUserId { get; }
    DateTimeOffset UpdatedAtUtc { get; }
    Guid? UpdatedByUserId { get; }
    int ConcurrencyVersion { get; }
}

/// <summary>
/// Base domain entity using sequential UUIDv7 primary keys.
/// </summary>
public abstract class DomainEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected DomainEntity()
    {
        Id = Guid.CreateVersion7();
    }

    protected DomainEntity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Entity identity cannot be empty.", nameof(id));
        }

        Id = id;
    }

    public Guid Id { get; protected set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}

