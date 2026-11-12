# MAGIC — PROJECT CONTEXT & MASTER VISION

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  
**Target Operating Stage:** Phase 0 — Planning and Architecture Only  

---

## 1. Executive Summary

**MAGIC** is a greenfield, high-reliability, modular, configurable **Universal Business Management ERP platform**. Unlike conventional Point of Sale (POS) tools or siloed billing utilities, MAGIC serves as the complete operational and financial operating system for commercial enterprises of varying scales, operational models, and vertical market specializations.

The platform provides a unified system of record spanning point-of-sale execution, sales lifecycle management, multi-warehouse inventory control, supply chain procurement, customer relationship management, human capital administration, statutory tax compliance (specifically tailored for Indian GST), and an immutable, GAAP-compliant **double-entry financial accounting core**.

---

## 2. Supported Business Models & Profiles

MAGIC achieves universal applicability without architectural fragmentation through a **Profile-Driven Capability Engine**. Modules and workflows dynamically adapt to the active business profile:

| Business Model | Primary Workflows Enabled | Key Configuration Attributes |
|---|---|---|
| **Retail Businesses** | High-speed POS, Barcode scanning, Cash drawer integration, Customer loyalty, Fast tender, End-of-Day (Z-Report) cash reconciliation. | Batch/Item barcode lookups, Touch-screen optimization, Per-counter cash drawers, Instant thermal printing. |
| **Wholesale Businesses** | Bulk volume pricing, Tiered discounts, Credit limits, Outstanding aging tracking, Multi-unit packing (e.g., Box > Carton > Pallet), Proforma invoices. | Ledger credit controls, Wholesale price sheets, Transport/Lorry Receipt (LR) tracking, Flexible credit terms. |
| **Distribution Businesses** | Route delivery, Sales representative order booking, Order dispatch staging, Lot/Batch traceability, Delivery Challans, E-Way Bill generation. | Warehouse bin locations, Route scheduling, Consignment tracking, Multi-vehicle allocation. |
| **Trading Businesses** | High-velocity buy-sell matching, Cross-docking, Drop-shipment tracking, Margin analytics, Foreign/Inter-state trade compliance. | Real-time landed cost tracking, Purchase order linking to Sales orders, Multi-currency support. |
| **Service Businesses** | Service item catalog, Time & Material billing, Milestone invoicing, AMC (Annual Maintenance Contracts), Technician assignment. | Non-stock service items, Task tracking, Service job cards, Periodic billing schedules. |
| **Multi-Branch Enterprises** | Centralized product catalog, Inter-branch stock transfers (IBT), Centralized vs. Decentralized purchasing, Branch-level P&L, Consolidated Balance Sheet. | Inter-branch clearing accounts, Branch-scoped permissions, Stock transit registers, Central consolidation. |

---

## 3. Core Product Philosophy & Tenets

1. **Accounting as the Absolute Foundation:**
   Every physical and commercial transaction—sales, purchases, inventory adjustments, scrap, payroll, and asset depreciation—flows into an immutable double-entry general ledger. Independent, desynchronized balances are strictly prohibited.
2. **Deterministic Financial Integrity:**
   All monetary amounts are represented as 64-bit integer paise (1 INR = 100 paise). Floating-point arithmetic for currency is banned across the entire stack.
3. **Domain Separation & Modular Isolation:**
   Business capabilities are segregated into cohesive domain modules. Data access across domains occurs strictly via published contracts, application service facades, or asynchronous domain events.
4. **Local Sovereignty (Zero Cloud Dependency for V1):**
   The primary operating footprint for V1 is entirely local: either standalone workstation or private Local Area Network (LAN). No cloud connectivity or external SaaS dependency is required to conduct business.
5. **Traceability and Audit Immutability:**
   Financial vouchers, stock ledger entries, and tax returns once posted cannot be modified in place. Corrections require documented reversal entries with explicit references to the original record.

---

## 4. Current Project Phase: Phase 0 (Planning Only)

The project is governed by a **Strict No-Build Instruction**. No application code, database migrations, package installations, or project file scaffolding shall take place until formal approval of the architecture and planning suite is granted by the system architect and project sponsor.

