using Magic.Domain.Accounting.Entities;
using Magic.Domain.Accounting.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Magic.Infrastructure.Persistence;

/// <summary>
/// Seeds essential initial accounting governance, Chart of Accounts taxonomy, and Indian fiscal periods
/// in local development environments.
/// </summary>
public static partial class AccountingDatabaseSeeder
{
    public static readonly Guid DefaultCompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DefaultBranchId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "{Message}")]
    private static partial void LogSeedInfo(ILogger logger, string message);

    public static async Task SeedDevelopmentAccountingAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AccountingSeeder");

        var companyId = DefaultCompanyId;
        var branchId = DefaultBranchId;
        var systemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        try
        {
            // 1. Seed Fiscal Year & Periods if empty
            if (!await dbContext.FiscalYears.AnyAsync(fy => fy.CompanyId == companyId))
            {
                LogSeedInfo(logger, "Seeding default Fiscal Year 2026-27 and 12 monthly accounting periods...");
                var startDate = new DateOnly(2026, 4, 1);
                var endDate = new DateOnly(2027, 3, 31);
                var fy = new FiscalYear(companyId, branchId, "FY2026-27", startDate, endDate, systemUserId, "Financial Year 2026-2027");

                for (var m = 1; m <= 12; m++)
                {
                    var calMonth = (m + 2) % 12 + 1; // 1->4 (Apr), 2->5 (May)... 10->1 (Jan), 12->3 (Mar)
                    var year = calMonth >= 4 ? 2026 : 2027;
                    var pStart = new DateOnly(year, calMonth, 1);
                    var pEnd = new DateOnly(year, calMonth, DateTime.DaysInMonth(year, calMonth));
                    var code = $"{year}-{calMonth:D2}";

                    var period = new FiscalPeriod(companyId, branchId, fy.Id, m, code, pStart, pEnd, systemUserId);
                    fy.AddPeriod(period);
                }

                dbContext.FiscalYears.Add(fy);
                await dbContext.SaveChangesAsync();
            }

            // 2. Seed Standard Chart of Accounts Taxonomy
            if (!await dbContext.AccountGroups.AnyAsync(g => g.CompanyId == companyId))
            {
                LogSeedInfo(logger, "Seeding standard Chart of Accounts groups and detail accounts...");

                // Root Groups
                var grpAssets = new AccountGroup(companyId, branchId, "1000", "Assets", AccountRootClass.Asset, systemUserId, isSystem: true);
                var grpLiabilities = new AccountGroup(companyId, branchId, "2000", "Liabilities", AccountRootClass.Liability, systemUserId, isSystem: true);
                var grpEquity = new AccountGroup(companyId, branchId, "3000", "Equity", AccountRootClass.Equity, systemUserId, isSystem: true);
                var grpRevenue = new AccountGroup(companyId, branchId, "4000", "Revenue", AccountRootClass.Revenue, systemUserId, isSystem: true);
                var grpExpense = new AccountGroup(companyId, branchId, "5000", "Expenses", AccountRootClass.Expense, systemUserId, isSystem: true);

                dbContext.AccountGroups.AddRange(grpAssets, grpLiabilities, grpEquity, grpRevenue, grpExpense);
                await dbContext.SaveChangesAsync();

                // Sub-Groups
                var grpCurrAssets = new AccountGroup(companyId, branchId, "1100", "Current Assets", AccountRootClass.Asset, systemUserId, parentGroupId: grpAssets.Id, isSystem: true);
                var grpBankCash = new AccountGroup(companyId, branchId, "1110", "Cash & Bank Balances", AccountRootClass.Asset, systemUserId, parentGroupId: grpAssets.Id, isSystem: true);
                var grpAR = new AccountGroup(companyId, branchId, "1200", "Accounts Receivable", AccountRootClass.Asset, systemUserId, parentGroupId: grpAssets.Id, isSystem: true);

                var grpCurrLiab = new AccountGroup(companyId, branchId, "2100", "Current Liabilities", AccountRootClass.Liability, systemUserId, parentGroupId: grpLiabilities.Id, isSystem: true);
                var grpAP = new AccountGroup(companyId, branchId, "2110", "Accounts Payable", AccountRootClass.Liability, systemUserId, parentGroupId: grpLiabilities.Id, isSystem: true);
                var grpGst = new AccountGroup(companyId, branchId, "2120", "Duties & Taxes (GST)", AccountRootClass.Liability, systemUserId, parentGroupId: grpLiabilities.Id, isSystem: true);

                var grpCap = new AccountGroup(companyId, branchId, "3100", "Capital & Reserves", AccountRootClass.Equity, systemUserId, parentGroupId: grpEquity.Id, isSystem: true);
                var grpSales = new AccountGroup(companyId, branchId, "4100", "Operating Revenue", AccountRootClass.Revenue, systemUserId, parentGroupId: grpRevenue.Id, isSystem: true);
                var grpDirectExp = new AccountGroup(companyId, branchId, "5100", "Direct Cost of Goods", AccountRootClass.Expense, systemUserId, parentGroupId: grpExpense.Id, isSystem: true);
                var grpAdminExp = new AccountGroup(companyId, branchId, "5200", "Administrative Expenses", AccountRootClass.Expense, systemUserId, parentGroupId: grpExpense.Id, isSystem: true);

                dbContext.AccountGroups.AddRange(
                    grpCurrAssets, grpBankCash, grpAR,
                    grpCurrLiab, grpAP, grpGst,
                    grpCap, grpSales, grpDirectExp, grpAdminExp);
                await dbContext.SaveChangesAsync();

                // Detail Accounts
                var accCash = new Account(companyId, branchId, grpBankCash.Id, "1111", "Cash on Hand", AccountRootClass.Asset, systemUserId, isControlAccount: true, controlAccountType: "CashControl");
                var accBank = new Account(companyId, branchId, grpBankCash.Id, "1112", "Operating Bank Account (Current)", AccountRootClass.Asset, systemUserId, isControlAccount: true, controlAccountType: "BankControl");
                var accDebtors = new Account(companyId, branchId, grpAR.Id, "1201", "Trade Debtors Control Account", AccountRootClass.Asset, systemUserId, isControlAccount: true, controlAccountType: "CustomerControl", allowDirectPosting: false);

                var accCreditors = new Account(companyId, branchId, grpAP.Id, "2111", "Trade Creditors Control Account", AccountRootClass.Liability, systemUserId, isControlAccount: true, controlAccountType: "VendorControl", allowDirectPosting: false);
                var accOutCgst = new Account(companyId, branchId, grpGst.Id, "2121", "Output CGST Payable", AccountRootClass.Liability, systemUserId, isControlAccount: true, controlAccountType: "GstControl");
                var accOutSgst = new Account(companyId, branchId, grpGst.Id, "2122", "Output SGST Payable", AccountRootClass.Liability, systemUserId, isControlAccount: true, controlAccountType: "GstControl");
                var accInCgst = new Account(companyId, branchId, grpGst.Id, "1131", "Input CGST Credit", AccountRootClass.Asset, systemUserId, isControlAccount: true, controlAccountType: "GstControl");
                var accInSgst = new Account(companyId, branchId, grpGst.Id, "1132", "Input SGST Credit", AccountRootClass.Asset, systemUserId, isControlAccount: true, controlAccountType: "GstControl");

                var accCap = new Account(companyId, branchId, grpCap.Id, "3101", "Owner's Equity & Capital", AccountRootClass.Equity, systemUserId);
                var accSales = new Account(companyId, branchId, grpSales.Id, "4101", "General Commercial Sales", AccountRootClass.Revenue, systemUserId);
                var accPurchases = new Account(companyId, branchId, grpDirectExp.Id, "5101", "Commercial Goods Purchases", AccountRootClass.Expense, systemUserId);
                var accRent = new Account(companyId, branchId, grpAdminExp.Id, "5201", "Office & Facility Rent", AccountRootClass.Expense, systemUserId);

                dbContext.Accounts.AddRange(
                    accCash, accBank, accDebtors,
                    accCreditors, accOutCgst, accOutSgst, accInCgst, accInSgst,
                    accCap, accSales, accPurchases, accRent);

                await dbContext.SaveChangesAsync();
                LogSeedInfo(logger, "Standard Chart of Accounts taxonomy seeded successfully.");
            }
        }
        catch (DbUpdateException)
        {
            // Handled gracefully in concurrent multi-threaded development/testing runs
        }
    }
}
