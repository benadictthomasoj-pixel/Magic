# MAGIC ERP — PHASE G: MULTI-BRANCH & MULTI-TERMINAL MANAGEMENT
## Module 18 Implementation Report

**Subsystem:** Module 18 — Multi-Branch & Multi-Terminal Management  
**Service:** `IBranchManagementService` / `BranchManagementService`  
**Controller:** `BranchesController` (`/api/v1/branches`)  
**Frontend Component:** `BranchManagementComponent` (`/billing/branches`)  

---

### 1. Enterprise Multi-Branch Infrastructure

The branch management architecture enables a single corporate tenant to operate multiple physical locations (retail stores, warehouses, regional offices) with segregated operations and consolidated reporting:
- **Company Profile:** Maintenance of legal entity name, trade name, master GSTIN, PAN, registered office address, base currency (`INR`), and fiscal year start month (April / Month 4).
- **Branch Registry:** Uniquely keyed by `(CompanyId, BranchCode)`. Stores physical address, state-specific GSTIN, branch manager assignment, and active status.
- **Warehouse Associations:** Links one or more inventory warehouses to a branch via `branches.warehouse_branch_assignments`.

---

### 2. POS Terminal Registration & Hardware Telemetry

Every billing desk or mobile POS device must be registered and authorized before issuing bills:
- **Registration Attributes:** Terminal Code (e.g. `POS-01`), Machine Hostname, Operating System, Printer Specification (Model and receipt paper width: 80mm or 58mm).
- **Security & Cashier Binding:** Terminals can be restricted to authorized cashiers or kept open for multi-user shift handover.
- **Revocation Protocol:** Administrative users can immediately toggle terminal authorization (`IsAuthorized = false`), instantly revoking invoice issuance privileges for compromised or decommissioned terminals.

---

### 3. Inter-Branch Stock Transfers (Double-Entry Ledger)

Stock transfers between branches follow a strict 4-stage lifecycle ensuring physical and financial traceability:

```
[Requested] ──► [Approved] ──► [Dispatched] ──► [Received]
```

1. **Requested:** Source and destination branches/warehouses are specified along with requested line quantities.
2. **Approved:** Originating branch supervisor signs off on the stock requisition.
3. **Dispatched:**
   - Validates that sufficient on-hand quantity exists in the source warehouse.
   - Deducts stock from the source warehouse balance.
   - Records an outbound `StockMovementRecord` (`MovementType = "TransferOut"`, `Direction = "Out"`).
4. **Received:**
   - Destination warehouse confirms receipt of goods.
   - Credits stock to the destination warehouse balance.
   - Records an inbound `StockMovementRecord` (`MovementType = "TransferIn"`, `Direction = "In"`).
   - Reconciles any intransit stock.

