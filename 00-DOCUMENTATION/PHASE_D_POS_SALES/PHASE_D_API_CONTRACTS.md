# MAGIC ERP — Phase D: POS & Sales API Contracts

**Document:** `00-DOCUMENTATION/PHASE_D_POS_SALES/PHASE_D_API_CONTRACTS.md`  
**Base URL:** `/api/v1`  
**Envelope:** `ApiResponse<T>` with correlation IDs and RFC-7807 problem details  

---

## 1. POS Operations (`/api/v1/pos`)
- `GET /api/v1/pos/lookup?query={term}` — Fast search by Barcode, SKU, or product name with real-time stock balance.
- `POST /api/v1/pos/checkout` — Atomic POS checkout: validates stock, creates invoice, records payments, deducts inventory, updates cashier session.
  - Request: `CreatePosSaleRequest`
  - Response: `SalesInvoiceDto`

## 2. Sales Invoices (`/api/v1/sales/invoices`)
- `GET /api/v1/sales/invoices` — Paginated sales invoice directory with filters (`searchTerm`, `status`, `customerId`, `cashierUserId`, `fromDate`, `toDate`, `page`, `pageSize`).
- `GET /api/v1/sales/invoices/{id}` — Full invoice details with line items and payment receipts.
- `POST /api/v1/sales/invoices/{id}/cancel` — Cancel invoice with inventory replenishment.

## 3. Payments (`/api/v1/sales/payments`)
- `POST /api/v1/sales/payments` — Record additional/split payments against existing unpaid/partial invoices.

## 4. Sales Returns & Credit Notes (`/api/v1/sales/returns`)
- `GET /api/v1/sales/returns` — List sales returns.
- `GET /api/v1/sales/returns/{id}` — Get return details.
- `POST /api/v1/sales/returns` — Create return request against original invoice.
- `POST /api/v1/sales/returns/{id}/confirm` — Confirm return, restock resalable items, and generate credit note.
- `GET /api/v1/sales/credit-notes` — List credit notes.

## 5. Cashier Sessions (`/api/v1/cashier/sessions`)
- `GET /api/v1/cashier/sessions/active` — Get currently active session for logged-in cashier.
- `POST /api/v1/cashier/sessions/open` — Open session with starting float.
- `POST /api/v1/cashier/sessions/{id}/movements` — Record cash deposit or withdrawal drop.
- `POST /api/v1/cashier/sessions/{id}/close` — Submit physical count and close register.
- `POST /api/v1/cashier/sessions/{id}/reconcile` — Supervisor approval of variance.

## 6. Sales Reports (`/api/v1/sales/reports`)
- `GET /api/v1/sales/reports/summary?fromDate={d}&toDate={d}` — Overall sales metrics (Gross sales, Invoices, Average bill, Net revenue).
- `GET /api/v1/sales/reports/by-payment-method` — Breakdown by Cash, UPI, Card, Customer Credit.
- `GET /api/v1/sales/reports/by-product` — Top-selling products and category volume.
- `GET /api/v1/sales/reports/hourly` — Hourly sales distribution for staffing and peak volume analysis.
