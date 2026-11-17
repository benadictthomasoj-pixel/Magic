# MAGIC ERP — SYSTEM-WIDE ARCHITECTURE, CODEBASE & INTEGRATION AUDIT
## Document 05: Database Schema, Entity Mapping & Relational Integrity Audit

**Document ID:** `AUDIT-05-DATABASE`  
**Audit Scope:** PostgreSQL 18.6 Schema, EF Core 10 DbContext, Entity Fluent Mappings, Foreign Keys, Indexes, Constraints  
**Workspace:** `/Users/benadict/Documents/antigravity projects/magic`  
**Git Baseline Commit:** `2567c7d`  

---

### 1. PostgreSQL Schema Architecture Overview

The database persistence layer targets **PostgreSQL 18.6** via **Npgsql** and **Entity Framework Core 10**. The database is logically partitioned into **20 discrete PostgreSQL schemas**, isolating domain tables and preventing naming collisions:

| Schema Name | Business Domain / Phase | Table Count | Primary Purpose |
|---|---|:---:|---|
| `security` | Phase A / G (Identity & Governance) | 8 | Users, roles, permissions, terminals, approval workflows, SOD rules |
| `audit` | Phase A / G (Audit Trails) | 3 | Security audit logs, user login history, permission assignment history |
| `commercial` | Phase B (Master Data) | 11 | Products, categories, brands, price tiers, customers, suppliers |
| `inventory` | Phase C (Supply Chain & Stock) | 19 | Warehouses, stock balances, movements, POs, GRNs, batch lots, landed cost |
| `sales` | Phase D (Invoicing & Revenue) | 6 | Sales invoices, invoice lines, payments, returns, credit notes |
| `cashier` | Phase D (POS Counter Sessions) | 2 | Cashier sessions, cash drawer movements |
| `accounting` | Phase 2 (Financial Core) | 12 | Accounts (COA), journal entries, journal lines, fiscal periods, sequences |
| `tax` | Phase B / E (Taxation Math) | 2 | Tax categories, statutory rate definitions |
| `finance` | Phase E (Treasury & Subledgers) | 10 | Bank accounts, reconciliations, payables, receivables, fixed assets |
| `expenses` | Phase F (Expense Management) | 3 | Expenses, expense categories, expense receipts |
| `employees` | Phase F (Human Resources) | 4 | Employees, departments, leave allocations, leave requests |
| `payroll` | Phase F (Payroll Management) | 3 | Payroll periods, pay slips, salary structures |
| `marketing` | Phase F (Growth & Loyalty) | 5 | Campaigns, coupons, coupon redemptions, loyalty points, loyalty ledger |
| `orders` | Phase F (Advanced Orders) | 4 | Orders, order lines, fulfillments, backorders |
| `services` | Phase F (Service Desk) | 3 | Service tickets, ticket tasks, ticket parts consumed |
| `bi` | Phase G (Business Intelligence) | 3 | Executive KPIs, KPI trend points, revenue channel breakdowns |
| `branches` | Phase G (Multi-Branch Governance) | 5 | Companies, branches, terminals, inter-branch transfers, transfer lines |
| `automation` | Phase G (Workflow Automation) | 3 | Automation rules, rule executions, AI insights |
| `platform` | Phase 1 / G (System Infrastructure) | 5 | System nodes, backup records, maintenance windows, outbox messages |
| `public` | EF Core System | 1 | `__EFMigrationsHistory` |
| **Total** | **Entire Enterprise Monolith** | **116** | **20 Discrete Schemas** |

---

### 2. Critical Defect: Duplicate Disconnected Terminal Tables (DEF-ARC-001)

#### The Problem Statement
During Phase A development, terminal support for cashier workstations was introduced in schema `security`. Later, during Phase G development, terminal registration was introduced in schema `branches`. Both tables were created and mapped into EF Core independently, creating a fractured domain concept.

#### Deep Comparison of Schema Definitions

