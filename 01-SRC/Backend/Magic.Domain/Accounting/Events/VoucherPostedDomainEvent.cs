using Magic.Domain.Accounting.Enums;
using Magic.Domain.Common;
using Magic.Domain.ValueObjects;

namespace Magic.Domain.Accounting.Events;

/// <summary>
/// Domain event published when a journal voucher is successfully committed to the General Ledger.
/// </summary>
public record VoucherPostedDomainEvent(
    Guid VoucherId,
    string VoucherNumber,
    VoucherType VoucherType,
    DateOnly VoucherDate,
    Money TotalAmount,
    Guid BranchId,
    Guid PostedByUserId) : DomainEventBase;
