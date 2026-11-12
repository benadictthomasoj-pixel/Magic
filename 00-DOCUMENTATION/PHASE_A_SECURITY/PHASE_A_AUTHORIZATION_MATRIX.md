# MAGIC ERP — PHASE A: AUTHORIZATION MATRIX & RBAC SPECIFICATION

**Document Version:** 1.0  
**Phase:** Phase A (Identity, Authentication & Tenancy Foundation)  
**Security Standard:** Role-Based Access Control (RBAC) + Tenancy Scoping  
**Status:** IMPLEMENTED & VERIFIED  

---

## 1. Executive Summary

MAGIC ERP enforces strict multi-tenant Role-Based Access Control (RBAC). Access decisions are evaluated at two layers:
1. **Server-Side API Gateway / Controller Boundary:** Every sensitive endpoint inspects verified cryptographic claims embedded in the caller's JWT. Claims (`perm`, `role`, `company_id`, `branch_id`) are cryptographically signed by the backend and cannot be spoofed.
2. **Client-Side Angular Navigation & UI Guards:** Route-level `canActivate: [authGuard]` and `canActivate: [permissionGuard]` prevent navigation to unauthorized views. Navigation items and operational buttons are conditionally rendered based on reactive signals in `AuthBoundaryService`.

---

## 2. Granular Permissions Registry

MAGIC registers 21 granular permissions spanning all core ERP functional areas:

| Permission Code | Module | Description |
|:---|:---|:---|
| `accounting:dashboard:view` | Accounting | View accounting health dashboard, financial KPIs, and metrics |
| `accounting:accounts:view` | Accounting | View hierarchical Chart of Accounts and account balances |
| `accounting:accounts:manage` | Accounting | Create, edit, activate, or deactivate ledger accounts |
| `accounting:vouchers:view` | Accounting | View Journal Voucher registry, search, and inspect voucher details |
| `accounting:vouchers:create` | Accounting | Create and save draft journal vouchers |
| `accounting:vouchers:post` | Accounting | Authorize and post balanced journal vouchers to the General Ledger |
| `accounting:vouchers:reverse` | Accounting | Generate counter-reversal journal vouchers with audit references |
| `accounting:ledger:view` | Accounting | Inspect General Ledger account statements, running balances, and Trial Balance |
| `accounting:periods:view` | Accounting | View fiscal years, accounting periods, and locking status |
| `accounting:periods:manage` | Accounting | Lock, unlock, close, and reopen fiscal accounting periods |
| `accounting:subledgers:view` | Accounting | View Accounts Receivable (AR) and Accounts Payable (AP) subledgers |
| `billing:dashboard:view` | Billing | View commercial billing overview, sales summaries, and counter metrics |
| `billing:pos:access` | Billing | Access retail POS checkout screen and barcode scanning interface |
| `billing:sales:view` | Billing | View commercial sales orders, dispatch notes, and sales tax invoices |
| `billing:sales:create` | Billing | Create, finalize, and print sales tax invoices |
| `billing:purchases:view` | Billing | View purchase orders, goods receipt notes (GRN), and vendor bills |
| `billing:inventory:view` | Billing | View inventory stock levels, warehouse batches, and stock movements |
| `admin:users:manage` | Admin | Create, edit, deactivate users and assign security roles |
| `admin:roles:manage` | Admin | Define custom roles and assign granular permission sets |
| `admin:terminals:manage` | Admin | Register, authorize, and revoke client hardware POS terminals |
| `admin:audit:view` | Admin | Inspect tamper-evident security and financial audit event logs |

---

## 3. Standard Roles & Permission Assignment Matrix

MAGIC provisions four pre-configured standard roles:

| Permission Code | System Administrator (`Administrator`) | Store Manager (`StoreManager`) | Senior Accountant (`Accountant`) | Front Desk Cashier (`Cashier`) |
|:---|:---:|:---:|:---:|:---:|
| `accounting:dashboard:view` | ✅ | ✅ | ✅ | ❌ |
| `accounting:accounts:view` | ✅ | ✅ | ✅ | ❌ |
| `accounting:accounts:manage` | ✅ | ✅ | ✅ | ❌ |
| `accounting:vouchers:view` | ✅ | ✅ | ✅ | ❌ |
| `accounting:vouchers:create` | ✅ | ✅ | ✅ | ❌ |
| `accounting:vouchers:post` | ✅ | ❌ | ✅ | ❌ |
| `accounting:vouchers:reverse` | ✅ | ❌ | ✅ | ❌ |
| `accounting:ledger:view` | ✅ | ✅ | ✅ | ❌ |
| `accounting:periods:view` | ✅ | ✅ | ✅ | ❌ |
| `accounting:periods:manage` | ✅ | ❌ | ✅ | ❌ |
| `accounting:subledgers:view` | ✅ | ✅ | ✅ | ❌ |
| `billing:dashboard:view` | ✅ | ✅ | ❌ | ✅ |
| `billing:pos:access` | ✅ | ✅ | ❌ | ✅ |
| `billing:sales:view` | ✅ | ✅ | ❌ | ✅ |
| `billing:sales:create` | ✅ | ✅ | ❌ | ✅ |
| `billing:purchases:view` | ✅ | ✅ | ❌ | ❌ |
| `billing:inventory:view` | ✅ | ✅ | ❌ | ❌ |
| `admin:users:manage` | ✅ | ❌ | ❌ | ❌ |
| `admin:roles:manage` | ✅ | ❌ | ❌ | ❌ |
| `admin:terminals:manage` | ✅ | ❌ | ❌ | ❌ |
| `admin:audit:view` | ✅ | ❌ | ❌ | ❌ |
| **Total Permissions Granted** | **21 / 21** | **15 / 21** | **11 / 21** | **4 / 21** |

