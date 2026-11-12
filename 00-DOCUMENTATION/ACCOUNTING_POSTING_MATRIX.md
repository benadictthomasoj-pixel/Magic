# MAGIC — COMPREHENSIVE ACCOUNTING POSTING MATRIX

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  

---

## 1. Architectural Posting Rules

Every financial transaction in MAGIC generates an immutable, balanced **Journal Voucher** in the `accounting.vouchers` and `accounting.journal_lines` tables:
$$\sum \text{DebitPaise} \equiv \sum \text{CreditPaise}$$

All monetary amounts are represented as 64-bit integer paise (₹1 = 100 paise).

---

## 2. Master Transaction Posting Matrix

### 2.1 Sales & Revenue Transactions

#### TX-01: Retail Counter Cash Sale (GST Inclusive)
- **Trigger:** POS cashier completes cash checkout.
- **Debits:**
  - `1111 Cash on Hand (Counter Float)`: [Final Payable Amount = Rounded Invoice Total]
- **Credits:**
  - `4110 Sales Revenue - Retail`: [Taxable Amount]
  - `2121 Output CGST Payable`: [CGST Amount]
  - `2122 Output SGST Payable`: [SGST Amount]
  - `4250 Rounding Off Adjustment`: [Round-off delta if positive credit, else debited if negative]

#### TX-02: Retail Counter Digital Sale (UPI / QR Code / Card)
- **Trigger:** POS cashier completes digital tender.
- **Debits:**
  - `1121 Bank Clearing / POS Merchant Settlement`: [Final Payable Amount = Rounded Invoice Total]
- **Credits:**
  - `4110 Sales Revenue - Retail`: [Taxable Amount]
  - `2121 Output CGST Payable`: [CGST Amount]
  - `2122 Output SGST Payable`: [SGST Amount]
  - `4250 Rounding Off Adjustment`: [Round-off delta if positive credit, else debited if negative]

#### TX-03: B2B Wholesale Credit Sale (Intra-State: CGST + SGST)
- **Trigger:** Wholesale Tax Invoice approved and posted.
- **Debits:**
  - `1130 Accounts Receivable (Customer Subledger)`: [Grand Total]
- **Credits:**
  - `4120 Sales Revenue - Wholesale`: [Taxable Amount]
  - `2121 Output CGST Payable`: [CGST Amount]
  - `2122 Output SGST Payable`: [SGST Amount]
  - `4250 Rounding Off Adjustment`: [Round-off delta]

#### TX-04: B2B Wholesale Credit Sale (Inter-State: IGST)
- **Trigger:** Wholesale Tax Invoice with Place of Supply $\ne$ Company State Code.
- **Debits:**
  - `1130 Accounts Receivable (Customer Subledger)`: [Grand Total]
- **Credits:**
  - `4120 Sales Revenue - Wholesale`: [Taxable Amount]
  - `2123 Output IGST Payable`: [IGST Amount]
  - `4250 Rounding Off Adjustment`: [Round-off delta]

#### TX-05: Sales Cost of Goods Sold (COGS) Recognition
- **Trigger:** Physical stock issue confirmation on Sales Invoice.
- **Debits:**
  - `5100 Cost of Goods Sold (COGS)`: [Total Item Valuation Cost]
- **Credits:**
  - `1140 Inventory Asset (Warehouse Stock)`: [Total Item Valuation Cost]

#### TX-06: Sales Return with Credit Note (Against Invoice)
- **Trigger:** Customer returns goods; Credit Note issued.
- **Debits:**
  - `4130 Sales Returns (Contra-Revenue)`: [Taxable Amount]
  - `2121 Output CGST Payable (Reversal)`: [CGST Amount]
  - `2122 Output SGST Payable (Reversal)`: [SGST Amount]
- **Credits:**
  - `1130 Accounts Receivable (Customer Subledger)`: [Credit Note Grand Total]
  - *(Companion Inventory Return Journal)*:
    - Dr `1140 Inventory Asset`: [Restored Valuation Cost]
    - Cr `5100 Cost of Goods Sold`: [Restored Valuation Cost]

---

### 2.2 Purchase & Procurement Transactions

#### TX-07: Credit Purchase of Inventory (Intra-State with ITC)
- **Trigger:** Purchase Invoice verified and posted.
- **Debits:**
  - `1140 Inventory Asset (or 2119 GRN Clearing)`: [Taxable Purchase Value]
  - `1151 Input Tax Credit (ITC) - CGST`: [Input CGST Amount]
  - `1152 Input Tax Credit (ITC) - SGST`: [Input SGST Amount]
