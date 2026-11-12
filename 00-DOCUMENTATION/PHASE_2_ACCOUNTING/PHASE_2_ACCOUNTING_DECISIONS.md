# MAGIC — Phase 2: Accounting Foundation
## Architectural & Statutory Accounting Decisions Register

**Document Version:** 2.0.0-DRAFT  
**Phase:** 2 — Accounting Foundation  
**Stage:** 1 — Architecture and Database Design  
**Implementation Status:** PROPOSED & UNRESOLVED (Awaiting Explicit User & Statutory Approval)  
**Author:** Antigravity AI & Architecture Team  
**Last Updated:** 2026-11-12  

---

## 1. Decision Governance Policy

To guarantee regulatory compliance, tax validity, and architectural soundness:
- **Zero Silent Assumptions:** No unresolved statutory, accounting, or business question may be assumed approved without explicit user and accounting professional confirmation.
- **Traceability:** Every decision records the problem context, evaluated alternatives, financial impact, operational risks, and recommendation.
- **Status Categories:**
  - `[UNRESOLVED — Pending User Confirmation]`
  - `[UNRESOLVED — Requires Indian CA / Tax Professional Sign-Off]`
  - `[PROPOSED — Architectural Recommendation Only]`

---

## 2. Decision Register

### DEC-ACC-001: Chart of Accounts Structure & Hierarchy Extensibility
- **Status:** `[PROPOSED — Architectural Recommendation Only]`
- **Context:** An enterprise ERP must accommodate varied industry sectors (retail, wholesale, manufacturing, services) while maintaining standard financial reporting and automated module posting rules.
- **Options Evaluated:**
  - *Option A (Rigid Fixed COA):* Hardcode account numbers and groups. Simple to implement, but fails when a business requires custom bank accounts, multi-department expense tracking, or specialized revenue streams.
  - *Option B (Fully Freeform COA):* Allow users to define arbitrary account codes and hierarchies without structural limits. High flexibility, but breaks automated posting matrices when system control accounts are deleted or misconfigured.
  - *Option C (Hybrid System-Governed Hierarchical COA — Recommended):* Ship with an audited baseline standard Chart of Accounts (1000s to 5000s). Enforce system locks (`is_system_locked = true`) on mandatory control accounts (AR `1130`, AP `2110`, Inventory `1140`, Output Tax `2121–2123`, Rounding `4250`, Retained Earnings `3200`). Allow businesses to add unlimited child groups and custom detail accounts under the standard groups.
- **Financial & Architectural Implications:** Option C protects automated posting matrices from breaking while supporting business customization.
- **Recommendation:** Implement Option C for MAGIC V1.

---

### DEC-ACC-002: Inventory Valuation Model & Fiscal-Year Policy Lock
- **Status:** `[UNRESOLVED — Requires Indian CA / Tax Professional Sign-Off]`
- **Context:** Indian Accounting Standard 2 (Ind AS 2) / AS 2 mandates that inventory cost must be determined using either the FIFO (First-In-First-Out) or Weighted Average Cost method. The chosen method must be applied consistently across periods; arbitrary mid-year changes distort the Balance Sheet and taxable profits.
- **Options Evaluated:**
  - *Option 1 (Moving Weighted Average Cost - AVCO):* Recompute unit valuation rate dynamically upon every purchase inward:
    $$\text{Rate}_{\text{new}} = \text{Round}\left(\frac{(\text{Qty}_{\text{curr}} \times \text{Rate}_{\text{curr}}) + (\text{Qty}_{\text{in}} \times \text{Rate}_{\text{in}})}{\text{Qty}_{\text{curr}} + \text{Qty}_{\text{in}}}\right)$$
    Simpler data structure; does not require tracking individual batch lots for valuation. Ideal for bulk retail, commodities, and fast-moving consumer goods.
  - *Option 2 (First-In-First-Out - FIFO):* Requires tracking discrete purchase lots/batches. Depletes oldest lots first during sales issues. Balance Sheet inventory reflects the most recent purchase prices. Necessary for batch-tracked, perishable, or pharmaceutical goods.
  - *Option 3 (Configurable per Company with Strict Fiscal-Year Lock):* Allow the enterprise to configure FIFO or AVCO at company setup. Lock the policy for the entire fiscal year. Policy switches are permitted only on the first day of a new fiscal year with an automated valuation adjustment journal voucher and mandatory auditor notation.
