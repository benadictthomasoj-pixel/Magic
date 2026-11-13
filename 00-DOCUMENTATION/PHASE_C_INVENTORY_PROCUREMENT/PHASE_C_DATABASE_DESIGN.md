# MAGIC ERP — Phase C: Database Schema Design (PostgreSQL 18)

**Schema:** `inventory`  
**Database Engine:** PostgreSQL 18  
**ORM:** Entity Framework Core 10  
**Primary Keys:** UUID Version 7 (`Guid.CreateVersion7()`)  
**Currency Standard:** 64-bit integer paise (`BIGINT`), zero floating point  
**Quantity Standard:** High-precision decimal (`numeric(14,4)`)  

---

## 1. Schema Tables Overview

The `inventory` schema encapsulates 13 primary relational tables and link tables:

1. `inventory.warehouses` — Physical warehouses and logistics centers.
2. `inventory.warehouse_branch_assignments` — Many-to-many branch authorization mappings.
3. `inventory.storage_locations` — Hierarchical locations (Zone, Aisle, Rack, Shelf, Bin).
4. `inventory.product_storage_locations` — Preferred picking and storage bins per product.
5. `inventory.stock_balances` — Real-time atomic stock projection by product, warehouse, location, and batch.
6. `inventory.stock_movements` — Immutable append-only audit ledger of every inventory event.
7. `inventory.stock_adjustments` — Header for manual stock corrections, damages, and reconciliations.
8. `inventory.stock_adjustment_lines` — Itemized quantity delta with mandatory business justification.
9. `inventory.stock_counts` — Physical stock counting and stocktake auditing sessions.
10. `inventory.stock_count_lines` — Counted vs. system quantity discrepancy comparisons.
11. `inventory.purchase_orders` — Commercial procurement orders with suppliers.
12. `inventory.purchase_order_lines` — Ordered line items with paise pricing and GST breakdown.
13. `inventory.goods_receipt_notes` — Formal inbound receipt documents against PO or direct delivery.
14. `inventory.goods_receipt_lines` — Accepted, rejected, and damaged goods counts with batch/expiry.
15. `inventory.purchase_returns` — Inbound goods return to vendor.
16. `inventory.purchase_return_lines` — Return quantities and debit/credit references.
17. `inventory.stock_transfers` — Inter-warehouse and inter-branch transfer headers.
18. `inventory.stock_transfer_lines` — Transfer quantities, dispatched, received, and damaged counts.
19. `inventory.reorder_rules` — Min/max safety stock, reorder points, and lead time configurations.

---

## 2. Table Definitions & Constraints

### 2.1 `inventory.warehouses`
```sql
CREATE TABLE inventory.warehouses (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "Code" varchar(50) NOT NULL,
    "Name" varchar(200) NOT NULL,
    "Description" text,
    "AddressLine1" varchar(250) NOT NULL,
    "AddressLine2" varchar(250),
    "City" varchar(100) NOT NULL,
    "State" varchar(100) NOT NULL,
    "StateCode" varchar(10) NOT NULL,
    "PinCode" varchar(20) NOT NULL,
    "Country" varchar(100) NOT NULL DEFAULT 'India',
    "Phone" varchar(50),
    "Email" varchar(256),
    "ManagerName" varchar(200),
    "CapacitySqFt" numeric(12,2),
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAtUtc" timestamptz NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "UpdatedAtUtc" timestamptz,
    "UpdatedByUserId" uuid,
    CONSTRAINT "UQ_warehouses_CompanyId_Code" UNIQUE ("CompanyId", "Code")
);
```

### 2.2 `inventory.warehouse_branch_assignments`
```sql
CREATE TABLE inventory.warehouse_branch_assignments (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "WarehouseId" uuid NOT NULL REFERENCES inventory.warehouses("Id") ON DELETE CASCADE,
    "BranchId" uuid NOT NULL,
    "IsPrimary" boolean NOT NULL DEFAULT false,
    "AssignedAtUtc" timestamptz NOT NULL,
    CONSTRAINT "UQ_wh_branch_Company_Wh_Branch" UNIQUE ("CompanyId", "WarehouseId", "BranchId")
);
```

### 2.3 `inventory.storage_locations`
```sql
CREATE TABLE inventory.storage_locations (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "WarehouseId" uuid NOT NULL REFERENCES inventory.warehouses("Id") ON DELETE CASCADE,
    "Code" varchar(50) NOT NULL,
    "Name" varchar(100) NOT NULL,
    "LocationType" varchar(30) NOT NULL, -- Zone, Aisle, Rack, Shelf, Bin
    "ParentLocationId" uuid REFERENCES inventory.storage_locations("Id") ON DELETE RESTRICT,
    "Barcode" varchar(100),
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAtUtc" timestamptz NOT NULL,
    CONSTRAINT "UQ_storage_locations_Warehouse_Code" UNIQUE ("WarehouseId", "Code")
);
```

