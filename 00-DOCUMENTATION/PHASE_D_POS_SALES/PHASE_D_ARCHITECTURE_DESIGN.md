# MAGIC ERP — Phase D: POS & Sales Billing Architecture Design

**Document:** `00-DOCUMENTATION/PHASE_D_POS_SALES/PHASE_D_ARCHITECTURE_DESIGN.md`  
**Project:** MAGIC – Universal Business Management ERP  
**Domain:** Point of Sale (POS), Sales Billing, Payments, Returns, and Cashier Governance  
**Architecture:** Modular Monolith, Domain-Driven Design (DDD), Clean Architecture  
**Standards:** Fail-Closed Multitenancy, Double-Entry Accounting Invariants, Integer-Paise Financial Math (`BIGINT`)  

---

## 1. Domain Overview & Subsystem Boundaries

The Sales and Point of Sale (POS) domain is the primary operational revenue engine of MAGIC ERP. It is structured into 5 core capability areas:

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                           PHASE D: POS & SALES ENGINE                        │
├─────────────────────┬──────────────────────┬─────────────────────────────────┤
│   1. POS CHECKOUT   │   2. INVOICE LIFECYCLE│   3. PAYMENT MANAGEMENT         │
│   • Barcode Scanner │   • Draft            │   • Cash, UPI, Card, Split      │
│   • Fast Cart       │   • Paid / Partial   │   • Exact Payment Reconciliation│
│   • Stock Check     │   • Cancelled / Void │   • Manual Ref & Receipt Print  │
├─────────────────────┼──────────────────────┼─────────────────────────────────┤
│   4. SALES RETURNS  │   5. CASHIER SESSIONS│   6. REPORTING & ANALYTICS      │
│   • Invoice-linked  │   • Float Opening    │   • Persisted Revenue KPIs      │
│   • Restock Toggles │   • Cash Closing     │   • Hourly / Cashier / Payment  │
│   • Credit Notes    │   • Variance Audit   │   • Return & Discount Breakdown │
└─────────────────────┴──────────────────────┴─────────────────────────────────┘
```

---

## 2. Domain Entities & State Machines

### A. Sales Invoice State Machine
A sales invoice progresses through immutable states:
- `Draft` -> Initial cart checkout state before full payment confirmation.
- `Paid` -> Total allocated payments equal `GrandTotalPaise`.
- `PartiallyPaid` -> Payment captured less than grand total, permitted if customer credit account configured.
- `Cancelled` -> Cancelled before posting/fulfillment (generates full reversal).
- `PartiallyRefunded` -> Approved return against subset of lines/quantities.
- `Refunded` -> All line items fully returned.

```
 [Draft] ──► [Paid] ───────────────► [PartiallyRefunded] ──► [Refunded]
    │           │
    ▼           ▼
[Cancelled]  [Credit Note Issued]
```

### B. Cashier Session State Machine
- `Open` -> Opened by cashier with initial opening float (`OpeningCashPaise`). Tracks all cash inflows and outflows.
- `Closed` -> Shift ended by cashier. Actual physical currency count recorded. Expected cash calculated deterministically.
- `Reconciled` -> Supervisor reviewed variance, added notes, and approved closure.

---

## 3. Financial Invariants & Money Rules

1. **Zero Floating-Point Money:** All unit prices, discounts, GST tax amounts, invoice totals, payments, and cash drawers are computed and persisted in 64-bit integer paise (`BIGINT`).
2. **Deterministic Tax Calculation:**
   - Intra-state transaction (`PlaceOfSupplyStateCode == SellerStateCode`): CGST + SGST split equally (`Rate / 2`).
   - Inter-state transaction (`PlaceOfSupplyStateCode != SellerStateCode`): IGST applied in full.
3. **Split Payment Balance Invariant:**
   $$\sum \text{Allocated Payments} = \text{GrandTotalPaise} - \text{PermittedCreditPaise}$$
4. **Sales Returns Invariant:**
   $$\text{ReturnedQuantity}_{\text{line}} \le \text{SoldQuantity}_{\text{line}} - \sum \text{PreviouslyReturned}_{\text{line}}$$

---

## 4. Cross-Module Integration Contracts

### A. Inventory Integration (Phase C)
- At checkout: Every sale atomically issues stock from `inventory.stock_balances` and writes an immutable record to `inventory.stock_movements` (`MovementType = "SalesIssue"`, `Direction = "Out"`).
- At return: Resalable returned merchandise writes an immutable record to `inventory.stock_movements` (`MovementType = "SalesReturn"`, `Direction = "In"`) and restores `inventory.stock_balances`.

### B. Valuation Restriction (`DEC-ACC-002`)
- In accordance with `DEC-ACC-002`, sales issue movements record physical quantity deductions and transaction selling prices without generating fabricated COGS / Inventory Valuation General Ledger journals until the enterprise valuation policy is ratified.

### C. Statutory Rounding Restriction (`DEC-ACC-003`)
- Provisional calculation logic is isolated strictly inside `IGstCalculationService`. Statutory GST tax rounding will remain modular and configurable.
