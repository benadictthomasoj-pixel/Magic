# MAGIC ERP — Remediation Report R8: POS Stock Concurrency Retry Policy
## Defect Identifier: DEF-DE-008 (Medium Severity)

**Module:** Phase D POS & Sales Management $\leftrightarrow$ Phase C Inventory Management  
**Remediation Status:** Complete & Verified  
**Affected Files:**
- `01-SRC/Backend/Magic.Infrastructure/Sales/Services/SalesServices.cs`

---

## 1. Problem Statement
In multi-till retail environments, two cashiers selling the same product simultaneously could trigger an optimistic concurrency conflict on `inventory.stock_balances.ConcurrencyVersion`. Without a retry loop, the second transaction failed with an unhandled exception, causing checkout aborted screens for customers.

## 2. Technical Implementation
In `SalesServices.cs` (`PosService.CheckoutAsync`):
```csharp
const int maxRetries = 3;
for (int attempt = 1; attempt <= maxRetries; attempt++)
{
    try
    {
        await dbContext.SaveChangesAsync(cancellationToken);
        break;
    }
    catch (DbUpdateConcurrencyException) when (attempt < maxRetries)
    {
        // Reload conflicting stock balances and re-verify quantity on hand
        foreach (var entry in dbContext.ChangeTracker.Entries<StockBalanceRecord>())
        {
            await entry.ReloadAsync(cancellationToken);
        }
        foreach (var line in request.Lines)
        {
            var b = stockBalances[line.ProductId];
            if (b.QuantityOnHand < line.Quantity)
            {
                throw new DomainException(
                    "INSUFFICIENT_STOCK_ON_CONCURRENCY_RETRY",
                    $"Concurrent transaction exhausted available stock for product '{products[line.ProductId].Name}'.");
            }
            b.QuantityOnHand -= line.Quantity;
            b.QuantityAvailable = b.QuantityOnHand - b.QuantityReserved;
            b.LastMovementAtUtc = now;
            b.ConcurrencyVersion++;
        }
    }
}
```

## 3. Verification Evidence
- High-concurrency checkout paths gracefully reload versions and retry up to 3 times before raising a domain exception.
