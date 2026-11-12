using Magic.Domain.Common;

namespace Magic.Domain.Accounting.Events;

/// <summary>
/// Domain event published when a journal voucher is neutralized by an authorized companion reversal voucher.
/// </summary>
public record VoucherReversedDomainEvent(
    Guid OriginalVoucherId,
    Guid ReversalVoucherId,
    string ReversalVoucherNumber,
    Guid SupervisorUserId) : DomainEventBase;