### 2.4 `inventory.stock_balances`
```sql
CREATE TABLE inventory.stock_balances (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "WarehouseId" uuid NOT NULL REFERENCES inventory.warehouses("Id") ON DELETE RESTRICT,
    "ProductId" uuid NOT NULL REFERENCES commercial.products("Id") ON DELETE RESTRICT,
    "VariantId" uuid REFERENCES commercial.product_variants("Id") ON DELETE RESTRICT,
    "StorageLocationId" uuid REFERENCES inventory.storage_locations("Id") ON DELETE RESTRICT,
    "BatchNumber" varchar(100),
    "ExpiryDate" date,
    "QuantityOnHand" numeric(14,4) NOT NULL DEFAULT 0,
    "QuantityReserved" numeric(14,4) NOT NULL DEFAULT 0,
    "QuantityAvailable" numeric(14,4) NOT NULL DEFAULT 0,
    "QuantityDamaged" numeric(14,4) NOT NULL DEFAULT 0,
    "QuantityExpired" numeric(14,4) NOT NULL DEFAULT 0,
    "QuantityBlocked" numeric(14,4) NOT NULL DEFAULT 0,
    "UnitCostPaise" bigint NOT NULL DEFAULT 0,
    "LastMovementAtUtc" timestamptz NOT NULL,
    "RowVersion" bytea NOT NULL,
    CONSTRAINT "CHK_stock_balances_non_negative_on_hand" CHECK ("QuantityOnHand" >= 0),
    CONSTRAINT "CHK_stock_balances_non_negative_available" CHECK ("QuantityAvailable" >= 0)
);

CREATE INDEX "IX_stock_balances_lookup" ON inventory.stock_balances 
("CompanyId", "WarehouseId", "ProductId", "VariantId", "StorageLocationId", "BatchNumber");
```

### 2.5 `inventory.stock_movements` (Immutable Event Ledger)
```sql
CREATE TABLE inventory.stock_movements (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "WarehouseId" uuid NOT NULL REFERENCES inventory.warehouses("Id") ON DELETE RESTRICT,
    "ProductId" uuid NOT NULL REFERENCES commercial.products("Id") ON DELETE RESTRICT,
    "VariantId" uuid REFERENCES commercial.product_variants("Id") ON DELETE RESTRICT,
    "StorageLocationId" uuid REFERENCES inventory.storage_locations("Id") ON DELETE RESTRICT,
    "MovementType" varchar(50) NOT NULL,
    "Direction" varchar(10) NOT NULL, -- In, Out
    "Quantity" numeric(14,4) NOT NULL,
    "BatchNumber" varchar(100),
    "ExpiryDate" date,
    "UnitCostPaise" bigint NOT NULL DEFAULT 0,
    "TotalCostPaise" bigint NOT NULL DEFAULT 0,
    "ReferenceType" varchar(50) NOT NULL,
    "ReferenceId" uuid,
    "ReferenceNumber" varchar(100),
    "Reason" text,
    "BalanceAfterOnHand" numeric(14,4) NOT NULL,
    "PerformedByUserId" uuid NOT NULL,
    "MovementDateUtc" timestamptz NOT NULL,
    "CreatedAtUtc" timestamptz NOT NULL
);

CREATE INDEX "IX_stock_movements_audit" ON inventory.stock_movements 
("CompanyId", "WarehouseId", "ProductId", "MovementDateUtc" DESC);
```

