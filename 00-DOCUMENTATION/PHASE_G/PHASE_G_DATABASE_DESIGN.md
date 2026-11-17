# MAGIC ERP — PHASE G: DATABASE DESIGN & SCHEMA SPECIFICATION

**Database Engine:** PostgreSQL 18 Local  
**Databases Updated:** `magic_dev` and `magic_test`  
**Migration Hash:** `20261113140631_AddPhaseGEnterpriseModules`  

---

### 1. Schema Breakdown & Table Registry

Phase G introduces 15 new relational tables distributed across 5 dedicated schemas:

| Schema Name | Table Name | Purpose | Primary Key | Key Foreign Keys |
|---|---|---|---|---|
| `branches` | `companies` | Corporate tenant legal profile & fiscal setup | `id` (UUID) | — |
| `branches` | `branches` | Physical retail stores, offices & fulfillment sites | `id` (UUID) | `company_id` -> `companies(id)` |
| `branches` | `terminal_registrations` | Counter POS hardware terminals & cashiers | `id` (UUID) | `branch_id` -> `branches(id)` |
| `branches` | `inter_branch_transfers` | Stock movement requests between branches | `id` (UUID) | `source_branch_id`, `destination_branch_id` |
| `branches` | `inter_branch_transfer_lines` | Itemized product quantities per transfer | `id` (UUID) | `transfer_request_id` -> `inter_branch_transfers(id)` |
| `administration` | `approval_workflows` | Configurable multi-tier monetary approval rules | `id` (UUID) | `company_id` |
| `administration` | `approval_requests` | Actionable document approval workflows | `id` (UUID) | `workflow_id` -> `approval_workflows(id)` |
| `administration` | `approval_actions` | Sign-off audit trail (Approve/Reject reasons) | `id` (UUID) | `request_id` -> `approval_requests(id)` |
| `automation` | `automation_rules` | Trigger-condition-action automation policies | `id` (UUID) | `company_id` |
| `automation` | `automation_executions` | Execution audit log with deduplication keys | `id` (UUID) | `rule_id` -> `automation_rules(id)` |
| `automation` | `notifications` | Internal in-app notification center items | `id` (UUID) | `company_id`, `branch_id` |
| `system_management` | `backups` | System backup records with SHA-256 checksums | `id` (UUID) | `company_id` |
| `system_management` | `system_configurations` | Runtime platform configuration parameters | `id` (UUID) | `company_id` |
| `reporting` | `report_definitions` | Dynamic saved query definitions | `id` (UUID) | `company_id` |
| `reporting` | `report_export_logs` | Audit trail of generated CSV/PDF exports | `id` (UUID) | `company_id` |

---

### 2. Indexes & Performance Tuning

1. **Multi-Column Filter Indexes:**
   - `ix_branches_company_code` on `branches.branches(company_id, branch_code)` UNIQUE.
   - `ix_terminals_company_code` on `branches.terminal_registrations(company_id, terminal_code)` UNIQUE.
   - `ix_transfers_company_status` on `branches.inter_branch_transfers(company_id, status)`.
   - `ix_notifications_unread` on `automation.notifications(company_id, is_read, created_at_utc DESC)`.
   - `ix_approvals_pending` on `administration.approval_requests(company_id, status)`.
   - `ix_backups_date` on `system_management.backups(company_id, created_at_utc DESC)`.
   - `ix_sys_config_key` on `system_management.system_configurations(company_id, config_key)` UNIQUE.

2. **Monetary Precision Standards:**
   - All monetary columns (`amount_paise`, `labor_total_paise`, `parts_total_paise`, `tax_amount_paise`, `minimum_amount_paise`) are strictly mapped as `bigint NOT NULL`.

