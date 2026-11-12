namespace Magic.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence entity storing monthly summarized balances and periodic movements per account and branch.
/// </summary>
public sealed class AccountPeriodBalanceRecord
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid FiscalPeriodId { get; set; }
    public Guid BranchId { get; set; }
    public long OpeningDebitPaise { get; set; }
    public long OpeningCreditPaise { get; set; }
    public long MovementDebitPaise { get; set; }
    public long MovementCreditPaise { get; set; }
    public long ClosingDebitPaise { get; set; }
    public long ClosingCreditPaise { get; set; }
    public DateTimeOffset LastUpdatedAtUtc { get; set; }
}
