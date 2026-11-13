using Asp.Versioning;
using Magic.Application.Marketing.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Marketing;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Marketing;

[Authorize]
[Route("api/v{version:apiVersion}/marketing")]
public sealed class MarketingController(
    IMarketingService marketingService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : OperationsBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<MarketingSummaryDto>>> GetSummary(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("marketing:campaigns:manage", "marketing:coupons:manage");
        var companyId = ResolveCompanyId();
        var summary = await marketingService.GetSummaryAsync(companyId, cancellationToken);
        return Envelope(summary);
    }

    [HttpGet("segments")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerSegmentDto>>>> GetSegments(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("marketing:segments:manage");
        var companyId = ResolveCompanyId();
        var items = await marketingService.GetSegmentsAsync(companyId, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("segments")]
    public async Task<ActionResult<ApiResponse<CustomerSegmentDto>>> CreateSegment(
        [FromBody] CreateCustomerSegmentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("marketing:segments:manage");
        var companyId = ResolveCompanyId();
        var item = await marketingService.CreateSegmentAsync(companyId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpGet("campaigns")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CampaignDto>>>> GetCampaigns(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("marketing:campaigns:manage");
        var companyId = ResolveCompanyId();
        var items = await marketingService.GetCampaignsAsync(companyId, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("campaigns")]
    public async Task<ActionResult<ApiResponse<CampaignDto>>> CreateCampaign(
        [FromBody] CreateCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("marketing:campaigns:manage");
        var companyId = ResolveCompanyId();
        var item = await marketingService.CreateCampaignAsync(companyId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpGet("coupons")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PromotionCouponDto>>>> GetCoupons(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("marketing:coupons:manage");
        var companyId = ResolveCompanyId();
        var items = await marketingService.GetCouponsAsync(companyId, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("coupons")]
    public async Task<ActionResult<ApiResponse<PromotionCouponDto>>> CreateCoupon(
        [FromBody] CreatePromotionCouponRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("marketing:coupons:manage");
        var companyId = ResolveCompanyId();
        var item = await marketingService.CreateCouponAsync(companyId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPost("coupons/validate")]
    public async Task<ActionResult<ApiResponse<ValidateCouponResponse>>> ValidateCoupon(
        [FromBody] ValidateCouponRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("marketing:coupons:manage", "sales:invoice:create", "billing:sales:create");
        var companyId = ResolveCompanyId();
        var result = await marketingService.ValidateCouponAsync(companyId, request, cancellationToken);
        return Envelope(result);
    }

    [HttpPost("coupons/redeem")]
    public async Task<ActionResult<ApiResponse<bool>>> RedeemCoupon(
        [FromBody] RedeemCouponApiRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("marketing:coupons:manage", "sales:invoice:create", "billing:sales:create");
        var companyId = ResolveCompanyId();
        await marketingService.RecordCouponRedemptionAsync(companyId, request.CouponId, request.CustomerId, request.SalesInvoiceId, request.DiscountAmountPaise, cancellationToken);
        return Envelope(true);
    }

    [HttpGet("loyalty/programs")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LoyaltyProgramDto>>>> GetLoyaltyPrograms(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("marketing:loyalty:manage");
        var companyId = ResolveCompanyId();
        var items = await marketingService.GetLoyaltyProgramsAsync(companyId, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("loyalty/programs")]
    public async Task<ActionResult<ApiResponse<LoyaltyProgramDto>>> CreateLoyaltyProgram(
        [FromBody] CreateLoyaltyProgramRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("marketing:loyalty:manage");
        var companyId = ResolveCompanyId();
        var item = await marketingService.CreateLoyaltyProgramAsync(companyId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpGet("loyalty/{customerId:guid}")]
    public async Task<ActionResult<ApiResponse<LoyaltyBalanceDto>>> GetCustomerLoyalty(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("marketing:loyalty:manage", "sales:invoice:create", "billing:sales:create");
        var companyId = ResolveCompanyId();
        var item = await marketingService.GetCustomerLoyaltyBalanceAsync(companyId, customerId, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("loyalty/{customerId:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LoyaltyTransactionDto>>>> GetCustomerLoyaltyHistory(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("marketing:loyalty:manage", "sales:invoice:create", "billing:sales:create");
        var companyId = ResolveCompanyId();
        var items = await marketingService.GetCustomerLoyaltyHistoryAsync(companyId, customerId, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("loyalty/redeem")]
    public async Task<ActionResult<ApiResponse<LoyaltyBalanceDto>>> RedeemLoyaltyPoints(
        [FromBody] RedeemLoyaltyPointsRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("marketing:loyalty:manage", "sales:invoice:create", "billing:sales:create");
        var companyId = ResolveCompanyId();
        var item = await marketingService.RedeemLoyaltyPointsAsync(companyId, request, cancellationToken);
        return Envelope(item);
    }
}

public sealed record RedeemCouponApiRequest(
    Guid CouponId,
    Guid CustomerId,
    Guid SalesInvoiceId,
    long DiscountAmountPaise);
