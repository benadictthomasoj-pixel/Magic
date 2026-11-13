# MAGIC ERP — PHASE B: DATABASE DESIGN SPECIFICATION
## Schema: `commercial` | PostgreSQL 18 & EF Core 10

**Document Version:** 1.0.0  
**Phase:** Phase B (Commercial Masters & Products)  
**Database Engine:** PostgreSQL 18.0  
**ORM:** Entity Framework Core 10.0.15  
**Status:** APPROVED ARCHITECTURE BASELINE  

---

## 1. Schema Overview

All commercial master entities are isolated in a dedicated PostgreSQL schema: `commercial`. This ensures clean separation from `accounting`, `security`, and `platform` schemas while allowing foreign-key relationships to `security.users`, `security.companies`, `security.branches`, and `accounting.accounts`.

### Table Summary:
1. `commercial.departments`: Operational organizational departments (e.g., Grocery, Electronics, Apparel, Pharmacy).
2. `commercial.categories`: Multi-level product categories with parent-child relationships and circularity guards.
3. `commercial.brands`: Manufacturer or commercial product brands.
4. `commercial.units_of_measure`: Standard units (PCS, KGS, LTR, MTR, BOX, etc.) with decimal precision.
5. `commercial.tax_codes`: Statutory GST tax codes with breakdown (CGST, SGST, IGST, Cess).
6. `commercial.products`: Master product catalog with SKU, primary barcode, HSN, integer paise pricing, and stock limits.
7. `commercial.product_variants`: Multi-attribute variants (Size, Color, Volume, etc.) with dedicated SKUs and prices.
8. `commercial.product_barcodes`: Multiple barcode aliases per product and variant.
9. `commercial.product_prices`: Branch-specific, tier-specific (Retail, Wholesale), and volume-based price lists.
10. `commercial.product_price_history`: Immutable audit trail of price changes.
11. `commercial.customer_groups`: Tiered customer classifications with default discounts and credit terms.
12. `commercial.customers`: B2B and B2C customer profiles with GSTIN, PAN, credit limit, and AR control account mapping.
13. `commercial.customer_addresses`: Multi-address support (Billing, Shipping) with state code mapping.
14. `commercial.suppliers`: Vendor directory with category, GSTIN, PAN, MSME/Udyam details, and AP control account mapping.
15. `commercial.supplier_addresses`: Registered office and warehouse supplier locations.
16. `commercial.supplier_bank_accounts`: Supplier bank details with encrypted account numbers at rest and masked display values.

---

## 2. Table Specifications & DDL

### 2.1 Departments (`commercial.departments`)
```sql
CREATE TABLE commercial.departments (
    "Id" UUID PRIMARY KEY,
    "CompanyId" UUID NOT NULL,
    "Code" VARCHAR(50) NOT NULL,
    "Name" VARCHAR(200) NOT NULL,
    "Description" TEXT,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL,
    "CreatedByUserId" UUID NOT NULL,
    "UpdatedAtUtc" TIMESTAMPTZ,
    "UpdatedByUserId" UUID,
    CONSTRAINT "UQ_departments_CompanyId_Code" UNIQUE ("CompanyId", "Code")
);
CREATE INDEX "IX_departments_CompanyId" ON commercial.departments ("CompanyId");
```

### 2.2 Categories (`commercial.categories`)
```sql
CREATE TABLE commercial.categories (
    "Id" UUID PRIMARY KEY,
    "CompanyId" UUID NOT NULL,
    "Code" VARCHAR(50) NOT NULL,
    "Name" VARCHAR(200) NOT NULL,
    "Description" TEXT,
    "ParentCategoryId" UUID REFERENCES commercial.categories("Id") ON DELETE RESTRICT,
    "DepartmentId" UUID REFERENCES commercial.departments("Id") ON DELETE SET NULL,
    "HsnCode" VARCHAR(20),
    "DefaultGstRate" NUMERIC(5,2),
    "DisplayOrder" INT NOT NULL DEFAULT 0,
    "ImageUrl" VARCHAR(500),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL,
    "CreatedByUserId" UUID NOT NULL,
    "UpdatedAtUtc" TIMESTAMPTZ,
    "UpdatedByUserId" UUID,
    CONSTRAINT "UQ_categories_CompanyId_Code" UNIQUE ("CompanyId", "Code")
);
CREATE INDEX "IX_categories_CompanyId" ON commercial.categories ("CompanyId");
CREATE INDEX "IX_categories_ParentCategoryId" ON commercial.categories ("ParentCategoryId");
CREATE INDEX "IX_categories_DepartmentId" ON commercial.categories ("DepartmentId");
```

