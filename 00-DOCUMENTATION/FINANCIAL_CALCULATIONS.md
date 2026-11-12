# MAGIC — FINANCIAL CALCULATIONS & GST MATHEMATICAL SPECIFICATION

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  

---

## 1. Monetary Unit Protocol: 64-Bit Integer Paise

To eliminate binary floating-point drift (e.g., `0.1 + 0.2 = 0.30000000000000004`), MAGIC enforces **Integer Arithmetic in Paise** across all layers:

- **Base Currency:** Indian Rupee (INR).
- **Fundamental Unit:** 1 Paise = ₹0.01.
- **Representation:** ₹1.00 = 100 Paise; ₹1,540.75 = 154,075 Paise.
- **Storage Type:**
  - C#: `long` (64-bit signed integer, Range: $\pm 9,223,372,036,854,775,807$ paise, equivalent to ₹92,233 Trillion).
  - PostgreSQL: `BIGINT NOT NULL`.
  - Frontend (TypeScript): Formatted via `Money` value object; raw numbers handled as integer paise (using `bigint` where needed).
- **Prohibition:** Floating-point types (`float`, `double`, `real`) are strictly forbidden for currency amounts.

---

## 2. Tax Calculation Mechanics (Indian GST Framework)

GST rates are expressed internally in **Basis Points (bps)** where $1\% = 100 \text{ bps}$.
- Examples: $5\% = 500 \text{ bps}$, $12\% = 1,200 \text{ bps}$, $18\% = 1,800 \text{ bps}$, $28\% = 2,800 \text{ bps}$.

### 2.1 Reverse Inclusive GST Calculation (Retail / B2C Pricing)
Used when retail selling prices (or MRP) are inclusive of all taxes.

Given:
- $\text{GrossAmountPaise}$ (Total price paid by customer in paise)
- $\text{GSTRateBps}$ (GST rate in basis points, e.g., 1800 for 18%)

#### Mathematical Derivation:
$$\text{TaxableAmountPaise} = \text{Round}\left( \frac{\text{GrossAmountPaise} \times 10,000}{10,000 + \text{GSTRateBps}} \right)$$

$$\text{TotalGSTPaise} = \text{GrossAmountPaise} - \text{TaxableAmountPaise}$$

#### Intra-State Tax Split (CGST + SGST):
In intra-state trade, GST is divided equally between Central GST and State GST. To ensure zero discrepancy when $\text{TotalGSTPaise}$ is an odd number:
$$\text{CGSTPaise} = \lfloor \text{TotalGSTPaise} / 2 \rfloor$$
$$\text{SGSTPaise} = \text{TotalGSTPaise} - \text{CGSTPaise}$$

*Invariant Proof:*
$$\text{CGSTPaise} + \text{SGSTPaise} = \lfloor \text{TotalGSTPaise} / 2 \rfloor + (\text{TotalGSTPaise} - \lfloor \text{TotalGSTPaise} / 2 \rfloor) \equiv \text{TotalGSTPaise}$$
$$\text{TaxableAmountPaise} + \text{CGSTPaise} + \text{SGSTPaise} \equiv \text{GrossAmountPaise}$$

#### Inter-State Tax (IGST):
$$\text{IGSTPaise} = \text{TotalGSTPaise}$$

---

### 2.2 Forward Exclusive GST Calculation (B2B / Wholesale Pricing)
Used when trade prices are quoted exclusive of tax.

Given:
- $\text{TaxableAmountPaise}$
- $\text{GSTRateBps}$

$$\text{TotalGSTPaise} = \text{Round}\left( \frac{\text{TaxableAmountPaise} \times \text{GSTRateBps}}{10,000} \right)$$
$$\text{GrossAmountPaise} = \text{TaxableAmountPaise} + \text{TotalGSTPaise}$$

---

### 2.3 Worked Numerical Verification

