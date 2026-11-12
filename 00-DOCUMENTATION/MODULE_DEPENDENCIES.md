# MAGIC — MODULE DEPENDENCY SPECIFICATION

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  

---

## 1. Architectural Dependency Hierarchy (DAG)

All module dependencies in MAGIC conform to a strict **Directed Acyclic Graph (DAG)**. Upward or circular dependencies between modules are strictly prohibited by automated architectural fitness tests (e.g., NetArchTest).

```
+----------------------------------------------------------------------------------------------------+
| LEVEL 0: PLATFORM & SYSTEM FOUNDATION                                                              |
|   Module 21: System Management                                                                     |
|   Module 19: Administration and Security                                                           |
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
| LEVEL 1: STRUCTURAL & FINANCIAL CORE                                                               |
|   Module 18: Multi-Branch and Multi-Terminal                                                        |
|   Module 06: Accounting (General Ledger, Accounts, Fiscal Years)                                   |
|   Module 08: Tax and Statutory Compliance                                                          |
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
| LEVEL 2: MASTER ENTITIES, SUBLEDGERS & PRICING/PROMOTIONS                                          |
|   Module 05: Inventory & Warehouse Management (includes Product Catalogue, Units, Warehouses)      |
|   Module 09: Customer CRM (Customers, Loyalty, Credit)                                             |
|   Module 10: Supplier Management (Vendors, AP Terms)                                               |
|   Module 12: Human Resources (Employees, Shifts)                                                   |
|   Module 14: Marketing & Customer Growth (Promotions, Coupons — evaluated downward by Sales/POS)   |
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
| LEVEL 3: TRANSACTIONAL OPERATIONAL ENGINES                                                         |
|   Module 03: Sales Management (Orders, Invoices, Delivery Challans)                                 |
|   Module 04: Purchase & Procurement (PO, GRN, Invoices)                                            |
|   Module 07: Financial Management (Bank Accounts, Reconciliation, Assets)                           |
|   Module 11: Expense Management (Claims, Petty Cash)                                               |
|   Module 13: Payroll (Salary Structures, Payruns)                                                  |
|   Module 15: Order Management (Pick, Pack, Fulfillment)                                            |
|   Module 16: Service Management (Job Cards, AMC)                                                   |
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
| LEVEL 4: FRONT-LINE EXECUTION                                                                      |
|   Module 02: POS and Billing (Fast Retail Counter, Shift Sessions)                                 |
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
| LEVEL 5: ANALYTICS, INTELLIGENCE & AUTOMATION                                                      |
|   Module 01: Dashboard and Business Intelligence                                                   |
|   Module 17: Reports and Analytics (Read-Only Consumer)                                            |
|   Module 20: Automation and AI (Auto Stock Update Staging)                                         |
+----------------------------------------------------------------------------------------------------+
```

---

## 2. Dependency Matrix

| Module | Directly Depends On (Allowed Downstream Calls) | May Be Called By (Upstream Callers) |
|---|---|---|
| **21. System Management** | *None (Root)* | All modules (via logging/telemetry) |
| **19. Admin & Security** | 21 | All modules (via AuthContext / RBAC) |
| **18. Multi-Branch** | 19, 21 | All Level 2, 3, 4, 5 modules |
| **06. Accounting** | 18, 19, 21 | 02, 03, 04, 07, 08, 11, 13, 16, 17, 20 |
| **08. Tax & Compliance** | 06, 18, 19, 21 | 02, 03, 04, 11, 13, 17, 20 |
| **05. Inventory & Products**| 06, 08, 18, 19, 21 | 02, 03, 04, 14, 15, 16, 17, 20 |
| **09. Customer CRM** | 06, 18, 19, 21 | 02, 03, 14, 15, 16, 17 |
| **10. Supplier Mgmt** | 06, 18, 19, 21 | 04, 17, 20 |
| **12. Human Resources** | 18, 19, 21 | 13, 16, 17 |
| **14. Marketing** | 05, 09, 18, 19, 21 *(No sync dependency on Sales/POS; reacts to Outbox events)* | 02, 03, 17 |
| **03. Sales Mgmt** | 05, 06, 08, 09, 14, 18, 19, 21 | 01, 02, 15, 17 |
| **04. Purchase & Proc.** | 05, 06, 08, 10, 18, 19, 21 | 01, 17, 20 |
| **07. Financial Mgmt** | 06, 18, 19, 21 | 01, 17 |
| **11. Expense Mgmt** | 06, 08, 18, 19, 21 | 01, 17 |
| **13. Payroll** | 06, 08, 12, 18, 19, 21 | 01, 17 |
| **15. Order Mgmt** | 03, 05, 09, 18, 19, 21 | 01, 17 |
| **16. Service Mgmt** | 05, 06, 08, 09, 18, 19, 21 | 01, 17 |
| **02. POS & Billing** | 03, 05, 06, 08, 09, 14, 18, 19, 21 | 01, 17 |
| **01. Dashboard & BI** | 02, 03, 04, 05, 06, 07, 18, 19, 21 | *None (UI Consumer)* |
| **17. Reports & Analytics**| Read-only across all modules | *None (UI Consumer)* |
| **20. Automation & AI** | 04, 05, 06, 08, 10, 18, 19, 21 | *None (UI Staging Worker)* |

---

## 3. Communication Patterns Between Modules

To guarantee that module boundaries are preserved, MAGIC supports only **three approved cross-module communication mechanisms**:

### Pattern A: Synchronous Transactional Contracts (Shared Interfaces)
- Defined in the shared `Contracts/` project.
- Used for all critical financial, stock, tax, and ledger mutations requiring **single-transaction ACID atomicity**.
- **Rule:** Interface arguments and return types must be pure Data Transfer Objects (DTOs) or primitive types. Domain entity objects must never leak across module boundaries.
- **Example:**
  ```csharp
  public interface IAccountingPostingContract
  {
      Task<VoucherPostingResultDto> PostVoucherAsync(
          CreateVoucherRequestDto request, 
          CancellationToken cancellationToken);
  }
  ```

### Pattern B: In-Process Domain Events (Transactional Outbox Pattern for Secondary Work)
- Used exclusively for **secondary asynchronous side effects** (e.g., dashboard KPI cache updates, loyalty points notification, marketing campaign attribution, audit log indexing) after the primary ACID transaction commits.
- Events are persisted to `platform.outbox_messages` within the same database transaction as the primary entity mutation, ensuring zero lost events.
- Core stock deductions and General Ledger postings must NEVER be deferred to post-commit asynchronous event handlers.

### Pattern C: Query Projections / Read Models
- Modules needing read-only projections of another module's state query dedicated read-only views or materialized projections.
- Direct entity joins across schema boundaries in EF Core are prohibited.

---

## 4. Enforcement Against Architectural Violations

1. **Static Analysis & Architecture Tests:**
   Unit tests will leverage `NetArchTest.Rules` to assert that:
   - Module namespaces only reference approved downstream namespaces.
   - Domain layers have zero dependencies on infrastructure, EF Core, or presentation packages.
2. **Schema Isolation in PostgreSQL:**
   Each module's migrations are isolated to their designated PostgreSQL schema. Cross-schema table foreign keys are restricted to fundamental entity IDs (`tenant_id`, `branch_id`, `user_id`).

