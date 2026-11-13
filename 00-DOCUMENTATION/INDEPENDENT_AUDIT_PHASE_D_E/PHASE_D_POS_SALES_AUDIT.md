# MAGIC ERP — INDEPENDENT TECHNICAL AUDIT
## Module Deep Dive: Phase D — POS & Sales Management

**Project:** MAGIC – Universal Business Management ERP  
**Scope:** Section 3 (D1 through D9)  
**Audit Baseline Commit:** `84e43aa`  
**Date:** 2026-11-13  

---

## 1. D1. POS Checkout Audit

### Inspected Code:
- Frontend: `01-SRC/Frontend/src/app/features/sales/pos/pos-checkout.component.ts`, `.html`
- Backend: `01-SRC/Backend/Magic.Infrastructure/Sales/Services/SalesServices.cs` (`PosService.LookupProductsAsync`, `PosService.CheckoutAsync`)
- Controller: `01-SRC/Backend/Magic.Api/Controllers/Sales/PosController.cs`

### Verification Results:
1. **Barcode Resolution:** Verified. `LookupProductsAsync` checks `Product.PrimaryBarcode` and `Product.Barcodes` using case-insensitive SQL matching (`EF.Functions.ILike`).
2. **Scanner Debounce / Duplicate Scans:** The frontend `onBarcodeScanned(barcode)` handles enter-key termination. However, if a handheld scanner outputs rapid keystrokes without debounce, duplicate items can be added to the in-memory cart before previous UI update triggers.
3. **Product Availability & Stock Gate:** In `CheckoutAsync` (lines 170-185), physical stock on hand is pre-validated against `StockBalanceRecord.QuantityOnHand`. If requested quantity exceeds on hand, checkout aborts with `InvalidOperationException`.
4. **Authoritative Backend Pricing:** Secure. While the frontend provides price override fields, line items default to `prod.SellingPricePaise` loaded directly from the database (line 204). Frontend manipulations of item prices are ignored unless explicitly overridden via `UnitPriceOverridePaise`.
5. **Multi-Tender Calculation:** Lines 244-249 sum tendered payments. If tendered is less than grand total, checkout is rejected. If tendered is greater, change is computed (`changeGivenPaise = totalPaidPaise - grandTotalPaise`).
6. **Defect Identified (DEF-DE-001): Missing Accounting Voucher Integration:**
   - In `PosService.CheckoutAsync` (lines 302-352), the transaction persists `SalesInvoiceRecord`, `SalesInvoiceLineRecord`, `SalesPaymentRecord`, and `StockMovementRecord`.
   - **Crucially missing:** There is **NO call to `IPostingService.PostVoucherAsync`**. No Sales Voucher is posted to `accounting.journal_vouchers`.
   - **Financial Consequence:** POS checkout is completely uncoupled from the General Ledger. The financial statements remain unaware of retail cash, bank collections, GST liabilities, and revenue.

---

## 2. D2. Pricing and Discount Audit

### Inspected Code:
- `PosService.CheckoutAsync` lines 201-242
- `SalesInvoiceRecord`, `SalesInvoiceLineRecord` entity mappings

### Verification Results:
1. **Integer Arithmetic:** Selling prices, subtotal, discount, and line totals strictly use `long` paise.
2. **Line vs Invoice Discount:** Line-level discounts are supported (`DiscountPercentage`, `DiscountPaise`).
3. **Negative Price / Quantity Check:** Line 172 verifies `if (line.Quantity <= 0) throw new ArgumentOutOfRangeException(...)`.
4. **Defect Identified (DEF-DE-003): Return Price Overpayment Leak:**
   - At checkout: `lineTaxable = Math.Max(0, lineSubtotal - lineDiscount);` (line 207). Tax is calculated on discounted taxable amount.
   - At return (`SalesReturnService.CreateReturnAsync` line 880-883):
     ```csharp
     var unitPrice = invLine.UnitPricePaise;
     var lineTaxAmount = (long)Math.Round((unitPrice * (long)reqLine.ReturnQuantity * invLine.GstRatePercentage) / 100m, MidpointRounding.AwayFromZero);
     var lineTotal = (unitPrice * (long)reqLine.ReturnQuantity) + lineTaxAmount;
     ```
   - **Flaw:** Proportional line discount is completely neglected during sales returns. If an item was sold for ₹1000 with a ₹200 discount (customer paid ₹800 + tax), returning it refunds ₹1000 + tax.

---

## 3. D3. GST and Tax Audit (Statutory DEC-ACC-003)

### Inspected Code:
- `GstCalculationService.CalculateTax` lines 14-37
- Entity configurations in `SalesSchemaConfigurations.cs`

### Verification Results:
1. **Statutory Isolation:** `GstCalculationService` implements `IGstCalculationService`. Tax calculation uses 64-bit integer paise with `MidpointRounding.AwayFromZero`:
   `var totalTax = (long)Math.Round((taxableAmountPaise * gstRate) / 100m, MidpointRounding.AwayFromZero);`
2. **Intra-State vs. Inter-State Logic:**
   - Intra-state (seller state == buyer state): Total tax is split 50/50 between CGST and SGST:
     `cgst = totalTax / 2; sgst = totalTax - cgst;` (deterministic attribution of odd paise).
   - Inter-state (seller state != buyer state): Full tax attributed to IGST (`Cgst = 0, Sgst = 0, Igst = totalTax`).
