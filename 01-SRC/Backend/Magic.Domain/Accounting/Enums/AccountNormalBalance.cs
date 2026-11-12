namespace Magic.Domain.Accounting.Enums;

/// <summary>
/// Normal balance side for an account.
/// Assets & Expenses have normal Debit balances.
/// Liabilities, Equity & Revenue have normal Credit balances.
/// </summary>
public enum AccountNormalBalance
{
    Debit = 1,
    Credit = 2,
}

