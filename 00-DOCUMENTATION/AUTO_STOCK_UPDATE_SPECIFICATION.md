# MAGIC — AUTO STOCK UPDATE SPECIFICATION

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  

---

## 1. Executive Purpose & Workflow Overview

**Auto Stock Update** is an advanced operational workflow designed to eliminate manual data entry of wholesale purchase invoices while maintaining strict inventory and financial controls.

```
+----------------------------------------------------------------------------------------------------+
| 1. Physical / PDF Wholesale Purchase Invoice                                                        |
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
| 2. External / Local AI Extraction converts invoice to Standard MAGIC Ingestion JSON                 |
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
| 3. User Pastes Structured Payload into Auto Stock Update Workspace                                 |
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
| 4. MAGIC Ingestion Engine (Validation, Supplier Matching, Barcode/SKU Reconciliation)               |
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
| 5. Interactive Review & Reconciliation Grid Tab (Discrepancies flagged, manual corrections made)   |
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
| 6. User Explicit "Commit Purchase" Action                                                          |
|    - Single Atomic PostgreSQL Transaction:                                                         |
|      * Purchase Invoice Created                                                                    |
|      * Inventory Stock Lots & Ledger Inwarded                                                      |
|      * General Ledger Double-Entry Journal Posted                                                  |
|      * Immutable Audit Trail Logged                                                                |
+----------------------------------------------------------------------------------------------------+
```

---

## 2. Ingestion JSON Schema Specification

External AI or OCR tools parse the wholesale invoice into the following normalized JSON interchange format:

```json
{
  "$schema": "https://magic.local/schemas/v1/auto-stock-ingest.json",
  "invoiceHeader": {
    "supplierInvoiceNumber": "INV-2026-9842",
    "invoiceDate": "2026-11-10",
    "supplier": {
      "gstin": "27AABCU9603R1ZM",
      "legalName": "Apex Wholesale Distributors Pvt Ltd",
      "tradeName": "Apex Wholesale",
      "address": "Gala 12, Industrial Estate, Pune, Maharashtra",
      "stateCode": "27"
    },
    "currency": "INR",
    "totalTaxableAmountPaise": 8474600,
    "totalCgstPaise": 211865,
    "totalSgstPaise": 211865,
    "totalIgstPaise": 0,
    "roundOffPaise": -30,
    "grandTotalPaise": 8898300
  },
  "lineItems": [
    {
      "lineIndex": 1,
      "rawDescription": "Basmati Rice 1121 Premium 25kg",
      "barcode": "8901030855214",
      "sku": "RIC-1121-25KG",
      "hsnCode": "10063020",
      "quantity": 40.0,
      "unitCode": "BAG",
      "batchNumber": "B2611-04",
      "manufacturingDate": "2026-10-15",
      "expiryDate": "2027-10-14",
      "purchaseRatePaise": 211865,
      "mrpPaise": 280000,
      "sellingPricePaise": 255000,
      "taxableAmountPaise": 8474600,
      "gstRateBps": 500,
      "cgstPaise": 211865,
      "sgstPaise": 211865,
      "igstPaise": 0,
      "lineTotalPaise": 8898330
    }
  ]
}
```

---

## 3. Mandatory Safeguards & Verification Protocols

1. **SKU & Barcode First Matching:**
   - The engine queries the item master by `barcode` first, then `sku`, then normalized `product_code`.
   - If an item does not exist, the system **never silently creates a new product**. It presents an amber alert: *"Unrecognized Item: Map to Existing Product or Authorize New Product Creation"*.
2. **Duplicate Invoice Detection:**
   - Checks `purchases.purchase_invoices` for matching `(supplier_id, supplier_invoice_number)`.
   - If found, ingestion halts with a hard error preventing double stock entry.
3. **Three-Way Price Separation:**
   - The system strictly distinguishes between:
     - `purchase_rate_paise`: Landed cost basis for inventory valuation.
     - `mrp_paise`: Maximum Retail Price printed on consumer packaging.
     - `selling_price_paise`: Active counter selling price for POS checkout.
4. **Tax & Line-Item Sum Reconciliation:**
   - Evaluates:
     $$\sum \text{LineItemTaxablePaise} \stackrel{?}{=} \text{HeaderTaxableAmountPaise}$$
     $$\sum (\text{LineItemTaxable} + \text{CGST} + \text{SGST} + \text{IGST}) + \text{RoundOff} \stackrel{?}{=} \text{GrandTotalPaise}$$
   - Any mathematical variance $> \pm 100 \text{ paise}$ is flagged as a blocking discrepancy.
5. **Human-in-the-Loop Isolation:**
   - Staged data resides in `automation.staged_purchase_invoices`.
   - **Zero stock changes** and **zero financial voucher postings** occur until the operator clicks "Commit Purchase".

