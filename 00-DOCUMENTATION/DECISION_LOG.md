# MAGIC — ARCHITECTURAL DECISION LOG (ADR)

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Confirmed vs. Unresolved Decisions Separated)  
**Last Updated:** 2026-11-12  

---

## 1. Confirmed Architectural Baseline (From Master Initialization)

### ADR-001: Monetary Storage Strategy (`long` Integer Paise + `decimal` Quantities/Rates)
- **Status:** **Confirmed Baseline**
- **Decision:** All INR monetary values are stored and computed as 64-bit integers (`long` in C#, `BIGINT` in PostgreSQL), representing integer paise (₹1 = 100 paise). Fractional quantities and rates use explicit `decimal` / PostgreSQL `NUMERIC` precision.

### ADR-002: PostgreSQL Logical Schema-per-Domain & Single-Database Isolation
- **Status:** **Confirmed Baseline**
- **Decision:** Partition PostgreSQL into logical domain schemas (`platform`, `organization`, `products`, `inventory`, `sales`, `purchases`, `accounting`, `finance`, `tax`, `customers`, `suppliers`, `employees`, etc.) within one authoritative PostgreSQL database per deployment initially. Direct frontend/WPF database access is prohibited.

### ADR-003: Reverse Inclusive GST & Double-Entry Accounting Foundation
- **Status:** **Confirmed Baseline**
- **Decision:** Reverse inclusive GST (`Taxable = Gross / (1 + Rate/100)`, `GST = Gross - Taxable`) and immutable double-entry General Ledger postings ($\sum \text{Debit} = \sum \text{Credit}$) execute strictly in the ASP.NET Core backend.

### ADR-004: Thermal Printing & Hardware Interfacing via WPF Native Bridge
- **Status:** **Confirmed Baseline**
- **Decision:** The WPF (.NET 10 LTS) desktop host bridges Chromium WebView2 with local printers, barcode scanners, cash drawers, and native Windows integrations without duplicating business calculations in WPF.

### ADR-005: Auto Stock Update Human-in-the-Loop Staging Protocol
- **Status:** **Confirmed Baseline**
- **Decision:** AI-extracted wholesale purchase invoices enter a dedicated verification workspace checking SKU/barcode, prices (purchase/selling/MRP), GST, supplier, duplicate invoice identity, and totals before atomic owner confirmation.

---

## 2. Unresolved Decisions & Technical Evaluations (Pending Explicit User Approval)

### OPEN-001: Terminal Network Disconnection & Offline Buffering Policy
- **Options Under Evaluation:**
  - *Option 1 (Recommended for V1):* **Strict Online LAN Operation.** Terminals require active LAN connection to the central server; checkout pauses safely if LAN drops, preventing overselling, credit breaches, and GST Rule 46 serial gaps.
  - *Option 2 (Proposed Only):* **Local SQLite Buffering & Synchronization.** Terminals buffer signed offline vouchers locally (`OFFLINE_SYNC_ARCHITECTURE.md`) with asynchronous conflict resolution.
- **Status:** **UNRESOLVED — Awaiting Explicit User Confirmation.**

### OPEN-002: Angular UI Component Library & Design System
- **Options Under Evaluation:**
  - *Option A:* **Angular Material + CDK** wrapped with custom MAGIC SCSS/CSS design tokens (official support, high accessibility, low dependency risk).
  - *Option B:* **PrimeNG** (richer enterprise tables/grids out of box, heavier dependency footprint).
  - *Option C:* **Pure Custom Design System** built on Angular CDK + SCSS/CSS design tokens (zero external UI vendor lock-in, higher initial build effort).
- **Status:** **UNRESOLVED — Recommendation: Angular CDK + Custom SCSS/CSS Tokens (or selective Angular Material primitives).**

### OPEN-003: Authentication & JWT Signing Implementation
- **Options Under Evaluation:**
  - *Option A:* **ASP.NET Core Identity** customized with Argon2id password hashing and standard ECDSA P-256 / HMAC-SHA256 JWT Bearer authentication.
  - *Option B:* **Custom Identity + Ed25519 (EdDSA) JWT provider** (requires third-party crypto library `NSec.Cryptography` or `BouncyCastle` for JWT validation; Ed25519 remains required separately for update package signatures).
- **Status:** **UNRESOLVED — Pending Security Sign-Off.**

### OPEN-004: Durable Background Job Infrastructure
- **Options Under Evaluation:**
  - *Option A (Recommended for V1):* Native **.NET 10 `IHostedService` / `BackgroundService`** paired with PostgreSQL `platform.outbox_messages` (zero third-party scheduler dependencies).
  - *Option B:* **Quartz.NET** backed by PostgreSQL ADO.NET job store for complex cron schedules.
- **Status:** **UNRESOLVED — Pending Technical Sign-Off.**

### OPEN-005: Statutory e-Invoicing / e-Way Bill & Inventory Valuation Switching Rules
- **Questions:**
  1. Confirm V1 NIC e-Invoicing/e-Way export scope (Offline JSON/Excel export vs. direct GSP API).
  2. Confirm whether inventory valuation switching (FIFO vs. Moving Weighted Average) is strictly locked per fiscal year after statutory review.
  3. Confirm current Indian statutory GST rounding rule details (line-level vs. invoice-total rounding under Section 170 CGST Act).
- **Status:** **UNRESOLVED — Pending User & Statutory Review.**


---

## Phase 1 Decision Resolutions (Approved 2026-11-12)

| Decision ID | Resolution | Status | Phase 1 Implementation Enforcement |
|---|---|---|---|
| **OPEN-001** | **Strict Online LAN Operation** | `[Approved]` | Single authoritative PostgreSQL 18 cluster (`magic_dev`); zero SQLite, offline sync, or local buffering packages/code. `ConnectivityMonitorService` enforces explicit offline warning state when LAN API is unreachable. |
| **OPEN-002** | **Pure Angular CDK + Custom SCSS & CSS Design Tokens** | `[Approved]` | Angular 22 (`@angular/cdk ^22.2.7`) with `@layer`-ordered SCSS design tokens, CSS `color-scheme` + `light-dark()`, `BreakpointObserver`, and `CdkTrapFocus`. Zero Angular Material or PrimeNG dependencies. |
| **OPEN-003** | **Concrete Authentication & Token Crypto Engine** | `[Deferred]` | Interfaces (`ICurrentUserContext`, `IPermissionEvaluator`, `ITokenService`, `ISupervisorElevationService`) and fail-closed `FailClosedPermissionEvaluator` / `DeferredTokenService` boundaries implemented; zero unapproved JWT crypto or Argon2id user tables added yet. |
| **OPEN-004** | **Native `.NET 10 IHostedService` / `BackgroundService` + PostgreSQL Outbox** | `[Approved]` | Implemented `OutboxProcessorBackgroundService` polling `platform.outbox_messages` via partial index & lock tokens. Zero Quartz.NET dependencies. |
| **OPEN-005** | **Tax & Statutory Scope (NIC e-Invoicing / Valuation Policy Lock)** | `[Open — Phase 2/4]` | Deferred without blocking Phase 1 technical foundation. |
