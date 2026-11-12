# MAGIC — Phase 2: Accounting Foundation
## Master Accounting Posting Matrix & Integration Protocols

**Document Version:** 2.0.0-DRAFT  
**Phase:** 2 — Accounting Foundation  
**Stage:** 1 — Architecture and Database Design  
**Implementation Status:** NOT AUTHORIZED (Proposed Specification Only)  
**Author:** Antigravity AI & Architecture Team  
**Last Updated:** 2026-11-12  

---

## 1. Architectural Posting Standards

Every business event that impacts the enterprise's financial standing generates an atomic, balanced **Journal Voucher** in `accounting.vouchers` and `accounting.journal_lines`.

### Fundamental Equation
$$\sum \text{DebitPaise} \equiv \sum \text{CreditPaise} \quad (\text{DebitPaise}, \text{CreditPaise} \in \mathbb{Z}_{\ge 0})$$

All monetary calculations use signed 64-bit integer paise (₹1.00 = 100 paise).  
Line items and tax splits are rounded using deterministic basis-point math with Section 170 CGST Act invoice round-off adjustments posted to `4250: Rounding Off Adjustment`.

---

## 2. Transaction Posting Matrix

### 2.1 Sales & Revenue Transactions

#### TX-01: Retail Counter Cash Sale (GST Inclusive)
- **Source Module:** `POS / Sales`
- **Triggering Business Event:** Cashier tenders cash and confirms checkout (`POS_CHECKOUT_COMPLETED`).
- **Required Validation:**
  - Cash tender amount $\ge$ rounded invoice total.
  - Active fiscal period is open.
  - Stock items have valid tax rates in basis points.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `1111: Cash on Hand (Counter Float)` [Total Rounded Invoice Amount]
  - **Credit:** `4110: Sales Revenue - Retail` [Taxable Value in paise]
  - **Credit:** `2121: Output CGST Payable` [CGST in paise]
  - **Credit:** `2122: Output SGST Payable` [SGST in paise]
  - **Debit / Credit:** `4250: Rounding Off Adjustment` [Delta paise per Section 170]
- **Posting Conditions:** Executes synchronously inside the POS checkout ACID transaction.
- **Reversal Behaviour:** Creates companion `REVERSAL` voucher swapping all debit and credit legs.
- **Idempotency Requirements:** Header `Idempotency-Key` prevents double-submission if network stalls.
- **Audit Requirements:** Logged with Cashier `user_id`, `terminal_id`, `shift_id`, `client_ip`, and `trace_correlation_id`.

#### TX-02: Retail Counter Digital Sale (UPI / QR / Card)
- **Source Module:** `POS / Sales`
- **Triggering Business Event:** Digital payment gateway or EDC terminal approves card/UPI tender (`POS_DIGITAL_TENDER_ACCEPTED`).
- **Required Validation:**
  - Gateway transaction reference / approval code present.
  - Active fiscal period is open.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `1121: Bank Clearing / POS Merchant Settlement` [Total Rounded Invoice Amount]
  - **Credit:** `4110: Sales Revenue - Retail` [Taxable Value]
  - **Credit:** `2121: Output CGST Payable` [CGST]
  - **Credit:** `2122: Output SGST Payable` [SGST]
  - **Debit / Credit:** `4250: Rounding Off Adjustment` [Round-off delta]
- **Posting Conditions:** Atomic with POS invoice creation.
- **Reversal Behaviour:** Generates reversal voucher; card void triggered if transaction in same batch.
- **Idempotency Requirements:** Gateway transaction reference combined with `Idempotency-Key`.
- **Audit Requirements:** Gateway Auth Code, terminal ID, and correlation ID logged.

#### TX-03: B2B Wholesale Credit Sale (Intra-State: CGST + SGST)
- **Source Module:** `Sales`
- **Triggering Business Event:** Authorized Wholesale Tax Invoice posted (`SALES_INVOICE_POSTED`).
- **Required Validation:**
  - Customer credit limit not exceeded (or authorized via supervisor elevation).
  - Customer GSTIN validated format (if B2B registered).
  - Place of supply matches Company State Code.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `1130: Accounts Receivable (Trade Debtors)` [Grand Invoice Total]
    - *Subledger Item created in `ar_subledger_items` linked to customer*.
  - **Credit:** `4120: Sales Revenue - Wholesale` [Taxable Value]
  - **Credit:** `2121: Output CGST Payable` [CGST]
  - **Credit:** `2122: Output SGST Payable` [SGST]
  - **Debit / Credit:** `4250: Rounding Off Adjustment` [Round-off delta]
