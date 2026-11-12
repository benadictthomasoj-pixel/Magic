using Magic.Domain.Accounting.Entities;
using Magic.Domain.Accounting.Enums;
using Magic.Domain.Common;
using Xunit;

namespace Magic.Domain.UnitTests;

public class ChartOfAccountsTests
{
    private readonly Guid _companyId = Guid.CreateVersion7();
    private readonly Guid _branchId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();

    [Fact]
    public void AccountGroup_ParentMismatchRootClass_ThrowsDomainException()
    {
        var assetGroup = new AccountGroup(
            _companyId,
            _branchId,
            "GRP-ASSETS",
            "Current Assets",
            AccountRootClass.Asset,
            _userId);

        var liabilityGroup = new AccountGroup(
            _companyId,
            _branchId,
            "GRP-LIAB",
            "Current Liabilities",
            AccountRootClass.Liability,
            _userId);

        var ex = Assert.Throws<DomainException>(() => assetGroup.SetParentGroup(liabilityGroup, _userId));
        Assert.Equal("DOMAIN_MISMATCHED_ROOT_CLASS", ex.ErrorCode);
    }

    [Fact]
    public void Account_DefaultNormalBalance_MatchesRootClass()
    {
        var group = new AccountGroup(_companyId, _branchId, "GRP-BANK", "Bank Accounts", AccountRootClass.Asset, _userId);

        var bankAccount = new Account(_companyId, _branchId, group.Id, "1121", "HDFC Bank", AccountRootClass.Asset, _userId);
        Assert.Equal(AccountNormalBalance.Debit, bankAccount.NormalBalance);

        var revenueAccount = new Account(_companyId, _branchId, group.Id, "4110", "Sales Revenue", AccountRootClass.Revenue, _userId);
        Assert.Equal(AccountNormalBalance.Credit, revenueAccount.NormalBalance);
    }

    [Fact]
    public void ControlAccount_DirectPostingWithoutExplicitFlag_IsBlocked()
    {
        var group = new AccountGroup(_companyId, _branchId, "GRP-AR", "Receivables", AccountRootClass.Asset, _userId);

        var tradeDebtors = new Account(
            _companyId,
            _branchId,
            group.Id,
            "1130",
            "Trade Debtors",
            AccountRootClass.Asset,
            _userId,
            isControlAccount: true,
            allowDirectPosting: false);

        var ex = Assert.Throws<DomainException>(() => tradeDebtors.AssertCanAcceptDirectLine());
        Assert.Equal("DOMAIN_CONTROL_ACCOUNT_POSTING_BLOCKED", ex.ErrorCode);
    }
}

