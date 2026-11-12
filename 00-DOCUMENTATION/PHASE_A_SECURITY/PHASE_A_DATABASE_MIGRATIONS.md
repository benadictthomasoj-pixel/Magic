# MAGIC ERP — PHASE A DATABASE MIGRATIONS
## PostgreSQL Schema & Relational Specifications for Identity & Security

**Migration Name:** `20261112103009_AddSecurityAndIdentitySchema`  
**Target Schema:** `security`  
**Engines Applied:** `magic_dev` (Port 5432) and `magic_test` (Port 5432)  
**ORM:** Entity Framework Core 10  
**Date:** 2026-11-12  

---

## 1. Schema Overview

The `security` schema isolates all identity, authentication, access control, session lifecycle, and terminal pairing tables from business domain tables (`accounting`, `sales`, `inventory`).

```
                              [security.users]
                                │       ▲
           ┌────────────────────┼───────┴────────────────────┐
           ▼                    ▼                            ▼
[security.user_roles]  [security.refresh_tokens]  [security.user_branch_assignments]
           │
           ▼
  [security.roles] ◄─── [security.role_permissions] ───► [security.permissions]
```

---

## 2. Table Specifications

### 2.1. `security.users`
| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `uuid` | PK, Not Null | Unique identifier |
| `CompanyId` | `uuid` | Not Null | Multi-tenant company context |
| `Username` | `varchar(100)` | Not Null | Display username |
| `NormalizedUsername` | `varchar(100)` | Not Null, Unique per Company | Uppercase normalized username |
| `Email` | `varchar(256)` | Not Null | User email address |
| `NormalizedEmail` | `varchar(256)` | Not Null, Unique per Company | Uppercase normalized email |
| `DisplayName` | `varchar(200)` | Not Null | Full display name |
| `PasswordHash` | `varchar(500)` | Not Null | Base64 PBKDF2 HMAC-SHA512 subkey |
| `PasswordSalt` | `varchar(200)` | Not Null | Base64 128-bit cryptographically secure salt |
| `IsActive` | `boolean` | Not Null, Default `true` | Account active flag |
| `IsLockedOut` | `boolean` | Not Null, Default `false` | Brute-force lockout flag |
| `LockoutEndUtc` | `timestamp with time zone` | Nullable | Expiration timestamp of lockout |
| `FailedLoginAttempts` | `integer` | Not Null, Default `0` | Consecutive failed attempts counter |
| `DefaultBranchId` | `uuid` | Nullable | Default primary branch assignment |
| `CreatedAtUtc` | `timestamp with time zone` | Not Null | Timestamp created |
| `UpdatedAtUtc` | `timestamp with time zone` | Nullable | Timestamp last modified |
| `LastLoginAtUtc` | `timestamp with time zone` | Nullable | Timestamp last successful login |

**Indexes:**
- `IX_users_CompanyId_NormalizedUsername` (Unique)
- `IX_users_CompanyId_NormalizedEmail` (Unique)

---

### 2.2. `security.roles`
| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `uuid` | PK, Not Null | Unique role identifier |
| `CompanyId` | `uuid` | Not Null | Company context |
| `Code` | `varchar(50)` | Not Null, Unique per Company | Role code (`Administrator`, `Cashier`, etc.) |
| `Name` | `varchar(100)` | Not Null | Display name of role |
| `Description` | `varchar(500)` | Nullable | Detailed description of role duties |
| `IsSystem` | `boolean` | Not Null | System protected role flag |
| `CreatedAtUtc` | `timestamp with time zone` | Not Null | Timestamp created |

**Indexes:**
- `IX_roles_CompanyId_Code` (Unique)

---

### 2.3. `security.permissions`
| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `uuid` | PK, Not Null | Unique permission identifier |
| `Code` | `varchar(100)` | Not Null, Unique | Granular claim (`accounting:vouchers:create`) |
| `Module` | `varchar(50)` | Not Null | Module group (`Accounting`, `Billing`, `Admin`) |
| `Description` | `varchar(500)` | Not Null | Purpose of claim |
| `CreatedAtUtc` | `timestamp with time zone` | Not Null | Timestamp created |

**Indexes:**
- `IX_permissions_Code` (Unique)

---