- **Posting Conditions:** Executes in primary invoice persistence transaction.
- **Reversal Behaviour:** If cancelled before dispatch, post Reversal Voucher; if goods dispatched, Credit Note (TX-05) required.
- **Idempotency Requirements:** Unique `Idempotency-Key` scoped to invoice.
- **Audit Requirements:** Creator user, approver supervisor (if credit overridden), and correlation ID.

#### TX-04: B2B Wholesale Credit Sale (Inter-State: IGST)
- **Source Module:** `Sales`
- **Triggering Business Event:** Out-of-state Wholesale Tax Invoice posted (`SALES_INVOICE_POSTED`).
- **Required Validation:** Place of supply $\ne$ Company State Code; valid 2-digit state code.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `1130: Accounts Receivable (Trade Debtors)` [Grand Invoice Total]
  - **Credit:** `4120: Sales Revenue - Wholesale` [Taxable Value]
  - **Credit:** `2123: Output IGST Payable` [Total IGST]
  - **Debit / Credit:** `4250: Rounding Off Adjustment` [Round-off delta]
- **Posting Conditions:** Same as TX-03.
- **Reversal Behaviour:** Same as TX-03.
- **Idempotency Requirements:** Same as TX-03.
- **Audit Requirements:** Same as TX-03.

#### TX-05: Sales Return with Credit Note (Against Invoice)
- **Source Module:** `Sales`
- **Triggering Business Event:** Credit Note approved for returned merchandise (`SALES_RETURN_APPROVED`).
- **Required Validation:** Original sales invoice reference exists and return quantity $\le$ original invoiced quantity.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `4130: Sales Returns (Contra-Revenue)` [Taxable Value of Returned Goods]
  - **Debit:** `2121: Output CGST Payable` (or `2123: Output IGST Payable`) [Reversal Tax Amount]
  - **Debit:** `2122: Output SGST Payable` [Reversal Tax Amount]
  - **Credit:** `1130: Accounts Receivable (Trade Debtors)` [Credit Note Total]
    - *Subledger update: Reduces customer outstanding balance or creates unapplied credit*.
  - *(Companion Inventory Return Journal)*:
    - **Debit:** `1140: Inventory Asset` [Restored Stock Valuation Cost]
    - **Credit:** `5100: Cost of Goods Sold (COGS)` [Restored Stock Valuation Cost]
- **Posting Conditions:** Supervisor PIN authorization mandatory for returns $> \text{threshold}$.
- **Reversal Behaviour:** Reversal voucher restores original invoice balance.
- **Idempotency Requirements:** Scoped to Credit Note UUID.
- **Audit Requirements:** Original invoice ID, supervisor elevation ID, customer ID.

---

### 2.2 Customer Receipts & Collections

#### TX-06: Customer Debt Receipt (Full or Partial Settlement)
- **Source Module:** `Finance / Accounting`
- **Triggering Business Event:** Payment received from customer via Bank or Cash (`CUSTOMER_PAYMENT_RECEIVED`).
- **Required Validation:** Allocation amount $\le$ open invoice outstanding amount.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `1120: Bank Accounts` (or `1111: Cash on Hand`) [Collected Amount]
  - **Debit:** `4210: Cash Discounts Allowed` [Discount Amount, if applicable]
  - **Credit:** `1130: Accounts Receivable (Trade Debtors)` [Gross Settled Amount]
    - *Subledger update: Itemized settlement in `ar_allocations` reducing `outstanding_amount_paise`*.
- **Posting Conditions:** Atomic update of GL voucher and AR subledger allocations.
- **Reversal Behaviour:** Reversal voucher unallocates the receipts in `ar_subledger_items`, restoring invoice debt.
- **Idempotency Requirements:** Unique `Idempotency-Key` prevents double-posting of collection.
- **Audit Requirements:** Bank reference / cheque number, cashier user ID, customer ID.

#### TX-07: Customer Advance Receipt (Unadjusted Deposit)
- **Source Module:** `Finance / Accounting`
- **Triggering Business Event:** Advance deposit collected prior to invoice generation (`CUSTOMER_ADVANCE_RECEIVED`).
- **Required Validation:** Customer account exists; payment instrument valid.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `1120: Bank Accounts` (or `1111: Cash on Hand`) [Advance Amount]
  - **Credit:** `2145: Customer Advance Liabilities` [Advance Amount]
    - *Subledger update: Recorded as open credit advance*.
