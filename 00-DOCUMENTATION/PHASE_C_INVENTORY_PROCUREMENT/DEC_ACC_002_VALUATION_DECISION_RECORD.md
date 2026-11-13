# DEC-ACC-002: Inventory Valuation Policy & General Ledger Freeze Record

**Status:** Unresolved / Deferred Decision Record  
**Owner:** Accounting Architecture Board & Finance Stakeholders  
**Subsystem:** MAGIC Inventory & Procurement (Phase C) vs. General Ledger (Phase A)  
**Date Logged:** 2026-11-13  

---

## 1. Context & Architectural Constraint

Accounting decision `DEC-ACC-002` concerns the valuation method for physical inventory and the corresponding automated Cost of Goods Sold (COGS) and inventory valuation journal voucher generation.

The two recognized methods under Ind AS 2 / IAS 2 are:
1. **FIFO (First-In, First-Out):**
   - Assumes the items of inventory that were purchased or produced first are sold first.
   - Closing inventory reflects recent acquisition prices.
   - In periods of rising prices (inflation), FIFO results in higher ending inventory valuations, lower COGS, and higher reported gross profit.
   - Requires lot/batch tracking or receipt queue pointer tracking.
2. **Moving Weighted Average (MWA):**
   - Continuously computes an updated weighted average cost per unit upon every new receipt:
     $$\text{New Cost} = \frac{(\text{Current Qty} \times \text{Current Avg Cost}) + (\text{Receipt Qty} \times \text{Receipt Cost})}{\text{Current Qty} + \text{Receipt Qty}}$$
   - Smooths out fluctuations in market purchase prices.
   - Widely favored in high-frequency retail and wholesale businesses with homogeneous goods.

---

## 2. Invariant & Phase C Implementation Boundary

In strict compliance with project rules:
1. **No Assumption Policy:** Neither FIFO nor Moving Weighted Average is prematurely chosen or hardcoded as the sole GL posting engine.
2. **Physical Quantity & Traceability Guarantee:**
   Phase C tracks exact physical inventory quantities (`OnHand`, `Available`, `Reserved`, `Damaged`, `Expired`, `Blocked`) at all times.
3. **Transaction Cost Preservation:**
   Every `GoodsReceiptLine` and `StockMovement` captures the actual purchase invoice price and unit cost in integer paise (`BIGINT`).
4. **Valuation Interface Design:**
   An extensible `IInventoryValuationStrategy` interface is designed to support both FIFO queue simulation and Moving Weighted Average computation when requested by financial reporting services.
5. **General Ledger Posting Freeze:**
   Automated GL journal postings for Inventory Valuation (Debit Inventory Asset Account, Credit GRN Clearing / Accounts Payable) and COGS (Debit COGS, Credit Inventory Asset) are **strictly frozen**. No fabricated or assumed accounting postings are executed in Phase C.

---

## 3. Extensible Valuation Strategy Interface

```csharp
namespace Magic.Application.Inventory;

public interface IInventoryValuationStrategy
{
    string ValuationMethodCode { get; }
    Task<long> CalculateInventoryValuePaiseAsync(
        Guid companyId, 
        Guid productId, 
        Guid? warehouseId, 
        CancellationToken cancellationToken = default);

    Task<long> CalculateIssueCostPaiseAsync(
        Guid companyId, 
        Guid productId, 
        Guid warehouseId, 
        decimal issueQuantity, 
        CancellationToken cancellationToken = default);
}
```

This ensures that once an executive decision is made regarding `DEC-ACC-002`, the accounting engine can plug directly into the stored movement ledger without altering Phase C physical stock records.