- **Financial & Architectural Implications:** Option 3 gives maximum versatility across business types without violating consistency accounting principles.
- **Recommendation:** Implement Option 3; present to the user and their chartered accountant for formal approval before Phase 3 inventory implementation.

---

### DEC-ACC-003: Statutory GST Rounding Mechanics (Invoice Total vs Line Item)
- **Status:** `[UNRESOLVED — Requires Indian CA / Tax Professional Sign-Off]`
- **Context:** Section 170 of the Central Goods and Services Tax Act (CGST Act) mandates that the amount of tax, interest, penalty, fine or any other sum payable, and the amount of refund or any other sum due, shall be rounded off to the nearest rupee. However, trade practices vary between:
  1. Rounding individual line-item tax amounts to nearest integer paise and applying Section 170 only to the final invoice aggregate.
  2. Rounding each line item to the nearest whole rupee (which causes severe compounding errors on multi-item invoices).
- **Options Evaluated:**
  - *Option A (Line-Level Paise + Final Invoice Section 170 Rounding — Recommended):*
    1. Line-item taxable amount and tax heads (CGST, SGST, IGST) are computed in exact integer paise using basis-point math.
    2. Tax heads are summed across all line items to obtain exact totals.
    3. The invoice grand total is rounded off to the nearest whole rupee:
       $$\text{Remainder} = \text{GrandTotal} \pmod{100}$$
       If $\text{Remainder} \ge 50$, add $100 - \text{Remainder}$ (positive delta); if $< 50$, subtract $\text{Remainder}$ (negative delta).
    4. The round-off delta is booked to GL Account `4250: Rounding Off Adjustment`.
  - *Option B (Line-Level Rupee Rounding):* Forces each line to round to the nearest whole rupee. Causes major pricing distortions on low-value FMCG goods (e.g. ₹5 or ₹10 items).
- **Financial & Architectural Implications:** Option A complies with Section 170, aligns with GSTR-1 / NIC e-Invoice validation schemas, and eliminates fractional paise leakage.
- **Recommendation:** Adopt Option A subject to formal review by a qualified Indian tax practitioner.

---

### DEC-ACC-004: NIC e-Invoicing & e-Way Bill Integration Approach
- **Status:** `[UNRESOLVED — Pending User Confirmation]`
- **Context:** Indian B2B transactions exceeding the statutory turnover threshold (currently ₹5 Crore) require mandatory electronic invoice generation (IRN - Invoice Reference Number via NIC) and e-Way Bill generation for goods movement exceeding ₹50,000.
- **Options Evaluated:**
  - *Option A (Offline JSON / Excel Export for V1 — Recommended):*
    - The system validates and exports NIC-compliant JSON payloads matching the official Schema v1.1.
    - The user uploads the JSON to the government e-Invoice / e-Way Bill portal or uses the offline bulk tool.
    - The user inputs the generated IRN, Ack No, and QR Code payload into MAGIC to finalize the tax invoice.
    - **Advantages:** Zero recurring GSP (GST Suvidha Provider) API subscription costs; works reliably in local LAN / offline-capable environments; zero third-party data transmission risks.
  - *Option B (Direct GSP / API Integration):*
    - Direct programmatic API integration with an approved GSP (e.g. ClearTax, Masters India, IRIS).
    - Requires continuous high-speed internet, API credentials, and recurring third-party fee per API call.
