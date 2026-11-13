# MAGIC ERP — PHASE B: API CONTRACTS SPECIFICATION
## REST APIs for Commercial Masters & Products

**Document Version:** 1.0.0  
**Phase:** Phase B (Commercial Masters & Products)  
**Standard:** RFC 9457 Problem Details + Standard Envelope `ApiResponse<T>`  
**Base URL:** `/api/v1/commercial/`  
**Status:** APPROVED CONTRACT SPECIFICATION  

---

## 1. Global Standards & Envelope

All endpoints return the standard MAGIC envelope:
```json
{
  "success": true,
  "data": { ... },
  "meta": { "totalCount": 100, "page": 1, "pageSize": 25 },
  "correlationId": "01a1cf60-25f8-7f1d-b94f-3979828fb315",
  "timestampUtc": "2026-11-13T06:00:00Z"
}
```

---

## 2. Product Master APIs (`/api/v1/commercial/products`)

### 2.1 List Products
* **Method:** `GET /api/v1/commercial/products`
* **Permission:** `commercial:products:view`
* **Query Parameters:**
  - `search`: string (matches Name, SKU, Barcode, ProductCode)
  - `categoryId`: UUID (filter by category)
  - `brandId`: UUID (filter by brand)
  - `productType`: int (filter by type: Goods, Service, etc.)
  - `isActive`: boolean (default true)
  - `page`: int (default 1)
  - `pageSize`: int (default 25)

### 2.2 Get Product By ID
* **Method:** `GET /api/v1/commercial/products/{id}`
* **Permission:** `commercial:products:view`
* **Response:** Returns full `ProductDetailDto` with variants, barcodes, pricing tiers, category, brand, UoM.

### 2.3 Create Product
* **Method:** `POST /api/v1/commercial/products`
* **Permission:** `commercial:products:create`
* **Payload:** `CreateProductRequest`
  - `productCode`: string
  - `name`: string
  - `description`: string
  - `productType`: int
  - `categoryId`: UUID
  - `brandId`: UUID?
  - `departmentId`: UUID?
  - `unitOfMeasureId`: UUID
  - `hsnSacCode`: string
  - `taxCodeId`: UUID?
  - `gstRatePercentage`: decimal
  - `sku`: string? (auto-generated if empty)
  - `primaryBarcode`: string?
  - `purchasePricePaise`: long
  - `costPricePaise`: long
  - `sellingPricePaise`: long
  - `mrpPaise`: long
  - `minSellingPricePaise`: long
  - `wholesalePricePaise`: long
  - `reorderLevel`: decimal
  - `minStockLevel`: decimal
  - `maxStockLevel`: decimal
  - `imageUrl`: string?
  - `variants`: list of `CreateVariantDto`?
  - `barcodes`: list of `CreateBarcodeDto`?

### 2.4 Update Product
* **Method:** `PUT /api/v1/commercial/products/{id}`
* **Permission:** `commercial:products:edit`

### 2.5 Archive / Deactivate Product
* **Method:** `DELETE /api/v1/commercial/products/{id}`
* **Permission:** `commercial:products:archive`

### 2.6 Barcode Lookup
* **Method:** `GET /api/v1/commercial/products/lookup/barcode/{barcode}`
* **Permission:** `commercial:products:view`
* **Response:** Resolves product, variant, UoM, MRP, and selling price for instant POS barcode scanner hit.

---

## 3. Category, Brand & Department APIs

### 3.1 Categories (`/api/v1/commercial/categories`)
* `GET /api/v1/commercial/categories`: Hierarchical tree or flat list with product counts.
* `GET /api/v1/commercial/categories/{id}`: Category details with children.
* `POST /api/v1/commercial/categories`: Create category (validates no circularity).
* `PUT /api/v1/commercial/categories/{id}`: Update category.
* `DELETE /api/v1/commercial/categories/{id}`: Safe delete (prevented if child categories or products exist).

### 3.2 Brands (`/api/v1/commercial/brands`)
* `GET /api/v1/commercial/brands`: List brands.
* `POST /api/v1/commercial/brands`: Register brand.
* `PUT /api/v1/commercial/brands/{id}`: Update brand.
* `DELETE /api/v1/commercial/brands/{id}`: Deactivate brand.

### 3.3 Departments (`/api/v1/commercial/departments`)
* `GET /api/v1/commercial/departments`: List departments.
* `POST /api/v1/commercial/departments`: Register department.
* `PUT /api/v1/commercial/departments/{id}`: Update department.

### 3.4 Units of Measure & Tax Codes
* `GET /api/v1/commercial/units`: List standard units of measure.
* `GET /api/v1/commercial/tax-codes`: List statutory GST tax codes.

---

## 4. Customer Master APIs (`/api/v1/commercial/customers`)

### 4.1 List Customers
* **Method:** `GET /api/v1/commercial/customers`
* **Permission:** `commercial:customers:view`
* **Query Parameters:** `search`, `customerType`, `gstType`, `groupId`, `page`, `pageSize`.

### 4.2 Get Customer By ID
* **Method:** `GET /api/v1/commercial/customers/{id}`
* **Permission:** `commercial:customers:view`
* **Response:** Full customer profile with addresses, credit terms, GL account linkage.

### 4.3 Create Customer
* **Method:** `POST /api/v1/commercial/customers`
* **Permission:** `commercial:customers:manage`
* **Payload:** `CreateCustomerRequest`
  - Validates GSTIN checksum and state code if registered.
  - Automatically provisions AR control account linkage.

### 4.4 Update Customer
* **Method:** `PUT /api/v1/commercial/customers/{id}`
* **Permission:** `commercial:customers:manage`

### 4.5 Update Credit Limit
* **Method:** `PUT /api/v1/commercial/customers/{id}/credit-limit`
* **Permission:** `commercial:credit:approve`
* **Payload:** `UpdateCreditLimitRequest` (NewCreditLimitPaise, CreditPeriodDays, Reason).

---

## 5. Supplier Master APIs (`/api/v1/commercial/suppliers`)

### 5.1 List Suppliers
* **Method:** `GET /api/v1/commercial/suppliers`
* **Permission:** `commercial:suppliers:view`

### 5.2 Get Supplier By ID
* **Method:** `GET /api/v1/commercial/suppliers/{id}`
* **Permission:** `commercial:suppliers:view`
* **Response:** Bank accounts show masked account numbers (`•••• •••• 1234`).

### 5.3 Get Unmasked Bank Account Details
* **Method:** `GET /api/v1/commercial/suppliers/{id}/bank-accounts/{bankAccountId}/unmasked`
* **Permission:** `commercial:suppliers:bank-details`
* **Security:** Emits `BANK_ACCOUNT_UNMASKED` security audit event.

### 5.4 Create Supplier
* **Method:** `POST /api/v1/commercial/suppliers`
* **Permission:** `commercial:suppliers:manage`
* **Payload:** `CreateSupplierRequest`
  - Encrypts bank account number before persistence.
  - Automatically provisions AP control account linkage.

### 5.5 Update Supplier
* **Method:** `PUT /api/v1/commercial/suppliers/{id}`
* **Permission:** `commercial:suppliers:manage`

