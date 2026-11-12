namespace Magic.Domain.Accounting.Enums;

/// <summary>
/// Root accounting classification under double-entry accounting.
/// Every account belongs to exactly one root class.
/// </summary>
public enum AccountRootClass
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5,
}