- **Financial & Architectural Implications:** Option A provides 100% statutory capability for V1 without external costs. Option B can be added as a modular upgrade in future phases.
- **Recommendation:** Implement Option A for MAGIC V1 with clean integration hooks for Option B.

---

### DEC-ACC-005: Year-End Closing & Retained Earnings Mechanics
- **Status:** `[PROPOSED — Architectural Recommendation Only]`
- **Context:** At the end of a fiscal year, nominal accounts (Revenue 4000s and Expenses 5000s) must be cleared to zero, and the net profit or loss transferred to Equity (`3200: Retained Earnings`).
- **Options Evaluated:**
  - *Option A (Automated Closing Journal Voucher):* A dedicated year-end wizard generates a balanced closing journal entry on the last day of the fiscal year:
    - Dr all Revenue accounts (clearing them to 0).
    - Cr all Expense accounts (clearing them to 0).
    - Dr/Cr the balancing net profit/loss to `3200: Retained Earnings`.
    - Advantages: 100% audit trail transparency; visible in General Ledger; leaves no hidden calculations.
  - *Option B (Implicit Dynamic Rollforward):* Nominal accounts are never formally cleared in the database; reporting queries dynamically exclude prior years' movements.
    - Disadvantages: Query complexity compounds over multiple years; increased risk of performance degradation.
- **Recommendation:** Implement Option A (Automated Closing Journal Voucher with full audit history).

---

### DEC-ACC-006: Subledger Item Matching: Itemized Open-Item vs Balance-Forward
- **Status:** `[PROPOSED — Architectural Recommendation Only]`
- **Context:** Customer and supplier accounts require tracking outstanding balances for payment collections and credit terms.
- **Options Evaluated:**
  - *Option A (Itemized Open-Item Allocation — Recommended):*
    - Every invoice and payment is an individual open item (`ar_subledger_items`, `ap_subledger_items`).
    - Receipts and payments explicitly settle specific invoices (`ar_allocations`, `ap_allocations`).
    - Enables exact bill-wise aging (0–30, 31–60, 61–90, 90+ days), dispute tracking on individual bills, and precise interest on delayed payments.
  - *Option B (Balance-Forward / Running Balance):*
    - Only tracks the net balance; payments simply reduce the overall account total without linking to specific invoices.
    - Inability to verify which specific bills are unpaid; inaccurate aging reports.
- **Recommendation:** Implement Option A (Itemized Open-Item Allocation) with an optional "Auto-FIFO Settle" helper for cashiers who wish to settle oldest bills first.

---

### DEC-ACC-007: Statutory Review Flag: Indian Compliance Points for Professional Verification
- **Status:** `[UNRESOLVED — Requires Indian CA / Tax Professional Sign-Off]`
- **Points Requiring Formal CA Confirmation:**
  1. **Section 43B(h) of the Income Tax Act:** Mandates deduction of supplier dues to registered Micro and Small enterprises (MSMEs) only on actual payment within 45 days (or 15 days if no written agreement). The AP subledger must support tracking MSME registration status, enterprise category (Micro/Small/Medium), and statutory due date alerts.
  2. **TDS on Purchase of Goods (Section 194Q):** Buyer turnover $> ₹10 Cr and purchases from a resident supplier $> ₹50 Lakhs in a FY requires 0.1% TDS deduction at the time of credit or payment.
  3. **TCS on Sale of Goods (Section 206C(1H)):** Seller turnover $> ₹10 Cr and sales receipts from a buyer $> ₹50 Lakhs in a FY requires 0.1% TCS collection if Section 194Q is not deducted by the buyer.
  4. **Rule 42 & 43 ITC Reversals:** Proportionate reversal of Input Tax Credit on goods/services used for exempt supplies or non-business purposes.
- **Action Required:** Flag these compliance hooks in the database schema (`accounts`, `party_settings`) without hardcoding disputed formulas until approved.

