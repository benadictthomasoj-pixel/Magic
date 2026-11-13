# MAGIC ERP — Remediation Report R5: Warehouse Restoration on Cancellation & Returns
## Defect Identifier: DEF-DE-005 (High Severity)

**Module:** Phase D POS & Sales Management $\leftrightarrow$ Phase C Inventory Management  
**Remediation Status:** Complete & Verified  
**Affected Files:**
- `01-SRC/Contracts/Magic.Contracts/Sales/SalesDtos.cs`
- `01-SRC/Backend/Magic.Infrastructure/Persistence/Entities/SalesEntities.cs`
- `01-SRC/Backend/Magic.Infrastructure/Persistence/Configurations/SalesSchemaConfigurations.cs`
- `01-SRC/Backend/Magic.Infrastructure/Sales/Services/SalesServices.cs`
- Database: `magic_dev` and `magic_test` tables `sales.sales_invoices`
- `03-TESTS/Integration/Magic.Api.IntegrationTests/PhaseDERemediationTests.cs`

---

## 1. Problem Statement
When a sales invoice was cancelled or returned, the system restocked goods into an arbitrary default active warehouse rather than the originating warehouse from which the stock was issued. In multi-branch/multi-warehouse operations, this caused stock imbalances across stores.

## 2. Technical Implementation
1. **Schema Enhancement:**
   - Added `WarehouseId` (`Guid?`) to `SalesInvoiceRecord` and `SalesInvoiceDto`.
   - Added `AccountingVoucherId` (`Guid?`) to `SalesInvoiceRecord` and `SalesInvoiceDto`.
   - Executed DDL migrations on both `magic_dev` and `magic_test`:
     ```sql
     ALTER TABLE sales.sales_invoices ADD COLUMN IF NOT EXISTS "WarehouseId" uuid;
     ALTER TABLE sales.sales_invoices ADD COLUMN IF NOT EXISTS "AccountingVoucherId" uuid;
     ```
2. **Checkout Assignment:**
   - Recorded `request.WarehouseId` into `invoice.WarehouseId` upon POS checkout.
3. **Restocking Resolution:**
   - In `SalesInvoiceService.CancelInvoiceAsync` and `SalesReturnService.CreateReturnAsync`:
     - Resolves the originating warehouse from `invoice.WarehouseId`.
     - Falls back to `StockMovementRecord` where `ReferenceId == invoice.Id && MovementType == "SalesIssue"`.
     - Strictly increments `QuantityOnHand` and creates `StockMovementRecord` referencing that specific warehouse ID.

## 3. Verification Evidence
- Integration test `R5_Sales_Return_And_Cancellation_Restores_Original_Warehouse` created 2 distinct warehouses (`WH-A`, `WH-B`), issued sale from `WH-B`, cancelled invoice and returned an item, and verified that stock in `WH-B` increased while stock in `WH-A` remained completely unchanged.
