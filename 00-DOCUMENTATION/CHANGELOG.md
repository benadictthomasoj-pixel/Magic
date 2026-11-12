# MAGIC — PROJECT CHANGELOG

All notable changes to the MAGIC architecture, specifications, and project assets are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [0.2.0-planning] - 2026-11-12

### Changed & Harmonized
- Standardized all 25+ planning documents strictly onto **MAGIC** greenfield project identity (purged residual legacy naming references).
- Updated runtime & ORM target baseline to **.NET 10 LTS** and **Entity Framework Core 10** across architecture and migration specs.
- Aligned transaction boundary rules so critical financial & stock mutations run synchronously in PostgreSQL ACID transactions while Outbox handles secondary async side effects.
- Eliminated circular dependency between `Sales/POS` and `Marketing` in `MODULE_DEPENDENCIES.md` and `MODULE_CATALOGUE.md`.
- Synchronized Chart of Accounts leaf codes across `ACCOUNTING_RULES.md` and `ACCOUNTING_POSTING_MATRIX.md`, corrected `TX-01/02` rounding parity, and fixed sample GST arithmetic in `AUTO_STOCK_UPDATE_SPECIFICATION.md`.
- Separated Confirmed Baseline Decisions from Unresolved Architectural Evaluations in `DECISION_LOG.md` and `MAGIC_PROJECT_CHARTER.md`.

### Added
- Consolidated Final Architecture Specification (`MAGIC_FINAL_ARCHITECTURE_SPECIFICATION.md`) and updated Consistency Audit Report (`ARCHITECTURE_CONSISTENCY_AUDIT.md`).

## [0.1.0-planning] - 2026-11-11

### Added
- Initial Phase 0 Planning & Architecture Review Suite (`00-DOCUMENTATION/`).


## [0.1.0-phase1] — 2026-11-12

### Added (Phase 1 Technical Foundation)
- Initialized Git repository on `main` (`.gitignore` protecting `00-DOCUMENTATION/`) with Phase 0 documentation baseline commit `737bcda`.
- Provisioned isolated PostgreSQL 18 development database `magic_dev` owned by dedicated role `magic_dev`, leaving pre-existing cluster databases untouched.
- Implemented `.NET 10` Clean Architecture solution (`Magic.sln`): `Magic.Contracts`, `Magic.Domain`, `Magic.Application`, `Magic.Infrastructure`, `Magic.Platform`, and `Magic.Api` with centralized package management (`Directory.Packages.props`) and analyzer enforcement (`Directory.Build.props`).
- Applied EF Core 10 migration `20261112043621_InitialPlatformFoundation` creating `platform.outbox_messages`, `platform.idempotency_records`, `audit.system_audit_logs`, and `platform.__ef_migrations_history` (plus `02-DATABASE/Migrations/0001_InitialPlatformFoundation.sql`).
- Implemented `01-SRC/Frontend` Angular 22 + `@angular/cdk` responsive shell, layered SCSS/CSS tokens, tri-state color-scheme switching, strict online LAN connectivity monitor, deferred `OPEN-003` boundary service, and WebView2 bridge service.
- Authored `01-SRC/Desktop/Magic.Desktop.Wpf` WPF + WebView2 container scaffold (`EnableWindowsTargeting=true`; runtime pending Windows 10/11 verification).
- Added Windows Service and Mode A/Mode B deployment templates (`04-DEPLOYMENT/`).
- Verified `15/15` `.NET` tests (`Unit`, `Architecture`, `Integration`) and `8/8` Angular Vitest specs passing.
