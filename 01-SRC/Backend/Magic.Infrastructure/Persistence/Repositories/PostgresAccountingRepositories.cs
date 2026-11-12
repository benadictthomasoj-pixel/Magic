using Magic.Application.Accounting.Persistence;
using Magic.Domain.Accounting.Entities;
using Magic.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Persistence.Repositories;

/// <summary>
/// PostgreSQL EF Core repository for Chart of Accounts taxonomy.
/// </summary>
public sealed class PostgresChartOfAccountsRepository(MagicDbContext dbContext) : IChartOfAccountsRepository
{
    public Task<AccountGroup?> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.AccountGroups
            .Include(g => g.ParentGroup)
            .Include(g => g.Children)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public Task<AccountGroup?> GetGroupByCodeAsync(Guid companyId, string groupCode, CancellationToken cancellationToken = default)
    {
        var normalized = groupCode.Trim().ToUpperInvariant();
        return dbContext.AccountGroups
            .Include(g => g.ParentGroup)
            .FirstOrDefaultAsync(g => g.CompanyId == companyId && g.Code == normalized, cancellationToken);
    }

    public Task<Account?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Accounts
            .Include(a => a.AccountGroup)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<Account?> GetAccountByCodeAsync(Guid companyId, string accountCode, CancellationToken cancellationToken = default)
    {
        var normalized = accountCode.Trim().ToUpperInvariant();
        return dbContext.Accounts
            .Include(a => a.AccountGroup)
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == normalized, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, Account>> GetAccountsByIdsAsync(
        IEnumerable<Guid> accountIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountIds);
        var ids = accountIds.Distinct().ToArray();
        var accounts = await dbContext.Accounts
            .AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        return accounts;
    }

    public async Task AddGroupAsync(AccountGroup group, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(group);
        await dbContext.AccountGroups.AddAsync(group, cancellationToken);
    }

    public async Task AddAccountAsync(Account account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        await dbContext.Accounts.AddAsync(account, cancellationToken);
    }
}

/// <summary>
/// PostgreSQL EF Core repository for Fiscal Years and Periods.
/// </summary>
public sealed class PostgresFiscalGovernanceRepository(MagicDbContext dbContext) : IFiscalGovernanceRepository
{
    public Task<FiscalYear?> GetFiscalYearByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.FiscalYears
            .Include(fy => fy.Periods)
            .FirstOrDefaultAsync(fy => fy.Id == id, cancellationToken);

    public Task<FiscalYear?> GetFiscalYearByDateAsync(Guid companyId, DateOnly effectiveDate, CancellationToken cancellationToken = default) =>
        dbContext.FiscalYears
            .Include(fy => fy.Periods)
            .FirstOrDefaultAsync(fy => fy.CompanyId == companyId && fy.StartDate <= effectiveDate && fy.EndDate >= effectiveDate, cancellationToken);

    public Task<FiscalPeriod?> GetPeriodByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.FiscalPeriods
            .Include(fp => fp.FiscalYear)
            .FirstOrDefaultAsync(fp => fp.Id == id, cancellationToken);

    public Task<FiscalPeriod?> GetPeriodByDateAsync(Guid companyId, DateOnly effectiveDate, CancellationToken cancellationToken = default) =>
        dbContext.FiscalPeriods
            .Include(fp => fp.FiscalYear)
            .FirstOrDefaultAsync(fp => fp.CompanyId == companyId && fp.StartDate <= effectiveDate && fp.EndDate >= effectiveDate, cancellationToken);

    public async Task AddFiscalYearAsync(FiscalYear fiscalYear, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fiscalYear);
        await dbContext.FiscalYears.AddAsync(fiscalYear, cancellationToken);
    }
}

/// <summary>
/// PostgreSQL EF Core repository for General Ledger journal vouchers.
/// </summary>
public sealed class PostgresGeneralLedgerRepository(MagicDbContext dbContext) : IGeneralLedgerRepository
{
    public Task<JournalVoucher?> GetVoucherByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Vouchers
            .Include(v => v.Lines)
            .Include(v => v.FiscalPeriod)
            .Include(v => v.ReversalVoucher)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<JournalVoucher?> GetVoucherByNumberAsync(Guid branchId, string voucherNumber, CancellationToken cancellationToken = default)
    {
        var normalized = voucherNumber.Trim().ToUpperInvariant();
        return dbContext.Vouchers
            .Include(v => v.Lines)
            .Include(v => v.FiscalPeriod)
            .FirstOrDefaultAsync(v => v.BranchId == branchId && v.VoucherNumber == normalized, cancellationToken);
    }

    public Task<JournalVoucher?> GetVoucherByIdempotencyKeyAsync(Guid branchId, Guid idempotencyKey, CancellationToken cancellationToken = default) =>
        dbContext.Vouchers
            .Include(v => v.Lines)
            .FirstOrDefaultAsync(v => v.BranchId == branchId && v.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task AddVoucherAsync(JournalVoucher voucher, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(voucher);
        await dbContext.Vouchers.AddAsync(voucher, cancellationToken);
    }
}