### 2.3 Brands (`commercial.brands`)
```sql
CREATE TABLE commercial.brands (
    "Id" UUID PRIMARY KEY,
    "CompanyId" UUID NOT NULL,
    "Code" VARCHAR(50) NOT NULL,
    "Name" VARCHAR(200) NOT NULL,
    "Description" TEXT,
    "LogoUrl" VARCHAR(500),
    "WebsiteUrl" VARCHAR(500),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL,
    "CreatedByUserId" UUID NOT NULL,
    "UpdatedAtUtc" TIMESTAMPTZ,
    "UpdatedByUserId" UUID,
    CONSTRAINT "UQ_brands_CompanyId_Code" UNIQUE ("CompanyId", "Code")
);
CREATE INDEX "IX_brands_CompanyId" ON commercial.brands ("CompanyId");
```

### 2.4 Units of Measure (`commercial.units_of_measure`)
```sql
CREATE TABLE commercial.units_of_measure (
    "Id" UUID PRIMARY KEY,
    "CompanyId" UUID NOT NULL,
    "Code" VARCHAR(20) NOT NULL,
    "Name" VARCHAR(100) NOT NULL,
    "Precision" INT NOT NULL DEFAULT 0,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL,
    "CreatedByUserId" UUID NOT NULL,
    CONSTRAINT "UQ_uom_CompanyId_Code" UNIQUE ("CompanyId", "Code")
);
```

### 2.5 Tax Codes (`commercial.tax_codes`)
```sql
CREATE TABLE commercial.tax_codes (
    "Id" UUID PRIMARY KEY,
    "CompanyId" UUID NOT NULL,
    "Code" VARCHAR(50) NOT NULL,
    "Description" VARCHAR(200) NOT NULL,
    "HsnSacCode" VARCHAR(20),
    "RatePercentage" NUMERIC(5,2) NOT NULL,
    "CgstPercentage" NUMERIC(5,2) NOT NULL,
    "SgstPercentage" NUMERIC(5,2) NOT NULL,
    "IgstPercentage" NUMERIC(5,2) NOT NULL,
    "CessPercentage" NUMERIC(5,2) NOT NULL DEFAULT 0,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "UQ_tax_codes_CompanyId_Code" UNIQUE ("CompanyId", "Code")
);
```

### 2.6 Products (`commercial.products`)
```sql
CREATE TABLE commercial.products (
    "Id" UUID PRIMARY KEY,
    "CompanyId" UUID NOT NULL,
    "ProductCode" VARCHAR(50) NOT NULL,
    "Name" VARCHAR(250) NOT NULL,
    "Description" TEXT,
    "ProductType" INT NOT NULL, -- 1 = Goods, 2 = Service, 3 = NonStock, 4 = Composite
    "CategoryId" UUID NOT NULL REFERENCES commercial.categories("Id") ON DELETE RESTRICT,
    "BrandId" UUID REFERENCES commercial.brands("Id") ON DELETE SET NULL,
    "DepartmentId" UUID REFERENCES commercial.departments("Id") ON DELETE SET NULL,
    "UnitOfMeasureId" UUID NOT NULL REFERENCES commercial.units_of_measure("Id") ON DELETE RESTRICT,
    "HsnSacCode" VARCHAR(20) NOT NULL,
    "TaxCodeId" UUID REFERENCES commercial.tax_codes("Id") ON DELETE RESTRICT,
    "GstRatePercentage" NUMERIC(5,2) NOT NULL,
    "Sku" VARCHAR(100) NOT NULL,
    "PrimaryBarcode" VARCHAR(100),
    "PurchasePricePaise" BIGINT NOT NULL DEFAULT 0,
    "CostPricePaise" BIGINT NOT NULL DEFAULT 0,
    "SellingPricePaise" BIGINT NOT NULL,
    "MrpPaise" BIGINT NOT NULL,
    "MinSellingPricePaise" BIGINT NOT NULL DEFAULT 0,
    "WholesalePricePaise" BIGINT NOT NULL DEFAULT 0,
    "ReorderLevel" NUMERIC(12,4) NOT NULL DEFAULT 0,
    "MinStockLevel" NUMERIC(12,4) NOT NULL DEFAULT 0,
    "MaxStockLevel" NUMERIC(12,4) NOT NULL DEFAULT 0,
    "ImageUrl" VARCHAR(500),
    "HasVariants" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL,
    "CreatedByUserId" UUID NOT NULL,
    "UpdatedAtUtc" TIMESTAMPTZ,
    "UpdatedByUserId" UUID,
    CONSTRAINT "UQ_products_CompanyId_Sku" UNIQUE ("CompanyId", "Sku"),
    CONSTRAINT "UQ_products_CompanyId_ProductCode" UNIQUE ("CompanyId", "ProductCode")
);
CREATE INDEX "IX_products_CompanyId_Name" ON commercial.products ("CompanyId", "Name");
CREATE INDEX "IX_products_CategoryId" ON commercial.products ("CategoryId");
CREATE INDEX "IX_products_BrandId" ON commercial.products ("BrandId");
CREATE INDEX "IX_products_PrimaryBarcode" ON commercial.products ("CompanyId", "PrimaryBarcode") WHERE "PrimaryBarcode" IS NOT NULL;
```