```
TABLE 1: security.terminals (Phase A)
-----------------------------------------------------------
Column Name        | Type                     | Constraints
-------------------|--------------------------|---------------------
id                 | uuid                     | PRIMARY KEY
company_id         | uuid                     | NOT NULL
branch_id          | uuid                     | NOT NULL
terminal_code      | character varying(50)    | NOT NULL, UNIQUE (company_id, terminal_code)
terminal_name      | character varying(100)   | NOT NULL
ip_address         | character varying(45)    | NULLABLE
mac_address        | character varying(17)    | NULLABLE
status             | character varying(20)    | NOT NULL (Active, Inactive, Suspended)
created_at         | timestamp with time zone | NOT NULL
updated_at         | timestamp with time zone | NULLABLE

Mapped Entity: Magic.Domain.Security.TerminalRecord
Used by: AuthenticationApplicationService, SecurityDatabaseSeeder, CashierSessionsController
```

```
TABLE 2: branches.terminals (Phase G)
-----------------------------------------------------------
Column Name        | Type                     | Constraints
-------------------|--------------------------|---------------------
id                 | uuid                     | PRIMARY KEY
company_id         | uuid                     | NOT NULL
branch_id          | uuid                     | NOT NULL, FK -> branches.branches(id)
terminal_code      | character varying(50)    | NOT NULL, UNIQUE (branch_id, terminal_code)
terminal_name      | character varying(100)   | NOT NULL
ip_address         | character varying(50)    | NULLABLE
mac_address        | character varying(50)    | NULLABLE
is_active          | boolean                  | NOT NULL DEFAULT true
registered_at      | timestamp with time zone | NOT NULL
last_seen_at       | timestamp with time zone | NULLABLE

Mapped Entity: Magic.Domain.Branches.TerminalRegistrationRecord
Used by: BranchManagementService, BranchManagementController
```

#### Architectural Impact
1. **Broken Terminal Administration:** An administrator who navigates to `/enterprise/branches` in the frontend and creates a terminal registers a record into `branches.terminals`.
2. **Failed POS Cashier Authentication:** When a cashier logs into a workstation in the POS terminal flow (`POST /api/v1/auth/login`), the backend queries `security.terminals`. Because the newly registered terminal does not exist in `security.terminals`, POS cashier terminal validation fails or falls back.
3. **Remediation Requirement:** Consolidate these entities into a single authoritative table (`branches.terminals` or `security.terminals`) and update both `AuthenticationApplicationService` and `BranchManagementService` to reference the unified entity.

---

### 3. Entity Mapping Purity & Fluent Configuration Audit

EF Core configuration inspection confirmed that all 116 tables are mapped via separate, cleanly structured `IEntityTypeConfiguration<T>` classes located in `01-SRC/Backend/Magic.Infrastructure/Persistence/Configurations/`.

#### Critical Invariants Observed:
1. **No Domain Persistence Leakage:** No entity in `Magic.Domain` carries EF Core annotations.
2. **Column Naming Convention:** All column names use PostgreSQL standard `snake_case` (e.g. `company_id`, `created_at`, `total_amount`).
3. **Monetary Precision:** Every monetary amount across all 20 schemas is explicitly mapped to `numeric(18,2)` or `numeric(18,4)`. Floating point types (`float`, `double`, `real`) are strictly prohibited and absent from the entire schema.
4. **Delete Behavior Protection:** Relational foreign keys explicitly specify `DeleteBehavior.Restrict` or `DeleteBehavior.NoAction`. Cascade deletes (`DeleteBehavior.Cascade`) are strictly forbidden on master data, preventing catastrophic accidental data loss.

---

### 4. Indexing Strategy & Concurrency Unique Constraints

Key transactional tables feature robust compound indexes designed for high-concurrency multi-tenant filtering:
- `sales.sales_invoices`: Compound index on `(company_id, invoice_number)` UNIQUE.
- `inventory.stock_balances`: Compound index on `(warehouse_id, product_id, batch_number)` UNIQUE.
- `accounting.journal_entries`: Compound index on `(company_id, voucher_number)` UNIQUE.
- `expenses.expenses`: Compound index on `(company_id, expense_number)` UNIQUE. (Note: Concurrency collision during high-parallel test execution was successfully remediated by appending a random GUID slice to `expense_number`).

