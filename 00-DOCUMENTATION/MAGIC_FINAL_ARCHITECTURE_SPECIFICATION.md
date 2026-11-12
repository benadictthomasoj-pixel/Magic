# MAGIC — CONSOLIDATED FINAL ARCHITECTURE SPECIFICATION (PHASE 0)

**Project:** MAGIC — Universal Business Management ERP  
**Version:** 1.0.0-phase0  
**Stage:** Phase 0 — Architecture Initialization & Planning Only (Strict No-Build)  
**Last Updated:** 2026-11-12  

---

> [!IMPORTANT]
> **STRICT NO-BUILD & GREENFIELD DECLARATION**  
> MAGIC is an independent, greenfield software platform built strictly from scratch with zero links, code reuse, or schema dependencies on any legacy application. No source code, project scaffolding, dependencies, database instances, or build executions have been created. Implementation begins only upon explicit user authorization.

---

## 1. Executive Consolidation & Document Audit Summary

All **26 planning documents** (27 total files in `00-DOCUMENTATION/` including `ARCHITECTURE_CONSISTENCY_AUDIT.md`) have been verified line-by-line as documented in `ARCHITECTURE_CONSISTENCY_AUDIT.md`. Prior draft inconsistencies—including residual legacy naming, `.NET 9` / `EF Core 9` stack mismatches, a `Sales <-> Marketing` dependency cycle, an async vs. sync transaction contradiction, COA leaf code omissions, and an Auto Stock Update sample math error—have been reconciled across the documentation suite.

---

## 2. Verified Technology Architecture & Supporting Evaluations

| Layer | Confirmed Technology | Architectural Role & Verification |
|---|---|---|
| **Desktop Host** | **WPF + C# (.NET 10 LTS)** | Native Windows shell, lifecycle, hardware peripherals (ESC/POS, serial scales, cash drawers), Windows notifications, update verification. Zero duplicated business logic. |
| **Embedded Browser** | **Microsoft WebView2** | Chromium runtime hosting the Angular SPA via a secure native bridge (`postMessage` / Host Objects). |
| **Frontend** | **Angular + TypeScript** | Feature-oriented SPA, lazy routing, Reactive Forms, RxJS, **Angular Signals initially** (NgRx evaluated later only if justified), **SCSS & CSS design tokens**, `HttpClient`. |
| **Backend** | **ASP.NET Core Web API (.NET 10 LTS)** | Modular Monolith API host, versioned REST (`/api/v1/`), **OpenAPI**, **ASP.NET Core Windows Service** hosting, business logic, tax & accounting engines. |
| **ORM & Provider** | **Entity Framework Core 10 + Npgsql** | Version-controlled code-first migrations, custom value converters (`Money` -> `long` paise), audit interceptors, optimistic/pessimistic locking. |
| **Database** | **PostgreSQL** | Single authoritative relational database per business deployment initially, partitioned by logical domain schemas. Zero direct frontend/WPF DB access. |
| **Target OS** | **Windows 10 / 11 (x64)** | Primary business counter and back-office workstation environment. |

### Supporting Technology Evaluations
- **Structured Logging:** **Serilog** (low-overhead rolling local file/JSON diagnostic sinks with correlation ID enrichment).
- **Observability:** **OpenTelemetry** (.NET instrumentation for request latency, EF Core query timing, and local trace correlation without external cloud collectors in V1).
- **Validation:** **ASP.NET Core validation + FluentValidation** in Application Layer command pipelines.
- **Authentication:** **ASP.NET Core Identity** (customized with Argon2id `IPasswordHasher<TUser>`) vs. custom lightweight token provider *(tracked in `OPEN-003`)*.
- **UI Component Library:** **Angular Material + CDK** vs. **PrimeNG** vs. **Custom Angular CDK + SCSS Design Tokens** *(tracked in `OPEN-002`)*.
- **Background Jobs:** **.NET 10 `IHostedService` / `BackgroundService`** backed by PostgreSQL `platform.outbox_messages` for V1; Quartz.NET evaluated only if complex cron persistence becomes necessary *(tracked in `OPEN-004`)*.