### 2.7 Product Variants (`commercial.product_variants`)
```sql
CREATE TABLE commercial.product_variants (
    "Id" UUID PRIMARY KEY,
    "ProductId" UUID NOT NULL REFERENCES commercial.products("Id") ON DELETE CASCADE,
    "Sku" VARCHAR(100) NOT NULL,
    "Barcode" VARCHAR(100),
    "VariantName" VARCHAR(200) NOT NULL,
    "AttributesJson" TEXT NOT NULL,
    "SellingPricePaise" BIGINT NOT NULL,
    "MrpPaise" BIGINT NOT NULL,
    "PurchasePricePaise" BIGINT NOT NULL DEFAULT 0,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL,
    "UpdatedAtUtc" TIMESTAMPTZ,
    CONSTRAINT "UQ_product_variants_Sku" UNIQUE ("Sku")
);
CREATE INDEX "IX_product_variants_ProductId" ON commercial.product_variants ("ProductId");
```

### 2.8 Product Barcodes (`commercial.product_barcodes`)
```sql
CREATE TABLE commercial.product_barcodes (
    "Id" UUID PRIMARY KEY,
    "CompanyId" UUID NOT NULL,
    "ProductId" UUID NOT NULL REFERENCES commercial.products("Id") ON DELETE CASCADE,
    "VariantId" UUID REFERENCES commercial.product_variants("Id") ON DELETE CASCADE,
    "Barcode" VARCHAR(100) NOT NULL,
    "BarcodeType" VARCHAR(20) NOT NULL DEFAULT 'EAN13',
    "IsPrimary" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "UQ_product_barcodes_CompanyId_Barcode" UNIQUE ("CompanyId", "Barcode")
);
CREATE INDEX "IX_product_barcodes_ProductId" ON commercial.product_barcodes ("ProductId");
```

### 2.9 Product Pricing Tiers (`commercial.product_prices`)
```sql
CREATE TABLE commercial.product_prices (
    "Id" UUID PRIMARY KEY,
    "CompanyId" UUID NOT NULL,
    "ProductId" UUID NOT NULL REFERENCES commercial.products("Id") ON DELETE CASCADE,
    "VariantId" UUID REFERENCES commercial.product_variants("Id") ON DELETE CASCADE,
    "BranchId" UUID,
    "PriceTier" VARCHAR(50) NOT NULL, -- RETAIL, WHOLESALE, SPECIAL, DISTRIBUTOR
    "MinQuantity" NUMERIC(12,4) NOT NULL DEFAULT 1,
    "UnitPricePaise" BIGINT NOT NULL,
    "DiscountPercentage" NUMERIC(5,2) NOT NULL DEFAULT 0,
    "EffectiveFromUtc" TIMESTAMPTZ NOT NULL,
    "EffectiveToUtc" TIMESTAMPTZ,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL,
    "CreatedByUserId" UUID NOT NULL
);
CREATE INDEX "IX_product_prices_ProductId" ON commercial.product_prices ("ProductId");
```

