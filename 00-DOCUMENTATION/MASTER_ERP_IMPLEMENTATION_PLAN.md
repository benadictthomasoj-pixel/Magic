# MAGIC ERP — MASTER IMPLEMENTATION PLAN
## Universal Business Management ERP | Phased Production Roadmap

**Document Version:** 2.0.0  
**Project:** MAGIC – Universal Business Management ERP  
**Technology Stack:** .NET 10 + ASP.NET Core + EF Core 10 + PostgreSQL 18 + Angular 22 + WPF WebView2  
**Last Updated:** 2026-11-12  
**Status:** Comprehensive Master Plan — Authorization Baseline  

---

## 1. Executive Summary & Architecture Principles

MAGIC is an enterprise-grade, greenfield Universal Business Management ERP designed for high reliability, financial correctness, and seamless operational flow across 21 core modules. The system enforces:

1. **Modular Monolith with Domain-Driven Design (DDD):** Clean separation across Platform, Domain, Application, Infrastructure, and Presentation layers.
2. **PostgreSQL 18 as the Single Source of Truth:** Direct frontend access to PostgreSQL is strictly prohibited. All queries and commands flow through the ASP.NET Core backend.
3. **Integer Money (Paise Arithmetic):** All monetary amounts are stored as 64-bit signed integers (`BIGINT` in PostgreSQL, `long` in C#), representing integer paise (₹1 = 100 paise). Zero floating-point money.
4. **Immutable General Ledger Posting Engine:** Every commercial event (Sales, Purchases, Payments, Adjustments) connects to the double-entry accounting engine ($\sum \text{Debit} = \sum \text{Credit}$). Posted vouchers cannot be modified or deleted. Reversals and credit/debit notes are used exclusively.
5. **Fail-Closed Security (OPEN-003):** No hardcoded credentials, mock identity bypasses, or open endpoints. All financial mutation endpoints reject unauthenticated or unauthorized requests with HTTP 401/403.
6. **Unified Enterprise Experience:** A single Angular frontend hosting two isolated workspaces (Billing and Accounting) switched via a top-centre segmented switcher (`[ Billing ]  [ Accounting ]`), preserving the official 5-second handwriting intro animation.

---

## 2. 8-Phase Controlled Implementation Strategy

To deliver all 21 modules with full backend, database, UI, and integration without regressions, development is structured into 8 controlled phases:

```
[Phase A: Foundation & Security] ────────► [Phase B: Commercial Masters]
               │                                      │
               ▼                                      ▼
[Phase C: Inventory & Procurement] ─────► [Phase D: Sales & POS Billing]
               │                                      │
               ▼                                      ▼
[Phase E: Accounting Integration] ──────► [Phase F: Commercial & Org Modules]
               │                                      │
               ▼                                      ▼
[Phase G: Reports & AI Automation] ─────► [Phase H: Windows Desktop Release]
```

---

### Phase A — Foundation & Security (Immediate Next Execution)
- **Target Modules:** Module 18 (Multi-Branch & Multi-Terminal Master), Module 19 (Administration & Security / OPEN-003), Module 21 (System Platform Foundation).
- **Core Deliverables:**
  - ASP.NET Core Identity tables in `security` schema (`security.users`, `security.roles`, `security.user_roles`, `security.refresh_tokens`, `security.terminal_sessions`).
  - Standard JWT Bearer token generation (HMAC-SHA256 / ES256) with 15-minute access tokens and secure 7-day refresh token rotation.
  - Branch and Terminal registration and session context validation.
  - Strict password hashing with PBKDF2 (HMAC-SHA512, 100,000+ iterations).
  - RBAC claim-based authorization policies and supervisor elevation service (`ISupervisorElevationService`).
  - Angular login dialog / authentication modal (or header status / session switcher) without compromising the brand intro showcase.

---

### Phase B — Commercial Masters
- **Target Modules:** Module 05 (Product Catalogue - Domain B), Module 08 (Tax Rates & HSN/SAC Master), Module 09 (Customer Directory), Module 10 (Supplier Directory).
- **Core Deliverables:**
  - PostgreSQL schemas `products`, `tax`, `customers`, `suppliers`.
  - Product Master (SKUs, barcodes, variants, categories, brands, UoMs, standard cost, retail MRP, wholesale rate, GST slab linkage).
  - Customer Master (B2B/B2C, GSTIN checksum validation, billing/shipping addresses, credit limits, payment terms).
  - Supplier Master (GSTIN, MSME category, payment terms, bank account details for payouts).
  - Tax Master (HSN/SAC codes, CGST/SGST/IGST rates, exemption flags).
  - Full CRUD REST APIs, DTOs, Angular master screens, search, sorting, and pagination.

---

### Phase C — Inventory & Procurement
- **Target Modules:** Module 04 (Purchase & Procurement), Module 05 (Warehouse & Stock Ledger).
- **Core Deliverables:**
  - PostgreSQL schema `inventory` (`inventory.warehouses`, `inventory.stock_balances`, `inventory.stock_ledger`, `inventory.stock_batches`, `inventory.stock_transfers`).
  - PostgreSQL schema `purchases` (`purchases.purchase_orders`, `purchases.goods_receipt_notes`, `purchases.purchase_invoices`, `purchases.debit_notes`).
  - Stock Ledger: Append-only transactional movements (Opening, Purchase, Sale, Transfer, Adjustment).
  - Inventory valuation policy enforcement (Moving Weighted Average vs. FIFO per fiscal year lock).
  - Purchase Orders, 3-Way Matching (PO vs. GRN vs. Bill), Landed Cost calculation.
  - Automatic double-entry journal vouchers to General Ledger (Inventory Dr, Input GST Dr, Accounts Payable Cr).

---

### Phase D — Sales & POS Billing
- **Target Modules:** Module 02 (POS & Billing), Module 03 (Sales Management), Module 15 (Order Management).
- **Core Deliverables:**
  - PostgreSQL schema `pos` (`pos.pos_sessions`, `pos.held_carts`, `pos.cash_drops`, `pos.cash_closing`).
  - PostgreSQL schema `sales` (`sales.sales_orders`, `sales.invoices`, `sales.invoice_items`, `sales.credit_notes`, `sales.delivery_challans`).
  - POS Terminal UI: Fast barcode scanning, product grid, split tenders (Cash, Card, UPI), hold/recall carts, receipt printing.
  - Wholesale Tax Invoices: GST reverse inclusive / forward exclusive calculations, customer credit checks.
  - Cashier Shift Management: Opening float, mid-shift cash drops, X-Report, Z-Report, physical denomination reconciliation.
  - Sales returns & Credit Notes with inventory restock and accounting reversal.

---

### Phase E — Accounting Integration & Treasury
- **Target Modules:** Module 06 (Financial Statements & Advanced GL), Module 07 (Financial Management & Treasury), Module 08 (Tax Settlement & GSTR).
- **Core Deliverables:**
  - Automated Posting Matrix integration across all commercial operations.
  - Subledger Reconciliation (AR Subledger vs. Account 11300, AP Subledger vs. Account 21100).
  - Treasury & Banking: Bank accounts, Bank Reconciliation Statement (BRS), cheque register, fund transfers.
  - Fixed Asset Register: Asset depreciation schedules (SLM / WDV) with automated monthly depreciation vouchers.
  - Financial Statements UI: Profit & Loss Statement, Balance Sheet, Cash Flow Statement (Direct & Indirect).
  - Tax Settlement: Input vs. Output GST set-off engine and GSTR-1 / GSTR-3B export generation.

---

### Phase F — Commercial Extensions & Human Resources
- **Target Modules:** Module 11 (Expense Management), Module 12 (HR & Attendance), Module 13 (Payroll), Module 14 (Marketing & Loyalty), Module 16 (Service Management).
- **Core Deliverables:**
  - Expense Management: Operational claims, petty cash float management, supervisor approvals, direct GL booking.
  - HR & Attendance: Employee records, departments, designations, biometric log ingestion, shift allocation, leave management.
  - Payroll: Configurable salary structures (Basic, HRA, DA, PF, ESI, PT, TDS), monthly payruns, payslips, automated payroll journal vouchers.
  - Marketing: Discount rules (BOGO, flat, percentage), coupon redemption, customer loyalty points.
  - Service Management: Workshop job cards, technician allocation, spare parts consumption, AMC contracts.

---

### Phase G — Business Intelligence & AI Automation
- **Target Modules:** Module 01 (Executive Dashboard & BI), Module 17 (Reports & Analytics Engine), Module 20 (Automation & AI / Auto Stock Update).
- **Core Deliverables:**
  - Executive BI Dashboard: Daily sales velocity, gross margins, cash flow runway, branch rankings, stock velocity.
  - Universal Reporting Engine: Multi-criteria date filters, column sorting, pagination, Excel (`.xlsx`) export, PDF generation, print layouts.
  - Auto Stock Update: AI-assisted invoice text/JSON parser with structured staging grid and mandatory human verification before commit.

---

### Phase H — System Management, Windows Release & Final Acceptance
- **Target Modules:** Module 18 & 21 (Final System Hardening), Desktop Host (`Magic.Desktop.Wpf`).
- **Core Deliverables:**
  - Database Backup & Restore: Automated `pg_dump` backup engine, SHA-256 archive checksums, automated isolated restore drill.
  - Windows Desktop: WPF + WebView2 packaging, hardware bridge validation (thermal receipt printer, cash drawer, barcode scanner).
  - Security Penetration Audit: Authentication brute-force protection, SQL injection prevention, cross-tenant isolation verification.
  - Release Acceptance Sign-off against all acceptance criteria.

---

## 3. Dependency & Precedence Matrix

| Step | Phase | Depends On | Unlocks |
|---|---|---|---|
| **1** | **Phase A: Foundation & Security** | None (Current repo) | User context, RBAC, branch isolation across all modules |
| **2** | **Phase B: Commercial Masters** | Phase A | Products, Customers, Suppliers for Procurement and Sales |
| **3** | **Phase C: Inventory & Procurement** | Phase B, Accounting Core | Real stock levels, POs, Vendor Bills, AP Subledger |
| **4** | **Phase D: Sales & POS Billing** | Phase B, Phase C, Accounting Core | POS terminal, Tax Invoices, AR Subledger, Cashier shifts |
| **5** | **Phase E: Accounting Integration** | Phase C, Phase D | Complete Financial Statements, BRS, Fixed Assets, Tax Returns |
| **6** | **Phase F: Commercial & Org Modules** | Phase A, Phase E | HR, Payroll, Expenses, Loyalty, Service Workshops |
| **7** | **Phase G: Reports & AI Automation** | Phase C, Phase D, Phase E | Universal BI, Report Exports, Auto Stock Update |
| **8** | **Phase H: Windows Release & Sign-off** | All previous phases | Production desktop package, backup verification, deployment |

