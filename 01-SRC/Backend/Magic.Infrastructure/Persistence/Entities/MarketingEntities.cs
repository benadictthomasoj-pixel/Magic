namespace Magic.Infrastructure.Persistence.Entities;

/// <summary>
/// Customer audience segment based on spending patterns or purchasing volume.
/// </summary>
public sealed class CustomerSegmentRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SegmentType { get; set; } = "HighSpender"; // VIP, Frequent, Inactive, HighSpender, NewCustomers
    public string? Description { get; set; }
    public long MinLifetimeSpendPaise { get; set; }
    public int MinOrdersCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>
/// Marketing campaign with budget tracking and attributed revenue.
/// </summary>
public sealed class CampaignRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? TargetSegmentId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public long BudgetPaise { get; set; }
    public long ActualSpendPaise { get; set; }
    public long AttributedRevenuePaise { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Active, Paused, Completed, Cancelled
    public DateTimeOffset CreatedAtUtc { get; set; }

    public CustomerSegmentRecord? TargetSegment { get; set; }
}

/// <summary>
/// Promotional discount coupon with redemption rules and usage caps.
/// </summary>
public sealed class PromotionCouponRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid? CampaignId { get; set; }
    public string DiscountType { get; set; } = "Percentage"; // Percentage, FixedAmount
    public long DiscountValue { get; set; } // bps (e.g. 1000 = 10%) or paise
    public long MinOrderAmountPaise { get; set; }
    public long MaxDiscountAmountPaise { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset ValidTo { get; set; }
    public int UsageLimitTotal { get; set; } = 1000;
    public int UsageLimitPerCustomer { get; set; } = 1;
    public int CurrentUsageCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public CampaignRecord? Campaign { get; set; }
}

/// <summary>
/// Coupon redemption record tying a discount to an invoice.
/// </summary>
public sealed class CouponRedemptionRecord
{
    public Guid Id { get; set; }
    public Guid CouponId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid SalesInvoiceId { get; set; }
    public long DiscountAmountPaise { get; set; }
    public DateTimeOffset RedeemedAtUtc { get; set; }

    public PromotionCouponRecord? Coupon { get; set; }
    public CustomerRecord? Customer { get; set; }
    public SalesInvoiceRecord? SalesInvoice { get; set; }
}

/// <summary>
/// Loyalty points program defining reward earning and redemption ratios.
/// </summary>
public sealed class LoyaltyProgramRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PointsPerHundredSpent { get; set; } = 1;
    public long CurrencyValuePerPointPaise { get; set; } = 100; // 1 point = 100 paise (1 INR)
    public int MinPointsToRedeem { get; set; } = 100;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>
/// Immutable ledger of customer reward points transactions.
/// </summary>
public sealed class LoyaltyTransactionRecord
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid CompanyId { get; set; }
    public string TransactionType { get; set; } = "Earned"; // Earned, Redeemed, Adjusted
    public int Points { get; set; }
    public int BalanceAfterPoints { get; set; }
    public Guid? SalesInvoiceId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public CustomerRecord? Customer { get; set; }
    public SalesInvoiceRecord? SalesInvoice { get; set; }
}

