# MAGIC ERP — Remediation Verification Report: R5
## Defect Identifier: DEF-DE-005 (Original Warehouse Restoration)

**Severity:** High  
**Status:** **PASS**  
**Audited Files:**
- `01-SRC/Contracts/Magic.Contracts/Sales/SalesDtos.cs`
- `01-SRC/Backend/Magic.Infrastructure/Persistence/Entities/SalesEntities.cs`
- `01-SRC/Backend/Magic.Infrastructure/Persistence/Configurations/SalesSchemaConfigurations.cs`
- `01-SRC/Backend/Magic.Infrastructure/Sales/Services/SalesServices.cs`
- PostgreSQL 18: `sales.sales_invoices`, `inventory.stock_balances`, `inventory.stock_movements`

---

## 1. Original Defect Summary
When an invoice was cancelled or goods were returned, the system restocked inventory into the first active warehouse found in the database rather than the warehouse from which the stock was originally sold. In multi-warehouse configurations, this caused stock to be deducted from Warehouse B during checkout but restored to Warehouse A upon return, corrupting warehouse balances.

## 2. Independent Code & Database Inspection
1. **Schema Verification:**
   - Verified via PostgreSQL query on both `magic_dev` and `magic_test`:
     `sales.sales_invoices` has column `WarehouseId uuid NULL` and `AccountingVoucherId uuid NULL`.
2. **Checkout Association:**
   - In `PosService.CheckoutAsync`, `invoice.WarehouseId = request.WarehouseId` is persisted.
3. **Cancellation & Return Logic:**
   - In `SalesInvoiceService.CancelInvoiceAsync` (lines 852–865) and `SalesReturnService.CreateReturnAsync` (lines 1188–1205):
     - First checks `invoice.WarehouseId`.
     - If null, queries `inventory.stock_movements` for `MovementType == "SalesIssue" && ReferenceId == invoice.Id` to obtain the source `WarehouseId`.
     - Restores stock exclusively into `stock_balances` and creates `StockMovementRecord` for that specific warehouse.

## 3. Test & Database Verification
- Test: `PhaseDERemediationTests.R5_Sales_Return_And_Cancellation_Restores_Original_Warehouse`
- Scenario:
  - Created Warehouse 1 (`WH1`) and Warehouse 2 (`WH2`).
  - Added opening stock: 50 units in WH1, 50 units in WH2.
  - Sold 10 units strictly from WH2. Stock in WH2 became 40; WH1 remained 50.
  - Cancelled the invoice.
- Verification:
  - Stock in WH2 was restored to 50 units.
  - Stock in WH1 remained strictly 50 units (never touched).
  - Stock movements confirmed `SalesInvoiceCancellation` referenced WH2's ID.
- Result: **PASS**