### 2.10 Product Price History (`commercial.product_price_history`)
```sql
CREATE TABLE commercial.product_price_history (
    "Id" UUID PRIMARY KEY,
    "CompanyId" UUID NOT NULL,
    "ProductId" UUID NOT NULL REFERENCES commercial.products("Id") ON DELETE CASCADE,
    "PriceType" VARCHAR(50) NOT NULL,
    "OldPricePaise" BIGINT NOT NULL,
    "NewPricePaise" BIGINT NOT NULL,
    "Reason" VARCHAR(250),
    "ChangedByUserId" UUID NOT NULL,
    "ChangedAtUtc" TIMESTAMPTZ NOT NULL
);
CREATE INDEX "IX_product_price_history_ProductId" ON commercial.product_price_history ("ProductId");
```

### 2.11 Customer Groups (`commercial.customer_groups`)
```sql
CREATE TABLE commercial.customer_groups (
    "Id" UUID PRIMARY KEY,
    "CompanyId" UUID NOT NULL,
    "Code" VARCHAR(50) NOT NULL,
    "Name" VARCHAR(150) NOT NULL,
    "DefaultDiscountPercentage" NUMERIC(5,2) NOT NULL DEFAULT 0,
    "DefaultCreditLimitPaise" BIGINT NOT NULL DEFAULT 0,
    "DefaultCreditDays" INT NOT NULL DEFAULT 0,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "UQ_customer_groups_CompanyId_Code" UNIQUE ("CompanyId", "Code")
);
```

### 2.12 Customers (`commercial.customers`)
```sql
CREATE TABLE commercial.customers (
    "Id" UUID PRIMARY KEY,
    "CompanyId" UUID NOT NULL,
    "CustomerCode" VARCHAR(50) NOT NULL,
    "Name" VARCHAR(250) NOT NULL,
    "CustomerType" INT NOT NULL, -- 1 = Individual, 2 = Retail, 3 = Wholesale, 4 = Business
    "GstType" INT NOT NULL, -- 1 = Registered, 2 = Unregistered, 3 = Composition, 4 = Overseas, 5 = SEZ
    "Gstin" VARCHAR(15),
    "Pan" VARCHAR(10),
    "ContactPerson" VARCHAR(150),
    "Phone" VARCHAR(30),
    "Email" VARCHAR(150),
    "CustomerGroupId" UUID REFERENCES commercial.customer_groups("Id") ON DELETE SET NULL,
    "AssignedBranchId" UUID,
    "CreditLimitPaise" BIGINT NOT NULL DEFAULT 0,
    "CreditPeriodDays" INT NOT NULL DEFAULT 0,
    "IsCreditHold" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "Notes" TEXT,
    "GlAccountId" UUID REFERENCES accounting.accounts("Id") ON DELETE RESTRICT,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL,
    "CreatedByUserId" UUID NOT NULL,
    "UpdatedAtUtc" TIMESTAMPTZ,
    "UpdatedByUserId" UUID,
    CONSTRAINT "UQ_customers_CompanyId_CustomerCode" UNIQUE ("CompanyId", "CustomerCode")
);
CREATE INDEX "IX_customers_CompanyId_Name" ON commercial.customers ("CompanyId", "Name");
CREATE INDEX "IX_customers_Gstin" ON commercial.customers ("CompanyId", "Gstin") WHERE "Gstin" IS NOT NULL;
CREATE INDEX "IX_customers_Phone" ON commercial.customers ("CompanyId", "Phone");
```

### 2.13 Customer Addresses (`commercial.customer_addresses`)
```sql
CREATE TABLE commercial.customer_addresses (
    "Id" UUID PRIMARY KEY,
    "CustomerId" UUID NOT NULL REFERENCES commercial.customers("Id") ON DELETE CASCADE,
    "AddressType" INT NOT NULL, -- 1 = Billing, 2 = Shipping, 3 = Both
    "AddressLine1" VARCHAR(250) NOT NULL,
    "AddressLine2" VARCHAR(250),
    "City" VARCHAR(100) NOT NULL,
    "District" VARCHAR(100),
    "State" VARCHAR(100) NOT NULL,
    "StateCode" VARCHAR(2) NOT NULL,
    "PinCode" VARCHAR(10) NOT NULL,
    "Country" VARCHAR(50) NOT NULL DEFAULT 'India',
    "IsDefaultBilling" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsDefaultShipping" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL
);
CREATE INDEX "IX_customer_addresses_CustomerId" ON commercial.customer_addresses ("CustomerId");
```

