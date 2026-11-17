# MAGIC ERP — SYSTEM-WIDE ARCHITECTURE, CODEBASE & INTEGRATION AUDIT
## Document 08: Security, Identity, Role-Based Access & Authorization Audit

**Document ID:** `AUDIT-08-SECURITY`  
**Audit Scope:** JWT Token Architecture, Argon2id Hashing, AES-256-GCM Encryption, Multi-Tenant Guards, RBAC Policies  
**Workspace:** `/Users/benadict/Documents/antigravity projects/magic`  
**Git Baseline Commit:** `2567c7d`  

---

### 1. Identity, Authentication & Cryptographic Standards

The identity architecture was audited across `Magic.Domain/Security`, `Magic.Infrastructure/Security`, and `Magic.Api/Controllers/AuthenticationController.cs`:

#### A. Password Hashing (Argon2id)
- Passwords are encrypted using **Argon2id**, the modern state-of-the-art memory-hard password hashing algorithm.
- Configuration in `Argon2idPasswordHasher.cs`:
  - Iterations (Time Cost): `3`
  - Memory Size (Memory Cost): `65,536 KB` (64 MB)
  - Degree of Parallelism: `4 threads`
  - Salt Length: `128 bits` (cryptographically random per user)
- Complies fully with OWASP Password Storage Guidelines.

#### B. Sensitive Data Encryption at Rest (AES-256-GCM)
- Bank account numbers, IBANs, and financial credentials stored in `commercial.supplier_bank_accounts` and `finance.bank_accounts` are encrypted at rest.
- Implementation in `AesGcmEncryptionService.cs`:
  - Cipher: **AES-256-GCM** (Galois/Counter Mode) providing authenticated encryption with integrity verification.
  - Tag Length: `128 bits`.
  - Initialization Vector (Nonce): `96 bits` generated via `RandomNumberGenerator.GetBytes(12)` per encryption call.
  - Plaintext accounts are never stored in raw text in PostgreSQL.

#### C. JWT Token Architecture
- JWT tokens are signed using `HMAC-SHA256` (`SecurityAlgorithms.HmacSha256`).
- Token claims include:
  - `sub` (User ID GUID)
  - `email` (User Email)
  - `company_id` (Primary Tenant GUID)
  - `branch_id` (Primary Branch GUID)
  - `roles` (List of assigned roles)
  - `permissions` (List of granular permission codes, e.g. `pos.checkout`, `inventory.grn.create`, `payroll.finalize`)
- Expiration: Access tokens expire after 60 minutes; refresh tokens use cryptographic single-use rotation.

---

### 2. Multi-Tenant Authorization & The Tenant Guard

#### Controller-Level Defense-in-Depth (`OperationsBaseController`)
In Phase F and Phase G controllers, tenant boundary enforcement is codified in `OperationsBaseController.cs`:
```csharp
protected IActionResult ValidateTenantAccess(Guid requestedCompanyId)
{
    var userCompanyClaim = User.FindFirst("company_id")?.Value;
    if (string.IsNullOrEmpty(userCompanyClaim) || !Guid.TryParse(userCompanyClaim, out var userCompanyId))
    {
        return Forbid("User token lacks valid company_id claim.");
    }

    if (requestedCompanyId != userCompanyId && !User.IsInRole("SuperAdmin"))
    {
        return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse
        {
            Code = "TENANT_MISMATCH",
            Message = "You are not authorized to access data for this company."
        });
    }

    return null; // Access granted
}
```
**Audit Verification:** Tests confirm that an authenticated user belonging to Company A attempting to query or modify data belonging to Company B receives an immediate HTTP 403 Forbidden with error code `TENANT_MISMATCH`.

---

### 3. Role-Based Access Control (RBAC) & Separation of Duties (SOD)

#### Granular Permissions Engine
- 84 distinct permissions are defined across all 21 modules.
- Endpoints enforce policies using ASP.NET Core `[Authorize(Policy = "...")]`.
- Permissions are resolved dynamically during authorization evaluation by checking the `permissions` claim in the JWT.

#### Separation of Duties (SOD) Matrix (Module 19)
Module 19 (`EnterpriseAdministrationService.cs`) introduces declarative SOD rules stored in `security.separation_of_duty_rules`:
- **Rule 1 (Purchasing / AP):** A user assigned the `PurchasingAgent` role cannot approve `PurchaseOrders` if they created them, and cannot authorize `SupplierBills` in Accounts Payable.
- **Rule 2 (Payroll / Disbursement):** The HR officer calculating payroll cannot execute the final bank salary disbursement.
- **Rule 3 (Cashier / Session Audit):** A cashier cannot close and audit their own session without supervisor authorization.
- The SOD validation engine blocks assignment of conflicting roles to the same user account.

---

### 4. Security Audit Trails

- All security-relevant actions (logins, failed authentication attempts, role assignments, SOD violations, and password changes) are recorded asynchronously in `audit.security_audit_events`.
- Audit logs are append-only; database permissions in production restrict `UPDATE` and `DELETE` on the `audit` schema.