---

## 3. Five-Layer Clean Architecture (Modular Monolith)

```
+---------------------------------------------------------------------------------------------------+
| LAYER 1: EXPERIENCE LAYER                                                                         |
|   WPF Desktop Host (.NET 10 LTS) <---> WebView2 <---> Angular Feature SPA (Signals, SCSS Tokens)  |
+---------------------------------------------------------------------------------------------------+
                                                  |  HTTPS / Versioned REST API (/api/v1/)
                                                  v
+---------------------------------------------------------------------------------------------------+
| LAYER 2: APPLICATION LAYER                                                                        |
|   Use Cases (Commands/Queries), Application Service Orchestrators, FluentValidation, RBAC Guards  |
+---------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+---------------------------------------------------------------------------------------------------+
| LAYER 3: DOMAIN LAYER (Zero External Framework / DB / UI Dependencies)                            |
|   Aggregates, Entities, Value Objects (Money, Quantity, GstRate), Invariants, Domain Services     |
+---------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+---------------------------------------------------------------------------------------------------+
| LAYER 4: INFRASTRUCTURE LAYER                                                                     |
|   EF Core 10 + Npgsql, PostgreSQL Schemas, Outbox Workers, Hardware Drivers, Serilog, Backups     |
+---------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+---------------------------------------------------------------------------------------------------+
| LAYER 5: PLATFORM LAYER                                                                           |
|   Windows Service Lifecycle, Security Infra, Update Signatures (Ed25519), Diagnostics, LAN Config |
+---------------------------------------------------------------------------------------------------+
```

**Dependency Rule:** Inner layers (Domain) never depend on Infrastructure, EF Core, WPF, or Angular.

---

## 4. Seven Business Domains & 21-Module Ownership Map

| Domain | Modules & Identifiers | Owned PostgreSQL Schemas / Tables |
|---|---|---|
| **A. Business Operations** | `MOD-02` POS & Billing, `MOD-03` Sales Mgmt, `MOD-04` Purchase & Procurement, `MOD-15` Order Mgmt, `MOD-16` Service Mgmt | `pos.*`, `sales.*`, `purchases.*`, `orders.*`, `services.*` |
| **B. Products & Supply Chain** | `MOD-05` Inventory, Warehouse & Product Catalogue, `MOD-10` Supplier Mgmt | `products.*`, `inventory.*`, `suppliers.*` |
| **C. Finance & Accounting** | `MOD-06` Accounting Core, `MOD-07` Financial Mgmt & Treasury, `MOD-11` Expense Mgmt | `accounting.*`, `finance.*`, `expenses.*` |
| **D. Tax & Compliance** | `MOD-08` Tax & Statutory Compliance (GST, HSN/SAC, ITC, GSTR Exports) | `tax.*` |
| **E. Customer & Growth** | `MOD-09` Customer CRM & Loyalty, `MOD-14` Marketing, Promotions & Coupons | `customers.*`, `marketing.*` |
| **F. People & Organisation** | `MOD-12` Human Resources, `MOD-13` Payroll, `MOD-18` Multi-Branch & Multi-Terminal | `employees.*`, `payroll.*`, `organization.*` |
| **G. Platform & Intelligence** | `MOD-01` Dashboard & BI, `MOD-17` Reports & Analytics, `MOD-20` Automation & AI (Auto Stock Update), `MOD-19` Admin & Security, `MOD-21` System Mgmt | `bi.*`, `reports.*`, `automation.*`, `auth.*`, `audit.*`, `platform.*` |

---

## 5. Module Dependency DAG & Transaction Protocols

1. **Strict Acyclic Hierarchy:**
   - **Level 0:** `MOD-21` System Mgmt, `MOD-19` Admin & Security
   - **Level 1:** `MOD-18` Multi-Branch, `MOD-06` Accounting, `MOD-08` Tax
   - **Level 2:** `MOD-05` Inventory & Products, `MOD-09` Customers, `MOD-10` Suppliers, `MOD-12` HR, `MOD-14` Marketing
   - **Level 3:** `MOD-03` Sales, `MOD-04` Purchases, `MOD-07` Finance, `MOD-11` Expenses, `MOD-13` Payroll, `MOD-15` Orders, `MOD-16` Services
   - **Level 4:** `MOD-02` POS & Billing
   - **Level 5:** `MOD-01` Dashboard & BI, `MOD-17` Reports, `MOD-20` Automation & AI