3. **Limitation / Observation:** GST-inclusive pricing reverse math (extracting base from gross) is not implemented; all line items assume GST-exclusive input rates. Statutory policy review by a qualified Indian Chartered Accountant remains unperformed (`DEC-ACC-003`).

---

## 4. D4. Payment Processing Audit

### Inspected Code:
- `PosService.CheckoutAsync` lines 244-271
- `SalesPaymentRecord` schema and controller methods

### Verification Results:
1. **Tender Methods Supported:** `Cash`, `Card`, `UPI`, `BankTransfer`, `Cheque`.
2. **PCI-DSS Compliance:** Card number and CVV are never captured or stored; only optional `ReferenceNumber` is persisted.
3. **Split Payments:** Verified. Multiple tender lines can be provided in `request.Payments`.
4. **Defect Identified (DEF-DE-002): Inability to Handle Customer Credit / Unpaid Invoices:**
   - POS checkout forcibly requires `totalPaidPaise >= grandTotalPaise`.
   - `invoice.OutstandingBalancePaise` is hardcoded to 0.
   - Partial payments or credit sales are unconditionally rejected with:
     `"Tendered amount of ₹... is less than required invoice total ₹..."`

---

## 5. D5. Invoice Lifecycle Audit

### Inspected Code:
- `SalesInvoiceService.GetInvoicesAsync`, `GetInvoiceByIdAsync`, `CancelInvoiceAsync`
- `SalesInvoiceRecord`

### Verification Results:
1. **Invoice Numbering:** Uses `$"INV-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}"`. Unique index on `invoice_number` prevents duplicates.
2. **Cancellation Workflow:** Invoices can be cancelled via `CancelInvoiceAsync` with a mandatory reason.
3. **Defect Identified (DEF-DE-005): Wrong Warehouse Restock on Cancellation:**
   - In `CancelInvoiceAsync` lines 643-646:
     ```csharp
     var defaultWarehouse = await dbContext.Warehouses
         .FirstOrDefaultAsync(w => w.CompanyId == companyId && w.IsActive, cancellationToken);
     ```
   - `SalesInvoiceRecord` does not persist `WarehouseId`. If an item was issued from Warehouse B, cancelling the invoice restocks it to Warehouse A (whichever warehouse is returned first).
4. **Defect Identified (DEF-DE-010): Payments Not Reversed on Invoice Cancellation:**
   - When an invoice is cancelled, the `SalesPaymentRecord` entries remain in `"Completed"` status.
   - The cashier session's `CashSalesPaise` is not adjusted, corrupting cashier closing.

---

## 6. D6. Sales Returns and Refunds Audit

### Inspected Code:
- `SalesReturnService.CreateReturnAsync` lines 813-1025
- `CreditNoteRecord` and `SalesReturnRecord`

### Verification Results:
1. **Quantity Capping:** Verified. Lines 836-845 query prior confirmed returns on the same invoice lines and reject requests where `ReturnQuantity > availableToReturn`.
2. **Resalable vs. Damaged Isolation:** Verified. If `reqLine.IsResalable` is true, stock is restocked via a `CustomerReturn` movement; if false, physical stock is not re-added to inventory.
3. **Credit Note Generation:** Formal `CreditNoteRecord` is generated with unique sequential/GUID number.
4. **Defect Identified (DEF-DE-004): Unconditional Cash Drawer Reduction on Non-Cash Returns:**
   - Line 989 unconditionally subtracts `totalRefundPaise` from `session.ExpectedCashPaise`, even if the customer paid by card/UPI or received a credit note for store credit.

---

## 7. D7. Inventory Integration Audit (DEC-ACC-002)

### Verification Results:
1. **Physical Movement Creation:** Verified. Every sale creates a `StockMovementRecord` of type `SalesIssue` with direction `Out`.
2. **DEC-ACC-002 Compliance:** Compliant. The system updates physical quantities (`QuantityOnHand`, `QuantityAvailable`) without fabricating FIFO, moving-average COGS, or valuation-dependent P&L postings.

---

## 8. D8. Cashier and Cash Closing Audit

### Inspected Code:
- `CashierSessionService` lines 1099-1378
- `CashMovementRecord`

### Verification Results:
1. **Session Lifecycle:** Open ➔ Movements (Float / Deposit / Withdrawal) ➔ Close with physical cash count.
2. **Variance Calculation:** Verified. `session.VariancePaise = request.ActualClosingCashPaise - session.ExpectedCashPaise;`.
3. **Defect:** Session closing variance is compromised by DEF-DE-004 and DEF-DE-010 (returns and cancelled invoices do not adjust cash figures accurately).

---

## 9. D9. Sales Reports Audit

### Inspected Code:
- `SalesReportingService` lines 1384-1584

### Verification Results:
1. **Cancelled Invoice Exclusion:** Verified. Invoices with `Status == "Cancelled"` are excluded from gross sales, tax, and product summaries.
2. **Refund Treatment:** Net sales correctly deducts confirmed returns: `netSales = grossSales - totalRefunds;`.
3. **DEC-ACC-002 Compliance:** Compliant. The reports strictly report Gross Turnover, Net Sales, Tax, and Discounts without fabricating gross profit or COGS.

