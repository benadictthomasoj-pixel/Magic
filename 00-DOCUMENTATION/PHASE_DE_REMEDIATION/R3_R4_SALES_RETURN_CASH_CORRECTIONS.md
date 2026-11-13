# MAGIC ERP — Remediation Report R3 & R4: Sales Returns Financial Corrections
## Defect Identifiers: DEF-DE-003 & DEF-DE-004 (High Severity)

**Module:** Phase D POS & Sales Management  
**Remediation Status:** Complete & Verified  
**Affected Files:**
- `01-SRC/Backend/Magic.Infrastructure/Sales/Services/SalesServices.cs`
- `03-TESTS/Integration/Magic.Api.IntegrationTests/PhaseDERemediationTests.cs`

---

## 1. R3: Discount-Aware Sales Returns (DEF-DE-003)
### Problem Statement
In `SalesReturnService.CreateReturnAsync`, return refunds were computed from the catalog or gross unit price without factoring in invoice line discounts. When a discounted item was returned, the customer received a refund greater than the amount originally paid, causing cash and accounting leakage.

### Technical Implementation
In `SalesReturnService.CreateReturnAsync`:
$$\text{invLineTaxable} = \max(0, (\text{UnitPricePaise} \times \text{InvoiceQty}) - \text{DiscountPaise})$$
$$\text{returnTaxable} = \left\lfloor \frac{\text{invLineTaxable} \times \text{ReturnQty}}{\text{InvoiceQty}} + 0.5 \right\rfloor$$
$$\text{returnTax} = \left\lfloor \frac{\text{LineTaxAmountPaise} \times \text{ReturnQty}}{\text{InvoiceQty}} + 0.5 \right\rfloor$$
$$\text{LineTotal} = \text{returnTaxable} + \text{returnTax}$$
$$\text{UnitPrice} = \left\lfloor \frac{\text{returnTaxable}}{\text{ReturnQty}} + 0.5 \right\rfloor$$

This ensures that all partial and full returns refund only the exact discounted net revenue and prorated tax paid.

---

## 2. R4: Tender-Aware Cash Drawer Deductions (DEF-DE-004)
### Problem Statement
When processing a sales return, the system unconditionally deducted the refund total from the cashier session's `ExpectedCashPaise`, even if the customer was refunded via Credit Note, UPI, or Card. This caused physical cash drawer shortages during end-of-shift reconciliation.

### Technical Implementation
In `SalesReturnService.CreateReturnAsync`:
```csharp
if (string.Equals(request.RefundMethod, "Cash", StringComparison.OrdinalIgnoreCase))
{
    session.ExpectedCashPaise = Math.Max(0, session.ExpectedCashPaise - totalRefundPaise);
    session.CashRefundsPaise += totalRefundPaise;
}
// Non-cash refunds (CreditNote, Card, Upi) leave session.ExpectedCashPaise intact
```

## 3. Verification Evidence
- Integration test `R3_Sales_Return_Apportions_Original_Invoice_Discount` in `PhaseDERemediationTests.cs` verified that returning an item discounted by ₹200 refunds ₹2,714 instead of ₹2,950.
- Integration test `R4_Sales_Return_Non_Cash_Does_Not_Reduce_Physical_Cash_Drawer` verified that returning via `CreditNote` leaves `session.ExpectedCashPaise` unaffected.
