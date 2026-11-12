# MAGIC — SYSTEM ARCHITECTURE SPECIFICATION

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  

---

## 1. Architectural Philosophy: Domain-Oriented Modular Monolith

MAGIC is engineered as a greenfield **Domain-Oriented Modular Monolith** adhering to **Clean Architecture** and **Domain-Driven Design (DDD)** principles.

### Why Modular Monolith?
1. **Operational Simplicity:** A small-to-medium enterprise ERP deployed on a local workstation or private LAN cannot bear the operational overhead of microservices (distributed tracing, network partitions, multi-process orchestration, Kubernetes).
2. **Transaction Integrity:** Financial accounting requires strict ACID transactions across sales, inventory, and ledger updates. A modular monolith allows in-process database transactions without the fragility of two-phase commits (2PC) or eventual consistency anomalies.
3. **High Performance & Zero Network Overhead:** Inter-module calls execute as in-memory method invocations or in-process domain events, yielding sub-millisecond execution times.
4. **Strict Boundaries:** While compiled and hosted in a unified backend process, logical modules maintain strict boundaries. Direct cross-module database writes are strictly banned.

---

## 2. Five-Layer Architecture Model

```
+---------------------------------------------------------------------------------------------------+
| 1. EXPERIENCE LAYER                                                                               |
|    - WPF Native Desktop Shell (.NET 10 LTS, Microsoft WebView2 Host, Hardware Abstraction Bridge) |
|    - Angular SPA (TypeScript, SCSS & CSS Design Tokens, Signals, Reactive Forms, HttpClient)      |
+---------------------------------------------------------------------------------------------------+
                                                  |  JSON over HTTP/HTTPS (Versioned REST / OpenAPI)
                                                  v
+---------------------------------------------------------------------------------------------------+
| 2. APPLICATION LAYER                                                                              |
|    - Versioned API Endpoints (ASP.NET Core Controllers / Minimal APIs + OpenAPI)                  |
|    - Use Cases: Commands, Queries, Validators & Application Service Orchestrators                 |
|    - Pipeline Behaviors: Input Validation, Authorization, Idempotency, Transaction, Audit         |
|    - Approved Cross-Module Contract Facades                                                       |
+---------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+---------------------------------------------------------------------------------------------------+
| 3. BUSINESS DOMAIN LAYER (Pure Domain Logic - Zero External Dependencies)                          |
|    - Domain Aggregates, Entities, and Value Objects (Money, GST, TaxRate, Quantity, SKU)          |
|    - Domain Services (Valuation Engines: FIFO/Avg, Double-Entry Voucher Balancer, Tax Calculators)|
|    - Domain Events (OrderPlaced, InvoicePosted, StockAdjusted, PaymentReconciled)                 |
|    - Business Invariants & Integrity Constraints                                                  |
+---------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+---------------------------------------------------------------------------------------------------+
| 4. INFRASTRUCTURE LAYER                                                                           |
|    - Persistence: Entity Framework Core 10 + Npgsql Provider                                      |
|    - PostgreSQL Logical Schemas (platform, organization, products, sales, accounting, etc.)       |
|    - Outbox Pattern & .NET Hosted Services / Durable Background Jobs                              |
|    - Peripheral Hardware Drivers (ESC/POS, RS-232 COM Serial Port, Windows Spooler)              |
|    - Structured Logging (Serilog) & Observability (OpenTelemetry)                                 |
+---------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+---------------------------------------------------------------------------------------------------+
| 5. PLATFORM LAYER (Cross-Cutting Substratum)                                                      |
|    - Identity, Authentication (ASP.NET Core Identity / Argon2id / JWT), and RBAC Engine           |
|    - Cryptographic Services (Ed25519 Update/Terminal Signatures, DPAPI / AES-GCM Secrets)         |
|    - Windows Service Hosting, Desktop Lifecycle, Updates, Backup & Recovery                       |
|    - Idempotency Store & Database Locking Controls                                                |
|    - Diagnostics & Local Network Communication Controls                                           |
+---------------------------------------------------------------------------------------------------+
```