**Scenario: Retail Item Sold at ₹1,000.00 Inclusive of 18% GST (Intra-state)**
1. $\text{GrossAmountPaise} = 100,000 \text{ paise}$.
2. $\text{GSTRateBps} = 1,800 \text{ bps}$.
3. $\text{TaxableAmountPaise} = \text{Round}\left( \frac{100,000 \times 10,000}{11,800} \right) = \text{Round}(84,745.7627) = 84,746 \text{ paise}$ (₹847.46).
4. $\text{TotalGSTPaise} = 100,000 - 84,746 = 15,254 \text{ paise}$ (₹152.54).
5. $\text{CGSTPaise} = \lfloor 15,254 / 2 \rfloor = 7,627 \text{ paise}$ (₹76.27).
6. $\text{SGSTPaise} = 15,254 - 7,627 = 7,627 \text{ paise}$ (₹76.27).
7. Verification: $84,746 + 7,627 + 7,627 = 100,000 \text{ paise}$ (₹1,000.00). **Exact zero error.**

**Scenario: Odd-Cent Tax Split (Item at ₹15.00 Inclusive of 5% GST)**
1. $\text{GrossAmountPaise} = 1,500 \text{ paise}$.
2. $\text{GSTRateBps} = 500 \text{ bps}$.
3. $\text{TaxableAmountPaise} = \text{Round}\left( \frac{1,500 \times 10,000}{10,500} \right) = \text{Round}(1,428.5714) = 1,429 \text{ paise}$ (₹14.29).
4. $\text{TotalGSTPaise} = 1,500 - 1,429 = 71 \text{ paise}$ (₹0.71).
5. $\text{CGSTPaise} = \lfloor 71 / 2 \rfloor = 35 \text{ paise}$ (₹0.35).
6. $\text{SGSTPaise} = 71 - 35 = 36 \text{ paise}$ (₹0.36).
7. Verification: $1,429 + 35 + 36 = 1,500 \text{ paise}$ (₹15.00). **Deterministic split with zero leak.**

---

## 3. Statutory Invoice Rounding (Section 170 CGST Act)

Under Section 170 of the Central Goods and Services Tax Act:
- The final invoice total payable by the customer must be rounded to the nearest whole rupee.
- A fractional part of a rupee equal to or exceeding 50 paise is rounded up to ₹1.00 (+100 paise).
- A fractional part under 50 paise is ignored (rounded down to ₹0.00).

### Implementation Protocol
1. Calculate line-item taxable amounts and tax heads in exact integer paise.
2. Sum all line items to obtain $\text{ExactInvoiceTotalPaise}$.
3. Calculate:
   $$\text{RemainderPaise} = \text{ExactInvoiceTotalPaise} \pmod{100}$$
4. If $\text{RemainderPaise} \ge 50$:
   $$\text{RoundOffDeltaPaise} = 100 - \text{RemainderPaise} \quad (\text{positive adjustment})$$
   $$\text{FinalPayablePaise} = \text{ExactInvoiceTotalPaise} + \text{RoundOffDeltaPaise}$$
5. If $\text{RemainderPaise} < 50$:
   $$\text{RoundOffDeltaPaise} = -\text{RemainderPaise} \quad (\text{negative adjustment})$$
   $$\text{FinalPayablePaise} = \text{ExactInvoiceTotalPaise} + \text{RoundOffDeltaPaise}$$
6. **Accounting Booking:** The $\text{RoundOffDeltaPaise}$ is posted directly to GL account `4250: Rounding Off Adjustment (Expense/Income)`.

---

## 4. Inventory Valuation Mathematical Models

MAGIC supports two configurable valuation models per enterprise (subject to statutory fiscal-year lock policy):

### 4.1 Moving Weighted Average Cost (AVCO)
Recomputed upon every inward goods receipt (GRN / Purchase Invoice):
$$\text{NewValuationRatePaise} = \text{Round}\left( \frac{(\text{CurrentQty} \times \text{CurrentRatePaise}) + (\text{InwardQty} \times \text{InwardRatePaise})}{\text{CurrentQty} + \text{InwardQty}} \right)$$

Sales issues deplete stock at the current $\text{NewValuationRatePaise}$ without altering the unit valuation rate.

### 4.2 First-In-First-Out (FIFO)
- Inventory is tracked in chronological **Batches / Lots**.
- Outward issues deplete the oldest available batch first until exhausted, then cascade to the next oldest batch.
- Balance Sheet inventory valuation equals the sum of remaining quantities multiplied by their respective lot purchase costs.

