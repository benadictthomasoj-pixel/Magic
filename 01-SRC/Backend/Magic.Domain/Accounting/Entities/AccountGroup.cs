using Magic.Domain.Accounting.Enums;
using Magic.Domain.Common;

namespace Magic.Domain.Accounting.Entities;

/// <summary>
/// Domain entity representing a hierarchical account group container within the Chart of Accounts.
/// Groups aggregate the balances of child groups and accounts; groups do not accept direct journal lines.
/// </summary>
public class AccountGroup : DomainEntity, IAuditableEntity
{
    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid? ParentGroupId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public AccountRootClass RootClass { get; private set; }
    public int HierarchyDepth { get; private set; }
    public bool IsSystem { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public AccountGroup? ParentGroup { get; private set; }
    public IReadOnlyCollection<AccountGroup> Children => _children.AsReadOnly();
    private readonly List<AccountGroup> _children = [];

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public int ConcurrencyVersion { get; private set; }

    private AccountGroup() { } // EF Core

    public AccountGroup(
        Guid companyId,
        Guid branchId,
        string code,
        string name,
        AccountRootClass rootClass,
        Guid createdByUserId,
        Guid? parentGroupId = null,
        int sortOrder = 0,
        int hierarchyDepth = 1,
        bool isSystem = false)
    {
        if (companyId == Guid.Empty) throw new DomainException("DOMAIN_EMPTY_COMPANY", "Company identity is required.");
        if (branchId == Guid.Empty) throw new DomainException("DOMAIN_EMPTY_BRANCH", "Branch identity is required.");
        DomainGuard.NotNullOrWhiteSpace(code, nameof(code));
        DomainGuard.NotNullOrWhiteSpace(name, nameof(name));

        CompanyId = companyId;
        BranchId = branchId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        RootClass = rootClass;
        ParentGroupId = parentGroupId;
        SortOrder = sortOrder;
        HierarchyDepth = Math.Max(1, hierarchyDepth);
        IsSystem = isSystem;
        IsActive = true;

        var now = DateTimeOffset.UtcNow;
        CreatedAtUtc = now;
        CreatedByUserId = createdByUserId;
        UpdatedAtUtc = now;
        ConcurrencyVersion = 1;
    }

    public void SetParentGroup(AccountGroup parentGroup, Guid updatedByUserId)
    {
        ArgumentNullException.ThrowIfNull(parentGroup);

        if (parentGroup.Id == Id)
        {
            throw new DomainException("DOMAIN_CIRCULAR_GROUP", "An account group cannot be its own parent.");
        }

        if (parentGroup.CompanyId != CompanyId)
        {
            throw new DomainException("DOMAIN_CROSS_COMPANY_GROUP", "Parent group must belong to the same company.");
        }

        if (parentGroup.RootClass != RootClass)
        {
            throw new DomainException(
                "DOMAIN_MISMATCHED_ROOT_CLASS",
                $"Parent group root class '{parentGroup.RootClass}' does not match child group root class '{RootClass}'.");
        }

        ParentGroupId = parentGroup.Id;
        ParentGroup = parentGroup;
        HierarchyDepth = parentGroup.HierarchyDepth + 1;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }

    public void Deactivate(Guid userId)
    {
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

