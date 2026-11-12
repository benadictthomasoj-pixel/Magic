namespace Magic.Domain.Accounting.Enums;

/// <summary>
/// Lifecycle status of an accounting journal voucher.
/// </summary>
public enum VoucherStatus
{
    /// <summary>Preliminary draft. Not reflected in ledger.</summary>
    Draft = 1,

    /// <summary>Approved by accounting supervisor; ready for posting.</summary>
    Approved = 2,

    /// <summary>Committed to general ledger. Strictly immutable.</summary>
    Posted = 3,

    /// <summary>Neutralized by a companion Reversal voucher.</summary>
    Reversed = 4,

    /// <summary>Draft cancelled prior to posting.</summary>
    Cancelled = 5,
}

