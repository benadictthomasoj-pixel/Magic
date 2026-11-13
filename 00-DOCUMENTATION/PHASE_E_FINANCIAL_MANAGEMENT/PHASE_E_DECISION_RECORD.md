# MAGIC ERP — PHASE E: DECISION RECORD & ARCHITECTURAL BOUNDARIES

**Project:** MAGIC – Universal Business Management ERP  
**Phase:** E — Financial Management & Treasury  

---

## 1. Statutory & Accounting Policy Isolation

| Decision ID | Area | Strategic Position | Implementation Boundary |
|---|---|---|---|
| **DEC-ACC-002** | Inventory Valuation (FIFO vs Weighted Avg) | **Frozen.** Do not fabricate automated GL COGS postings from inventory movements. | Inventory records track physical movements; commercial purchase invoices record commercial liability against Purchase Expense (`5101`). |
| **DEC-ACC-003** | GST Rounding & Split Allocation | Encapsulated inside `IGstCalculationService`. | Half-paise statutory rounding and Intra-state (CGST 50% + SGST 50%) vs Inter-state (IGST 100%) split strictly computed via domain service. |
| **DEC-ACC-004** | E-Invoice & E-Way Bill Integration | Out of scope for local Phase E core ledger. | Schema provisions `e_invoice_irn` and `e_way_bill_number` as optional strings; no mock external portal calls. |
| **DEC-ACC-007** | MSME 45-day payment statutory deadlines & TDS 194Q | Contractual due dates preserved. | Due date calculations use agreed payment terms. Statutory reporting flags are separate from primary settlement ledger arithmetic. |

---

## 2. Bank Account Masking & Reveal Security Boundary

To protect enterprise banking credentials:
1. `account_number` is never exposed in standard list or detail APIs.
2. `masked_account_number` (displaying only the last 4 digits, e.g. `••••••••5678`) is returned to normal users.
3. Only elevated users with the `finance:bank-accounts:reveal` permission can request the unmasked account number via `POST /api/v1/finance/bank-accounts/{id}/reveal`.
4. Every call to the reveal endpoint automatically writes an immutable entry into `platform.security_audit_events` with the user ID, timestamp, and client IP.