- **Credits:**
  - `2110 Accounts Payable (Supplier Subledger)`: [Invoice Grand Total]
  - `4250 Rounding Off Adjustment`: [Round-off delta]

#### TX-08: Credit Purchase of Inventory (Inter-State with ITC)
- **Trigger:** Purchase Invoice from out-of-state vendor.
- **Debits:**
  - `1140 Inventory Asset`: [Taxable Purchase Value]
  - `1153 Input Tax Credit (ITC) - IGST`: [Input IGST Amount]
- **Credits:**
  - `2110 Accounts Payable (Supplier Subledger)`: [Invoice Grand Total]

#### TX-09: Landed Cost Apportionment (Inward Freight / Handling)
- **Trigger:** Transporter freight bill allocated to a Goods Receipt Note.
- **Debits:**
  - `1140 Inventory Asset (Capitalized to Item Valuation)`: [Freight Amount]
  - `1151 Input Tax Credit (ITC) - CGST (RCM or Direct)`: [GST on Freight]
  - `1152 Input Tax Credit (ITC) - SGST`: [GST on Freight]
- **Credits:**
  - `2115 Freight / Transporter Payable`: [Total Transporter Bill]

#### TX-10: Purchase Return with Debit Note
- **Trigger:** Defective goods returned to vendor.
- **Debits:**
  - `2110 Accounts Payable (Supplier Subledger)`: [Debit Note Grand Total]
- **Credits:**
  - `1140 Inventory Asset`: [Taxable Cost of Returned Goods]
  - `1151 Input Tax Credit (ITC) - CGST (Reversal)`: [CGST Reversal]
  - `1152 Input Tax Credit (ITC) - SGST (Reversal)`: [SGST Reversal]

---

### 2.3 Treasury, Cash & Banking Transactions

#### TX-11: Customer Payment Receipt (Bank Transfer / NEFT / RTGS)
- **Trigger:** Payment received against customer outstanding ledger.
- **Debits:**
  - `1120 Bank Current Account`: [Amount Received]
- **Credits:**
  - `1130 Accounts Receivable (Customer Subledger)`: [Amount Received]

#### TX-12: Customer Early Settlement Discount
- **Trigger:** Cash discount granted for prompt settlement within terms.
- **Debits:**
  - `4210 Cash Discounts Allowed (Expense)`: [Discount Amount]
- **Credits:**
  - `1130 Accounts Receivable (Customer Subledger)`: [Discount Amount]

#### TX-13: Supplier Payment Disbursement
- **Trigger:** Bank payment voucher authorized for vendor bill.
- **Debits:**
  - `2110 Accounts Payable (Supplier Subledger)`: [Gross Settlement]
- **Credits:**
  - `1120 Bank Current Account`: [Net Paid]
  - `4310 Cash Discounts Received (Income)`: [Discount Earned if applicable]

#### TX-14: Contra Voucher: Cash Deposited into Bank
- **Trigger:** Physical cash deposited from counter to commercial bank account.
- **Debits:**
  - `1120 Bank Current Account`: [Deposited Cash]
- **Credits:**
  - `1110 Cash on Hand`: [Deposited Cash]

#### TX-15: Contra Voucher: Cash Withdrawn from Bank for Petty Cash
- **Trigger:** Cheque cashed to replenish counter float.
- **Debits:**
  - `1115 Petty Cash Float`: [Withdrawn Cash]
- **Credits:**
  - `1120 Bank Current Account`: [Withdrawn Cash]

---

### 2.4 Inventory Adjustments & Transfers

#### TX-16: Physical Stock Audit Deficit / Scrap / Shrinkage
- **Trigger:** Cycle count discovers missing or damaged stock.
- **Debits:**
  - `5120 Stock Adjustment Loss (Expense)`: [Valuation Cost of Missing Units]
- **Credits:**
  - `1140 Inventory Asset`: [Valuation Cost of Missing Units]

#### TX-17: Physical Stock Audit Surplus
- **Trigger:** Cycle count discovers untracked physical units.
- **Debits:**
  - `1140 Inventory Asset`: [Valuation Cost of Found Units]
