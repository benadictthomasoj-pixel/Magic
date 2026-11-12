# MAGIC — SECURITY RULES & PROTOCOLS

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  

---

## 1. Security Philosophy: Defense in Depth

MAGIC adheres to a strict **Zero-Trust Local Security Model**. Because ERP systems safeguard proprietary financial records, confidential supplier cost structures, and statutory tax filings, security must be baked into every layer rather than delegated to superficial frontend checks.

---

## 2. Authentication & Credential Governance

1. **Password Hashing:**
   - Enforce **Argon2id** (Memory cost: 65,536 KB, Time cost: 3 iterations, Parallelism: 4 threads).
   - Legacy MD5, SHA-1, or bare SHA-256 without salt are strictly banned.
2. **Token & Authentication Architecture (Approved OPEN-003):**
   - Implemented via **ASP.NET Core Identity** with standard JWT Bearer tokens (`Microsoft.AspNetCore.Authentication.JwtBearer`) and claim-based RBAC.
   - **Signing Algorithms:** HMAC-SHA256 (symmetric shared secret for single-store LAN deployments) and ECDSA P-256 (`ES256`, asymmetric key pair for multi-branch/enterprise environments). Non-standard/custom JWT signing engines (such as Ed25519 for tokens) are avoided to eliminate external crypto dependencies for HTTP authentication.
   - **Access Token lifespan:** 15 minutes.
   - **Refresh Token lifespan:** 7 days, stored with cryptographically secure random bytes in PostgreSQL (`security.refresh_tokens`), enforcing single-use rotation and immediate family revocation on reuse detection.
   - **Context Claims:** Branch ID and Terminal ID claims are validated server-side on every request. Client-supplied headers are validated against authenticated token claims; if mismatched or unassigned, requests fail-closed with 403 Forbidden.
   - In desktop mode, client credentials and tokens are stored in the Windows Credential Locker / DPAPI.
3. **Session & Brute-Force Protection:**
   - Maximum 5 consecutive failed login attempts before a 15-minute account lockout.
   - Telemetry tracks client IP, terminal ID, and timestamp on all authentication attempts.
4. **Prohibited Practices:**
   - Never hardcode passwords, secret keys, or bypass accounts.
   - Never expose plaintext passwords or internal tokens in API responses.
   - Never trust frontend-only role or permission claims without server-side validation.

---

## 3. Granular Role-Based Access Control (RBAC)

Permissions follow the format: `<domain>:<resource>:<action>`

- Examples:
  - `accounting:vouchers:post`
  - `accounting:periods:lock`
  - `pos:discount:apply`
  - `inventory:stock:adjust`
  - `sales:invoices:post`
  - `reports:financial:export`

### Supervisor Override Mechanism
Certain sensitive retail and commercial operations exceed standard cashier or junior clerk authority:
- Applying discounts exceeding branch threshold (e.g. $> 10\%$).
- Issuing a sales return or refund.
- Cancelling or voiding a finalized receipt.
- Modifying credit limits for a customer.

**Protocol:**
1. The frontend detects that an action requires elevated authority and prompts for **Supervisor Authorization**.
2. The supervisor enters their secure PIN or biometric credentials.
3. The frontend submits the command along with `X-Supervisor-Auth-Token`.
4. The backend verifies the supervisor's permission claim independently before committing the transaction and records the supervisor's user ID in the audit log.

---

## 4. Local Area Network (LAN) & Terminal Security

1. **Database Isolation:**
   - PostgreSQL port `5432` must **never** be exposed directly to the local network.
   - PostgreSQL listens strictly on `127.0.0.1` or is bound to a dedicated internal loopback socket.
   - Client terminals communicate exclusively with the ASP.NET Core API on port `5001`.
2. **Terminal Registration & Authorization:**
   - When a new terminal connects to the in-store server over LAN, it enters a `PENDING_AUTHORIZATION` state.
   - An administrator must log into the Server console to approve the terminal, assigning it a unique `TerminalId` and cryptographically signed `TerminalSecret`.
   - Unauthorized machines or rogue Wi-Fi devices cannot query business APIs.
3. **Transport Encryption:**
   - All LAN traffic must be encrypted over HTTPS.
   - On initial setup, MAGIC generates a localized Self-Signed Root Authority (CA) and server certificate installed in the Windows Local Computer Trusted Store, eliminating SSL warning dialogs inside WebView2.

---

## 5. Immutable Audit Trail

Every state-mutating transaction writes an immutable audit record to `audit.system_audit_logs`:

```sql
CREATE TABLE audit.system_audit_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    event_timestamp TIMESTAMPTZ NOT NULL DEFAULT CLOCK_TIMESTAMP(),
    user_id UUID NOT NULL,
    branch_id UUID NOT NULL,
    terminal_id UUID,
    client_ip VARCHAR(45) NOT NULL,
    action_name VARCHAR(100) NOT NULL,
    entity_name VARCHAR(100) NOT NULL,
    entity_id UUID NOT NULL,
    before_state JSONB,
    after_state JSONB,
    override_by_supervisor_id UUID,
    correlation_id UUID NOT NULL
);
```
Audit logs are append-only. Immutability is enforced at both the PostgreSQL trigger level and database role permission level:

1. **PostgreSQL Immutability Trigger:**
   Trigger `trg_prevent_audit_log_tampering` executes function `audit.prevent_audit_log_tampering()` `BEFORE UPDATE OR DELETE` on `audit.system_audit_logs`. Any attempt to update or delete rows throws a PostgreSQL exception with SQLState `55000` (`MAGIC_AUDIT_LOGS_ARE_APPEND_ONLY`). This trigger halts tampering even if executed by the table owner.

2. **Least-Privilege Database Role (`magic_app`):**
   The application connects via a restricted PostgreSQL role `magic_app` separated from the migration/owner role `magic_dev`:
   - `magic_app` possesses `SELECT, INSERT, UPDATE, DELETE` on operational business tables (`platform.*`, etc.).
   - On `audit.system_audit_logs`, `magic_app` has strictly `SELECT, INSERT` privileges.
   - `UPDATE`, `DELETE`, and `TRUNCATE` privileges on `audit.system_audit_logs` are explicitly revoked:
     ```sql
     REVOKE UPDATE, DELETE, TRUNCATE ON TABLE audit.system_audit_logs FROM magic_app;
     ```

