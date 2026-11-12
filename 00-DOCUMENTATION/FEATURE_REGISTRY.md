# MAGIC — MASTER FEATURE REGISTRY & ROADMAP

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  

---

## 1. Feature Lifecycle Status Definitions

Every feature tracked in MAGIC conforms to the following lifecycle states:

- **Proposed:** Feature scoped, documented, and awaiting user review/authorization (Current state for all features).
- **Approved:** Authorized by user for development in the designated phase.
- **Designed:** Detailed technical specifications, schema DDL, and API contracts finalized.
- **In Progress:** Active implementation within approved scope.
- **In Review:** Code review, security scan, and architectural compliance verification.
- **Tested:** Executed unit, integration, financial, and concurrency tests passed.
- **Released:** Packaged into active deployment build.
- **Deprecated:** Scheduled for obsolescence.

---

## 2. Feature Master Registry

| Feature ID | Module | Feature Name | Description | Phase | Status | Owning Module |
|---|---|---|---|---|---|---|
| **FEAT-0101** | BI | Real-Time Sales Velocity | Sub-second gross/net sales telemetry by branch. | Phase 7 | `[Proposed]` | Dashboard & BI |
| **FEAT-0102** | BI | Margin Analysis Heatmap | Gross profit margin visualization by product category. | Phase 7 | `[Proposed]` | Dashboard & BI |
| **FEAT-0201** | POS | Fast Counter Barcode Scanning | Sub-50ms barcode lookup and line-item insertion. | Phase 3 | `[Proposed]` | POS & Billing |
| **FEAT-0202** | POS | Split Multi-Tender Payment | Tender splits between Cash, Card, UPI, and Account. | Phase 3 | `[Proposed]` | POS & Billing |
| **FEAT-0203** | POS | Cart Hold and Recall | Park active checkout session for multi-customer lines. | Phase 3 | `[Proposed]` | POS & Billing |
| **FEAT-0204** | POS | Cashier Shift X/Z Reports | Cash drawer float opening, drops, and shift closing. | Phase 3 | `[Proposed]` | POS & Billing |
| **FEAT-0205** | POS | ESC/POS Thermal Printing | Raw byte streaming to 80mm/58mm receipt printers. | Phase 3 | `[Proposed]` | POS & Billing |
| **FEAT-0301** | Sales | B2B Commercial Invoicing | Tax invoice creation with HSN classification & e-Way. | Phase 3 | `[Proposed]` | Sales Mgmt |
| **FEAT-0302** | Sales | Multi-Tier Pricing Sheets | Wholesale, distributor, and retail pricing rules. | Phase 3 | `[Proposed]` | Sales Mgmt |
| **FEAT-0303** | Sales | Customer Credit Control | Hard/soft stops on overdue customer accounts. | Phase 3 | `[Proposed]` | Sales Mgmt |
| **FEAT-0401** | Purchases | Purchase Order Lifecycle | Requisition to PO to GRN three-way matching. | Phase 3 | `[Proposed]` | Purchases |
| **FEAT-0402** | Purchases | Landed Cost Apportionment | Apportion freight/handling to inward item stock valuation. | Phase 3 | `[Proposed]` | Purchases |
| **FEAT-0501** | Inventory | Multi-Warehouse Hierarchy | Warehouses, zones, racks, and bin locations. | Phase 3 | `[Proposed]` | Inventory |
| **FEAT-0502** | Inventory | Batch and Expiry Control | Lot numbers, manufacturing date, and shelf-life tracking. | Phase 3 | `[Proposed]` | Inventory |
| **FEAT-0503** | Inventory | FIFO Valuation Engine | First-in-first-out cost of goods sold depletion. | Phase 3 | `[Proposed]` | Inventory |
| **FEAT-0504** | Inventory | Moving Weighted Average | Continuous moving average valuation on inward receipts. | Phase 3 | `[Proposed]` | Inventory |
| **FEAT-0505** | Inventory | Stock Adjustments & Scrap | Documented physical audit discrepancies and write-offs. | Phase 3 | `[Proposed]` | Inventory |
| **FEAT-0601** | Accounting | Double-Entry Voucher Engine | Balanced debits and credits in 64-bit integer paise. | Phase 2 | `[Proposed]` | Accounting |
| **FEAT-0602** | Accounting | Hierarchical Chart of Accounts | Flexible account groups across Assets/Liab/Eq/Rev/Exp. | Phase 2 | `[Proposed]` | Accounting |
| **FEAT-0603** | Accounting | Immutable Ledger & Reversals | Non-destructive reversal vouchers for posted entries. | Phase 2 | `[Proposed]` | Accounting |
| **FEAT-0604** | Accounting | Period Locking & Closing | Fiscal year closing, period locking, retained earnings. | Phase 2 | `[Proposed]` | Accounting |
| **FEAT-0701** | Finance | Bank Account Reconciliation | BRS with statement import and discrepancy matching. | Phase 4 | `[Proposed]` | Financial Mgmt |
| **FEAT-0702** | Finance | Fixed Asset Depreciation | Straight Line & Written Down Value depreciation. | Phase 4 | `[Proposed]` | Financial Mgmt |
| **FEAT-0801** | Tax | Reverse Inclusive GST Math | Mathematical deduction of base price and tax split. | Phase 4 | `[Proposed]` | Tax Compliance |
| **FEAT-0802** | Tax | Statutory Rounding Off (S.170)| Nearest-rupee rounding with dedicated variance posting. | Phase 4 | `[Proposed]` | Tax Compliance |
| **FEAT-0803** | Tax | GSTR-1 & GSTR-3B Datasets | Statutory tax reporting datasets and JSON exports. | Phase 4 | `[Proposed]` | Tax Compliance |
| **FEAT-0901** | CRM | Unified Customer Master | Addresses, GSTIN, PAN, and credit terms. | Phase 5 | `[Proposed]` | Customer CRM |
| **FEAT-0902** | CRM | Customer Loyalty Ledger | Points accrual, redemption rules, and expiry. | Phase 5 | `[Proposed]` | Customer CRM |
| **FEAT-1001** | Suppliers | Supplier Directory & MSME | Bank details, MSME classification, and payment terms. | Phase 5 | `[Proposed]` | Supplier Mgmt |
| **FEAT-1101** | Expenses | Petty Cash Float Management | Petty cash book, vouchers, and custodian reconciliations.| Phase 4 | `[Proposed]` | Expense Mgmt |
| **FEAT-1201** | HR | Employee Profiles & Shifts | Employee master, departments, and shift calendars. | Phase 6 | `[Proposed]` | Human Resources|
| **FEAT-1301** | Payroll | Salary Structure & Payruns | Automated salary generation and accounting journal post.| Phase 6 | `[Proposed]` | Payroll |
| **FEAT-1401** | Marketing | Promotional Rule Engine | BOGO, volume discounts, coupons, and expiry. | Phase 5 | `[Proposed]` | Marketing |
| **FEAT-1501** | Orders | Order Allocation & Pick/Pack | Available-to-promise inventory reservation and picking. | Phase 6 | `[Proposed]` | Order Mgmt |
| **FEAT-1601** | Services | Service Job Cards & AMC | Repair turnaround, spare parts issue, AMC renewals. | Phase 6 | `[Proposed]` | Service Mgmt |
| **FEAT-1701** | Reports | Financial Statements Suite | Balance Sheet, Profit & Loss, Trial Balance, Day Book. | Phase 7 | `[Proposed]` | Reports & Analytics |
| **FEAT-1801** | Multi-Branch | Inter-Branch Stock Transfers | Outward challan, in-transit register, inward receipt. | Phase 8 | `[Proposed]` | Multi-Branch |
| **FEAT-1802** | Multi-Branch | Terminal Registration & Pairing| Hardware fingerprint binding and cryptographic tokens. | Phase 8 | `[Proposed]` | Multi-Branch |
| **FEAT-1901** | Admin | Argon2id Authentication & RBAC | Fine-grained permissions and JWT token lifecycle. | Phase 1 | `[Proposed]` | Admin & Security|
| **FEAT-1902** | Admin | Supervisor Override Workflow | Elevation PIN for discounts, refunds, and voids. | Phase 1 | `[Proposed]` | Admin & Security|
| **FEAT-1903** | Admin | Immutable System Audit Trail | Who, when, what, and before/after state logging. | Phase 1 | `[Proposed]` | Admin & Security|
| **FEAT-2001** | Automation | Auto Stock Update Workspace | Staged invoice ingestion, SKU matching, verification. | Phase 7 | `[Proposed]` | Automation & AI |
| **FEAT-2101** | Platform | Automated Database Backups | Scheduled `pg_dump`, SHA-256 verification, restore test. | Phase 1 | `[Proposed]` | System Mgmt |