---

## 3. Technology Stack Justification & Validation

| Layer | Selected Technology | Architectural Justification |
|---|---|---|
| **Desktop Application** | **WPF (.NET 10 LTS / C#)** | Native Windows integration, long-term support stability, direct access to Win32/Hardware COM ports, native Windows notifications, update/recovery lifecycle. |
| **Embedded Shell** | **Microsoft WebView2** | Evergreen Chromium rendering engine with GPU acceleration, isolated sandboxing, and secure native bridge (`postMessage` / Host Objects) to WPF. |
| **Frontend Framework** | **Angular (TypeScript)** | Feature-oriented modular architecture, Angular Router lazy-loading, Reactive Forms, RxJS, Angular Signals initially (NgRx evaluated if complexity warrants), SCSS & CSS design tokens. |
| **Backend Framework** | **ASP.NET Core Web API (.NET 10 LTS)** | High-throughput Kestrel engine, versioned REST APIs, OpenAPI specification, native `.NET Worker Service` / Windows Service hosting (`Microsoft.Extensions.Hosting.WindowsServices`). |
| **ORM** | **Entity Framework Core 10** | Value object mapping (`Money` to 64-bit integer paise), database interceptors for audit/soft-delete, compiled queries, version-controlled schema migrations. |
| **Database** | **PostgreSQL** | Authoritative ACID relational store (one database per business deployment initially), `BIGINT` / `NUMERIC` precision, schema isolation, row-level locking (`SELECT FOR UPDATE`). |
| **PostgreSQL Provider** | **Npgsql** | High-performance .NET provider for PostgreSQL with native JSONB, array, and transaction support. |
| **Target OS** | **Windows 10 / 11 (x64)** | Standard desktop and local server operating system target for target retail/commercial deployments. |

### Supporting Technology Evaluation
- **Logging & Observability:** **Serilog** for structured local file/JSON diagnostic sinks; **OpenTelemetry** for correlation IDs and performance tracing where diagnostic complexity warrants it.
- **Validation:** ASP.NET Core model validation combined with explicit server-side **FluentValidation** pipelines in the Application Layer.
- **Authentication:** **ASP.NET Core Identity** (with hardened Argon2id `IPasswordHasher<TUser>` and JWT Bearer / session controls) evaluated against custom lightweight token auth; standard ECDSA P-256 / HMAC-SHA256 JWT signing evaluated vs. Ed25519 token signing.
- **API Documentation:** Native .NET 10 **Microsoft.AspNetCore.OpenApi** contract generation.
- **Backup:** PostgreSQL-native `pg_dump` / `pg_restore` orchestrated via .NET Hosted Services with SHA-256 checksums and automated restore verification.

---

## 4. Module Boundaries & Communication Rules

To maintain strict modularity without microservice sprawl, the following architectural invariants are enforced:

1. **Single Module Ownership:**
   Every database table and domain entity is owned by exactly **one** module. For example, `accounting.journal_entries` is owned exclusively by the `Accounting` module; `inventory.stock_lots` is owned exclusively by the `Inventory` module.
2. **Zero Cross-Module Direct Data Access:**
   Module A must never execute queries or mutations directly against tables owned by Module B. EF Core `DbContext` instances are scoped to domain boundaries or mediated through an overarching Unit-of-Work with explicit boundary checks.
3. **Synchronous Transactional Workflows (Application Services & Contracts):**
   Critical financial and inventory workflows must execute **synchronously inside a single PostgreSQL ACID database transaction**. When Module A coordinates a cross-module business operation, it calls explicit C# interface contracts exposed in `Contracts/` (e.g., `IAccountingPostingContract`, `IInventoryMovementContract`, `ICustomerLedgerContract`).
   - *Example — Finalizing a Sale:* Within one atomic PostgreSQL transaction, the Sales/POS Application Service orchestrates:
     1. Sales invoice persistence (`SalesModule`).
     2. Physical stock decrement & stock ledger posting (`InventoryModule`).
     3. Tender / payment record creation (`FinanceModule` / `SalesModule`).
     4. Double-entry General Ledger voucher posting (`AccountingModule`).
     5. Customer AR subledger balance update (`CustomerCrmModule`).
     6. Outbox event record persistence (`Platform` Outbox) for secondary non-critical side effects.
4. **Asynchronous Secondary Decoupling (Domain Events & Outbox):**
   Secondary non-transactional side effects (e.g., dashboard KPI cache refresh, loyalty points notification, scheduled report triggers, marketing campaign tracking) execute asynchronously via **Domain Events** persisted in the transactional outbox table and dispatched post-commit.
5. **No Circular Dependencies:**
   Module dependencies must form a strict Directed Acyclic Graph (DAG). If Module A depends on Module B, Module B can never depend on Module A.

---

## 5. Deployment Topologies

MAGIC natively supports two primary local deployment models:

### 5.1 Standalone Single-Workstation Topology
- **Host:** Single Windows PC (e.g., small retail store or standalone trading desk).
- **Process Architecture:**
  - PostgreSQL runs as a local Windows service listening on `127.0.0.1:5432`.
  - ASP.NET Core backend runs as an embedded process or local background service listening on `http://127.0.0.1:5000`.
  - WPF Desktop Application launches, hosts WebView2, and navigates to the local backend/frontend.
  - Thermal printer and barcode scanner interface directly via USB/COM.

### 5.2 Multi-Terminal Local Area Network (LAN) Topology
- **Host Computer (In-Store Server):**
  - Dedicated or primary PC running PostgreSQL 16 and the ASP.NET Core Backend configured as a managed **Windows Service**.
  - Kestrel listens securely on the private LAN interface (`https://192.168.1.X:5001`).
- **Terminal Workstations (POS Counters 1 to N, Back-Office Workstations):**
  - Thin desktop clients running the WPF + WebView2 container.
  - Clients connect over LAN to the host backend via authenticated, TLS-encrypted REST and WebSocket APIs.
  - **Zero Direct Database Exposure:** Terminals never connect directly to PostgreSQL port 5432. All traffic flows through the authenticated API gateway layer.
  - **Terminal Registration:** Each client terminal must be registered and approved by an Administrator, binding a hardware fingerprint to a cryptographic Terminal Token.

---

## 6. Hardware Interfacing Architecture

Web browsers are traditionally restricted from accessing low-level hardware. MAGIC resolves this cleanly through the native WPF Desktop Shell:

```
+--------------------------------------------------------------------------------------------------+
| WEBVIEW2 FRONTEND (Angular UI)                                                                   |
|   - Sends print/open-drawer command via `window.chrome.webview.postMessage(payload)` or Localhost API|
+--------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+--------------------------------------------------------------------------------------------------+
| WPF DESKTOP HARDWARE BRIDGE (C# Host Wrapper)                                                    |
|   - Captures native message event                                                                |
|   - Dispatches to Hardware Controller Subsystem:                                                 |
|       * ESC/POS RAW Command Generator (Hex byte streaming to USB/LAN printer)                     |
|       * Windows Print Spooler (Win32 GDI / Direct XPS vector output)                              |
|       * Cash Drawer Trigger (Sends Kick-Pulse byte `0x1B, 0x70, 0x00, 0x19, 0xFA` via printer)   |
|       * Weigh Scale Reader (Asynchronous SerialPort listening on COM1..COM8 with debounce)       |
|       * Customer Pole Display (VFD 20x2 character stream over RS-232)                             |
+--------------------------------------------------------------------------------------------------+
```

---

## 7. Business Domain Architecture (7 Major Domains)

MAGIC organizes all 21 capabilities into seven logical domains with clear ownership and boundaries:

| Domain | Capabilities & Modules Included | Primary Ownership Focus |
|---|---|---|
| **A. Business Operations** | POS & Billing, Sales Management, Purchase & Procurement, Order Management, Service Management | Front-line counter execution, commercial sales/purchase documents, fulfillment, service job cards. |
| **B. Products & Supply Chain** | Product Catalogue, Inventory & Warehouse, Stock Transfers, Supplier Management | SKU/barcodes, UoM conversions, warehouse bins, batches/expiry, FIFO/Average valuation, supplier profiles. |
| **C. Finance & Accounting** | General Ledger, AR, AP, Cash & Bank Management, Expenses, Budgeting, Fixed Assets, Financial Statements | Authoritative double-entry ledger, subledger reconciliation, depreciation, periods, P&L / Balance Sheet. |
| **D. Tax & Compliance** | Tax & Statutory Compliance (GST, HSN/SAC, ITC, Output Tax, Tax Registers, Tax Reconciliation) | Reverse/forward GST formulas, CGST/SGST/IGST rules, statutory filing exports (GSTR-1/3B/2B). |
| **E. Customer & Growth** | Customer CRM, Loyalty, Marketing, Campaigns, Promotions, Customer Analytics | Customer profiles, credit controls, loyalty points ledger, promotional/coupon rules. |
| **F. People & Organisation** | Human Resources, Attendance, Payroll, Multi-Branch & Multi-Terminal Management | Employee profiles, shifts, salary payruns, branch hierarchy, terminal pairing. |
| **G. Platform & Intelligence** | Dashboard & BI, Reports & Analytics, Automation & AI (Auto Stock Update), Admin & Security, System Management | Executive dashboards, cross-module read reports, staged invoice ingestion, RBAC, backups, updates. |

---

## 8. Logical Code Structure Blueprints

### 8.1 Target Backend Module Structure
Every backend module follows Clean Architecture layering internally:

```text
ModuleName/
    Domain/          (Entities, ValueObjects, Enums, Rules, Events — zero external dependencies)
    Application/     (Commands, Queries, DTOs, Interfaces, Validators, Services)
    Infrastructure/  (Persistence, Configurations, Repositories, Integrations)
    Presentation/    (Controllers, RequestModels, ResponseModels)
    Tests/           (Unit, Integration)
    README.md
```

### 8.2 Target Frontend Feature Structure
```text
Frontend/
    app/
        core/        (authentication, authorization, navigation, configuration, http)
        shared/      (components, directives, pipes, utilities)
        layouts/     (admin, staff, shared)
        features/    (dashboard, pos, sales, purchases, inventory, accounting, finance, tax,
                      customers, suppliers, expenses, hr, payroll, marketing, orders, services,
                      reports, administration)
        state/       (Angular Signals stores initially; NgRx only if justified)
        routing/
    assets/          (icons, images, fonts)
    environments/
```

---

## 9. Target Repository Governance Structure

```text
MAGIC/
├── 00-DOCUMENTATION/       (01-PRODUCT through 13-CHANGELOG + Core 19 Docs)
├── 01-SRC/                 (Desktop, Frontend, Backend, Contracts)
├── 02-DATABASE/            (Migrations, Seeds, Schemas, ReferenceData)
├── 03-TESTS/               (Unit, Integration, Architecture, Financial, Security, Performance, E2E)
├── 04-DEPLOYMENT/          (Windows, Installer, DatabaseSetup, LocalNetwork, Updates, Recovery)
├── 05-TOOLS/
└── 06-PROJECT-MANAGEMENT/  (Milestones, FeatureRegistry, TechnicalDebt, ReleaseNotes, DecisionRecords)
```
*(Folders in `01-SRC` through `06-PROJECT-MANAGEMENT` are created only after explicit Phase 1 implementation authorization.)*



---

## Phase 1 Technical Foundation Implementation & Approved Packages (2026-11-12)

### Solution Structure (`Magic.sln`)
- `01-SRC/Contracts/Magic.Contracts` — API envelopes (`ApiResponse<T>`, `ApiErrorResponse`), HTTP header constants (`MagicHttpHeaders`), diagnostics DTOs, security/audit/event/desktop bridge contracts.
- `01-SRC/Backend/Magic.Domain` — Pure domain base types (`DomainEntity` with UUIDv7, `DomainException`, `DomainGuard`, `Money` in 64-bit integer paise).
- `01-SRC/Backend/Magic.Application` — Persistence abstractions (`IUnitOfWork`, `IDatabaseConnectivityProbe`), `ValidationRunner`, `SystemDiagnosticsService`.
- `01-SRC/Backend/Magic.Infrastructure` — `MagicDbContext` (`Npgsql.EntityFrameworkCore.PostgreSQL`), `PostgresUnitOfWork`, `PostgresAuditLogger`, `OutboxProcessorBackgroundService`, deferred security boundaries, `PostgresHealthCheck`.
- `01-SRC/Backend/Magic.Platform` — Serilog logging, OpenTelemetry tracing, options validation, Windows Service hosting integration (`Microsoft.Extensions.Hosting.WindowsServices`).
- `01-SRC/Backend/Magic.Api` — ASP.NET Core .NET 10 Web API (`/health`, `/health/ready`, `/api/v1/system/status`, `/api/v1/system/context`, `/openapi/v1.json`), `GlobalExceptionHandler`, `RequestContextMiddleware`.
- `01-SRC/Desktop/Magic.Desktop.Wpf` — Cross-targeted `net10.0-windows` WPF + Microsoft WebView2 shell scaffold (compiled on macOS via `EnableWindowsTargeting=true`; runtime execution **Pending Windows 10/11 Verification**).
- `01-SRC/Frontend` — Angular 22 SPA with `@angular/cdk`, custom SCSS/CSS design tokens, and strict online connectivity monitoring.

### Central NuGet Package Inventory (`Directory.Packages.props`)
| Package | Version | Justification |
|---|---|---|
| `Microsoft.EntityFrameworkCore` / `.Design` | `10.0.15` | .NET 10 LTS ORM, migrations, and transactional unit of work. |
| `Npgsql.EntityFrameworkCore.PostgreSQL` / `Npgsql` | `10.0.3` | Native PostgreSQL 18 driver, `timestamptz`, `jsonb`, and partial index support. |
| `Serilog.AspNetCore` | `10.0.0` | Structured correlation-enriched diagnostics across requests and outbox workers. |
| `OpenTelemetry.Extensions.Hosting` / `.AspNetCore` / `.Http` | `1.21.0` | Local-first request and database latency/trace instrumentation. |
| `FluentValidation` / `.DependencyInjectionExtensions` | `12.2.0` | Structured field-level command/input validation mapped to `ApiErrorResponse`. |
| `Asp.Versioning.Mvc` / `.ApiExplorer` / `.OpenApi` | `10.2.1` | Strict `/api/v1/` URI segment versioning paired with .NET 10 OpenAPI per version. |
| `Microsoft.AspNetCore.OpenApi` | `10.0.15` | OpenAPI 3 document generation (`/openapi/v1.json`). |
| `Microsoft.Extensions.Hosting.WindowsServices` | `10.0.15` | Native Windows Service lifecycle integration for Mode A/B local LAN hosts. |
| `Microsoft.Web.WebView2` | `1.0.4314.35` | Embedded Chromium WebView2 host inside `Magic.Desktop.Wpf`. |
| `Microsoft.NET.Test.Sdk` (`18.12.0`), `xunit` (`2.9.3`), `xunit.runner.visualstudio` (`4.0.1`), `Microsoft.AspNetCore.Mvc.Testing` (`10.0.15`), `NetArchTest.Rules` (`1.3.2`) | `.NET 10 aligned` | Automated unit, Clean Architecture dependency boundary, and live PostgreSQL API integration suites. |
