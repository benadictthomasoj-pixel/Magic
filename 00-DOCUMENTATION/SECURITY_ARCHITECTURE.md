# MAGIC ERP — SECURITY ARCHITECTURE SPECIFICATION
## Authoritative Identity, Authentication, Authorization & Auditing (OPEN-003)

**Document Version:** 2.0.0  
**Project:** MAGIC – Universal Business Management ERP  
**Standard:** OpenID Connect / OAuth 2.0 / ASP.NET Core Identity + JWT Bearer  
**Security Baseline:** Strict Fail-Closed Policy (OPEN-003 Approved Resolution)  
**Last Updated:** 2026-11-12  

---

## 1. Authentication & Token Architecture

MAGIC ERP implements standard ASP.NET Core Authentication with JWT Bearer tokens, providing enterprise-grade security for both single-store LAN and multi-store distributed environments.

### 1.1. Cryptographic Token Specifications
- **Access Tokens:**
  - Standard JWT formatted according to RFC 7519.
  - Signed with **HMAC-SHA256** (for single-store LAN deployments) or **ECDSA P-256 (`ES256`)** (for multi-branch / cloud deployments).
  - Short-lived lifespan: **15 minutes**.
  - Payload claims:
    - `sub`: User ID (UUID).
    - `name`: Username / Display Name.
    - `email`: Normalized user email.
    - `company_id`: Authorized Company ID.
    - `branch_id`: Authorized Primary Branch ID.
    - `terminal_id`: Active Terminal ID (if logged in from a designated POS terminal).
    - `roles`: Assigned roles (`Administrator`, `StoreManager`, `Cashier`, `Accountant`).
    - `permissions`: Fine-grained permission claims (e.g. `pos:checkout`, `sales:invoice:post`).

- **Refresh Tokens:**
  - 256-bit cryptographically secure random bytes generated via `RandomNumberGenerator.GetBytes(64)`.
  - Stored in PostgreSQL table `security.refresh_tokens` as a SHA-256 hash.
  - Lifespan: **7 days**.
  - **Single-Use Rotation:** Every token refresh revokes the presented refresh token and issues a new refresh token.
  - **Token Family Reuse Detection:** If an already-used or revoked refresh token is presented, the entire token family (all refresh tokens issued to that user session) is immediately revoked, and active sessions are terminated.

### 1.2. Password Storage Policy
- Passwords are never stored in plaintext or reversible encryption.
- Hashed using **PBKDF2 with HMAC-SHA512** with 100,000+ iterations and per-user cryptographically random 128-bit salt (or **Argon2id** where native libsodium is available).
- Enforced password policy: Minimum 10 characters, at least 1 uppercase, 1 lowercase, 1 number, and 1 symbol.
- Account lockout: 5 consecutive failed login attempts locks the account for 15 minutes, emitting security audit events.

---

## 2. Server-Side Context & Tenancy Enforcement

1. **Header vs. Token Verification:**
   - Client requests provide tenant headers (`X-Company-ID`, `X-Branch-ID`, `X-Terminal-ID`).
   - The backend `AccountingBaseController` and security middleware compare the provided headers against the claims embedded in the validated JWT.
   - If a client attempts to supply a `CompanyId` or `BranchId` that does not match their token claims, the request **fails immediately with HTTP 403 Forbidden**.
2. **EF Core Global Query Filters:**
   - Every domain query automatically binds to the current tenant:
     `modelBuilder.Entity<T>().HasQueryFilter(e => e.CompanyId == currentContext.CompanyId);`
   - Cross-tenant data leakage is structurally impossible at the ORM layer.

---

## 3. Role-Based Access Control (RBAC) & Supervisor Elevation

### 3.1. Role Hierarchy
- **`Administrator`**: Full platform and company configuration access.
- **`StoreManager`**: Branch-level supervision, inventory adjustments, price overrides, credit limit overrides.
- **`Accountant`**: Full Chart of Accounts, Vouchers, Period governance, Bank reconciliation.
- **`Cashier`**: High-speed retail POS checkout, shift opening/closing, cash drops.

### 3.2. Supervisor Elevation Service (`ISupervisorElevationService`)
For sensitive operations triggered during checkout (e.g. approving a line-item discount exceeding 10%, voiding an invoice line, overriding a customer credit limit):
- The POS terminal prompts for a **Supervisor PIN** or one-time approval token.
- The request transmits the supervisor authorization payload.
- The backend verifies the supervisor's permissions independently, creates a transient elevated scope for that single operation, and logs the supervisor's ID and reason to `audit.system_audit_logs`.

---

## 4. Tamper-Evident System Audit Trail

- Every financial mutation and security event is recorded in `audit.system_audit_logs`.
- **Database Engine Protection:** PostgreSQL trigger `trg_prevent_audit_log_modification` blocks any `UPDATE` or `DELETE` with SQLSTATE `55000`.
- Recorded metadata:
  - Timestamp (UTC).
  - Actor User ID & Username.
  - Company ID, Branch ID, Terminal ID.
  - Client IP Address and User-Agent.
  - Action (`VOUCHER_POST`, `ACCOUNT_CREATE`, `INVOICE_VOID`, `PERIOD_LOCK`).
  - Target Entity Type and Entity ID.
  - State Diff JSON (Before and After snapshots).

---

## 5. Security Rules Compliance

1. **Zero Hardcoded Credentials:** No hardcoded admin passwords, backdoor keys, or bypass tokens exist in any source file or migration.
2. **Fail-Closed Default:** Unauthenticated requests to protected endpoints return `401 Unauthorized` (`AUTH_REQUIRED_OPEN_003`).
3. **No Mock Identities in Production:** All testing fixtures that require test tokens use ephemeral cryptographic keys generated during test harness execution.

