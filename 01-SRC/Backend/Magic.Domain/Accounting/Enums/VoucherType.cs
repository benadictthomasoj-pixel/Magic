namespace Magic.Domain.Accounting.Enums;

/// <summary>
/// Double-entry journal voucher transaction categories.
/// </summary>
public enum VoucherType
{
    /// <summary>General journal adjustment, accruals, period-end entries.</summary>
    Journal = 1,

    /// <summary>Automated sales transaction voucher.</summary>
    Sales = 2,

    /// <summary>Automated purchase transaction voucher.</summary>
    Purchase = 3,

    /// <summary>Disbursement of cash or bank funds.</summary>
    Payment = 4,

    /// <summary>Collection of cash or bank funds.</summary>
    Receipt = 5,

    /// <summary>Internal transfer between cash and bank accounts.</summary>
    Contra = 6,

    /// <summary>Non-destructive corrective reversal voucher mirroring an earlier voucher.</summary>
    Reversal = 7,
}

