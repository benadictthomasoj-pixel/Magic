# MAGIC — API ARCHITECTURE & CONTRACT SPECIFICATION

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  

---

## 1. RESTful API Design Standards

MAGIC exposes an ASP.NET Core (.NET 10 LTS) backend Web API following RESTful resource-oriented design principles with OpenAPI documentation.

- **URL Prefix:** `/api/v1/`
- **Data Exchange Format:** UTF-8 JSON.
- **Date/Time Representation:** ISO 8601 with UTC offset (`2026-11-11T10:30:00.000Z`).
- **Identifiers:** Canonical UUIDv7 string representation (`019318f2-39c2-7489-8d5a-5264b3c95972`).
- **Currency Amounts:** Integer paise (`gross_amount_paise: 100000` = ₹1,000.00).

---

## 2. Standard Request Headers

Every API request from the Angular frontend (or local terminal) must supply the following headers:

| Header Name | Type | Description |
|---|---|---|
| `Authorization` | `Bearer <token>` | Signed Ed25519 JWT carrying user claims and permissions. |
| `X-Branch-Id` | `UUID` | Active operational branch context. |
| `X-Terminal-Id` | `UUID` | Hardware terminal identity token. |
| `Idempotency-Key` | `UUID` *(Optional for GET; Mandatory for critical POST/PUT)* | Unique idempotency key to prevent double-billing or duplicate posting. |
| `X-Correlation-Id` | `UUID` | Distributed tracing correlation ID for log aggregation. |

---

## 3. Standard Response Envelopes

### 3.1 Successful Response (`200 OK`, `201 Created`)
```json
{
  "success": true,
  "data": { ... },
  "meta": {
    "page": 1,
    "pageSize": 50,
    "totalCount": 1240
  },
  "correlationId": "019318f2-39c2-7489-8d5a-5264b3c95972",
  "timestamp": "2026-11-11T10:30:00.000Z"
}
```

### 3.2 Error Response (RFC 7807 Problem Details)
```json
{
  "success": false,
  "error": {
    "type": "https://magic.local/errors/financial/unbalanced-voucher",
    "title": "Unbalanced Journal Voucher Error",
    "status": 422,
    "detail": "The sum of debits (150000 paise) does not equal the sum of credits (145000 paise). Delta: 5000 paise.",
    "errorCode": "ACC_VOUCHER_UNBALANCED",
    "invalidFields": [
      {
        "field": "lines",
        "message": "Debit and credit lines must balance exactly."
      }
    ]
  },
  "correlationId": "019318f2-39c2-7489-8d5a-5264b3c95972",
  "timestamp": "2026-11-11T10:30:00.000Z"
}
```

---

## 4. Key Domain API Endpoints

### 4.1 POS & Fast Billing
- `POST /api/v1/pos/sessions/open` — Open counter shift with declared cash float.
- `POST /api/v1/pos/checkout` — Atomic retail checkout (Idempotent).
- `POST /api/v1/pos/carts/hold` — Stash active customer cart.
- `GET /api/v1/pos/carts/held` — Retrieve list of parked carts.
- `POST /api/v1/pos/sessions/close` — Finalize counter shift (X/Z Report generation).

### 4.2 Inventory Control
- `GET /api/v1/inventory/items/lookup?q={query}` — Sub-50ms barcode / SKU lookup.
- `GET /api/v1/inventory/items/{id}/stock` — Real-time warehouse balance and ATP (Available to Promise).
- `POST /api/v1/inventory/transfers` — Initiate inter-branch stock transfer.
- `POST /api/v1/inventory/transfers/{id}/receive` — Inward receipt of transit stock.

### 4.3 Accounting & General Ledger
- `POST /api/v1/accounting/vouchers` — Post manual or integrated journal voucher.
- `POST /api/v1/accounting/vouchers/{id}/reverse` — Execute immutable reversal voucher.
- `GET /api/v1/accounting/general-ledger?accountId={id}&from={date}&to={date}` — Account ledger.
- `GET /api/v1/accounting/trial-balance?asOfDate={date}` — Trial balance computation.
- `POST /api/v1/accounting/periods/lock` — Lock closed fiscal period.

### 4.4 Automation: Auto Stock Update
- `POST /api/v1/automation/auto-stock/stage` — Ingest raw parsed invoice payload into staging workspace.
- `GET /api/v1/automation/auto-stock/staged/{id}` — Retrieve staged invoice with validation discrepancies.
- `PUT /api/v1/automation/auto-stock/staged/{id}` — Update user corrections in staging grid.
- `POST /api/v1/automation/auto-stock/staged/{id}/commit` — Execute verified atomic purchase, inventory inward, and accounting posting.