2. **Synchronous ACID Core Workflows:** Sales invoice posting, POS checkout, Purchase receipt, and Auto Stock Update commit coordinate `Sales/Purchases` + `Inventory` + `Payment` + `Accounting` + `Customer/Supplier Subledger` **synchronously inside one PostgreSQL ACID transaction** via approved interfaces in `Contracts/` with row-level locking (`SELECT ... FOR UPDATE`) and unique `Idempotency-Key` records.
3. **Secondary Asynchronous Outbox:** `platform.outbox_messages` dispatches secondary work post-commit (BI KPI refresh, loyalty accrual, campaign attribution).

---

## 6. Accounting, Financial & Inventory Invariants

- **Money Representation:** 64-bit signed integer paise (`long` / `BIGINT`), where ₹1 = 100 paise. Zero binary floating-point usage for currency. Fractional quantities and unit rates use `decimal` / `NUMERIC(14,4)` and `NUMERIC(18,4)`.
- **Reverse Inclusive GST:**
  $$\text{TaxablePaise} = \text{Round}\left(\frac{\text{GrossPaise} \times 10,000}{10,000 + \text{RateBps}}\right), \quad \text{TotalGSTPaise} = \text{GrossPaise} - \text{TaxablePaise}$$
  Intra-state split: $\text{CGST} = \lfloor \text{TotalGST} / 2 \rfloor$, $\text{SGST} = \text{TotalGST} - \text{CGST}$ (ensuring zero dropped paise).
- **Double-Entry Immutability:** Every posted voucher enforces $\sum \text{DebitPaise} \equiv \sum \text{CreditPaise}$. Posted journals are immutable; corrections require reversal vouchers (`reversal_of_voucher_id`) and replacements. AR/AP subledgers reconcile with GL control accounts `1130` and `2110`. Period locks require authorized reopening.
- **Inventory & Auto Stock Update:** Configurable FIFO and Moving Weighted Average valuation. Auto Stock Update parses structured wholesale invoice inputs into an isolated staging review grid (`automation.*`), enforcing SKU/barcode matching, separate purchase/selling/MRP pricing, duplicate invoice detection, and GST total checks before owner confirmation commits stock and GL entries atomically.

---

## 7. Security, Deployment & Testing Baseline

- **Security:** Argon2id password hashing, granular RBAC (`domain:resource:action`), server-side authorization on all endpoints, supervisor override approvals (`X-Supervisor-Auth-Token`), immutable append-only audit logs (`audit.system_audit_logs`), authenticated terminal registration, zero PostgreSQL exposure over LAN.
- **Deployment:** Local-first V1 operating in **Mode A (Single Computer)** or **Mode B (LAN Dedicated Server running ASP.NET Core Windows Service + PostgreSQL + WPF/WebView2 Clients)**. Scheduled `pg_dump` backups with SHA-256 verification and isolated restore testing. Legitimate Windows Authenticode executable signing separated from Ed25519 application update signatures; never bypass Windows Defender or Smart App Control.
- **Testing:** Multi-tier pyramid covering xUnit domain unit tests, Testcontainers PostgreSQL integration tests, 100k-case GST/rounding financial tests, multi-terminal checkout concurrency/idempotency tests, and Playwright (WebView2 CDP) + FlaUI (WPF) E2E tests.

---

## 8. Architectural Risks & Unresolved Decisions (Requiring User Sign-Off)