- **Posting Conditions:** Recorded under liabilities until formal invoice is posted.
- **Reversal Behaviour:** Reversal voucher cancels advance receipt.
- **Idempotency Requirements:** Scoped to advance transaction UUID.
- **Audit Requirements:** Customer ID, payment mode, receipt voucher number.

#### TX-08: Application of Customer Advance to Final Invoice
- **Source Module:** `Sales / Accounting`
- **Triggering Business Event:** Sales Invoice posted against existing customer advance (`CUSTOMER_ADVANCE_APPLIED`).
- **Required Validation:** Customer advance balance $\ge$ applied amount.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `2145: Customer Advance Liabilities` [Applied Amount]
  - **Credit:** `1130: Accounts Receivable (Trade Debtors)` [Applied Amount]
    - *Subledger update: Marks advance allocated and settles invoice line*.
- **Posting Conditions:** Executes concurrently with the sales invoice posting.
- **Reversal Behaviour:** Reversal voucher restores advance liability balance and re-opens invoice balance.
- **Idempotency Requirements:** Bound to invoice generation idempotency token.
- **Audit Requirements:** Advance receipt reference and sales invoice reference.

---

### 2.3 Purchase & Procurement Transactions

#### TX-09: Credit Purchase of Inventory (Intra-State with ITC)
- **Source Module:** `Purchases`
- **Triggering Business Event:** Purchase Bill verified and posted (`PURCHASE_BILL_CONFIRMED`).
- **Required Validation:**
  - Supplier GSTIN and invoice number unique for that supplier in the fiscal year.
  - HSN / tax rates validated.
  - Three-way match verified (Purchase Order $\leftrightarrow$ GRN $\leftrightarrow$ Supplier Bill).
- **Expected Debit and Credit Accounts:**
  - **Debit:** `1140: Inventory Asset` (or `2119: GRN Clearing Account`) [Taxable Purchase Value]
  - **Debit:** `1151: Input Tax Credit (ITC) - CGST` [Input CGST Amount]
  - **Debit:** `1152: Input Tax Credit (ITC) - SGST` [Input SGST Amount]
  - **Credit:** `2110: Accounts Payable (Trade Creditors)` [Grand Bill Total]
    - *Subledger update: Creates open AP bill item in `ap_subledger_items`*.
  - **Debit / Credit:** `4250: Rounding Off Adjustment` [Round-off delta]
- **Posting Conditions:** Atomic commit with purchase bill approval and stock ledger valuation.
- **Reversal Behaviour:** Reversal voucher cancels supplier liability and reverses ITC.
- **Idempotency Requirements:** Scoped to Supplier UUID + Supplier Bill Number.
- **Audit Requirements:** Supplier invoice number, verification user ID, GRN reference.

#### TX-10: Credit Purchase of Inventory (Inter-State with ITC)
- **Source Module:** `Purchases`
- **Triggering Business Event:** Out-of-state Purchase Bill verified (`PURCHASE_BILL_CONFIRMED`).
- **Required Validation:** Supplier state code $\ne$ Company state code.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `1140: Inventory Asset` [Taxable Purchase Value]
  - **Debit:** `1153: Input Tax Credit (ITC) - IGST` [Input IGST Amount]
  - **Credit:** `2110: Accounts Payable (Trade Creditors)` [Grand Bill Total]
  - **Debit / Credit:** `4250: Rounding Off Adjustment` [Round-off delta]
- **Posting Conditions:** Same as TX-09.
- **Reversal Behaviour:** Same as TX-09.
- **Idempotency Requirements:** Same as TX-09.
- **Audit Requirements:** Same as TX-09.

#### TX-11: Purchase Return with Debit Note
- **Source Module:** `Purchases`
- **Triggering Business Event:** Goods returned to supplier; Debit Note approved (`PURCHASE_RETURN_APPROVED`).
- **Required Validation:** Original purchase bill reference exists; return quantity verified.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `2110: Accounts Payable (Trade Creditors)` [Grand Debit Note Total]
    - *Subledger update: Reduces supplier liability*.
  - **Credit:** `1140: Inventory Asset` [Restored Item Valuation Cost]
  - **Credit:** `1151: Input Tax Credit (ITC) - CGST (Reversal)` [Reversed CGST]
  - **Credit:** `1152: Input Tax Credit (ITC) - SGST (Reversal)` [Reversed SGST]
