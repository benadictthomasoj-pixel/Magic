# MAGIC ERP — Phase B UI Implementation Specification

## Overview
Phase B UI implements four enterprise-grade commercial master management workspaces in the Angular 22 frontend:
1. `/billing/products` — Product Master & Catalog
2. `/billing/categories` — Categories, Brands, and Departments
3. `/billing/customers` — Customer Directory & Credit Governance
4. `/billing/suppliers` — Supplier Master & Bank Account Security

All workspaces strictly adhere to MAGIC's enterprise aesthetic:
- Gold/Dark color scheme (`#c59b27`, `#0c1427`, `#1e293b`).
- Zero floating-point money display (64-bit integer paise converted to ₹ with two decimal places in UI).
- Real-time slide-over drawers for Create, View, Edit, and Audit workflows.
- Responsive pagination, search debounce, status filter badges, and keyboard shortcuts.
- Fail-closed security boundaries with authenticated state protection.

---

## Workspace Specifications

### 1. Products Management (`/billing/products`)
- **Route:** `/billing/products`
- **Component:** `ProductsListComponent` (`01-SRC/Frontend/src/app/features/commercial/products/products-list.component.ts`)
- **Features:**
  - Full-text search by SKU, barcode, or product name.
  - Category, Brand, and Status (Active/Archived) filtering.
  - Server-side pagination with dynamic page sizes.
  - Side drawer for product creation and modification:
    - Auto-generated or manual unique SKU.
    - EAN-13 barcode generation with mod-10 check digit calculation.
    - Category & Brand selection from live commercial master endpoints.
    - Unit of Measurement (UOM) selection (PCS, KGS, LTR, BOX, MTR, PKT).
    - HSN/SAC code input with tax compliance rules.
    - GST rate selection (0%, 5%, 12%, 18%, 28%).
    - Purchase price, MRP, and selling price input with automatic paise conversion.
  - Interactive Barcode Scanner Lookup widget with audio/visual feedback.
  - Product archive / restore actions with confirmation dialogs.

### 2. Category & Brand Hierarchy (`/billing/categories`)
- **Route:** `/billing/categories`
- **Component:** `CategoriesManagementComponent` (`01-SRC/Frontend/src/app/features/commercial/categories/categories-management.component.ts`)
- **Features:**
  - Multi-tab management: Categories, Brands, Departments.
  - Tree-structured category view showing hierarchical nesting and product counts.
  - Circular hierarchy prevention during parent category assignment.
  - Deletion guard preventing category deletion if active products reference it.
  - Brand management with country of origin and active/inactive status toggle.
  - Department management for high-level commercial organization.

### 3. Customer Directory & Credit Governance (`/billing/customers`)
- **Route:** `/billing/customers`
- **Component:** `CustomersListComponent` (`01-SRC/Frontend/src/app/features/commercial/customers/customers-list.component.ts`)
- **Features:**
  - Customer directory with search across name, code, phone, and GSTIN.
  - Customer type categorization: Individual, Retail, Wholesale, Business.
  - Strict 15-character Indian GSTIN format validation (with explicit labeling indicating format verification, not external portal check).
  - Derived Accounts Receivable balance display (read-only, populated from accounting subledger).
  - Credit Limit Update Drawer:
    - Requires explicit modification reason.
    - Records tamper-evident audit record upon change (`CUSTOMER_CREDIT_LIMIT_MODIFIED`).
    - Enforces fail-closed permission check (`commercial:credit:approve`).

### 4. Supplier Master & Sensitive Bank Security (`/billing/suppliers`)
- **Route:** `/billing/suppliers`
- **Component:** `SuppliersListComponent` (`01-SRC/Frontend/src/app/features/commercial/suppliers/suppliers-list.component.ts`)
- **Features:**
  - Supplier listing with category, payment terms (Net 15, Net 30, Net 45, Net 60, Due on Receipt), and MSME registration tag.
  - Sensitive Bank Account Security:
    - Masked display by default: `•••• •••• •••• 1234`.
    - Secure "Reveal / Unmask" action guarded by `commercial:suppliers:bank-details` permission.
    - Explicit unmasking reason captured in dialog.
    - Backend decrypts AES-256-GCM ciphertext on-demand, generates tamper-evident `SUPPLIER_BANK_UNMASKED` security audit log, and returns plain text.
    - Automatic 30-second UI timer that re-masks the sensitive details to protect against shoulder surfing.

---

## Security Remediation & Login Screen Cleanliness
- **Zero Hardcoded Credentials:** Removed all default usernames (`ben`, `admin`) and passwords from `login.page.ts`.
- **Zero Development Shortcuts:** Completely removed the `Quick Sign-in` button group (`Admin`, `Accountant`, `Cashier`) from `login.page.html` and SCSS.
- **Fail-Closed Auth Boundary:** Empty input state on load, requiring deliberate credential entry and server-side authentication token exchange.

