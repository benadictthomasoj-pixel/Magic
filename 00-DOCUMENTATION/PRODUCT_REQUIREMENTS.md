# MAGIC — PRODUCT REQUIREMENTS SPECIFICATION (PRS)

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  

---

## 1. Scope of the System

MAGIC addresses the end-to-end operational requirements of small-to-medium and mid-market enterprises across retail, wholesale, distribution, trading, and service verticals.

The functional scope encompasses **21 core modules** organized into **7 major business domains**:

```
+----------------------------------------------------------------------------------------------------+
|                                           MAGIC CORE ERP                                           |
+----------------------------------------------------------------------------------------------------+
| A. BUSINESS OPERATIONS                   | B. PRODUCTS & SUPPLY CHAIN                              |
|  - POS & Billing (Fast Counter)          |  - Product Catalogue & Inventory                        |
|  - Sales Management (B2B / Wholesale)    |  - Warehouse & Stock Transfers                          |
|  - Purchase & Procurement                |  - Supplier Management                                  |
|  - Order & Service Management            |                                                         |
+----------------------------------------------------------------------------------------------------+
| C. FINANCE & ACCOUNTING                  | D. TAX & COMPLIANCE                                     |
|  - Double-Entry Accounting Core          |  - GST Engine & HSN/SAC                                 |
|  - Financial Management & Treasury       |  - Tax Registers & Reconciliation                       |
|  - Expense Management & Fixed Assets     |  - Statutory Reports (GSTR-1 / 3B / 2B)                 |
+----------------------------------------------------------------------------------------------------+
| E. CUSTOMER & GROWTH                     | F. PEOPLE & ORGANISATION                                |
|  - Customer CRM & Loyalty                |  - HR & Biometric Attendance                            |
|  - Marketing, Campaigns & Promotions     |  - Payroll & Branch / Terminal Management               |
+----------------------------------------------------------------------------------------------------+
| G. PLATFORM & INTELLIGENCE                                                                         |
|  - Dashboard & Business Intelligence     - Reports & Analytics                                     |
|  - Automation & AI (Auto Stock Update)   - Administration, Security, Backup & System Management    |
+----------------------------------------------------------------------------------------------------+
```

---

## 2. Comprehensive Module Functional Requirements

### 2.1 Dashboard and Business Intelligence
- Real-time visualization of key performance indicators (Gross Sales, Net Realization, Margin %, Receivables Aging, Payables Aging, Low Stock Alerts).
- Branch-comparative metrics and cashier settlement reconciliation.
- Pre-aggregated dashboard read-models to prevent intensive ad-hoc queries against operational tables.

### 2.2 POS and Billing (Retail Counter)
- High-velocity keyboard-driven checkout optimized for non-mouse operations.
- Fast SKU, barcode, and alias lookup with sub-50ms latency.
- Multi-tender splits (Cash, Card, UPI, Store Credit, Customer Account).
- Hold/Recall cart sessions for multi-customer queues.
- Offline counter resilience with local transaction buffering in LAN-disconnected state.
- Peripheral integration: ESC/POS receipt printers, pole displays, weigh scales (RS-232), cash drawers.
- Cashier shift opening, float declaration, mid-day cash drops, and shift closing (X/Z Reports).

### 2.3 Sales Management (B2B, Wholesale & Commercial)
- Full sales lifecycle: Quotation / Estimate -> Proforma Invoice -> Sales Order -> Delivery Challan -> Tax Invoice -> Sales Return / Credit Note.
- Multi-tier pricing sheets (Retail, Wholesale, Distributor, Special Contract).
- Customer credit limit checks and hard/soft stops on overdrawn accounts.
- E-Way bill and e-Invoicing data preparation with HSN/SAC classification.
- Sales representative attribution and commission tracking.

### 2.4 Purchase and Procurement
- Purchase Requisition -> Purchase Order (PO) -> Goods Receipt Note (GRN) -> Purchase Invoice -> Purchase Return / Debit Note.
- Landed cost allocation (freight, customs, handling, insurance apportioned to inventory item costs).
- Supplier invoice reconciliation against GRN (Three-way matching: PO vs. GRN vs. Invoice).
- Quality check / inspection recording on incoming goods.

