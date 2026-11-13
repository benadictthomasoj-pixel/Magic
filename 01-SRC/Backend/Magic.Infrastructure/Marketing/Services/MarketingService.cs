using Magic.Application.Marketing.Services;
using Magic.Contracts.Marketing;
using Magic.Domain.Common;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Marketing.Services;

public sealed class MarketingService(MagicDbContext dbContext) : IMarketingService
{
    public async Task<MarketingSummaryDto> GetSummaryAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var activeCampaigns = await dbContext.Campaigns.CountAsync(c => c.CompanyId == companyId && c.Status == "Active", cancellationToken);
        var activeCoupons = await dbContext.PromotionCoupons.CountAsync(c => c.CompanyId == companyId && c.IsActive, cancellationToken);
        var loyaltyMembers = await dbContext.LoyaltyTransactions.Where(l => l.CompanyId == companyId).Select(l => l.CustomerId).Distinct().CountAsync(cancellationToken);
        var totalAttributedRev = await dbContext.Campaigns.Where(c => c.CompanyId == companyId).SumAsync(c => c.AttributedRevenuePaise, cancellationToken);

        return new MarketingSummaryDto(activeCampaigns, activeCoupons, loyaltyMembers, totalAttributedRev);
    }

    public async Task<IReadOnlyList<CustomerSegmentDto>> GetSegmentsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.CustomerSegments
            .Where(s => s.CompanyId == companyId && s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new CustomerSegmentDto(
                s.Id,
                s.CompanyId,
                s.Name,
                s.SegmentType,
                s.Description,
                s.MinLifetimeSpendPaise,
                s.MinOrdersCount,
                s.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerSegmentDto> CreateSegmentAsync(Guid companyId, CreateCustomerSegmentRequest request, CancellationToken cancellationToken = default)
    {
        var segment = new CustomerSegmentRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = request.Name.Trim(),
            SegmentType = request.SegmentType,
            Description = request.Description,
            MinLifetimeSpendPaise = request.MinLifetimeSpendPaise,
            MinOrdersCount = request.MinOrdersCount,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.CustomerSegments.Add(segment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CustomerSegmentDto(
            segment.Id,
            segment.CompanyId,
            segment.Name,
            segment.SegmentType,
            segment.Description,
            segment.MinLifetimeSpendPaise,
            segment.MinOrdersCount,
            segment.IsActive);
    }

    public async Task<IReadOnlyList<CampaignDto>> GetCampaignsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Campaigns
            .Include(c => c.TargetSegment)
            .Where(c => c.CompanyId == companyId)
            .OrderByDescending(c => c.StartDate)
            .Select(c => new CampaignDto(
                c.Id,
                c.CompanyId,
                c.Name,
                c.Description,
                c.TargetSegmentId,
                c.TargetSegment != null ? c.TargetSegment.Name : null,
                c.StartDate,
                c.EndDate,
                c.BudgetPaise,
                c.ActualSpendPaise,
                c.AttributedRevenuePaise,
                c.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<CampaignDto> CreateCampaignAsync(Guid companyId, CreateCampaignRequest request, CancellationToken cancellationToken = default)
    {
        var campaign = new CampaignRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = request.Name.Trim(),
            Description = request.Description,
            TargetSegmentId = request.TargetSegmentId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            BudgetPaise = request.BudgetPaise,
            ActualSpendPaise = 0,
            AttributedRevenuePaise = 0,
            Status = "Active",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.Campaigns.Add(campaign);
        await dbContext.SaveChangesAsync(cancellationToken);

        string? segName = null;
        if (campaign.TargetSegmentId.HasValue)
        {
            segName = (await dbContext.CustomerSegments.FirstOrDefaultAsync(s => s.Id == campaign.TargetSegmentId.Value, cancellationToken))?.Name;
        }

        return new CampaignDto(
            campaign.Id,
            campaign.CompanyId,
            campaign.Name,
            campaign.Description,
            campaign.TargetSegmentId,
            segName,
            campaign.StartDate,
            campaign.EndDate,
            campaign.BudgetPaise,
            campaign.ActualSpendPaise,
            campaign.AttributedRevenuePaise,
            campaign.Status);
    }

    public async Task<IReadOnlyList<PromotionCouponDto>> GetCouponsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PromotionCoupons
            .Include(c => c.Campaign)
            .Where(c => c.CompanyId == companyId && c.IsActive)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new PromotionCouponDto(
                c.Id,
                c.CompanyId,
                c.Code,
                c.CampaignId,
                c.Campaign != null ? c.Campaign.Name : null,
                c.DiscountType,
                c.DiscountValue,
                c.MinOrderAmountPaise,
                c.MaxDiscountAmountPaise,
                c.ValidFrom,
                c.ValidTo,
                c.UsageLimitTotal,
                c.UsageLimitPerCustomer,
                c.CurrentUsageCount,
                c.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<PromotionCouponDto> CreateCouponAsync(Guid companyId, CreatePromotionCouponRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await dbContext.PromotionCoupons.AnyAsync(c => c.CompanyId == companyId && c.Code == code, cancellationToken);
        if (exists)
            throw new DomainException("DUPLICATE_COUPON_CODE", $"Coupon code {code} already exists.", StatusCodes.Status400BadRequest);

        var coupon = new PromotionCouponRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = code,
            CampaignId = request.CampaignId,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MinOrderAmountPaise = request.MinOrderAmountPaise,
            MaxDiscountAmountPaise = request.MaxDiscountAmountPaise,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            UsageLimitTotal = request.UsageLimitTotal,
            UsageLimitPerCustomer = request.UsageLimitPerCustomer,
            CurrentUsageCount = 0,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.PromotionCoupons.Add(coupon);
        await dbContext.SaveChangesAsync(cancellationToken);

        string? campaignName = null;
        if (coupon.CampaignId.HasValue)
        {
            campaignName = (await dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == coupon.CampaignId.Value, cancellationToken))?.Name;
        }

        return new PromotionCouponDto(
            coupon.Id,
            coupon.CompanyId,
            coupon.Code,
            coupon.CampaignId,
            campaignName,
            coupon.DiscountType,
            coupon.DiscountValue,
            coupon.MinOrderAmountPaise,
            coupon.MaxDiscountAmountPaise,
            coupon.ValidFrom,
            coupon.ValidTo,
            coupon.UsageLimitTotal,
            coupon.UsageLimitPerCustomer,
            coupon.CurrentUsageCount,
            coupon.IsActive);
    }

    public async Task<ValidateCouponResponse> ValidateCouponAsync(Guid companyId, ValidateCouponRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var now = DateTimeOffset.UtcNow;

        var coupon = await dbContext.PromotionCoupons
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Code == code && c.IsActive, cancellationToken);

        if (coupon == null)
            return new ValidateCouponResponse(false, "Coupon code is invalid or deactivated.", null, 0);

        if (now < coupon.ValidFrom || now > coupon.ValidTo)
            return new ValidateCouponResponse(false, "Coupon has expired or is not yet active.", coupon.Id, 0);

        if (coupon.CurrentUsageCount >= coupon.UsageLimitTotal)
            return new ValidateCouponResponse(false, "Coupon usage limit has been reached.", coupon.Id, 0);

        if (request.OrderSubtotalPaise < coupon.MinOrderAmountPaise)
            return new ValidateCouponResponse(false, $"Minimum order amount of ₹{coupon.MinOrderAmountPaise / 100.0:F2} required.", coupon.Id, 0);

        var customerUsage = await dbContext.CouponRedemptions
            .CountAsync(r => r.CouponId == coupon.Id && r.CustomerId == request.CustomerId, cancellationToken);

        if (customerUsage >= coupon.UsageLimitPerCustomer)
            return new ValidateCouponResponse(false, "You have already used this coupon the maximum allowed times.", coupon.Id, 0);

        // Calculate discount
        long discountPaise = 0;
        if (coupon.DiscountType == "Percentage")
        {
            // DiscountValue in basis points (1000 = 10%)
            discountPaise = (long)Math.Round((double)request.OrderSubtotalPaise * (double)coupon.DiscountValue / 10000.0);
            if (coupon.MaxDiscountAmountPaise > 0 && discountPaise > coupon.MaxDiscountAmountPaise)
            {
                discountPaise = coupon.MaxDiscountAmountPaise;
            }
        }
        else
        {
            discountPaise = Math.Min(coupon.DiscountValue, request.OrderSubtotalPaise);
        }

        return new ValidateCouponResponse(true, null, coupon.Id, discountPaise);
    }

    public async Task RecordCouponRedemptionAsync(Guid companyId, Guid couponId, Guid customerId, Guid salesInvoiceId, long discountAmountPaise, CancellationToken cancellationToken = default)
    {
        var coupon = await dbContext.PromotionCoupons.FirstOrDefaultAsync(c => c.Id == couponId && c.CompanyId == companyId, cancellationToken);
        if (coupon != null)
        {
            coupon.CurrentUsageCount++;
            if (coupon.CampaignId.HasValue)
            {
                var campaign = await dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == coupon.CampaignId.Value, cancellationToken);
                if (campaign != null)
                {
                    campaign.AttributedRevenuePaise += discountAmountPaise;
                }
            }

            dbContext.CouponRedemptions.Add(new CouponRedemptionRecord
            {
                Id = Guid.NewGuid(),
                CouponId = coupon.Id,
                CustomerId = customerId,
                SalesInvoiceId = salesInvoiceId,
                DiscountAmountPaise = discountAmountPaise,
                RedeemedAtUtc = DateTimeOffset.UtcNow
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<LoyaltyProgramDto>> GetLoyaltyProgramsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.LoyaltyPrograms
            .Where(l => l.CompanyId == companyId && l.IsActive)
            .OrderBy(l => l.Name)
            .Select(l => new LoyaltyProgramDto(
                l.Id,
                l.CompanyId,
                l.Name,
                l.PointsPerHundredSpent,
                l.CurrencyValuePerPointPaise,
                l.MinPointsToRedeem,
                l.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<LoyaltyProgramDto> CreateLoyaltyProgramAsync(Guid companyId, CreateLoyaltyProgramRequest request, CancellationToken cancellationToken = default)
    {
        var program = new LoyaltyProgramRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = request.Name.Trim(),
            PointsPerHundredSpent = request.PointsPerHundredSpent,
            CurrencyValuePerPointPaise = request.CurrencyValuePerPointPaise,
            MinPointsToRedeem = request.MinPointsToRedeem,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.LoyaltyPrograms.Add(program);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new LoyaltyProgramDto(
            program.Id,
            program.CompanyId,
            program.Name,
            program.PointsPerHundredSpent,
            program.CurrencyValuePerPointPaise,
            program.MinPointsToRedeem,
            program.IsActive);
    }

    public async Task<LoyaltyBalanceDto> GetCustomerLoyaltyBalanceAsync(Guid companyId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Id == customerId && c.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("CUSTOMER_NOT_FOUND", "Customer does not exist.", StatusCodes.Status404NotFound);

        var lastTx = await dbContext.LoyaltyTransactions
            .Where(t => t.CustomerId == customerId && t.CompanyId == companyId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var balance = lastTx?.BalanceAfterPoints ?? 0;
        var program = await dbContext.LoyaltyPrograms.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.IsActive, cancellationToken);
        var valuePerPoint = program?.CurrencyValuePerPointPaise ?? 100;

        return new LoyaltyBalanceDto(customer.Id, customer.Name, balance, balance * valuePerPoint);
    }

    public async Task<IReadOnlyList<LoyaltyTransactionDto>> GetCustomerLoyaltyHistoryAsync(Guid companyId, Guid customerId, CancellationToken cancellationToken = default)
    {
        return await dbContext.LoyaltyTransactions
            .Where(t => t.CustomerId == customerId && t.CompanyId == companyId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new LoyaltyTransactionDto(
                t.Id,
                t.CustomerId,
                t.TransactionType,
                t.Points,
                t.BalanceAfterPoints,
                t.SalesInvoiceId,
                t.Notes,
                t.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<LoyaltyBalanceDto> AwardLoyaltyPointsAsync(Guid companyId, Guid customerId, Guid salesInvoiceId, long orderSubtotalPaise, CancellationToken cancellationToken = default)
    {
        var program = await dbContext.LoyaltyPrograms.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.IsActive, cancellationToken);
        if (program == null || program.PointsPerHundredSpent <= 0)
        {
            return await GetCustomerLoyaltyBalanceAsync(companyId, customerId, cancellationToken);
        }

        var pointsEarned = (int)(orderSubtotalPaise / 10000) * program.PointsPerHundredSpent; // 10000 paise = 100 INR
        if (pointsEarned <= 0)
        {
            return await GetCustomerLoyaltyBalanceAsync(companyId, customerId, cancellationToken);
        }

        var currentBalance = (await GetCustomerLoyaltyBalanceAsync(companyId, customerId, cancellationToken)).TotalPointsBalance;
        var newBalance = currentBalance + pointsEarned;

        var tx = new LoyaltyTransactionRecord
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CompanyId = companyId,
            TransactionType = "Earned",
            Points = pointsEarned,
            BalanceAfterPoints = newBalance,
            SalesInvoiceId = salesInvoiceId,
            Notes = $"Points earned from invoice {salesInvoiceId}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.LoyaltyTransactions.Add(tx);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetCustomerLoyaltyBalanceAsync(companyId, customerId, cancellationToken);
    }

    public async Task<LoyaltyBalanceDto> RedeemLoyaltyPointsAsync(Guid companyId, RedeemLoyaltyPointsRequest request, CancellationToken cancellationToken = default)
    {
        var currentBalance = (await GetCustomerLoyaltyBalanceAsync(companyId, request.CustomerId, cancellationToken)).TotalPointsBalance;
        if (currentBalance < request.PointsToRedeem)
        {
            throw new DomainException("INSUFFICIENT_POINTS", $"Insufficient loyalty points. Balance: {currentBalance}, Requested: {request.PointsToRedeem}", StatusCodes.Status400BadRequest);
        }

        var newBalance = currentBalance - request.PointsToRedeem;
        var tx = new LoyaltyTransactionRecord
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            CompanyId = companyId,
            TransactionType = "Redeemed",
            Points = -request.PointsToRedeem,
            BalanceAfterPoints = newBalance,
            SalesInvoiceId = request.SalesInvoiceId,
            Notes = $"Redeemed on invoice {request.SalesInvoiceId}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.LoyaltyTransactions.Add(tx);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetCustomerLoyaltyBalanceAsync(companyId, request.CustomerId, cancellationToken);
    }
}

