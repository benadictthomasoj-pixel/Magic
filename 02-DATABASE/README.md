# MAGIC — Phase 1 Database Foundation (`02-DATABASE/`)

This directory holds version-controlled EF Core 10 schema evolution assets for MAGIC against PostgreSQL (`magic_dev` in local development).

## Strict Phase 1 Boundaries
- **Approved Schemas Created in Phase 1:**
  - `platform` — Technical infrastructure tables ONLY (`outbox_messages`, `idempotency_records`).
  - `audit` — Immutable append-only audit trail (`system_audit_logs`).
- **Deferred Business Schemas:**
  - No business module tables (`sales`, `purchases`, `inventory`, `products`, `accounting`, `finance`, `tax`, `customers`, `suppliers`, `employees`, `payroll`, `services`, `marketing`, `automation`, `orders`) or concrete authentication identity tables (`auth.*` deferred per `OPEN-003`) are created in Phase 1.
- **No SQLite / No Offline Sync:**
  - Strict Online LAN Mode confirmed (`OPEN-001`). Zero local SQLite buffers exist.

## Folders
- `Migrations/` — Exported idempotent SQL scripts verified from EF Core 10 migrations (`dotnet ef migrations script --idempotent`).
- `Schemas/` — Baseline schema documentation and invariant verification notes.
- `Seeds/` — Empty in Phase 1 (no test or synthetic business master data seeded prematurely).