- **Posting Conditions:** Physical stock decrement in Inventory module must commit atomically.
- **Reversal Behaviour:** Reversal voucher restores original bill liability and inventory asset.
- **Idempotency Requirements:** Scoped to Debit Note UUID.
- **Audit Requirements:** Supplier bill number, reason code, dispatch note reference.

---

### 2.4 Supplier Payments & Disbursements

#### TX-12: Supplier Bill Payment (Disbursement)
- **Source Module:** `Finance / Accounting`
- **Triggering Business Event:** Payment released to vendor via Bank or Cash (`SUPPLIER_PAYMENT_DISBURSED`).
- **Required Validation:** Payment amount $\le$ open bill liability.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `2110: Accounts Payable (Trade Creditors)` [Gross Settled Amount]
    - *Subledger update: Itemized settlement in `ap_allocations`*.
  - **Credit:** `1120: Bank Accounts` (or `1111: Cash on Hand`) [Disbursed Amount]
  - **Credit:** `4310: Cash Discounts Received` [Discount Amount, if applicable]
- **Posting Conditions:** Atomic update of GL voucher and AP subledger.
- **Reversal Behaviour:** Reversal voucher unallocates supplier bill and restores outstanding liability.
- **Idempotency Requirements:** Scoped to Payment Voucher UUID and bank reference.
- **Audit Requirements:** Bank instrument reference (UTR / cheque number), authorizer user ID.

#### TX-13: Supplier Advance Payment
- **Source Module:** `Finance / Accounting`
- **Triggering Business Event:** Advance disbursed to supplier prior to bill receipt (`SUPPLIER_ADVANCE_PAID`).
- **Required Validation:** Supplier account active; bank funds available.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `1165: Supplier Advances (Asset)` [Advance Amount]
    - *Subledger update: Recorded as open debit advance*.
  - **Credit:** `1120: Bank Accounts` (or `1111: Cash on Hand`) [Disbursed Amount]
- **Posting Conditions:** Recorded as an asset until purchase bill is verified.
- **Reversal Behaviour:** Reversal voucher cancels advance disbursement.
- **Idempotency Requirements:** Scoped to payment idempotency token.
- **Audit Requirements:** Supplier ID, approval token, UTR reference.

#### TX-14: Application of Supplier Advance to Purchase Bill
- **Source Module:** `Purchases / Accounting`
- **Triggering Business Event:** Purchase Bill matched against existing advance deposit (`SUPPLIER_ADVANCE_APPLIED`).
- **Required Validation:** Unapplied advance balance $\ge$ applied amount.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `2110: Accounts Payable (Trade Creditors)` [Applied Amount]
  - **Credit:** `1165: Supplier Advances (Asset)` [Applied Amount]
    - *Subledger update: Settles open bill and clears advance item*.
- **Posting Conditions:** Committed atomically with purchase bill posting.
- **Reversal Behaviour:** Restores supplier advance asset and re-opens bill balance.
- **Idempotency Requirements:** Scoped to bill settlement token.
- **Audit Requirements:** Purchase bill UUID and advance voucher UUID.

---

### 2.5 Inventory & Warehouse Adjustments

#### TX-15: Cost of Goods Sold (COGS) Recognition on Sale
- **Source Module:** `Inventory / Sales`
- **Triggering Business Event:** Physical inventory issued upon sales delivery (`STOCK_SALES_ISSUE_CONFIRMED`).
- **Required Validation:** Inventory valuation cost calculated via approved valuation model (FIFO or Moving Average).
- **Expected Debit and Credit Accounts:**
  - **Debit:** `5100: Cost of Goods Sold (COGS)` [Total Valuation Cost in paise]
  - **Credit:** `1140: Inventory Asset (Finished Goods)` [Total Valuation Cost in paise]
- **Posting Conditions:** Executes concurrently with the sales invoice inventory dispatch.
- **Reversal Behaviour:** Sales return restores inventory asset and credits COGS (see TX-05).
- **Idempotency Requirements:** Scoped to delivery challan / sales invoice UUID.
- **Audit Requirements:** Warehouse ID, item batch ID, valuation formula version.

#### TX-16: Physical Inventory Count Variance — Stock Loss / Scrap
- **Source Module:** `Inventory`
- **Triggering Business Event:** Physical stock audit identifies deficit vs system balance (`STOCK_DEFICIT_CONFIRMED`).
- **Required Validation:** Supervisor elevation required for variances exceeding branch monetary threshold.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `5120: Stock Adjustment Loss / Scrap (Expense)` [Lost Stock Cost in paise]
  - **Credit:** `1140: Inventory Asset` [Lost Stock Cost in paise]