- **Credits:**
  - `4320 Inventory Adjustment Gain (Income)`: [Valuation Cost of Found Units]

#### TX-18: Inter-Branch Stock Outward Dispatch (IBT)
- **Trigger:** Source branch dispatches stock to transit.
- **Debits:**
  - `1145 Stock in Transit (Clearing Asset)`: [Transfer Valuation Cost]
- **Credits:**
  - `1140 Inventory Asset (Source Warehouse)`: [Transfer Valuation Cost]

#### TX-19: Inter-Branch Stock Inward Receipt
- **Trigger:** Destination branch receives and inspects transit goods.
- **Debits:**
  - `1140 Inventory Asset (Destination Warehouse)`: [Transfer Valuation Cost]
- **Credits:**
  - `1145 Stock in Transit (Clearing Asset)`: [Transfer Valuation Cost]

---

### 2.5 Payroll & Employee Compensation

#### TX-20: Monthly Payroll Accrual Run
- **Trigger:** HR payrun approved for the month.
- **Debits:**
  - `5210 Salaries & Wages Expense`: [Gross Basic + HRA + Allowances]
  - `5215 Employer PF Contribution`: [Employer PF Share]
  - `5216 Employer ESIC Contribution`: [Employer ESIC Share]
- **Credits:**
  - `2131 Net Salaries Payable`: [Net Pay after all deductions]
  - `2132 Provident Fund (PF) Payable`: [Employee + Employer PF]
  - `2133 ESIC Payable`: [Employee + Employer ESIC]
  - `2134 Professional Tax (PT) Payable`: [Deducted PT]
  - `2135 TDS on Salaries (Section 192) Payable`: [Deducted TDS]
  - `1170 Staff Advances (Subledger)`: [Recovered salary advances]

#### TX-21: Monthly Payroll Bank Disbursement
- **Trigger:** Bulk bank transfer payout to employees.
- **Debits:**
  - `2131 Net Salaries Payable`: [Total Disbursed]
- **Credits:**
  - `1120 Bank Current Account`: [Total Disbursed]

---

### 2.6 Fixed Assets & Depreciation

#### TX-22: Fixed Asset Purchase & Capitalization
- **Trigger:** Machinery, computer hardware, or vehicle acquired.
- **Debits:**
  - `1210 Property, Plant & Equipment (Asset Head)`: [Base Asset Cost]
  - `1151 Input Tax Credit (ITC) - Capital Goods CGST`: [Eligible ITC]
  - `1152 Input Tax Credit (ITC) - Capital Goods SGST`: [Eligible ITC]
- **Credits:**
  - `2110 Accounts Payable / Bank Account`: [Grand Total]

#### TX-23: Monthly / Annual Asset Depreciation
- **Trigger:** Scheduled depreciation calculation (Straight Line / Written Down Value).
- **Debits:**
  - `5310 Depreciation Expense`: [Depreciation Amount]
- **Credits:**
  - `1220 Accumulated Depreciation (Contra-Asset)`: [Depreciation Amount]

---

### 2.7 Statutory Tax Settlements & Period Closing

#### TX-24: Monthly GST Liability Offset & Payment
- **Trigger:** GSTR-3B filing and tax settlement.
- **Debits:**
  - `2121 Output CGST Payable`: [Total CGST Liability Offset]
  - `2122 Output SGST Payable`: [Total SGST Liability Offset]
  - `2123 Output IGST Payable`: [Total IGST Liability Offset]
- **Credits:**
  - `1151 Input Tax Credit (ITC) - CGST`: [Utilized Input CGST]
  - `1152 Input Tax Credit (ITC) - SGST`: [Utilized Input SGST]
  - `1153 Input Tax Credit (ITC) - IGST`: [Utilized Input IGST]
  - `1120 Bank Current Account (Cash Ledger Payment)`: [Net Cash Balance Paid]

#### TX-25: Fiscal Year Closing Journal
- **Trigger:** Financial Controller closes the fiscal year.
- **Step 1:** Close all Revenue (Class 4000) accounts to `3210 Income Summary Account`.
- **Step 2:** Close all Expense (Class 5000) accounts to `3210 Income Summary Account`.
- **Step 3:** Transfer net balance of `3210 Income Summary Account` (Net Profit / Net Loss) to `3200 Retained Earnings (Equity)`.
- **Step 4:** Set temporary nominal accounts to zero opening balance for the new fiscal year.

