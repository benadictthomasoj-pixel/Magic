# MAGIC ERP — ACCOUNTING INTEGRATION SPECIFICATION
## Comprehensive Double-Entry Posting Engine & Operational Integration Matrix

**Document Version:** 2.0.0  
**Project:** MAGIC – Universal Business Management ERP  
**Core Invariant:** Double-Entry Equilibrium ($\sum \text{Debit} = \sum \text{Credit}$ to exact integer paise)  
**Database Schema:** `accounting`  
**Posting Authority:** Single Source of Financial Truth (No shadow balances permitted)  
**Last Updated:** 2026-11-12  

---

## 1. Accounting Invariants & Rules

1. **Integer Paise Storage:** All ledger debit and credit lines are signed 64-bit integers (`long` / `BIGINT`). Zero floating-point money.
2. **Atomic Double-Entry Transactions:** Operational actions (Sales, Purchases, Payments, Stock Adjustments) and their corresponding journal voucher are committed in a **single atomic PostgreSQL transaction**.
3. **Immutability of Posted Records:** Once a voucher is marked `posted = true`, it is protected by PostgreSQL triggers (`trg_protect_posted_voucher_header` and `trg_protect_posted_journal_lines`). Direct updates and deletions are rejected by the database engine.
4. **Correction via Reversal Only:** Corrections must be executed via an explicit reversal voucher (`POST /api/v1/accounting/vouchers/{id}/reverse`) or a commercial credit/debit note.
5. **Fiscal Period Locking:** Postings are rejected if the transaction date falls within a locked or closed fiscal period.

---

## 2. Standard Chart of Accounts (COA) Control Accounts

| Account Code | Account Name | Account Group | Class | Normal Balance | Purpose |
|:---:|:---|:---:|:---:|:---:|:---|
| **11100** | Cash on Hand | Current Assets | Class 1 (Assets) | Debit | Cash counter and store safe balances |
| **11200** | Bank Accounts (Main Operating) | Current Assets | Class 1 (Assets) | Debit | Primary operating bank accounts |
| **11300** | Accounts Receivable (Trade Debtors) | Current Assets | Class 1 (Assets) | Debit | Customer credit control account (AR Subledger) |
| **11400** | Merchandise Inventory Asset | Current Assets | Class 1 (Assets) | Debit | Real-time valuation of stock on hand |
| **11510** | Input CGST (ITC Eligible) | Duties & Taxes | Class 1 (Assets) | Debit | Central GST tax credits paid on purchases |
| **11520** | Input SGST (ITC Eligible) | Duties & Taxes | Class 1 (Assets) | Debit | State GST tax credits paid on purchases |
| **11530** | Input IGST (ITC Eligible) | Duties & Taxes | Class 1 (Assets) | Debit | Integrated GST tax credits on interstate buys |
| **21100** | Accounts Payable (Trade Creditors) | Current Liabilities | Class 2 (Liabilities) | Credit | Vendor liability control account (AP Subledger) |
| **21210** | Output CGST Liability | Duties & Taxes | Class 2 (Liabilities) | Credit | Central GST collected on outward taxable sales |
| **21220** | Output SGST Liability | Duties & Taxes | Class 2 (Liabilities) | Credit | State GST collected on outward taxable sales |
| **21230** | Output IGST Liability | Duties & Taxes | Class 2 (Liabilities) | Credit | Integrated GST collected on interstate sales |
| **21300** | Salaries & Wages Payable | Current Liabilities | Class 2 (Liabilities) | Credit | Accrued employee compensation liability |
| **21400** | Statutory Deductions Payable | Current Liabilities | Class 2 (Liabilities) | Credit | PF, ESI, Professional Tax, TDS liabilities |
| **41100** | Sales Revenue (Merchandise) | Operating Inward | Class 4 (Inward) | Credit | Gross taxable commercial sales turnover |
| **41200** | Service Revenue | Operating Inward | Class 4 (Inward) | Credit | Workshop repair and AMC service income |
| **51100** | Cost of Goods Sold (COGS) | Direct Operating | Class 5 (Outward) | Debit | Cost of inventory depleted during sales |
| **52100** | Operating Expenses (General) | Indirect Operating | Class 5 (Outward) | Debit | Rent, utilities, supplies, freight |
| **52200** | Salaries & Wages Expense | Indirect Operating | Class 5 (Outward) | Debit | Gross employee compensation expense |
| **53100** | Depreciation Expense | Non-Cash Operating | Class 5 (Outward) | Debit | Periodical fixed asset depreciation charge |

---

## 3. Universal Transaction-to-GL Posting Matrix

### TX-01: Retail POS Cash Checkout (Intrastate)
- **Debit:** Cash on Hand (`11100`) — Gross Receipt Amount
- **Credit:** Sales Revenue (`41100`) — Net Taxable Turnover
- **Credit:** Output CGST Liability (`21210`) — Central GST
- **Credit:** Output SGST Liability (`21220`) — State GST
- *(Perpetual Inventory)*:
  - **Debit:** Cost of Goods Sold (`51100`) — Total Item Cost
  - **Credit:** Merchandise Inventory (`11400`) — Total Item Cost

