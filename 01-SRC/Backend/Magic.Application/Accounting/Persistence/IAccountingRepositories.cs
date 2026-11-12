using Magic.Domain.Accounting.Entities;
using Magic.Domain.Accounting.Enums;

namespace Magic.Application.Accounting.Persistence;

/// <summary>
/// Allocates concurrency-safe, gapless sequential voucher numbers per branch, fiscal year, and voucher type.
/// </summary>
public interface IVoucherNumberGenerator
{
    Task<string> NextVoucherNumberAsync(
        Guid branchId,
        Guid fiscalYearId,
        string branchCode,
        string fyCode,
        VoucherType voucherType,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Persistence operations for Chart of Accounts (groups and detail accounts).
/// </summary>
public interface IChartOfAccountsRepository
{
    Task<AccountGroup?> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccountGroup?> GetGroupByCodeAsync(Guid companyId, string groupCode, CancellationToken cancellationToken = default);
    Task<Account?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Account?> GetAccountByCodeAsync(Guid companyId, string accountCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, Account>> GetAccountsByIdsAsync(IEnumerable<Guid> accountIds, CancellationToken cancellationToken = default);
    Task AddGroupAsync(AccountGroup group, CancellationToken cancellationToken = default);
    Task AddAccountAsync(Account account, CancellationToken cancellationToken = default);
}

/// <summary>
/// Persistence operations for Fiscal Governance (fiscal years and periods).
/// </summary>
public interface IFiscalGovernanceRepository
{
    Task<FiscalYear?> GetFiscalYearByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FiscalYear?> GetFiscalYearByDateAsync(Guid companyId, DateOnly effectiveDate, CancellationToken cancellationToken = default);
    Task<FiscalPeriod?> GetPeriodByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FiscalPeriod?> GetPeriodByDateAsync(Guid companyId, DateOnly effectiveDate, CancellationToken cancellationToken = default);
    Task AddFiscalYearAsync(FiscalYear fiscalYear, CancellationToken cancellationToken = default);
}

/// <summary>
/// Persistence operations for General Ledger vouchers and companion reversals.
/// </summary>
public interface IGeneralLedgerRepository
{
    Task<JournalVoucher?> GetVoucherByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<JournalVoucher?> GetVoucherByNumberAsync(Guid branchId, string voucherNumber, CancellationToken cancellationToken = default);
    Task<JournalVoucher?> GetVoucherByIdempotencyKeyAsync(Guid branchId, Guid idempotencyKey, CancellationToken cancellationToken = default);
    Task AddVoucherAsync(JournalVoucher voucher, CancellationToken cancellationToken = default);
}
