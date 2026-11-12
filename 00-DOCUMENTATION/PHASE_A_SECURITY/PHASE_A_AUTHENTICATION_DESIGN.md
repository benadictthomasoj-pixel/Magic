# MAGIC ERP — PHASE A AUTHENTICATION DESIGN
## Cryptographic Token Lifecycle, Password Security & Session Management

**Document Version:** 1.0.0  
**Phase:** Phase A — Foundation & Security  
**Standard:** OpenID Connect / OAuth 2.0 / ASP.NET Core Identity  
**Key Standard:** RFC 7519 (JWT), RFC 7515 (JWS)  
**Date:** 2026-11-12  

---

## 1. Authentication Lifecycle Diagram

```
[ Client ]                             [ API Gateway ]                        [ PostgreSQL DB ]
    │                                         │                                      │
    │─── POST /api/v1/auth/login ────────────►│                                      │
    │    (Username, Password, BranchId)       │─── Query User & Salt ───────────────►│
    │                                         │◄── Return User Record ───────────────│
    │                                         │                                      │
    │                                         │─── Verify PBKDF2 Hash                │
    │                                         │─── Generate JWT (15 min)             │
    │                                         │─── Generate 64-byte Refresh Token    │
    │                                         │─── Persist RefreshTokenRecord ──────►│
    │◄── 200 OK (Access + Refresh Token) ─────│                                      │
    │                                         │                                      │
    │─── GET /api/v1/accounting/vouchers ────►│                                      │
    │    (Bearer <AccessToken>)               │─── Validate JWT Signature & Claims   │
    │◄── 200 OK (Vouchers List) ──────────────│                                      │
    │                                         │                                      │
    │ [After 15 minutes]                      │                                      │
    │─── POST /api/v1/auth/refresh ──────────►│                                      │
    │    (RefreshToken)                       │─── Query Token by Hash ─────────────►│
    │                                         │◄── Return Token Record ──────────────│
    │                                         │                                      │
    │                                         │─── Check IsRevoked == false          │
    │                                         │─── Revoke Current Token              │
    │                                         │─── Insert Rotated Token (Same Family)►│
    │◄── 200 OK (New Access + Refresh Token) ─│                                      │
```

---

## 2. Password Hashing Specification

- **Algorithm:** PBKDF2 (Password-Based Key Derivation Function 2) using `HMAC-SHA512`.
- **Iteration Count:** **100,000 iterations**.
- **Salt:** 128-bit (16 bytes) generated per user via `System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)`.
- **Subkey Length:** 256-bit (32 bytes).
- **Comparison:** Constant-time comparison using `CryptographicOperations.FixedTimeEquals()` to defend against timing attacks.

---

## 3. JWT Access Token Structure

### 3.1. Header
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

### 3.2. Payload Claims
```json
{
  "sub": "a1b2c3d4-0000-0000-0000-000000000001",
  "unique_name": "accountant1",
  "email": "accountant@magic.local",
  "jti": "e8a9b2c3d4e5f6a7b8c9d0e1f2a3b4c5",
  "company_id": "11111111-1111-1111-1111-111111111111",
  "branch_id": "22222222-2222-2222-2222-222222222222",
  "terminal_id": "33333333-3333-3333-3333-333333333333",
  "role": ["Accountant"],
  "permission": [
    "accounting:dashboard:view",
    "accounting:accounts:view",
    "accounting:vouchers:create",
    "accounting:vouchers:post",
    "accounting:vouchers:reverse",
    "accounting:ledger:view",
    "accounting:periods:view"
  ],
  "exp": 1731427200,
  "iss": "Magic.Api",
  "aud": "Magic.Client"
}
```

---

## 4. Single-Use Refresh Token Rotation & Reuse Detection

### 4.1. Rotation
When an access token expires after 15 minutes, the client requests token rotation via `POST /api/v1/auth/refresh`:
1. The server computes the SHA-256 hash of the presented refresh token and queries `security.refresh_tokens`.
2. If valid, the presented token is marked `is_revoked = true`, `revocation_reason = "Rotated"`, and `replaced_by_token_hash` is recorded.
3. A newly generated refresh token is assigned to the exact same `TokenFamily` and saved to the database.
4. A new 15-minute access token and the new refresh token are returned to the client.

### 4.2. Reuse Detection & Family Invalidation
If an attacker intercepts or steals an already-used refresh token and attempts to replay it:
1. The server discovers `is_revoked == true`.
2. **Immediate Threat Response:** The server instantly invokes `RevokeFamilyAsync(tokenRecord.TokenFamily)`.
3. All refresh tokens belonging to that `TokenFamily` are permanently revoked.
4. An alert security audit log `TOKEN_REUSE_DETECTED` is recorded.
5. When the legitimate user attempts their next refresh, their token will also be recognized as revoked, forcing both attacker and victim to terminate sessions and re-authenticate.

---

## 5. Account Lockout & Brute-Force Protection

- **Failed Counter:** Every failed password check increments `FailedLoginAttempts` on the user record.
- **Threshold:** On the 5th consecutive failure:
  - `IsLockedOut` is set to `true`.
  - `LockoutEndUtc` is set to `DateTime.UtcNow.AddMinutes(15)`.
  - A `ACCOUNT_LOCKED` security audit event is logged.
  - The endpoint returns `HTTP 423 Locked`.
- **Auto-Recovery:** Once 15 minutes have elapsed, the next login attempt automatically clears the lockout and resets `FailedLoginAttempts` to 0 upon successful password verification.
- **Immediate Reset on Success:** Any successful login immediately resets `FailedLoginAttempts = 0`, `IsLockedOut = false`, `LockoutEndUtc = null`.