---

## 3. High-Level Phased Development Roadmap

```
+----------------------------------------------------------------------------------------------------+
| PHASE 0: Requirements & Architecture Review (CURRENT STAGE - PLANNING ONLY)                         |
|   - Master guidelines review, module catalogues, schema DDL, API contracts, financial math.       |
|   - GATE: Explicit User Authorization Required before Phase 1.                                     |
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
| PHASE 1: Desktop, Backend & Database Foundation                                                    |
|   - WPF Shell, WebView2 container, ASP.NET Core host, PostgreSQL schemas, RBAC, Auth, Audit logs.   |
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
| PHASE 2: Full Double-Entry Accounting Foundation                                                   |
|   - Chart of Accounts, Journal Vouchers, Immutable Ledger, Period Locking, Subledger Controls.      |
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
| PHASE 3: POS, Inventory, Sales & Purchases                                                         |
|   - Fast Counter POS, Multi-warehouse stock, Batch/Expiry, FIFO/Moving Avg, Sales & PO lifecycles. |
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
| PHASE 4: Financial Management, Tax & Expenses                                                      |
|   - Reverse Inclusive GST Engine, Section 170 Rounding, Bank Reconciliation (BRS), Fixed Assets.   |
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
| PHASE 5: CRM, Suppliers & Marketing                                                                |
|   - Customer Credit/Loyalty, Supplier Onboarding, MSME, Promotional Engine, Coupon Rules.          |
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
| PHASE 6: HR, Payroll, Orders & Service Management                                                  |
|   - Biometric Attendance, Salary Payruns, Order Pick/Pack Allocation, Service Job Cards, AMC.       |
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
| PHASE 7: Reporting, BI & Auto Stock Update (AI Automation)                                         |
|   - Vector PDF / Excel Financial Statements Suite, Auto Stock Update Staging Workspace & Commit.   |
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
| PHASE 8: Multi-Branch & Multi-Terminal Infrastructure                                              |
|   - Inter-Branch Stock Transfers (IBT), Terminal Authorization Tokens, Branch Financial Consolidation|
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
| PHASE 9: Production Hardening, Acceptance & Release                                                |
|   - Concurrency stress testing, Authenticode packaging, Windows Service installer, Release Signoff.|
+----------------------------------------------------------------------------------------------------+
```

