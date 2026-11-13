# MAGIC ERP — Phase C: REST API Contracts Specification

All Phase C APIs follow the standard MAGIC API envelope:
```json
{
  "success": true,
  "data": { ... },
  "meta": { "totalCount": 100, "page": 1, "pageSize": 20 },
  "correlationId": "...",
  "timestampUtc": "2026-11-13T..."
}
```

---

## 1. Warehouse Management APIs (`/api/v1/inventory/warehouses`)
- `GET /api/v1/inventory/warehouses` — List warehouses with search, active status filter, and pagination.
- `GET /api/v1/inventory/warehouses/{id}` — Get warehouse details including branch assignments and storage locations.
- `POST /api/v1/inventory/warehouses` — Create warehouse.
- `PUT /api/v1/inventory/warehouses/{id}` — Update warehouse.
- `POST /api/v1/inventory/warehouses/{id}/archive` — Archive/deactivate warehouse.
- `GET /api/v1/inventory/warehouses/{id}/locations` — List storage locations (zones, aisles, racks, shelves, bins).
- `POST /api/v1/inventory/warehouses/{id}/locations` — Create storage location.

---

## 2. Inventory & Stock Balances APIs (`/api/v1/inventory/stock`)
- `GET /api/v1/inventory/stock/balances` — Query stock balances by product, warehouse, branch, batch.
- `GET /api/v1/inventory/stock/movements` — Query immutable movement history with filters.
- `GET /api/v1/inventory/stock/product/{productId}` — Product-wise stock distribution across all warehouses.
- `POST /api/v1/inventory/stock/opening-stock` — Post initial opening stock movements.

---

## 3. Stock Adjustments APIs (`/api/v1/inventory/adjustments`)
- `GET /api/v1/inventory/adjustments` — List stock adjustments with status and reason filters.
- `GET /api/v1/inventory/adjustments/{id}` — Get adjustment details with lines.
- `POST /api/v1/inventory/adjustments` — Create draft stock adjustment.
- `POST /api/v1/inventory/adjustments/{id}/approve` — Approve and execute stock adjustment movements.
- `POST /api/v1/inventory/adjustments/{id}/reject` — Reject adjustment.

---

## 4. Stock Counts APIs (`/api/v1/inventory/counts`)
- `GET /api/v1/inventory/counts` — List physical stock-count sessions.
- `GET /api/v1/inventory/counts/{id}` — Get stock-count details with counted lines and discrepancies.
- `POST /api/v1/inventory/counts` — Create stock-count session.
- `POST /api/v1/inventory/counts/{id}/record-lines` — Record counted quantities.
- `POST /api/v1/inventory/counts/{id}/reconcile` — Approve and post reconciliation adjustment movements.

---

## 5. Purchase Orders APIs (`/api/v1/procurement/purchase-orders`)
- `GET /api/v1/procurement/purchase-orders` — List POs with status and supplier filters.
- `GET /api/v1/procurement/purchase-orders/{id}` — Get PO details with line items and GST breakdown.
- `POST /api/v1/procurement/purchase-orders` — Create PO.
- `PUT /api/v1/procurement/purchase-orders/{id}` — Update draft PO.
- `POST /api/v1/procurement/purchase-orders/{id}/approve` — Approve PO.
- `POST /api/v1/procurement/purchase-orders/{id}/cancel` — Cancel PO.

---

## 6. Goods Receipt Notes APIs (`/api/v1/procurement/goods-receipts`)
- `GET /api/v1/procurement/goods-receipts` — List GRNs.
- `GET /api/v1/procurement/goods-receipts/{id}` — Get GRN details with lines.
- `POST /api/v1/procurement/goods-receipts` — Create draft GRN (linked to PO or direct).
- `POST /api/v1/procurement/goods-receipts/{id}/confirm` — Atomically confirm receipt, update PO received quantities, and post inventory movements.

---

## 7. Purchase Returns APIs (`/api/v1/procurement/purchase-returns`)
- `GET /api/v1/procurement/purchase-returns` — List purchase returns.
- `GET /api/v1/procurement/purchase-returns/{id}` — Get return details.
- `POST /api/v1/procurement/purchase-returns` — Create draft purchase return.
- `POST /api/v1/procurement/purchase-returns/{id}/confirm` — Confirm return and deduct inventory.

---

## 8. Stock Transfers APIs (`/api/v1/inventory/transfers`)
- `GET /api/v1/inventory/transfers` — List stock transfers.
- `GET /api/v1/inventory/transfers/{id}` — Get transfer details.
- `POST /api/v1/inventory/transfers` — Create transfer request.
- `POST /api/v1/inventory/transfers/{id}/dispatch` — Dispatch stock from source warehouse.
- `POST /api/v1/inventory/transfers/{id}/receive` — Confirm receipt at destination warehouse.
- `POST /api/v1/inventory/transfers/{id}/cancel` — Cancel transfer (if not yet dispatched).

---

## 9. Reorder Management APIs (`/api/v1/inventory/reorder`)
- `GET /api/v1/inventory/reorder/rules` — List reorder rules.
- `POST /api/v1/inventory/reorder/rules` — Create/update reorder rule.
- `GET /api/v1/inventory/reorder/suggestions` — Generate reorder suggestions based on current stock vs. reorder points.