- **Posting Conditions:** Atomic with physical inventory balance adjustment.
- **Reversal Behaviour:** Reversal voucher cancels adjustment; re-opens stock count reconciliation.
- **Idempotency Requirements:** Scoped to Stock Audit Reconciliation UUID.
- **Audit Requirements:** Supervisor elevation ID, reason code (damaged, expired, theft), warehouse ID.

#### TX-17: Physical Inventory Count Variance — Stock Gain
- **Source Module:** `Inventory`
- **Triggering Business Event:** Physical stock audit identifies surplus vs system balance (`STOCK_SURPLUS_CONFIRMED`).
- **Required Validation:** Supervisor elevation required; valuation rate set to current moving average or last purchase cost.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `1140: Inventory Asset` [Surplus Valuation Cost in paise]
  - **Credit:** `4320: Inventory Adjustment Gain (Other Income)` [Surplus Valuation Cost in paise]
- **Posting Conditions:** Same as TX-16.
- **Reversal Behaviour:** Same as TX-16.
- **Idempotency Requirements:** Same as TX-16.
- **Audit Requirements:** Same as TX-16.

---

### 2.6 Operating Expenses & Fixed Assets

#### TX-18: Direct Operating Expense Payment (Cash / Bank)
- **Source Module:** `Expenses / Accounting`
- **Triggering Business Event:** Expense voucher paid directly from cash or bank (`EXPENSE_VOUCHER_PAID`).
- **Required Validation:** Expense category mapped to valid 5200-series account; payee specified.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `5200: Operating Expense Account` (e.g. `5220: Electricity & Utilities`) [Expense Amount]
  - **Debit:** `1150: GST Input Tax Credit` [Eligible ITC, if registered GST invoice]
  - **Credit:** `1120: Bank Accounts` (or `1115: Petty Cash Float`) [Paid Amount]
- **Posting Conditions:** Immediate journal creation upon payment release.
- **Reversal Behaviour:** Reversal voucher cancels expense posting.
- **Idempotency Requirements:** Scoped to Expense Claim UUID.
- **Audit Requirements:** Vendor bill attachment, approving officer user ID.

#### TX-19: Fixed Asset Purchase (Capitalization)
- **Source Module:** `Fixed Assets / Purchases`
- **Triggering Business Event:** Capital asset purchased and capitalized (`FIXED_ASSET_CAPITALIZED`).
- **Required Validation:** Asset master record created with depreciation schedule and asset tag.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `1210: Property, Plant & Equipment (PPE)` [Asset Cost]
  - **Debit:** `1150: GST Input Tax Credit` [Capital Goods ITC, if eligible]
  - **Credit:** `2110: Accounts Payable` (or `1120: Bank Accounts`) [Total Payable Amount]
- **Posting Conditions:** Atomic commit with asset register inward.
- **Reversal Behaviour:** Reversal voucher de-capitalizes asset.
- **Idempotency Requirements:** Scoped to Asset Capitalization Order UUID.
- **Audit Requirements:** Asset serial number, location tag, purchase invoice ID.

#### TX-20: Monthly Fixed Asset Depreciation Amortization
- **Source Module:** `Fixed Assets / Accounting`
- **Triggering Business Event:** End-of-month depreciation execution job (`DEPRECIATION_RUN_COMPLETED`).
- **Required Validation:** Asset active; remaining net book value $> 0$.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `5310: Depreciation Expense` [Monthly Amortization Amount in paise]
  - **Credit:** `1220: Accumulated Depreciation (Contra-Asset)` [Monthly Amortization Amount in paise]
- **Posting Conditions:** Automated scheduled posting per branch/company.
- **Reversal Behaviour:** Reversal voucher undoes monthly depreciation run.
- **Idempotency Requirements:** Scoped to `(AssetId, FiscalPeriodId)`.
- **Audit Requirements:** Depreciation engine version, calculation basis (Straight Line vs WDV).

---

### 2.7 Payroll & Employee Remuneration