### 2.6 `inventory.purchase_orders` & Lines
```sql
CREATE TABLE inventory.purchase_orders (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "WarehouseId" uuid NOT NULL REFERENCES inventory.warehouses("Id") ON DELETE RESTRICT,
    "OrderNumber" varchar(50) NOT NULL,
    "SupplierId" uuid NOT NULL REFERENCES commercial.suppliers("Id") ON DELETE RESTRICT,
    "OrderDateUtc" timestamptz NOT NULL,
    "ExpectedDeliveryDate" date,
    "Status" varchar(30) NOT NULL DEFAULT 'Draft',
    "SubtotalPaise" bigint NOT NULL DEFAULT 0,
    "DiscountPaise" bigint NOT NULL DEFAULT 0,
    "TaxableAmountPaise" bigint NOT NULL DEFAULT 0,
    "CgstPaise" bigint NOT NULL DEFAULT 0,
    "SgstPaise" bigint NOT NULL DEFAULT 0,
    "IgstPaise" bigint NOT NULL DEFAULT 0,
    "CessPaise" bigint NOT NULL DEFAULT 0,
    "RoundOffPaise" bigint NOT NULL DEFAULT 0,
    "TotalAmountPaise" bigint NOT NULL DEFAULT 0,
    "PaymentTerms" varchar(100) NOT NULL DEFAULT 'Net 30',
    "Notes" text,
    "CreatedByUserId" uuid NOT NULL,
    "ApprovedByUserId" uuid,
    "ApprovedAtUtc" timestamptz,
    "CancelledByUserId" uuid,
    "CancelledAtUtc" timestamptz,
    "CancellationReason" text,
    "CreatedAtUtc" timestamptz NOT NULL,
    "UpdatedAtUtc" timestamptz,
    CONSTRAINT "UQ_purchase_orders_CompanyId_OrderNumber" UNIQUE ("CompanyId", "OrderNumber")
);

CREATE TABLE inventory.purchase_order_lines (
    "Id" uuid PRIMARY KEY,
    "PurchaseOrderId" uuid NOT NULL REFERENCES inventory.purchase_orders("Id") ON DELETE CASCADE,
    "ProductId" uuid NOT NULL REFERENCES commercial.products("Id") ON DELETE RESTRICT,
    "VariantId" uuid REFERENCES commercial.product_variants("Id") ON DELETE RESTRICT,
    "OrderedQuantity" numeric(14,4) NOT NULL,
    "ReceivedQuantity" numeric(14,4) NOT NULL DEFAULT 0,
    "CancelledQuantity" numeric(14,4) NOT NULL DEFAULT 0,
    "UnitPricePaise" bigint NOT NULL DEFAULT 0,
    "DiscountPercentage" numeric(6,2) NOT NULL DEFAULT 0,
    "DiscountPaise" bigint NOT NULL DEFAULT 0,
    "TaxCodeId" uuid REFERENCES commercial.tax_codes("Id"),
    "GstRatePercentage" numeric(5,2) NOT NULL DEFAULT 0,
    "TaxableAmountPaise" bigint NOT NULL DEFAULT 0,
    "CgstPaise" bigint NOT NULL DEFAULT 0,
    "SgstPaise" bigint NOT NULL DEFAULT 0,
    "IgstPaise" bigint NOT NULL DEFAULT 0,
    "TotalLineAmountPaise" bigint NOT NULL DEFAULT 0
);
```

### 2.7 `inventory.goods_receipt_notes` & Lines
```sql
CREATE TABLE inventory.goods_receipt_notes (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "WarehouseId" uuid NOT NULL REFERENCES inventory.warehouses("Id") ON DELETE RESTRICT,
    "GrnNumber" varchar(50) NOT NULL,
    "PurchaseOrderId" uuid REFERENCES inventory.purchase_orders("Id") ON DELETE RESTRICT,
    "SupplierId" uuid NOT NULL REFERENCES commercial.suppliers("Id") ON DELETE RESTRICT,
    "SupplierInvoiceNumber" varchar(100),
    "SupplierInvoiceDate" date,
    "ReceiptDateUtc" timestamptz NOT NULL,
    "Status" varchar(30) NOT NULL DEFAULT 'Draft',
    "Notes" text,
    "CreatedByUserId" uuid NOT NULL,
    "ConfirmedByUserId" uuid,
    "ConfirmedAtUtc" timestamptz,
    "CreatedAtUtc" timestamptz NOT NULL,
    "UpdatedAtUtc" timestamptz,
    CONSTRAINT "UQ_goods_receipt_notes_CompanyId_GrnNumber" UNIQUE ("CompanyId", "GrnNumber")
);

CREATE TABLE inventory.goods_receipt_lines (
    "Id" uuid PRIMARY KEY,
    "GoodsReceiptNoteId" uuid NOT NULL REFERENCES inventory.goods_receipt_notes("Id") ON DELETE CASCADE,
    "PurchaseOrderLineId" uuid REFERENCES inventory.purchase_order_lines("Id") ON DELETE RESTRICT,
    "ProductId" uuid NOT NULL REFERENCES commercial.products("Id") ON DELETE RESTRICT,
    "VariantId" uuid REFERENCES commercial.product_variants("Id") ON DELETE RESTRICT,
    "StorageLocationId" uuid REFERENCES inventory.storage_locations("Id") ON DELETE RESTRICT,
    "ReceivedQuantity" numeric(14,4) NOT NULL,
    "AcceptedQuantity" numeric(14,4) NOT NULL,
    "RejectedQuantity" numeric(14,4) NOT NULL DEFAULT 0,
    "DamagedQuantity" numeric(14,4) NOT NULL DEFAULT 0,
    "BatchNumber" varchar(100),
    "ExpiryDate" date,
    "UnitPricePaise" bigint NOT NULL DEFAULT 0,
    "TaxableAmountPaise" bigint NOT NULL DEFAULT 0,
    "CgstPaise" bigint NOT NULL DEFAULT 0,
    "SgstPaise" bigint NOT NULL DEFAULT 0,
    "IgstPaise" bigint NOT NULL DEFAULT 0,
    "TotalLineAmountPaise" bigint NOT NULL DEFAULT 0,
    "RejectionReason" text
);
```