### 2.14 Suppliers (`commercial.suppliers`)
```sql
CREATE TABLE commercial.suppliers (
    "Id" UUID PRIMARY KEY,
    "CompanyId" UUID NOT NULL,
    "SupplierCode" VARCHAR(50) NOT NULL,
    "Name" VARCHAR(250) NOT NULL,
    "Category" VARCHAR(100) NOT NULL,
    "GstType" INT NOT NULL,
    "Gstin" VARCHAR(15),
    "Pan" VARCHAR(10),
    "ContactPerson" VARCHAR(150),
    "Phone" VARCHAR(30),
    "Email" VARCHAR(150),
    "PaymentTerms" VARCHAR(100) NOT NULL DEFAULT 'Net 30',
    "CreditPeriodDays" INT NOT NULL DEFAULT 30,
    "MsmeType" VARCHAR(50),
    "UdyamRegistrationNumber" VARCHAR(50),
    "IsMsmeRegistered" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "Notes" TEXT,
    "GlAccountId" UUID REFERENCES accounting.accounts("Id") ON DELETE RESTRICT,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL,
    "CreatedByUserId" UUID NOT NULL,
    "UpdatedAtUtc" TIMESTAMPTZ,
    "UpdatedByUserId" UUID,
    CONSTRAINT "UQ_suppliers_CompanyId_SupplierCode" UNIQUE ("CompanyId", "SupplierCode")
);
CREATE INDEX "IX_suppliers_CompanyId_Name" ON commercial.suppliers ("CompanyId", "Name");
CREATE INDEX "IX_suppliers_Gstin" ON commercial.suppliers ("CompanyId", "Gstin") WHERE "Gstin" IS NOT NULL;
```

### 2.15 Supplier Addresses (`commercial.supplier_addresses`)
```sql
CREATE TABLE commercial.supplier_addresses (
    "Id" UUID PRIMARY KEY,
    "SupplierId" UUID NOT NULL REFERENCES commercial.suppliers("Id") ON DELETE CASCADE,
    "AddressType" INT NOT NULL, -- 1 = RegisteredOffice, 2 = Warehouse, 3 = Dispatch
    "AddressLine1" VARCHAR(250) NOT NULL,
    "AddressLine2" VARCHAR(250),
    "City" VARCHAR(100) NOT NULL,
    "State" VARCHAR(100) NOT NULL,
    "StateCode" VARCHAR(2) NOT NULL,
    "PinCode" VARCHAR(10) NOT NULL,
    "Country" VARCHAR(50) NOT NULL DEFAULT 'India',
    "IsPrimary" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL
);
CREATE INDEX "IX_supplier_addresses_SupplierId" ON commercial.supplier_addresses ("SupplierId");
```

### 2.16 Supplier Bank Accounts (`commercial.supplier_bank_accounts`)
```sql
CREATE TABLE commercial.supplier_bank_accounts (
    "Id" UUID PRIMARY KEY,
    "SupplierId" UUID NOT NULL REFERENCES commercial.suppliers("Id") ON DELETE CASCADE,
    "BankName" VARCHAR(150) NOT NULL,
    "BranchName" VARCHAR(150) NOT NULL,
    "AccountNumberEncrypted" TEXT NOT NULL,
    "AccountNumberMasked" VARCHAR(30) NOT NULL,
    "IfscCode" VARCHAR(15) NOT NULL,
    "AccountType" VARCHAR(30) NOT NULL DEFAULT 'Current',
    "IsPrimary" BOOLEAN NOT NULL DEFAULT TRUE,
    "IsVerified" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL,
    "CreatedByUserId" UUID NOT NULL
);
CREATE INDEX "IX_supplier_bank_accounts_SupplierId" ON commercial.supplier_bank_accounts ("SupplierId");
```

---

## 3. Data Integrity & Migration Principles

1. **Deterministic Tenant Partitioning:** Every query against root tables checks `CompanyId = currentTenantId`.
2. **Double-Entry Financial Readiness:** All financial values use 64-bit integer paise.
3. **Encryption Architecture:** Bank account numbers are AES-GCM encrypted using an application-level key provider and stored in `AccountNumberEncrypted`, while `AccountNumberMasked` caches the last 4 digits for standard UI lists.

