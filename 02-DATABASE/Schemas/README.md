# MAGIC Phase 1 PostgreSQL Schema Summary

In accordance with `DATABASE_DESIGN.md` and the Phase 1 technical foundation scope, only technical platform and immutable audit schemas exist in Phase 1. **Zero business module tables** (sales, billing, inventory, accounting, customers, tax rules) are created yet.

## Active Schemas (`magic_dev`)

### `platform` Schema
| Table | Purpose | Key Columns & Constraints |
|---|---|---|
| `platform.__ef_migrations_history` | EF Core 10 migration ledger | `"MigrationId"` (PK), `"ProductVersion"` |
| `platform.idempotency_records` | Duplicate-submission guard across LAN retries | `id` (UUID PK), unique `(branch_id, idempotency_key, endpoint_path)`, `request_hash`, `expires_at_utc` |
| `platform.outbox_messages` | Transactional outbox for native `.NET 10 BackgroundService` worker (`OPEN-004`) | `id` (UUID PK), partial index `ix_outbox_messages_unprocessed` on `occurred_on_utc WHERE processed_at_utc IS NULL`, `lock_token`, `lock_expires_at_utc` |

### `audit` Schema
| Table | Purpose | Key Columns & Constraints |
|---|---|---|
| `audit.system_audit_logs` | Immutable platform/security/override audit trail | `id` (`bigint GENERATED ALWAYS AS IDENTITY` PK), `event_timestamp_utc`, `branch_id`, `correlation_id`, `before_state` (`jsonb`), `after_state` (`jsonb`) |

## Reserved Schemas (Deferred to Phase 2+)
`core`, `catalog`, `inventory`, `sales`, `purchasing`, `accounting`, `tax`, `reporting`