### 2.8 `inventory.stock_transfers` & Lines
```sql
CREATE TABLE inventory.stock_transfers (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "TransferNumber" varchar(50) NOT NULL,
    "SourceBranchId" uuid NOT NULL,
    "SourceWarehouseId" uuid NOT NULL REFERENCES inventory.warehouses("Id") ON DELETE RESTRICT,
    "DestinationBranchId" uuid NOT NULL,
    "DestinationWarehouseId" uuid NOT NULL REFERENCES inventory.warehouses("Id") ON DELETE RESTRICT,
    "Status" varchar(30) NOT NULL DEFAULT 'Draft',
    "TransferDateUtc" timestamptz NOT NULL,
    "DispatchedAtUtc" timestamptz,
    "DispatchedByUserId" uuid,
    "ReceivedAtUtc" timestamptz,
    "ReceivedByUserId" uuid,
    "TrackingNumber" varchar(100),
    "CarrierName" varchar(100),
    "Notes" text,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAtUtc" timestamptz NOT NULL,
    "UpdatedAtUtc" timestamptz,
    CONSTRAINT "UQ_stock_transfers_CompanyId_TransferNumber" UNIQUE ("CompanyId", "TransferNumber")
);

CREATE TABLE inventory.stock_transfer_lines (
    "Id" uuid PRIMARY KEY,
    "StockTransferId" uuid NOT NULL REFERENCES inventory.stock_transfers("Id") ON DELETE CASCADE,
    "ProductId" uuid NOT NULL REFERENCES commercial.products("Id") ON DELETE RESTRICT,
    "VariantId" uuid REFERENCES commercial.product_variants("Id") ON DELETE RESTRICT,
    "SourceStorageLocationId" uuid REFERENCES inventory.storage_locations("Id") ON DELETE RESTRICT,
    "DestinationStorageLocationId" uuid REFERENCES inventory.storage_locations("Id") ON DELETE RESTRICT,
    "RequestedQuantity" numeric(14,4) NOT NULL,
    "DispatchedQuantity" numeric(14,4) NOT NULL DEFAULT 0,
    "ReceivedQuantity" numeric(14,4) NOT NULL DEFAULT 0,
    "DamagedQuantity" numeric(14,4) NOT NULL DEFAULT 0,
    "MissingQuantity" numeric(14,4) NOT NULL DEFAULT 0,
    "UnitCostPaise" bigint NOT NULL DEFAULT 0
);
```

### 2.9 `inventory.reorder_rules`
```sql
CREATE TABLE inventory.reorder_rules (
    "Id" uuid PRIMARY KEY,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid,
    "WarehouseId" uuid REFERENCES inventory.warehouses("Id") ON DELETE CASCADE,
    "ProductId" uuid NOT NULL REFERENCES commercial.products("Id") ON DELETE CASCADE,
    "VariantId" uuid REFERENCES commercial.product_variants("Id") ON DELETE CASCADE,
    "PreferredSupplierId" uuid REFERENCES commercial.suppliers("Id") ON DELETE SET NULL,
    "MinStockLevel" numeric(14,4) NOT NULL DEFAULT 0,
    "MaxStockLevel" numeric(14,4) NOT NULL DEFAULT 0,
    "ReorderPoint" numeric(14,4) NOT NULL DEFAULT 0,
    "SafetyStock" numeric(14,4) NOT NULL DEFAULT 0,
    "LeadTimeDays" int NOT NULL DEFAULT 7,
    "ReorderQuantity" numeric(14,4) NOT NULL DEFAULT 0,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAtUtc" timestamptz NOT NULL,
    "UpdatedAtUtc" timestamptz
);
```

