namespace Magic.Contracts.Marketing;

public sealed record CustomerSegmentDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string SegmentType,
    string? Description,
    long MinLifetimeSpendPaise,
    int MinOrdersCount,
    bool IsActive);

public sealed record CreateCustomerSegmentRequest(
    string Name,
    string SegmentType,
    string? Description,
    long MinLifetimeSpendPaise,
    int MinOrdersCount);

public sealed record CampaignDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string? Description,
    Guid? TargetSegmentId,
    string? TargetSegmentName,
    DateOnly StartDate,
    DateOnly EndDate,
    long BudgetPaise,
    long ActualSpendPaise,
    long AttributedRevenuePaise,
    string Status);

public sealed record CreateCampaignRequest(
    string Name,
    string? Description,
    Guid? TargetSegmentId,
    DateOnly StartDate,
    DateOnly EndDate,
    long BudgetPaise);

public sealed record PromotionCouponDto(
    Guid Id,
    Guid CompanyId,
    string Code,
    Guid? CampaignId,
    string? CampaignName,
    string DiscountType,
    long DiscountValue,
    long MinOrderAmountPaise,
    long MaxDiscountAmountPaise,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidTo,
    int UsageLimitTotal,
    int UsageLimitPerCustomer,
    int CurrentUsageCount,
    bool IsActive);

public sealed record CreatePromotionCouponRequest(
    string Code,
    Guid? CampaignId,
    string DiscountType,
    long DiscountValue,
    long MinOrderAmountPaise,
    long MaxDiscountAmountPaise,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidTo,
    int UsageLimitTotal,
    int UsageLimitPerCustomer);

public sealed record ValidateCouponRequest(
    string Code,
    Guid CustomerId,
    long OrderSubtotalPaise);

public sealed record ValidateCouponResponse(
    bool IsValid,
    string? RejectionReason,
    Guid? CouponId,
    long CalculatedDiscountPaise);

public sealed record LoyaltyProgramDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    int PointsPerHundredSpent,
    long CurrencyValuePerPointPaise,
    int MinPointsToRedeem,
    bool IsActive);

public sealed record CreateLoyaltyProgramRequest(
    string Name,
    int PointsPerHundredSpent,
    long CurrencyValuePerPointPaise,
    int MinPointsToRedeem);

public sealed record LoyaltyBalanceDto(
    Guid CustomerId,
    string CustomerName,
    int TotalPointsBalance,
    long MonetaryValuePaise);

public sealed record LoyaltyTransactionDto(
    Guid Id,
    Guid CustomerId,
    string TransactionType,
    int Points,
    int BalanceAfterPoints,
    Guid? SalesInvoiceId,
    string? Notes,
    DateTimeOffset CreatedAtUtc);

public sealed record RedeemLoyaltyPointsRequest(
    Guid CustomerId,
    int PointsToRedeem,
    Guid SalesInvoiceId);

public sealed record MarketingSummaryDto(
    int ActiveCampaignsCount,
    int ActiveCouponsCount,
    int TotalLoyaltyMembersCount,
    long AttributedRevenuePaise);

