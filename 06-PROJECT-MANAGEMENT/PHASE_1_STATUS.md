# MAGIC Project Management (`06-PROJECT-MANAGEMENT/`)

## Phase 1 Foundation Status Tracker
- **Phase 0 (Architecture & Planning):** Completed — Baseline archived at Git commit `737bcda` in `00-DOCUMENTATION/`.
- **Phase 1 (Technical Foundation):** **Completed on macOS ARM64 Baseline** (2026-11-12).
  - `OPEN-001` **Approved:** Strict Online LAN Operation enforced (zero SQLite / offline sync).
  - `OPEN-002` **Approved:** Pure Angular CDK (`^22.2.7`) + layered SCSS & CSS design tokens (zero Angular Material / PrimeNG).
  - `OPEN-003` **Deferred:** Concrete authentication & token crypto deferred; fail-closed interfaces and boundaries active.
  - `OPEN-004` **Approved:** Native `.NET 10` `BackgroundService` + PostgreSQL `platform.outbox_messages` (zero Quartz.NET).
- **Verified Build & Test Results (macOS ARM64 + PostgreSQL 18.6 `magic_dev`):**
  - `dotnet build Magic.sln --configuration Release`: **0 warnings, 0 errors** (10 projects including cross-targeted `Magic.Desktop.Wpf`).
  - `dotnet test Magic.sln --configuration Release --no-build`: **15 / 15 passed** (`Magic.Domain.UnitTests` 5/5, `Magic.ArchitectureTests` 4/4, `Magic.Api.IntegrationTests` 6/6).
  - `npx ng build`: Production bundle clean (`351.44 kB` initial raw / `90.85 kB` transfer).
  - `npx ng test --watch=false` (`Vitest 5.1.0`): **6 / 6 test files, 8 / 8 tests passed**.
- **Platform Caveat (Mandatory):** Live WPF window startup, Microsoft WebView2 runtime initialization, hardware peripheral bridging, and Windows Service execution (`04-DEPLOYMENT/Windows/Register-MagicWindowsService.ps1`) cannot run on macOS ARM64 and remain **Pending Windows 10/11 Verification**.
- **Phase 2 Gate:** Accounting & business modules await explicit user authorization and concrete `OPEN-003` authentication decisions where required.
