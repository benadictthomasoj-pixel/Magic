# MAGIC — TECHNICAL DEBT TRACKER

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  

---

## 1. Technical Debt Management Policy

Technical debt is an inevitable byproduct of iterative software development. However, in financial and enterprise software, unmanaged technical debt leads to data corruption, balance sheet discrepancies, and security vulnerabilities.

### Policy Rules
1. Any temporary workaround or architectural compromise must be registered here immediately.
2. Debt items must carry a severity rating (Low, Medium, High, Critical) and an assigned remediation milestone.
3. No release candidate may be promoted to production with open `High` or `Critical` technical debt.

---

## 2. Technical Debt Registry

| Debt ID | Title / Area | Description | Severity | Impacted Module | Target Remediation Phase |
|---|---|---|---|---|---|
| **DEBT-001** | Direct LAN TLS Certificate Validation | Initial development may use self-signed certificates for LAN testing, requiring custom CA trust installation on client terminals. | Medium | Multi-Branch / Platform | Phase 8 |
| **DEBT-002** | Read-Model Materialization | Currently, reporting queries run against normalized PostgreSQL transaction tables with indexing. Large data volumes (> 1M rows) may require continuous background rollup tables. | Medium | Reports & BI | Phase 7 |
| **DEBT-003** | Print Template Customization Engine | V1 will provide fixed ESC/POS and vector A4 templates. Visual drag-and-drop receipt designer will be deferred. | Low | POS / Sales | Phase 9 |

---

## 3. Remediated Phase 1 Audit Items (Closed 2026-11-12)

All Critical and High findings identified during the Phase 1 Independent Technical Audit have been resolved and verified with automated test suites:

- **CRIT-001 (Outbox Concurrency):** Resolved with atomic `FOR UPDATE SKIP LOCKED`, worker lease tokens, and real consumer dispatch.
- **CRIT-002 (Audit Immutability):** Resolved with trigger `trg_prevent_audit_log_tampering` (SQLState `55000`) and least-privilege `magic_app` role.
- **CRIT-003 (Idempotency Enforcement):** Resolved with `IdempotencyMiddleware` enforcing cache replay and 422 payload mismatch rejection.
- **HIGH-001 (Cross-Stack Header Inconsistency):** Aligned all contracts and frontend to standard `Idempotency-Key`.
- **HIGH-002 (Health DTO Contract Discrepancy):** Aligned `detail` property across frontend models and status view.
- **HIGH-003 (Money JSON Serialization):** Implemented `MoneyJsonConverter` serializing scalar 64-bit integer paise and EF Core `MoneyConverter`.
- **HIGH-004 (WebView2 Security & Origin Validation):** Hardened `%LOCALAPPDATA%` UserDataFolder, NavigationStarting allowlist, and origin checks.
- **HIGH-005 (Configuration & Logging Order):** Set `AllowedHosts = "*"`, fixed logging enrichment order, sanitized template placeholders.
- **HIGH-006 (Desktop Capabilities Contract Drift):** Synchronized capabilities DTO between WPF and TypeScript.
- **HIGH-007 (OPEN-003 Auth Decision Finalized):** Approved ASP.NET Core Identity + Standard JWT + RBAC architecture.