### Critical Risks Tracked
1. **Offline Billing vs. Statutory & Stock Integrity:** Enabling offline SQLite buffering risks stock overselling, credit-limit double-spending, and GST Rule 46 invoice sequence gaps.
2. **UI Dependency Bloat:** Selecting a heavy UI suite may add upgrade friction in Angular; a lightweight CDK + SCSS design token core mitigates lock-in.
3. **Windows SmartScreen / Unsigned Binary Friction:** Initial builds without EV Authenticode signing will trigger SmartScreen prompts on Windows 10/11 unless signed via legitimate low-cost/OSS or organizational certificates.
4. **Cross-Schema Coupling:** Improper EF Core navigation properties across module schemas can erode modular boundaries; enforced via NetArchTest architectural tests.

### Open Decisions Waiting Explicit Confirmation (`DECISION_LOG.md`)
1. **OPEN-001 (High Priority):** **Strict Online LAN Operation** *(Recommended for V1)* vs. **Local SQLite Buffering & Synchronization** (`OFFLINE_VS_ONLINE_COMPARATIVE_ANALYSIS.md`).
2. **OPEN-002:** Frontend UI Component Library (**Angular Material + CDK** vs. **PrimeNG** vs. **Pure Angular CDK + Custom SCSS Design Tokens**).
3. **OPEN-003:** Authentication Token Engine (**ASP.NET Core Identity + Argon2id + Standard ECDSA/HMAC JWT** vs. **Custom Ed25519 JWT Provider**).
4. **OPEN-004:** Background Job Runner (**Native .NET 10 `IHostedService` + Outbox** vs. **Quartz.NET**).
5. **OPEN-005:** Statutory NIC e-Invoicing/e-Way export scope (Offline JSON/Excel vs. GSP API), Fiscal-Year Valuation Policy lock, and GST invoice vs. line-level rounding confirmation under current Indian GST rules.

---

## 9. Phased Implementation Roadmap (Blocked Pending Authorization)

- **Phase 0 (Current):** Requirements & Architecture Specification *(Strict No-Build)*
- **Phase 1:** Desktop WPF Shell, WebView2 Bridge, ASP.NET Core .NET 10 Host, PostgreSQL Schemas, Auth, Audit, Backups
- **Phase 2:** Double-Entry Accounting Core, COA, Vouchers, Subledgers, Period Locks
- **Phase 3:** POS, Product Catalogue, Inventory (FIFO/Moving Avg), Sales & Purchases
- **Phase 4:** Financial Management (BRS, Fixed Assets), Tax Engine (GST/HSN/Exports), Expenses
- **Phase 5:** Customer CRM, Loyalty, Suppliers & Marketing Promotions
- **Phase 6:** HR, Payroll, Order Fulfillment & Service Management
- **Phase 7:** BI Dashboards, Financial Reports Suite & Auto Stock Update
- **Phase 8:** Multi-Branch, Multi-Terminal LAN Hardening & Future Expansion
- **Phase 9:** Concurrency Stress Testing, Windows Packaging, Authenticode Signing & Production Release

---

## 10. Phase 1 Foundation Transition Addendum (2026-11-12)

Phase 1 technical foundation has been implemented under approved decisions `OPEN-001` (**Strict Online LAN**), `OPEN-002` (**Pure Angular CDK + SCSS/CSS Tokens**), `OPEN-003` (**Deferred Auth/Tokens — Boundaries Only**), and `OPEN-004` (**Native `.NET 10 BackgroundService` + PostgreSQL Outbox**):
- **Solution & Tooling:** `Magic.sln` across `.NET 10.0.403`, Node `v26.5.0`, Angular CLI `22.2.6`, and isolated PostgreSQL `18.6` (`magic_dev`).
- **Verification Snapshot:** `.NET` Release build (`0` warnings, `0` errors), `15/15` `.NET` automated tests passing, Angular production bundle built (`351.44 kB` raw / `90.85 kB` transfer) with `8/8` Vitest specs passing, and `Magic.Desktop.Wpf` cross-targeted compile clean (**Pending Windows 10/11 Verification** for live desktop runtime/WebView2/Windows Service execution).
- **Next Gate:** All business modules (`Accounting`, `Tax`, `Inventory`, `POS`, `Sales`, `Purchases`, etc.) and concrete `OPEN-003` authentication implementation remain deferred pending explicit user authorization.