### 2.5 Inventory and Warehouse Management
- Multi-location warehouse hierarchy (Warehouse -> Zone -> Aisle -> Rack -> Bin).
- SKU, EAN/UPC barcode, Brand, Category, HSN/SAC code, and attribute management.
- Dual unit of measure (UoM) with dynamic conversion factors (e.g., Primary: Bags, Secondary: Kgs; 1 Bag = 50 Kgs).
- Batch/Lot tracking with manufacturing date, expiry date, and shelf-life alerts.
- Serial number tracking for high-value / warranty-tracked items.
- Valuation engines: Standard support for **FIFO (First-In-First-Out)** and **Moving Weighted Average**.
- Inter-warehouse and Inter-branch stock transfers with transit tracking.
- Physical stock counting, cycle counting, scrap recording, and discrepancy adjustments.

### 2.6 Accounting (The Financial Heart)
- Strict double-entry general ledger.
- Hierarchical Chart of Accounts (Assets, Liabilities, Equity, Revenue, Expenses) with arbitrary grouping depth.
- Voucher Types: Sales Voucher, Purchase Voucher, Payment Voucher, Receipt Voucher, Journal Voucher, Contra Voucher, Credit Note, Debit Note.
- Subledgers for Accounts Receivable (AR) and Accounts Payable (AP) reconciling to GL control accounts.
- Cost Centers and Profit Centers for multidimensional financial reporting.
- Fiscal Year lifecycle: Period locking, closing entries, retained earnings roll-forward.
- Immutable posted entries; audit trails detailing who, when, and source document correlation.

### 2.7 Financial Management & Treasury
- Bank account management, cheque book tracking, and electronic payment registers.
- Bank Reconciliation Statement (BRS) with automated statement import matching.
- Cash flow forecasting (direct and indirect method).
- Fixed Asset Register: Asset capitalization, depreciation schedules (Straight Line Method & Written Down Value), disposal, and impairment.
- Accruals, prepayments, and deferred revenue amortization schedules.

### 2.8 Tax and Statutory Compliance (Indian GST Focus)
- Multi-rate GST engine: 0%, 0.1%, 0.25%, 1.5%, 3%, 5%, 12%, 18%, 28%, and Compensation Cess.
- Reverse inclusive tax calculation for retail prices.
- Forward exclusive tax calculation for B2B transactions.
- Automated tax split: CGST + SGST (Intra-state) vs. IGST (Inter-state) based on Place of Supply and Origin State Code.
- GSTR-1, GSTR-3B, GSTR-2B reconciliation export formats.
- Tax Deducted at Source (TDS) and Tax Collected at Source (TCS) threshold tracking and ledger allocation.

### 2.9 Customer CRM
- Unified customer master with billing/shipping addresses, GSTIN, PAN, and credit terms.
- Interaction history, complaints, purchase cadence analytics, and churn detection.
- Customer loyalty program: Points accrual, tier upgrades, redemption rules, and expiry.
- Statement of Accounts (SOA) and outstanding aging buckets (0-30, 31-60, 61-90, 90+ days).

### 2.10 Supplier Management
- Supplier directory with bank details, GSTIN, MSME registration type, and payment terms.
- Vendor rating, delivery performance, price-trend tracking, and lead-time analysis.
- Accounts payable aging schedules and payment scheduling.

### 2.11 Expense Management
- Petty cash voucher management and custodian balances.
- Operational expense booking with GST input tax credit (ITC) eligibility tags.
- Multi-level approval hierarchy for high-value expense authorizations.

### 2.12 Human Resources (HR)
- Employee master profiles, statutory identifiers (PAN, Aadhaar, UAN, ESIC), department, and designation.
- Shift management, biometric attendance log ingestion, leave policies, and holiday calendars.

### 2.13 Payroll
- Salary component definition: Basic, HRA, DA, Special Allowances, PF, ESI, Professional Tax (PT), TDS.
- Monthly payroll generation, payslip distribution, and automatic posting to Accounting (Salary Expense Dr, Statutory Payables Cr, Net Salary Payable Cr).