---

## 4. API Endpoint Protection Mapping

All API endpoints are defended by the `[Authorize]` attribute on base controllers, supplemented by explicit `EnsurePermission(...)` verification:

### 4.1 Authentication Endpoints (`/api/v1/auth`)
| Method | Route | Permission Required | Notes |
|:---|:---|:---|:---|
| `POST` | `/api/v1/auth/login` | None (`[AllowAnonymous]`) | Protected by rate limiting and 5-attempt account lockout |
| `POST` | `/api/v1/auth/refresh` | None (`[AllowAnonymous]`) | Validates refresh token signature, expiry, and single-use rotation |
| `POST` | `/api/v1/auth/logout` | Authenticated session | Revokes refresh token and terminates session |
| `POST` | `/api/v1/auth/change-password` | Authenticated session | Requires verification of current password |
| `GET` | `/api/v1/auth/me` | Authenticated session | Returns claims, roles, and assigned permissions |
| `POST` | `/api/v1/auth/terminals/register` | None (`[AllowAnonymous]`) | Machine registration; pending administrative approval |

### 4.2 Accounting Endpoints (`/api/v1/accounting`)
| Method | Route | Permission Required | Enforcement Handler |
|:---|:---|:---|:---|
| `GET` | `/api/v1/accounting/accounts` | `accounting:accounts:view` | `ChartOfAccountsController.GetAccounts` |
| `GET` | `/api/v1/accounting/accounts/{id}` | `accounting:accounts:view` | `ChartOfAccountsController.GetAccountById` |
| `POST` | `/api/v1/accounting/accounts` | `accounting:accounts:manage` | `ChartOfAccountsController.CreateAccount` |
| `GET` | `/api/v1/accounting/vouchers` | `accounting:vouchers:view` | `JournalVouchersController.GetVouchers` |
| `GET` | `/api/v1/accounting/vouchers/{id}` | `accounting:vouchers:view` | `JournalVouchersController.GetVoucherById` |
| `POST` | `/api/v1/accounting/vouchers` | `accounting:vouchers:create` | `JournalVouchersController.CreateDraftVoucher` |
| `POST` | `/api/v1/accounting/vouchers/{id}/post` | `accounting:vouchers:post` | `JournalVouchersController.PostVoucher` |
| `POST` | `/api/v1/accounting/vouchers/{id}/reverse` | `accounting:vouchers:reverse` | `JournalVouchersController.ReverseVoucher` |
| `GET` | `/api/v1/accounting/fiscal-years` | `accounting:periods:view` | `FiscalGovernanceController.GetFiscalYears` |
| `POST` | `/api/v1/accounting/periods/{id}/lock` | `accounting:periods:manage` | `FiscalGovernanceController.LockPeriod` |
| `POST` | `/api/v1/accounting/periods/{id}/reopen` | `accounting:periods:manage` | `FiscalGovernanceController.ReopenPeriod` |

---

## 5. Security Invariant: Separation of Duties

MAGIC ERP enforces strict financial separation of duties:
1. **Posting Segregation:** The `StoreManager` role can create draft vouchers (`accounting:vouchers:create`) but **cannot** post vouchers to the general ledger or lock fiscal periods. Only `Administrator` and `Accountant` possess posting authority.
2. **Cashier Isolation:** The `Cashier` role is strictly confined to POS sales and receipt issuing. Any attempt by a cashier token to access accounting balances, chart of accounts, or administrative user lists immediately returns `HTTP 403 Forbidden` and emits a security audit violation.
3. **Multi-Tenant Boundary:** In addition to role permissions, `AccountingBaseController` matches the token's `company_id` and `branch_id` against the resource tenant. Cross-tenant reads and writes are prevented regardless of permission grants.
