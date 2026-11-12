# MAGIC — INDEPENDENT GREENFIELD PROJECT CHARTER

**Project Name:** MAGIC  
**Product Classification:** Universal Business Management ERP  
**Project Architecture:** Domain-Oriented Modular Monolith (Clean Architecture)  
**Creation Date:** 2026-11-11  
**Project Status:** Strict Planning & Architecture Review Only (Phase 0)  

---

## 1. Absolute Greenfield Declaration

**MAGIC is a 100% independent, greenfield software project.**

In accordance with explicit project governance directives:
1. **Zero Legacy Ties:** MAGIC maintains no connection, coupling, or shared assets with any prior legacy software, prior databases, historical APIs, legacy codebases, or past project directories.
2. **Clean-Slate Engineering:** No legacy code, database schemas, stored procedures, third-party libraries, or configuration files may be imported, copied, migrated, or reused.
3. **Sole System of Truth:** MAGIC is engineered strictly and exclusively from its own approved architectural specifications, domain models, and engineering protocols.
4. **Preservation Policy:** All existing MAGIC planning documentation created in `00-DOCUMENTATION/` is permanently preserved and protected against unauthorized modification or deletion.

---

## 2. Architecture Baseline & Open Decisions

In accordance with MAGIC documentation governance, confirmed baseline mandates are separated from open architectural decisions pending formal sign-off:

| Decision Area | Governance Status | Architectural Policy |
|---|---|---|
| **Technology Stack Baseline** | **Confirmed Baseline** | WPF + WebView2 (.NET 10 LTS), Angular + TypeScript + Signals + SCSS/CSS Tokens, ASP.NET Core (.NET 10 LTS), EF Core 10, Npgsql, PostgreSQL. |
| **Terminal Offline Buffering** | **UNRESOLVED (Proposed Only)** | Comparative evaluation active between Strict Online LAN vs. Local SQLite Buffering. Neither approved without explicit confirmation. |
| **Statutory e-Invoicing & e-Way** | **UNRESOLVED (Recommended: Offline Export)** | Evaluating NIC-Compliant Offline JSON/Excel Export vs. Direct GSP API integration. |
| **Inventory Valuation Switching** | **UNRESOLVED (Pending Accounting Review)** | Configurable FIFO & Moving Weighted Average; fiscal-year switch restriction pending statutory/accounting sign-off. |
| **Operating State** | **Strict No-Build (Phase 0)** | Zero source code, project scaffolding, package installation, or database initialization until explicit authorization. |

---

## 3. Technology Foundation for MAGIC

- **Desktop Shell:** Windows Presentation Foundation (WPF) on .NET 10 LTS
- **UI Container:** Microsoft Edge WebView2 (Chromium Evergreen)
- **Frontend Presentation:** Angular (TypeScript, SCSS & CSS Design Tokens, Reactive Forms, RxJS, Angular Signals initially)
- **Backend Host & Engine:** ASP.NET Core Web API on .NET 10 LTS (Versioned REST APIs, OpenAPI, Windows Service Hosting)
- **Relational Storage:** PostgreSQL with Logical Schema Domain Separation
- **ORM & Data Access:** Entity Framework Core 10 + Npgsql
- **Target OS:** Windows 10 / 11 (x64)

