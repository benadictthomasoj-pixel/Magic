using Magic.Domain.Accounting.Enums;
using Magic.Domain.Common;

namespace Magic.Domain.Accounting.Entities;

/// <summary>
/// Domain entity representing a leaf General Ledger detail account.
/// All transaction journal lines attach strictly to detail accounts.
/// Enforces normal balance rules, control account posting restrictions, and lifecycle states.
/// </summary>
public class Account : DomainEntity, IAuditableEntity
{
    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid AccountGroupId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public AccountRootClass RootClass { get; private set; }
    public AccountNormalBalance NormalBalance { get; private set; }
    public bool IsControlAccount { get; private set; }
    public string? ControlAccountType { get; private set; }
    public bool AllowDirectPosting { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsSystemLocked { get; private set; }
    public AccountGroup? AccountGroup { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public int ConcurrencyVersion { get; private set; }

    private Account() { } // EF Core

    public Account(
        Guid companyId,
        Guid branchId,
        Guid accountGroupId,
        string code,
        string name,
        AccountRootClass rootClass,
        Guid createdByUserId,
        bool isControlAccount = false,
        string? controlAccountType = null,
        bool allowDirectPosting = true,
        bool isSystemLocked = false,
        AccountNormalBalance? customNormalBalance = null)
    {
        if (companyId == Guid.Empty) throw new DomainException("DOMAIN_EMPTY_COMPANY", "Company identity is required.");
        if (branchId == Guid.Empty) throw new DomainException("DOMAIN_EMPTY_BRANCH", "Branch identity is required.");
        if (accountGroupId == Guid.Empty) throw new DomainException("DOMAIN_EMPTY_ACCOUNT_GROUP", "Account group identity is required.");
        DomainGuard.NotNullOrWhiteSpace(code, nameof(code));
        DomainGuard.NotNullOrWhiteSpace(name, nameof(name));

        CompanyId = companyId;
        BranchId = branchId;
        AccountGroupId = accountGroupId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        RootClass = rootClass;
        NormalBalance = customNormalBalance ?? GetDefaultNormalBalance(rootClass);
        IsControlAccount = isControlAccount;
        ControlAccountType = isControlAccount ? controlAccountType?.Trim().ToUpperInvariant() : null;
        AllowDirectPosting = isControlAccount ? allowDirectPosting : true;
        IsActive = true;
        IsSystemLocked = isSystemLocked;

        var now = DateTimeOffset.UtcNow;
        CreatedAtUtc = now;
        CreatedByUserId = createdByUserId;
        UpdatedAtUtc = now;
        ConcurrencyVersion = 1;
    }

    public static AccountNormalBalance GetDefaultNormalBalance(AccountRootClass rootClass) =>
        rootClass switch
        {
            AccountRootClass.Asset or AccountRootClass.Expense => AccountNormalBalance.Debit,
            AccountRootClass.Liability or AccountRootClass.Equity or AccountRootClass.Revenue => AccountNormalBalance.Credit,
            _ => throw new DomainException("DOMAIN_UNKNOWN_ROOT_CLASS", $"Unknown root class {rootClass}."),
        };

    public void AssertCanAcceptDirectLine()
    {
        if (!IsActive)
        {
            throw new DomainException("DOMAIN_ACCOUNT_INACTIVE", $"Account '{Code} - {Name}' is inactive.");
        }

        if (IsControlAccount && !AllowDirectPosting)
        {
            throw new DomainException(
                "DOMAIN_CONTROL_ACCOUNT_POSTING_BLOCKED",
                $"Direct journal entry to control account '{Code} - {Name}' is strictly prohibited. Postings must originate via subledgers.");
        }
    }

    public void UpdateName(string name, Guid userId)
    {
        DomainGuard.NotNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedByUserId = userId;
    }

    public void Deactivate(Guid userId)
    {
        if (IsSystemLocked)
        {
            throw new DomainException("DOMAIN_SYSTEM_ACCOUNT_LOCKED", $"System account '{Code} - {Name}' cannot be deactivated.");
        }

        IsActive = false;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedByUserId = userId;
    }

    public void Activate(Guid userId)
    {
        IsActive = true;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedByUserId = userId;
    }
}