### TX-02: Retail POS Digital / UPI / Card Checkout
- **Debit:** Bank Operating / Card Clearing (`11200`) — Gross Receipt Amount
- **Credit:** Sales Revenue (`41100`) — Net Taxable Turnover
- **Credit:** Output CGST Liability (`21210`) — Central GST
- **Credit:** Output SGST Liability (`21220`) — State GST
- *(Perpetual Inventory)*:
  - **Debit:** Cost of Goods Sold (`51100`) — Total Item Cost
  - **Credit:** Merchandise Inventory (`11400`) — Total Item Cost

### TX-03: Wholesale B2B Credit Tax Invoice (Interstate)
- **Debit:** Accounts Receivable (`11300`) — Gross Invoice Value (Posted to AR Subledger)
- **Credit:** Sales Revenue (`41100`) — Net Taxable Base
- **Credit:** Output IGST Liability (`21230`) — Integrated GST
- *(Perpetual Inventory)*:
  - **Debit:** Cost of Goods Sold (`51100`) — Total Item Cost
  - **Credit:** Merchandise Inventory (`11400`) — Total Item Cost

### TX-04: Sales Return / Credit Note
- **Debit:** Sales Returns / Revenue (`41100`) — Net Taxable Value
- **Debit:** Output CGST Liability (`21210`) — CGST Adjustment
- **Debit:** Output SGST Liability (`21220`) — SGST Adjustment
- **Credit:** Accounts Receivable (`11300`) or Cash (`11100`) — Gross Refund Value
- *(Inventory Restock)*:
  - **Debit:** Merchandise Inventory (`11400`) — Restocked Item Cost
  - **Credit:** Cost of Goods Sold (`51100`) — Restocked Item Cost

### TX-05: Wholesale Purchase Invoice (Intrastate with Input Tax Credit)
- **Debit:** Merchandise Inventory (`11400`) — Net Taxable Item Cost
- **Debit:** Input CGST ITC (`11510`) — Central GST Paid
- **Debit:** Input SGST ITC (`11520`) — State GST Paid
- **Credit:** Accounts Payable (`21100`) — Gross Bill Amount (Posted to AP Subledger)

### TX-06: Purchase Return / Debit Note
- **Debit:** Accounts Payable (`21100`) — Gross Debit Note Amount
- **Credit:** Merchandise Inventory (`11400`) — Returned Item Cost
- **Credit:** Input CGST ITC (`11510`) — Reversed Central Tax Credit
- **Credit:** Input SGST ITC (`11520`) — Reversed State Tax Credit

### TX-07: Customer Payment Receipt (Receipt Voucher)
- **Debit:** Bank Accounts (`11200`) or Cash (`11100`) — Amount Received
- **Credit:** Accounts Receivable (`11300`) — Amount Received (Allocated to AR Invoices)

### TX-08: Supplier Payment Disbursement (Payment Voucher)
- **Debit:** Accounts Payable (`21100`) — Amount Disbursed (Allocated to AP Invoices)
- **Credit:** Bank Accounts (`11200`) or Cash (`11100`) — Amount Disbursed

### TX-09: Direct Operating Expense
- **Debit:** Operating Expense (`52100`) — Net Taxable Expense
- **Debit:** Input GST ITC (`11510`/`11520`) — Eligible ITC (if tax invoice available)
- **Credit:** Cash on Hand (`11100`) or Bank Accounts (`11200`) — Total Paid

### TX-10: Petty Cash Float Disbursement & Replenishment
- **Float Disbursement:** Debit Petty Cash (`11110`), Credit Main Cash (`11100`)
- **Expense Settlement:** Debit Operating Expense (`52100`), Credit Petty Cash (`11110`)

### TX-11: Physical Stock Audit Adjustment (Inventory Variance)
- **Stock Surplus:** Debit Merchandise Inventory (`11400`), Credit Inventory Gain (`41300`)
- **Stock Deficit/Wastage:** Debit Inventory Loss / Shrinkage (`51200`), Credit Merchandise Inventory (`11400`)

### TX-12: Fixed Asset Depreciation Batch
- **Debit:** Depreciation Expense (`53100`) — Monthly Depreciation Amount
- **Credit:** Accumulated Depreciation (`12190`) — Monthly Depreciation Amount

### TX-13: Monthly Payroll Booking & Disbursement
- **Accrual:**
  - **Debit:** Salaries & Wages Expense (`52200`) — Gross Payroll
  - **Credit:** Statutory Deductions Payable (`21400`) — PF + ESI + PT + TDS
  - **Credit:** Salaries Payable (`21300`) — Net Salary Due
- **Disbursement:**
  - **Debit:** Salaries Payable (`21300`) — Net Salary Disbursed
  - **Credit:** Bank Operating (`11200`) — Net Salary Disbursed

### TX-14: Monthly GST Statutory Settlement
- **Debit:** Output CGST Liability (`21210`) — Offset Amount
- **Debit:** Output SGST Liability (`21220`) — Offset Amount
- **Credit:** Input CGST ITC (`11510`) — Set-off Utilized
- **Credit:** Input SGST ITC (`11520`) — Set-off Utilized
- **Credit:** Bank Accounts (`11200`) — Net Cash Tax Remittance (via Challan PMT-06)

