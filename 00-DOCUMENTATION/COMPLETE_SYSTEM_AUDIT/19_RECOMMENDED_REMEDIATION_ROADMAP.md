# MAGIC ERP — SYSTEM-WIDE ARCHITECTURE, CODEBASE & INTEGRATION AUDIT
## Document 19: Prioritized Remediation Roadmap & Execution Plan

**Document ID:** `AUDIT-19-ROADMAP`  
**Audit Scope:** Strategic Phasing, Task Sizing, Risk Assessment, Acceptance Criteria for Discovered Defects  
**Workspace:** `/Users/benadict/Documents/antigravity projects/magic`  
**Git Baseline Commit:** `2567c7d`  

---

### 1. Strategic Remediation Phasing Overview

To resolve the defects identified during the system-wide audit without introducing regressions, remediation should follow a structured three-sprint roadmap:

```
┌─────────────────────────────────────────────────────────────┐
│                 SPRINT 1: CRITICAL REPAIRS                  │
│       (Frontend Dev Mode, Terminal Model, Middleware)       │
│                                                             │
│   - DEF-INT-001: Fix EnterpriseApiService relative paths    │
│   - DEF-ARC-001: Consolidate duplicate terminal tables      │
│   - DEF-SYS-001: Remove duplicate RequestContextMiddleware  │
└──────────────────────────────┬──────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────┐
│           SPRINT 2: CORE INTEGRATION ENRICHMENT             │
│        (POS Marketing, Outbox Pipeline, AP 3-Way Match)     │
│                                                             │
│   - DEF-INT-002: Connect POS Checkout to Marketing Loyalty   │
│   - DEF-ARC-002: Enqueue domain events & register consumers │
│   - DEF-INT-003: Add PO/GRN selector to Payables UI         │
└──────────────────────────────┬──────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────┐
│          SPRINT 3: AUTOMATION, HARDWARE & REFACTORING       │
│           (Rule Scheduler, Native Win32 Spooler, DI)        │
│                                                             │
│   - DEF-ARC-003: Periodic background timer for auto rules   │
│   - DEF-DESK-001: Native Windows ESC/POS thermal printing   │
│   - DEF-CODE-001: Unify controller base inheritance         │
└─────────────────────────────────────────────────────────────┘
```

---

### 2. Sprint 1: Critical Repairs (Immediate Priority)

#### Objective: Restore complete developer workflow, consolidate security models, and optimize pipeline.

1. **Task 1.1: Fix EnterpriseApiService Relative Pathing (DEF-INT-001)**
   - **Target File:** `01-SRC/Frontend/src/app/core/services/enterprise-api.service.ts`
   - **Action:** Add `private readonly baseUrl = `${environment.apiBaseUrl}/v1`;` and update all HTTP calls.
   - **Acceptance Criteria:** `ng serve` loads `/enterprise/bi`, `/enterprise/branches`, and `/enterprise/system` without `<` JSON parse errors.
   - **Risk:** Zero risk; pure path prefixing.

2. **Task 1.2: Consolidate Duplicate Terminal Entities (DEF-ARC-001)**
   - **Target Files:**
     - `Magic.Domain/Branches/TerminalRegistrationRecord.cs`
     - `Magic.Infrastructure/Persistence/Configurations/TerminalConfiguration.cs`
     - `BranchManagementService.cs`, `AuthenticationApplicationService.cs`
   - **Action:** Point both services to the single authoritative terminal entity; generate migration to merge existing rows.
   - **Acceptance Criteria:** Terminal created in Multi-Branch UI can immediately authenticate via POS login endpoint.
   - **Risk:** Low; requires careful schema migration.

3. **Task 1.3: Eliminate Redundant Middleware Registration (DEF-SYS-001)**
   - **Target File:** `01-SRC/Backend/Magic.Api/Program.cs`
   - **Action:** Remove line 81 (`app.UseMiddleware<RequestContextMiddleware>();`), retaining line 116.
   - **Acceptance Criteria:** API builds and runs; Serilog logs authenticated user claims on single-pass execution.
   - **Risk:** Zero risk.

---

### 3. Sprint 2: Core Integration Enrichment (High Priority)

#### Objective: Connect the marketing growth loop, activate asynchronous outbox messaging, and streamline procurement 3-way matching.

1. **Task 2.1: Hook Marketing Loyalty & Coupons into POS Checkout (DEF-INT-002)**
   - **Target Files:** `PosService.cs`, `PosCheckoutRequest.cs`
   - **Action:** Inject `IMarketingService` into `PosService`; invoke `AwardLoyaltyPointsAsync` and `RecordCouponRedemptionAsync`.
   - **Acceptance Criteria:** Integration test passes verifying customer loyalty points increase following a completed checkout.

2. **Task 2.2: Implement Outbox Event Dispatch & Consumers (DEF-ARC-002)**
   - **Target Files:** `PosService.cs`, `GoodsReceiptService.cs`, `InfrastructureServiceCollectionExtensions.cs`
   - **Action:** Enqueue `OrderCompletedIntegrationEvent` and `StockReceivedIntegrationEvent`; register corresponding consumers.
   - **Acceptance Criteria:** Background worker picks up and processes outbox records asynchronously.

3. **Task 2.3: Add 3-Way Match GRN Selector to Payables UI (DEF-INT-003)**
   - **Target File:** `payables-management.component.ts`
   - **Action:** Add dropdown to select confirmed GRNs, auto-populating supplier bill line items.
   - **Acceptance Criteria:** Accountant can create a supplier bill directly from a confirmed GRN without re-entering line items.

---

### 4. Sprint 3: Autonomous Automation & Native Hardware (Medium/Low Priority)

1. **Task 3.1: Scheduled Periodic Evaluation for Automation Rules (DEF-ARC-003)**
   - **Action:** Create `AutomationRuleSchedulerBackgroundService` evaluating active rules on a 15-minute periodic timer.
2. **Task 3.2: Native ESC/POS Thermal Printer Driver for WPF (DEF-DESK-001)**
   - **Action:** Implement `WindowsNativeHardwareBridge.cs` using Win32 raw print spooler APIs.
3. **Task 3.3: Unified Controller Base Inheritance (DEF-CODE-001)**
   - **Action:** Refactor all Phase A–E controllers to inherit from a unified `MagicApiControllerBase`.

