# MAGIC ERP — PHASE G: DEFECT REGISTER & REMEDIATION LOG

**Reporting Period:** Phase G Implementation  
**Status:** ALL DEFECTS RESOLVED  

---

### Defect Log & Resolutions

| Defect ID | Severity | Component | Root Cause Description | Remediation Applied | Status |
|---|---|---|---|---|---|
| **DEF-G-001** | High | `BranchesController` | Ambiguous reference to `RegisterTerminalRequest` between Api and Contracts. | Added explicit alias `using RegisterTerminalRequest = Magic.Contracts.Branches.RegisterTerminalRequest;`. | Resolved |
| **DEF-G-002** | Medium | `OperationsBaseController` | Controllers unable to access current user context due to parameter capture in primary constructor. | Exposed `protected ICurrentUserContext CurrentUser => currentUserContext;` in base class. | Resolved |
| **DEF-G-003** | Medium | `ReportService` / `AiAssistantProvider` | Entity property mismatch on `GrandTotalPaise` vs `TotalGrossPaise` and `ProductCode`. | Standardized entity queries to match exact database schema properties. | Resolved |
| **DEF-G-004** | Low | `SystemManagementService` | String localization warnings (CA1305) on string interpolation of numbers. | Enforced `CultureInfo.InvariantCulture` and `FormattableString.Invariant`. | Resolved |
| **DEF-G-005** | Medium | `enterprise-api.service.ts` | Invalid import path for `ApiResponse` (`../models/` instead of `../contracts/api.models`). | Updated import to reference `../contracts/api.models`. | Resolved |
| **DEF-G-006** | Low | `system-management.component.html` | Missing `databaseProvider` on `DatabaseMigrationSummary` interface. | Added optional `databaseProvider?: string` to interface and mapped in service. | Resolved |
| **DEF-G-007** | Medium | `PhaseGIntegrationTests` | Restore simulation test passed `Guid.NewGuid()` which failed lookup with 404 instead of validating unsafe database check. | Updated test to create a real backup first, then assert 400 Bad Request on unsafe target database. | Resolved |
| **DEF-G-008** | High | `SystemManagementService` | Sensitive configurations masked with `********` instead of the mandated `***REDACTED***`. | Updated `RedactSensitiveData` to output `***REDACTED***` satisfying Section 8.5. | Resolved |
| **DEF-G-009** | High | `ServiceDeskService` | Concurrent test runs caused unique index collision on `QuotationNumber` and `RequestNumber`. | Added short unique random GUID suffix to generated sequence numbers. | Resolved |
| **DEF-G-010** | Medium | `PhaseFExtendedScenariosTests` | Reconcile test assumed initial balance 500,000 paise on newly created petty cash account which starts at 0. | Added explicit balance initialization in test fixture before running reconciliation scenarios. | Resolved |
