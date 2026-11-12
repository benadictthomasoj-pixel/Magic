# MAGIC ERP — PHASE A: SECURITY ARCHITECTURE & THREAT MODEL REVIEW

**Document Version:** 1.0  
**Phase:** Phase A (Identity, Authentication & Tenancy Foundation)  
**Security Evaluation Standard:** OWASP Top 10 + Financial ERP Audit Standards  
**Status:** COMPLETED & VERIFIED  

---

## 1. Cryptographic Standards & Implementation

### 1.1 Password Hashing Architecture
* **Algorithm:** PBKDF2 (Password-Based Key Derivation Function 2)
* **Pseudo-Random Function (PRF):** HMAC-SHA512
* **Iteration Count:** 100,000 iterations
* **Salt Entropy:** 128-bit cryptographically secure pseudorandom salt (`RandomNumberGenerator.GetBytes(16)`)
* **Derived Key Length:** 256 bits (32 bytes)
* **Verification:** Constant-time comparison (`CryptographicOperations.FixedTimeEquals`) to prevent timing side-channel attacks.

### 1.2 Access Token (JWT) Security
* **Signing Algorithm:** HMAC-SHA256 with 256-bit minimum secret key length.
* **Token Lifetime:** 15 minutes (short-lived to minimize replay exposure).
* **Claims Structure:**
  - `sub`: Unique User ID (UUID).
  - `name`: User login identifier.
  - `display_name`: Human-readable full name.
  - `company_id`: Tenant Isolation UUID.
  - `branch_id`: Operational Branch UUID.
  - `terminal_id`: Registered hardware terminal UUID.
  - `role`: Array of assigned security roles.
  - `perm`: Array of verified permission claim strings.
* **Issuer & Audience Validation:** Enforced by ASP.NET Core JWT Bearer middleware (`ValidateIssuer = true`, `ValidateAudience = true`, `ValidateLifetime = true`, `ClockSkew = TimeSpan.FromSeconds(30)`).

---

## 2. Refresh Token Rotation & Token Family Reuse Detection

To support persistent sessions without granting unbounded access token lifetimes, MAGIC implements single-use refresh token rotation with token family reuse detection:

```
[Client Login]
       │
       ▼
Issues: Access Token (15m) + Refresh Token A (7d, Family: F1)
       │
       ▼ [Token A Used to Refresh]
Server marks Token A as "IsRevoked = true", "ReplacedByTokenId = Token B"
Issues: Access Token (15m) + Refresh Token B (7d, Family: F1)
       │
   ┌───┴────────────────────────────────────────┐
   ▼ Normal Flow                                ▼ Compromised / Replayed Flow
[Client uses Token B]                 [Attacker attempts to replay Token A]
Issues Token C                        Server detects Token A is ALREADY REVOKED!
Family F1 continues healthy           CRITICAL INCIDENT: Token Reuse Detected
                                      Immediate Action:
                                      1. ALL tokens in Family F1 marked Revoked.
                                      2. User active sessions invalidated.
                                      3. Logged to `security.audit_events`.
                                      4. Returns HTTP 401 Unauthorized.
```

---

## 3. Account Lockout & Brute-Force Prevention

* **Threshold:** 5 consecutive failed authentication attempts.
* **Lockout Duration:** 15 minutes (`LockoutEndUtc = UtcNow.AddMinutes(15)`).
* **Reset Trigger:** Any successful login resets `AccessFailedCount = 0` and clears `LockoutEndUtc`.
* **Response Contract:** Returns `HTTP 423 Locked` with remaining lockout duration.
* **Audit Notification:** Every failed attempt records a `LOGIN_FAILED` audit event; crossing the threshold emits an `ACCOUNT_LOCKED` security alert.

---

## 4. Tenancy & Privilege Boundary Enforcement

### 4.1 Server-Side Context Authority
MAGIC rejects client-supplied tenant headers if they conflict with verified identity:
* `RequestContextMiddleware` inspects the verified `ClaimsPrincipal` on `HttpContext.User`.
* `CompanyId` and `BranchId` are extracted strictly from token claims.
* Client-supplied headers (e.g., `X-Company-Id`) cannot override or elevate tenant boundaries.
* Unauthenticated requests are assigned an empty context and blocked by controller `[Authorize]` attributes.

### 4.2 Endpoint Defense-in-Depth
* **Gate 1:** ASP.NET Core authentication middleware verifies token signature and expiration.
* **Gate 2:** Base controller `[Authorize]` attribute ensures the caller holds a valid identity.
* **Gate 3:** Granular `EnsurePermission(permissionCode)` checks user claims before executing business logic.
* **Gate 4:** EF Core query filters and tenancy parameters restrict database reads and writes to the user's authorized company.

---

## 5. Threat Modeling & Mitigation Analysis

| Threat (OWASP) | Vulnerability Vector | MAGIC ERP Phase A Defense |
|:---|:---|:---|
| **A01: Broken Access Control** | User navigates to accounting screens or calls accounting APIs without role | Server-side `EnsurePermission` checks return HTTP 403; Angular `permissionGuard` blocks client-side route activation. |
| **A02: Cryptographic Failures** | Weak password hashes, plaintext tokens, or hardcoded signing keys | PBKDF2 with 100,000 iterations & SHA-512; random 128-bit salt; signing keys configured via environment/secrets; tokens transmitted over HTTPS. |
| **A03: Injection** | SQL injection via login or username parameters | EF Core uses parameterized queries for all database operations; usernames and emails normalized before query. |
| **A07: Identification & Auth Failures** | Credential stuffing, brute-force password guessing, stolen refresh tokens | 5-attempt account lockout; single-use refresh token rotation; token family invalidation on replay; short-lived (15m) access tokens. |
| **A09: Security Logging & Monitoring** | Undetected unauthorized access attempts | Every auth event (success, failure, lockout, token rotation, token reuse, password change) is written to `security.audit_events`. |

---

## 6. Windows Desktop (WebView2) Environment Security

* **Credential Storage:** Tokens are stored in browser memory / `localStorage` within the isolated WebView2 user data folder.
* **Process Isolation:** The WebView2 runtime operates in a sandboxed process separate from the host WPF application.
* **Origin Boundary:** All API calls are made over explicit HTTP/HTTPS loopback or local network addresses with strict CORS and CSP policies.
* **Session Termination:** Logging out clears client tokens immediately, and calls the backend `/logout` endpoint to invalidate server-side refresh tokens.

