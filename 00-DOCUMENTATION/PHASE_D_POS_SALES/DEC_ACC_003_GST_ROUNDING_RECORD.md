# Architecture Decision Record: DEC-ACC-003

## Title: Provisional GST Tax Rounding Strategy & Statutory Isolation

**Status:** PROVISIONAL (Statutory Ratification Pending)  
**Date:** 2026-11-13  
**Context:** Phase D POS & Sales Billing Implementation  

---

### Context
Under Indian Goods and Services Tax (GST) provisions:
- Section 170 of the CGST Act prescribes rounding to the nearest whole rupee for the final tax payable on tax invoices.
- However, line-item level rounding vs. invoice-level aggregate rounding can produce minor single-paise rounding discrepancies across multi-line invoices containing mixed GST tax slabs (0%, 5%, 12%, 18%, 28%).
- As instructed, this decision remains unresolved until professional chartered accountant and statutory review is completed.

---

### Decision
1. **Isolated Strategy Interface:**
   All GST calculations and rounding are quarantined behind a dedicated service:
   `IGstCalculationService` (`01-SRC/Backend/Magic.Domain/Sales/Services/IGstCalculationService.cs`).
2. **Provisional Model (Half-Up Banker's Paise Rounding):**
   - For line items: Taxable amount is computed in integer paise:
     $$\text{TaxPaise} = \left\lfloor \frac{\text{TaxableValuePaise} \times \text{RatePercentage}}{100} + 0.5 \right\rfloor$$
   - Total CGST and SGST are split evenly when intra-state (`Rate / 2`). Odd-paise distributions allocate the single remainder paise to CGST deterministically to ensure $\text{CGST} + \text{SGST} = \text{TotalTax}$.
3. **Statutory Non-Claim:**
   - The system does not claim statutory e-Invoicing or GST portal clearance until official GSP/NIC API connectors are authenticated in future phases.
