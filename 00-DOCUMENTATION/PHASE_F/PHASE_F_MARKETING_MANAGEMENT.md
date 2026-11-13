# MAGIC ERP — MODULE 14: MARKETING & CUSTOMER GROWTH SPECIFICATION
## Segmentation, Campaigns, Coupons & Loyalty Programs

**Route:** `/billing/marketing`  
**API Controller:** `MarketingController` (`/api/v1/marketing`)  
**Database Schema:** `marketing`

---

## 1. Functional Scope

1. **Customer Segmentation:**
   - Dynamic spending tiers, purchase frequency, and inactive customer detection based on verified invoice history.
2. **Campaign Tracking:**
   - Promotional campaigns with budget tracking, date windows, and attributed sales revenue.
3. **Discount Coupons & Vouchers:**
   - Percentage or fixed amount coupons with minimum basket spend and maximum discount caps.
   - Enforces total and per-customer usage limits; rejects duplicate uses.
   - Preserves POS financial invariants (GST calculated on discounted taxable amount).
4. **Loyalty Program:**
   - Configurable points accrual rate (e.g. 1 point per 100 spent).
   - Point redemption at POS checkout against total payable.
   - Full transaction history tracking points earned, redeemed, and adjusted.

