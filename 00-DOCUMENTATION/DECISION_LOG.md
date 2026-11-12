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

### OPEN-003: Authentication, JWT Signing & Access Control Architecture
- **Status:** **[Approved — 2026-11-12]**
- **Approved Decision:** Standard ASP.NET Core Identity with Standard JWT Bearer Tokens & Claim-Based RBAC.
  - **Token Provider:** Microsoft ASP.NET Core Authentication JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`).
  - **Signing Algorithm:** HMAC-SHA256 (symmetric shared secret for single-store LAN deployments) and ECDSA P-256 (`ES256`, asymmetric key pair for multi-store / enterprise environments). Non-standard/custom JWT signing engines (e.g. Ed25519 for tokens) are rejected to eliminate third-party crypto dependencies for HTTP authentication.
  - **Token Lifecycle:** 15-minute short-lived access tokens; 7-day refresh tokens stored with cryptographically secure random bytes in PostgreSQL (`security.refresh_tokens`), enforcing one-time use rotation and immediate revocation of token family upon reuse detection.
  - **Password Hashing:** ASP.NET Core Identity password hasher (PBKDF2 with HMAC-SHA512, 100,000+ iterations) or Argon2id via standard provider.
  - **Authorization:** Fine-grained permission claims (e.g., `sales.invoice:create`, `inventory.stock:adjust`, `audit.trail:read`).
  - **Context Enforcement:** Branch ID and Terminal ID claims are validated server-side on every request. Client-supplied headers are validated against authenticated token claims; if mismatched or unassigned, requests fail-closed with 403 Forbidden.
  - **Supervisor Elevation:** `ISupervisorElevationService` contract provides transient time-bound authorization elevations for sensitive supervisor overrides (e.g., price override, credit override, transaction void).

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
| **OPEN-003** | **ASP.NET Core Identity + Standard JWT Bearer + RBAC** | `[Approved]` | Approved architecture: HMAC-SHA256 (LAN single-store) & ECDSA P-256 (multi-store), 15-minute access tokens, PostgreSQL refresh token rotation, PBKDF2/Argon2id, server-side validated branch/terminal claims, fail-closed `ISupervisorElevationService`. Phase 1 includes contracts and context boundaries; persistence entities planned for Phase 2 user/auth schema. |
| **OPEN-004** | **Native `.NET 10 IHostedService` / `BackgroundService` + PostgreSQL Outbox** | `[Approved]` | Implemented `OutboxProcessorBackgroundService` with `FOR UPDATE SKIP LOCKED`, worker lease tokens, exponential retry tracking, and real `IIntegrationEventConsumer` dispatch. Zero Quartz.NET dependencies. |
| **OPEN-005** | **Tax & Statutory Scope (NIC e-Invoicing / Valuation Policy Lock)** | `[Open — Phase 2/4]` | Deferred without blocking Phase 1 technical foundation. |