#### TX-21: Monthly Payroll Accrual
- **Source Module:** `Payroll`
- **Triggering Business Event:** Monthly payrun finalized and approved (`PAYROLL_RUN_APPROVED`).
- **Required Validation:** Attendance finalized; statutory deduction brackets verified.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `5210: Salaries & Wages Expense` [Gross Pay in paise]
  - **Debit:** `5215: Employer PF Contribution Expense` [Employer PF Amount]
  - **Debit:** `5216: Employer ESIC Contribution Expense` [Employer ESIC Amount]
  - **Credit:** `2131: Net Salaries Payable` [Net Payable to Employees]
  - **Credit:** `2132: Provident Fund (PF) Payable` [Employee + Employer PF]
  - **Credit:** `2133: ESIC Payable` [Employee + Employer ESIC]
  - **Credit:** `2134: Professional Tax (PT) Payable` [Deducted PT]
  - **Credit:** `2135: TDS on Salaries (Sec 192) Payable` [Deducted Withholding Tax]
  - **Credit:** `1170: Staff Advances (Subledger)` [Recovered Staff Advances, if any]
- **Posting Conditions:** Executes atomically upon HR Director / Finance approval of payrun.
- **Reversal Behaviour:** Payrun cancellation generates reversal voucher.
- **Idempotency Requirements:** Scoped to Payrun Batch UUID.
- **Audit Requirements:** Payrun batch ID, payroll officer user ID, approval token.

#### TX-22: Salary Disbursement (Bank Transfer)
- **Source Module:** `Finance / Payroll`
- **Triggering Business Event:** Bank transfer / NACH batch executed for salary payments (`SALARY_DISBURSED`).
- **Required Validation:** Bank clearing reference confirmed.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `2131: Net Salaries Payable` [Disbursed Amount]
  - **Credit:** `1120: Bank Accounts` [Disbursed Amount]
- **Posting Conditions:** Bank statement debit confirmation.
- **Reversal Behaviour:** Reversal voucher restores net salaries payable.
- **Idempotency Requirements:** Bank Batch Transfer Reference UUID.
- **Audit Requirements:** Bank UTR, payroll batch ID.

---

### 2.8 Statutory GST Settlement & Remittance

#### TX-23: Monthly GST Output vs Input Offset (Electronic Credit Ledger Utilization)
- **Source Module:** `Tax / Accounting`
- **Triggering Business Event:** End-of-month GSTR-3B tax offset execution (`GST_MONTHLY_OFFSET_EXECUTED`).
- **Required Validation:**
  - Statutory offset rules followed (IGST credit offset first against IGST, then CGST/SGST; CGST credit cannot offset SGST; SGST credit cannot offset CGST).
  - *Note: Statutory review flag pending final rule confirmation*.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `2121: Output CGST Payable` [Utilized Amount]
  - **Debit:** `2122: Output SGST Payable` [Utilized Amount]
  - **Debit:** `2123: Output IGST Payable` [Utilized Amount]
  - **Credit:** `1151: Input Tax Credit (ITC) - CGST` [Utilized Amount]
  - **Credit:** `1152: Input Tax Credit (ITC) - SGST` [Utilized Amount]
  - **Credit:** `1153: Input Tax Credit (ITC) - IGST` [Utilized Amount]
- **Posting Conditions:** Monthly manual or automated tax settlement preparation.
- **Reversal Behaviour:** Reversal voucher restores pre-settlement input and output tax balances.
- **Idempotency Requirements:** Scoped to `(CompanyId, FiscalPeriodId, "GST_OFFSET")`.
- **Audit Requirements:** Tax manager approval ID, GSTR-3B draft reference.

#### TX-24: GST Challan Cash Remittance (Net Liability Payment)
- **Source Module:** `Tax / Finance`
- **Triggering Business Event:** Payment of net GST cash liability via PMT-06 challan (`GST_CHALLAN_PAID`).
- **Required Validation:** Bank payment receipt and CPIN / CIN challan reference.
- **Expected Debit and Credit Accounts:**
  - **Debit:** `2121: Output CGST Payable` (Remaining cash balance)
  - **Debit:** `2122: Output SGST Payable` (Remaining cash balance)
  - **Debit:** `2123: Output IGST Payable` (Remaining cash balance)
  - **Credit:** `1120: Bank Accounts` [Total Remitted Challan Amount]
- **Posting Conditions:** Executed upon GST portal bank clearance.
- **Reversal Behaviour:** Reversal voucher cancels payment record.
- **Idempotency Requirements:** Scoped to GST Portal CIN (Challan Identification Number).
- **Audit Requirements:** CIN, Bank UTR, tax period code.

