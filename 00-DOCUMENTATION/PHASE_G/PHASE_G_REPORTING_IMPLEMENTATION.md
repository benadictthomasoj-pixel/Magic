# MAGIC ERP — PHASE G: ADVANCED REPORTING & BUSINESS INTELLIGENCE
## Module 17 Implementation Report

**Subsystem:** Module 17 — Reports & Business Intelligence  
**Service:** `IReportService` / `ReportService`  
**Controller:** `ReportsController` (`/api/v1/reports`)  
**Frontend Component:** `EnterpriseReportsComponent` (`/billing/reports`)  

---

### 1. Unified Executive Dashboard

The Executive Dashboard aggregates high-level operational telemetry from all core subsystems into a single atomic payload:
- **Commercial & Sales:** Gross Sales, Net Sales (deducting returns and allowances), Total Tax Collected.
- **Inbound Procurement:** Total Goods Received Note (GRN) purchases across all active suppliers.
- **Direct Operating Expenses:** Summarized from petty cash disbursements and direct operating expenditure vouchers.
- **Profitability Metrics & Standard Disclosure:**
  - Gross Profit is calculated as `NetSalesPaise - CostOfGoodsSoldPaise`.
  - **Accounting Decision Disclosure:** Labeled explicitly with status `"Provisional - DEC-ACC-002 (Inventory Valuation FIFO vs MWA Unresolved)"`.
- **Treasury Position:** Liquid cash balances on hand plus verified bank ledger balances.
- **Working Capital:** Total Accounts Receivable (AR) vs Accounts Payable (AP).
- **Inventory Health:** Total physical units on hand and count of items breaching their reorder alert threshold.
- **HR & Payroll:** Active headcount and previous calendar month payroll disbursement liability.
- **Branch Performance:** Tabulated gross sales and completed order count per registered branch.

---

### 2. Analytical Subsystem Reports

1. **Sales by Period, Customer, and Category:**
   - Aggregates invoiced quantities, net sales amounts, and tax totals grouped by date, party, or merchandise hierarchy.
2. **Purchases by Supplier:**
   - Summarizes inbound procurement volumes and payables exposure per trade creditor.
3. **Inventory Summary & Low-Stock Alerts:**
   - Real-time stock valuation based on ledger-verified quantities on hand across warehouses.
4. **General Ledger Reconciliation:**
   - Verifies trial balance debits against credits and validates that no orphan financial transactions exist.
5. **HR & Payroll Analytics:**
   - Summarizes monthly gross salary distributions, statutory deductions (PF, ESI, TDS), and attendance records.

---

### 3. Report Export Subsystem

- Supports format exports to `csv`, `pdf`, and preview formats.
- Generates RFC 4180-compliant CSV files with correct column escaping.
- Generates structured binary PDF representations using tabular layouts and metadata headers.
- Emits download streams with appropriate `Content-Disposition` and `Content-Type` headers.
- Logs all export actions to `reporting.report_export_logs` for compliance auditing.
