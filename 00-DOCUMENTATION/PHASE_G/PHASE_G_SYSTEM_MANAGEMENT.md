# MAGIC ERP — PHASE G: SYSTEM MANAGEMENT & PLATFORM OPERATIONS
## Module 21 Implementation Report

**Subsystem:** Module 21 — System Management & Platform Operations  
**Service:** `ISystemManagementService` / `SystemManagementService`  
**Controller:** `SystemManagementController` (`/api/v1/system-management`)  
**Frontend Component:** `SystemManagementComponent` (`/billing/system`)  

---

### 1. Platform Health & Diagnostics

The platform health dashboard provides real-time telemetry into:
- **Application Status:** Overall runtime state (`Healthy`, `Degraded`, `Maintenance`).
- **Database Connectivity:** Direct ping to PostgreSQL 18 with version reporting.
- **Transactional Outbox Queue:** Real-time count of pending vs failed messages in `OutboxMessages`.
- **Background Job Workers:** Monitoring of active recurring job loops (Outbox processor, token cleanup, inventory reconciler).
- **Environment Telemetry:** Application version, framework version (`.NET 10.0`), uptime, and recent error log excerpts.

---

### 2. Database Backup & SHA-256 Checksum Engine

- **Automated / Manual Backup Creation:** Generates full schema and data dumps stored under secure local storage paths.
- **Cryptographic Integrity:** Computes and records the SHA-256 hash of the generated backup archive.
- **Retention Management:** Enforces retention policies (default: 30 days) before automated archiving or cleanup.

---

### 3. Isolated Restore Safety Protocol (Production Guardrail)

Section 8.3 of the specification mandates strict isolation for database restore operations:
- **Direct Overwrite Prohibition:** The system explicitly rejects any restore command targeting primary production databases (`magic_dev`, `magic_app`, `magic_prod`):
  ```csharp
  if (request.TargetDatabaseName.Equals("magic_dev", StringComparison.OrdinalIgnoreCase) ||
      request.TargetDatabaseName.Equals("magic_prod", StringComparison.OrdinalIgnoreCase))
  {
      throw new DomainException("UNSAFE_RESTORE_PROHIBITED", "Direct in-place restoration over live production databases is strictly prohibited.", 400);
  }
  ```
- **Isolated Scratch Validation:** Restorations are simulated into ephemeral, isolated scratch databases (`magic_restore_sandbox_*`) to verify schema integrity and foreign key constraints without risking production corruption.

---

### 4. System Configurations & Secret Redaction

- Manages runtime application configuration items in `system_management.system_configurations`.
- **Mandatory Redaction Standard:** All confidential configuration parameters (database passwords, JWT signing keys, payment gateway API keys) are strictly masked as `***REDACTED***` across all view and log contexts.

