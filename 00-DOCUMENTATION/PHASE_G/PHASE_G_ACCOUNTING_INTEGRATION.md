# MAGIC ERP — PHASE G: ACCOUNTING INTEGRATION & RECONCILIATION

**Financial Truth Engine:** Double-Entry General Ledger (`accounting` schema)  
**Monetary Standard:** Signed 64-bit integer paise (`long`/`bigint`)  

---

### 1. General Ledger as Single Source of Truth

Phase G maintains absolute adherence to the architectural rule: **no reporting, branch transfer, or operational decision may bypass or contradict the General Ledger**.
- All revenue displayed in executive BI dashboards is queried directly from `SalesInvoices` whose journal vouchers are posted in `JournalEntries` / `Vouchers`.
- Operating expenses reconcile with double-entry debits against Expense GL Accounts (5xxx series) and credits against Bank/Cash GL Accounts (1xxx series).
- Accounts Receivable (AR) reflects unpaid balances on customer sales invoices with subledger links to customer accounts.
- Accounts Payable (AP) reflects unsettled liabilities on supplier purchase invoices with subledger links to trade creditor accounts.

---

### 2. Treatment of Unresolved Accounting Decisions

In accordance with strict system rules, the following provisional accounting decisions remain unresolved and are explicitly labeled:

1. **DEC-ACC-002: FIFO vs Moving Weighted Average (MWA) Valuation:**
   - Inventory valuation and Cost of Goods Sold calculations currently reflect provisional FIFO logic.
   - All executive dashboard outputs, API DTOs, and reporting screens display:
     `"Provisional - DEC-ACC-002 (Inventory Valuation FIFO vs MWA Unresolved)"`.
2. **DEC-ACC-003: Multi-Currency Unrealized Forex Gains/Losses:**
   - Multi-currency transactions evaluate at transaction-date spot rate.
   - Period-end revaluation entries remain flagged provisional pending final corporate treasury policy sign-off.
3. **DEC-ACC-004: Direct Write-Off vs Allowance Method for Bad Debts:**
   - Aging reports identify overdue customer receivables (>30, >60, >90 days).
   - Write-offs require formal supervisory journal vouchers rather than automatic provisioning.
4. **DEC-ACC-007: Depreciation Method (Straight Line vs Written Down Value):**
   - Fixed asset registers maintain book values provisional until schedule finalization.

---

### 3. Financial Reconciliation Verification

Automated integration test `Scenario17_FinancialReport_ReconcilesWithGeneralLedger` verifies that:
- Total Debits equal Total Credits across all posted financial vouchers.
- Subsystem transaction sums (Sales, Purchases, Expenses) reconcile 100% with the respective GL account balances.
- No orphan or unbalanced voucher entries exist in the system.
