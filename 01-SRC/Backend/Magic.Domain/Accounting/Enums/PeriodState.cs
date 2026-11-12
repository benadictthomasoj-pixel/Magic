namespace Magic.Domain.Accounting.Enums;

/// <summary>
/// Operational status of a monthly accounting fiscal period.
/// </summary>
public enum PeriodState
{
    /// <summary>Accepts validated transactions within the period date boundaries.</summary>
    Open = 1,

    /// <summary>Soft-lock during period-end reconciliation. Standard clerk posting blocked; supervisor elevation allowed.</summary>
    Closing = 2,

    /// <summary>Hard-lock. Completely frozen. Postings rejected unconditionally unless supervisor elevation token validated.</summary>
    Locked = 3,
}

