# MAGIC — ACCOUNTING ENGINE RULES & PROTOCOLS

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  

---

## 1. Fundamental Accounting Postulate: Absolute Double-Entry Integrity

The financial module of MAGIC is the **single source of financial truth** for the enterprise. It implements a strict, GAAP-compliant, immutable double-entry general ledger.

### Invariant Equation
For every posted voucher $V$:
$$\sum_{i=1}^{n} \text{DebitPaise}_i = \sum_{j=1}^{m} \text{CreditPaise}_j \quad \text{where } \text{DebitPaise}, \text{CreditPaise} \in \mathbb{Z}_{\ge 0}$$

No transaction can be committed to the database if the net balance $\sum \text{Debit} - \sum \text{Credit} \ne 0$.

---

## 2. Standard Chart of Accounts (COA) Structure

Accounts are organized hierarchically under five fundamental accounting classes:

```
1000-1999: ASSETS
  1100: Current Assets
    1110: Cash & Cash Equivalents
      1111: Cash on Hand (Counter Float)
      1115: Petty Cash Float
    1120: Bank Accounts (Current Accounts, Savings, Fixed Deposits)
      1121: Bank Clearing / POS Merchant Settlement
    1130: Accounts Receivable / Trade Debtors (Customer Subledger Control)
    1140: Inventory Asset (Finished Goods, Raw Materials)
      1145: Stock in Transit (Inter-Branch Clearing Asset)
    1150: GST Input Tax Credit (ITC)
      1151: Input Tax Credit (ITC) - CGST
      1152: Input Tax Credit (ITC) - SGST
      1153: Input Tax Credit (ITC) - IGST
    1160: Prepaid Expenses & Advance Payments
    1170: Staff Advances (Employee Subledger)
  1200: Non-Current Assets
    1210: Property, Plant & Equipment (PPE)
    1220: Accumulated Depreciation (Contra-Asset)

2000-2999: LIABILITIES
  2100: Current Liabilities
    2110: Accounts Payable / Trade Creditors (Supplier Subledger Control)
      2115: Freight / Transporter Payable
      2119: GRN Clearing Account
    2120: Statutory Dues Payable
      2121: Output CGST Payable
      2122: Output SGST Payable
      2123: Output IGST Payable
    2130: Payroll & Employee Dues Payable
      2131: Net Salaries Payable
      2132: Provident Fund (PF) Payable
      2133: ESIC Payable
      2134: Professional Tax (PT) Payable
      2135: TDS on Salaries (Section 192) Payable
    2140: Accrued Expenses & Short-Term Provisions
  2200: Non-Current Liabilities
    2210: Term Loans & Long-Term Borrowings

3000-3999: EQUITY
  3100: Owner's Capital / Share Capital
  3199: Opening Balance Suspense Account (Cutover Temporary)
  3200: Retained Earnings
  3210: Income Summary Account (Year-End Closing)
  3300: Drawings / Distributions

4000-4999: REVENUE
  4100: Operating Revenue
    4110: Sales Revenue - Retail
    4120: Sales Revenue - Wholesale
    4130: Sales Returns (Contra-Revenue)
  4200: Discounts & Adjustments
    4210: Cash Discounts Allowed (Contra-Revenue / Expense)
    4250: Rounding Off Adjustment (Section 170)
  4300: Other Income
    4310: Cash Discounts Received
    4320: Inventory Adjustment Gain

5000-5999: EXPENSES
  5100: Cost of Goods Sold (COGS)
    5120: Stock Adjustment Loss / Scrap
  5200: Operating Expenses
    5210: Salaries & Wages Expense
    5215: Employer PF Contribution
    5216: Employer ESIC Contribution
  5300: Depreciation & Amortization Expense
    5310: Depreciation Expense
  5400: Financial & Bank Charges
```

---

## 3. Standard Posting Rules by Business Transaction

All operational modules interface with Accounting via pre-configured, audited posting templates:

| Business Transaction | Source Module | Debited Account(s) | Credited Account(s) |
|---|---|---|---|
| **B2C Cash Retail Sale** | POS | Cash on Hand (1111) | Sales Revenue - Retail (4110)<br>Output CGST Payable (2121)<br>Output SGST Payable (2122) |
| **B2B Credit Sale** | Sales | Customer AR Control (1130) | Sales Revenue - Wholesale (4120)<br>Output IGST / CGST / SGST |
| **Sales Cost of Goods** | Inventory | Cost of Goods Sold (5100) | Inventory Asset (1140) |
| **Credit Purchase of Stock**| Purchases | Inventory Asset / GRN Clearing (1140 / 2119)<br>Input CGST Credit (1151)<br>Input SGST Credit (1152) | Supplier AP Control (2110) |
| **Customer Receipt (UPI)** | POS / CRM | Bank Account / Clearing (1120 / 1121) | Customer AR Control (1130) |
| **Supplier Payment** | Finance | Supplier AP Control (2110) | Bank Current Account (1120) |
| **Monthly Payroll Run** | Payroll | Salary Expense (5210)<br>Employer PF Contribution (5215) | Net Salary Payable (2131)<br>PF Payable (2132)<br>TDS Payable (2135) |
| **Asset Depreciation** | Finance | Depreciation Expense (5310) | Accumulated Depreciation (1220) |
| **Inter-Branch Stock Transfer**| Multi-Branch | Stock in Transit (1145) | Origin Warehouse Stock (1140) |
| **Stock Scrap / Loss** | Inventory | Stock Adjustment Loss (5120) | Inventory Asset (1140) |

---

## 4. Subledger Reconciliations

1. **Accounts Receivable (AR) Control Account:**
   The balance in GL account `1130 (Accounts Receivable)` must at all times match the sum of individual customer balances in `customers.customers`:
   $$\text{Balance}(\text{GL}_{1130}) \equiv \sum_{k=1}^{N} \text{CustomerBalance}_k$$
2. **Accounts Payable (AP) Control Account:**
   The balance in GL account `2110 (Accounts Payable)` must match the sum of individual supplier balances in `suppliers.suppliers`:
   $$\text{Balance}(\text{GL}_{2110}) \equiv \sum_{m=1}^{M} \text{SupplierBalance}_m$$
3. **Inventory Asset Account:**
   The balance in GL account `1140 (Inventory Asset)` must equal the sum of item valuation quantities $\times$ item valuation rates from `inventory.stock_balances`.

---

## 5. Audit Controls & Immutability Rules

1. **No In-Place Modifications:**
   Once a voucher transition status is marked `POSTED`, its header fields (`posting_date`, `voucher_type`, `total_debit_paise`) and child `journal_lines` are locked permanently at the database trigger level.
2. **Reversals and Replacements:**
   If a posted voucher contains an error, the operator must execute a **Voucher Reversal**. The system creates a new linked counter-voucher with inverted debit/credit lines, referencing `reversal_of_voucher_id`. The user may then post a corrected replacement voucher.
3. **Accounting Period Locking:**
   Accounting periods (e.g., Monthly / Quarterly / Annual) can be locked by the Financial Controller. No voucher with a `posting_date` inside a locked period may be created or reversed.
4. **Reopening Authorization:**
   Reopening a locked period requires explicit supervisor authorization, logging an immutable audit record containing: Reason, Authorized By, Timestamp, and IP Address.