### 2.14 Marketing and Customer Growth
- Promotional rule engine: Buy X Get Y Free, Flat Discounts, Spend Threshold Discounts, Coupon Codes.
- Customer segmentation for promotional campaigns (SMS/WhatsApp notifications staged via local export or external webhook).

### 2.15 Order Management
- Backorder management, order allocation, partial fulfillment tracking, and cancellation controls.
- Integration between sales orders and purchase requisitions for just-in-time stock fulfillment.

### 2.16 Service Management
- Service job cards, technician assignment, estimated vs. actual repair turnaround.
- Spare parts consumption linking directly to inventory decrements.
- Annual Maintenance Contract (AMC) tracking, periodic visit schedules, and renewal reminders.

### 2.17 Reports and Analytics
- Financial: Balance Sheet, Profit & Loss, Trial Balance, Cash Flow, Day Book, General Ledger.
- Inventory: Stock Summary, Fast/Slow-Moving, Stock Ageing, Reorder Level Report, Stock Valuation.
- Sales & Tax: Item-wise Sales, Register-wise Sales, GST Tax Summary, B2B vs. B2C Sales Register.
- Export engine: Standard Excel (XLSX), CSV, and vector PDF reports.

### 2.18 Multi-Branch and Multi-Terminal
- Centralized company configuration supporting unlimited logical branches.
- Isolated branch data visibility with headquarters consolidation capabilities.
- Inter-Branch Stock Transfers (Outward Challan -> In-Transit -> Inward Receipt).
- Terminal registration, terminal authorization tokens, and per-terminal hardware bindings.

### 2.19 Administration and Security
- Role-Based Access Control (RBAC) with granular functional permissions (View, Create, Edit, Delete, Approve, Export).
- Supervisor authorization override (e.g., cashier attempting a manual discount above 10% requires manager PIN/password).
- Complete immutable audit logging of all write operations.

### 2.20 Automation and AI (Auto Stock Update)
- Specialized purchase invoice ingestion workspace.
- AI-extracted structured data (JSON/Tabular) is pasted into the staging review workspace.
- High-fidelity reconciliation: Supplier matching, SKU/Barcode verification, tax calculation check, invoice total validation.
- Interactive correction grid with discrepancy highlighting before one-click atomic posting to Purchases, Inventory, and Accounting.

### 2.21 System Management (Platform Foundation)
- Automated local database backup scheduling, backup verification, and point-in-time restore testing.
- Database health monitoring, index vacuuming, and connection pool telemetry.
- Application configuration management, theme settings, and error log viewer.

---

## 3. Non-Functional Requirements (NFRs)

| Dimension | Target Metric / Requirement | Architectural Mechanism |
|---|---|---|
| **Response Latency** | POS Barcode lookup: < 50ms; Invoice Save: < 200ms; Financial Ledger Query: < 500ms. | Optimized B-Tree indexes, EF Core compiled queries, normalized relational structures. |
| **Monetary Precision** | Zero floating-point drift. 100% exact integer arithmetic in paise. | C# `long` representation for paise; PostgreSQL `BIGINT` column storage. |
| **Reliability & ACID** | All inventory + accounting + sales writes must be strictly atomic. Zero partial commits. | PostgreSQL explicit database transactions with Read Committed or Serializable isolation where appropriate. |
| **Concurrency** | Multi-terminal simultaneous billing against shared inventory without race-condition overselling. | Row-level locking (`SELECT FOR UPDATE`) on stock balances during finalization or optimistic version checks. |
| **Local Network Scope** | 1 Server + up to 20 concurrent LAN desktop clients in standard local deployment. | Kestrel web server listening on LAN interface; connection pool size configured for 100 active connections. |
| **Auditability** | 100% change traceability for financial records. Reversible vouchers only. | Immutable voucher rows; append-only change logs; soft-delete with tombstone records for non-financial master entities. |
| **Hardware Compatibility**| Standard thermal POS printers (80mm / 58mm), ESC/POS commands, USB/COM barcode scanners. | WPF Host hardware abstraction bridge communicating over raw TCP / Spooler / SerialPort APIs. |