### 2.4. `security.user_roles`
- **Composite Primary Key:** `(UserId, RoleId)`
- **Foreign Keys:**
  - `FK_user_roles_users_UserId` -> `security.users(Id)` (ON DELETE CASCADE)
  - `FK_user_roles_roles_RoleId` -> `security.roles(Id)` (ON DELETE CASCADE)

---

### 2.5. `security.role_permissions`
- **Composite Primary Key:** `(RoleId, PermissionId)`
- **Foreign Keys:**
  - `FK_role_permissions_roles_RoleId` -> `security.roles(Id)` (ON DELETE CASCADE)
  - `FK_role_permissions_permissions_PermissionId` -> `security.permissions(Id)` (ON DELETE CASCADE)

---

### 2.6. `security.user_branch_assignments`
- **Composite Primary Key:** `(UserId, BranchId)`
- **Columns:** `CompanyId` (UUID), `IsPrimary` (boolean), `AssignedAtUtc` (timestamptz)
- **Foreign Key:** `FK_user_branch_assignments_users_UserId` -> `security.users(Id)` (ON DELETE CASCADE)
- **Index:** `IX_user_branch_assignments_CompanyId_BranchId`

---

### 2.7. `security.refresh_tokens`
| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `uuid` | PK, Not Null | Unique token record identifier |
| `TokenHash` | `varchar(256)` | Not Null, Unique | SHA-256 hash of raw 64-byte token |
| `UserId` | `uuid` | Not Null, FK -> `security.users` | Owner user ID |
| `CompanyId` | `uuid` | Not Null | Company context |
| `BranchId` | `uuid` | Nullable | Branch context |
| `TerminalId` | `uuid` | Nullable | Terminal context |
| `TokenFamily` | `uuid` | Not Null | Family ID for reuse detection tracking |
| `IsRevoked` | `boolean` | Not Null | Revocation state |
| `RevokedAtUtc` | `timestamp with time zone` | Nullable | Revocation timestamp |
| `RevocationReason` | `varchar(200)` | Nullable | Reason for revocation |
| `ReplacedByTokenHash` | `varchar(256)` | Nullable | Pointer to subsequent rotated token hash |
| `ExpiresAtUtc` | `timestamp with time zone` | Not Null | 7-day expiration timestamp |
| `CreatedAtUtc` | `timestamp with time zone` | Not Null | Creation timestamp |
| `CreatedByIp` | `varchar(100)` | Nullable | Client IP address |

**Indexes:**
- `IX_refresh_tokens_TokenHash` (Unique)
- `IX_refresh_tokens_TokenFamily`
- `IX_refresh_tokens_UserId_IsRevoked`

---

### 2.8. `security.terminals`
| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `uuid` | PK, Not Null | Terminal identifier |
| `CompanyId` | `uuid` | Not Null | Company context |
| `BranchId` | `uuid` | Not Null | Physical branch context |
| `TerminalCode` | `varchar(50)` | Not Null | e.g. `POS-01` |
| `MachineName` | `varchar(200)` | Not Null | Workstation hostname |
| `OperatingSystem` | `varchar(100)` | Nullable | OS platform (e.g. Windows 11) |
| `IsAuthorized` | `boolean` | Not Null | Administrative authorization flag |
| `RegisteredAtUtc` | `timestamp with time zone` | Not Null | Registration timestamp |
| `LastHeartbeatAtUtc` | `timestamp with time zone` | Nullable | Last active timestamp |

**Indexes:**
- `IX_terminals_CompanyId_BranchId_TerminalCode` (Unique)

---

### 2.9. `security.audit_events`
| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `uuid` | PK, Not Null | Event identifier |
| `CompanyId` | `uuid` | Not Null | Company context |
| `BranchId` | `uuid` | Nullable | Branch context |
| `TerminalId` | `uuid` | Nullable | Terminal context |
| `UserId` | `uuid` | Nullable | Actor user ID |
| `Username` | `varchar(100)` | Nullable | Actor username |
| `EventType` | `varchar(100)` | Not Null | Event code (`LOGIN_SUCCESS`, `ACCOUNT_LOCKED`) |
| `IpAddress` | `varchar(100)` | Nullable | Client IP address |
| `DetailsJson` | `text` | Nullable | JSON payload of event metadata |
| `TimestampUtc` | `timestamp with time zone` | Not Null | UTC timestamp |

**Indexes:**
- `IX_audit_events_CompanyId_TimestampUtc`
- `IX_audit_events_EventType`

