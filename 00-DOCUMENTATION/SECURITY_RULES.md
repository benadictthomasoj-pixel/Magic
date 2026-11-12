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
2. **Token & Authentication Architecture:**
   - Implemented via **ASP.NET Core Identity** (customized with Argon2id `IPasswordHasher<TUser>`) or an equivalent audited cryptographic implementation; JWT signing strategy (standard ECDSA P-256 vs. Ed25519/EdDSA via custom crypto provider) is tracked for final confirmation before Phase 1.
   - Access Token lifespan: 15 minutes.
   - Refresh Token lifespan: 12 hours (with sliding expiration and single-use rotation stored in PostgreSQL).
   - In single-workstation desktop mode, the desktop shell stores credentials securely in the Windows Credential Locker / DPAPI.
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
Audit logs are append-only. No `UPDATE` or `DELETE` grants are permitted on the `audit` schema.

