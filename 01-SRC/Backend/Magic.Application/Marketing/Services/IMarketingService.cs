using Magic.Contracts.Marketing;

namespace Magic.Application.Marketing.Services;

public interface IMarketingService
{
    Task<MarketingSummaryDto> GetSummaryAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerSegmentDto>> GetSegmentsAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<CustomerSegmentDto> CreateSegmentAsync(Guid companyId, CreateCustomerSegmentRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CampaignDto>> GetCampaignsAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<CampaignDto> CreateCampaignAsync(Guid companyId, CreateCampaignRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PromotionCouponDto>> GetCouponsAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<PromotionCouponDto> CreateCouponAsync(Guid companyId, CreatePromotionCouponRequest request, CancellationToken cancellationToken = default);
    Task<ValidateCouponResponse> ValidateCouponAsync(Guid companyId, ValidateCouponRequest request, CancellationToken cancellationToken = default);
    Task RecordCouponRedemptionAsync(Guid companyId, Guid couponId, Guid customerId, Guid salesInvoiceId, long discountAmountPaise, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LoyaltyProgramDto>> GetLoyaltyProgramsAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<LoyaltyProgramDto> CreateLoyaltyProgramAsync(Guid companyId, CreateLoyaltyProgramRequest request, CancellationToken cancellationToken = default);
    Task<LoyaltyBalanceDto> GetCustomerLoyaltyBalanceAsync(Guid companyId, Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoyaltyTransactionDto>> GetCustomerLoyaltyHistoryAsync(Guid companyId, Guid customerId, CancellationToken cancellationToken = default);
    Task<LoyaltyBalanceDto> AwardLoyaltyPointsAsync(Guid companyId, Guid customerId, Guid salesInvoiceId, long orderSubtotalPaise, CancellationToken cancellationToken = default);
    Task<LoyaltyBalanceDto> RedeemLoyaltyPointsAsync(Guid companyId, RedeemLoyaltyPointsRequest request, CancellationToken cancellationToken = default);
}

