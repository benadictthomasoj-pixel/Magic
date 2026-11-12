using Magic.Domain.Common;

namespace Magic.Domain.Accounting.Events;

/// <summary>
/// Domain event published when an accounting fiscal period is transitioned to LOCKED state.
/// </summary>
public record FiscalPeriodLockedDomainEvent(
    Guid FiscalPeriodId,
    string PeriodCode,
    Guid LockedByUserId) : DomainEventBase;
