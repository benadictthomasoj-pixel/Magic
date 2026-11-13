# MAGIC ERP — PHASE F ACCOUNTING POSTING MATRIX
## Double-Entry General Ledger Integration Specifications

**Project:** MAGIC — Universal Business Management ERP  
**Phase:** F — Extended Business Modules  
**Rule:** Every journal voucher must balance ($\sum \text{Debits} = \sum \text{Credits}$), preserve integer paise precision, be immutable once posted, and link back to source transaction IDs.

---

## 1. Expense Management Posting Matrix

### 1.1 Direct Cash Expense (PaymentMethod = Cash)
- **Voucher Type:** `JournalVoucherType.Payment`
- **Trigger:** On expense marked as Paid (or directly approved if paid immediately).
```
Debit:  52xx / Category Expense Account (e.g. 5201 Rent / Supplies)    [Amount Paiss]
Debit:  1131 / 1132 / 1133 Input GST Credit (if GST registered)        [Tax Paiss]
Credit: 1111 / Cash on Hand (Control Account)                          [Total Paiss]
```

### 1.2 Direct Bank Expense (PaymentMethod = Bank)
- **Voucher Type:** `JournalVoucherType.Payment`
- **Trigger:** On expense paid from Bank Account.
```
Debit:  52xx / Category Expense Account                                 [Amount Paiss]
Debit:  1131 / 1132 / 1133 Input GST Credit (if GST registered)        [Tax Paiss]
Credit: 1112 / Operating Bank Account (GL Account of Bank)             [Total Paiss]
```

### 1.3 Petty Cash Expense (PaymentMethod = PettyCash)
- **Voucher Type:** `JournalVoucherType.Payment`
- **Trigger:** Recorded against petty cash float.
```
Debit:  52xx / Category Expense Account                                 [Amount Paiss]
Debit:  1131 / 1132 Input GST Credit (if applicable)                    [Tax Paiss]
Credit: 111x / Petty Cash GL Account (Asset)                           [Total Paiss]
```

### 1.4 Unpaid / Credit Expense (PaymentMethod = Payable)
- **Voucher Type:** `JournalVoucherType.Journal`
- **Trigger:** Approved expense payable to vendor.
```
Debit:  52xx / Category Expense Account                                 [Amount Paiss]
Debit:  1131 / 1132 Input GST Credit (if applicable)                    [Tax Paiss]
Credit: 2111 / Trade Creditors Control Account (with AP Subledger item) [Total Paiss]
```

### 1.5 Expense Reversal / Cancellation
- **Voucher Type:** `JournalVoucherType.Journal`
- **Trigger:** Cancellation of a previously posted expense.
- **Rule:** Strict counter-reversal (swap debits and credits of the original voucher) referencing `OriginalVoucherId`.

---

## 2. Petty Cash Management Posting Matrix

### 2.1 Float Allocation (Transfer from Bank to Petty Cash)
- **Voucher Type:** `JournalVoucherType.Contra`
```
Debit:  111x / Petty Cash GL Account (Current Asset)                   [Amount Paiss]
Credit: 1112 / Operating Bank Account (Asset)                          [Amount Paiss]
```

### 2.2 Float Replenishment
- **Voucher Type:** `JournalVoucherType.Contra`
```
Debit:  111x / Petty Cash GL Account                                   [Amount Paiss]
Credit: 1112 / Operating Bank Account                                  [Amount Paiss]
```

### 2.3 Petty Cash Shortage (Cash Discrepancy)
- **Voucher Type:** `JournalVoucherType.Journal`
```
Debit:  5290 / Petty Cash Loss / Shortage Expense (Class 5)            [Shortage Paiss]
Credit: 111x / Petty Cash GL Account                                   [Shortage Paiss]
```

---

## 3. Payroll Management Posting Matrix

### 3.1 Payroll Accrual (Monthly Finalization)
- **Voucher Type:** `JournalVoucherType.Journal`
- **Trigger:** On `PayrollPeriod` finalization.
```
Debit:  5210 / Salaries, Wages & Allowances Expense (Gross Earnings)   [Total Gross Paiss]
Debit:  5211 / Employer Statutory Contributions Expense                [Employer PF + ESI Paiss]
Credit: 2130 / Net Salaries Payable (Liability)                        [Total Net Paiss]
Credit: 2131 / Employee PF Payable (Liability)                         [Employee PF Paiss]
Credit: 2132 / Employer PF Payable (Liability)                         [Employer PF Paiss]
Credit: 2133 / ESI Contribution Payable (Liability)                    [Total ESI Paiss]
Credit: 2134 / Professional Tax (PT) Payable (Liability)               [Total PT Paiss]
Credit: 2135 / TDS (Salaries) Payable (Liability)                      [Total TDS Paiss]
```

### 3.2 Payroll Bank Disbursement
- **Voucher Type:** `JournalVoucherType.Payment`
- **Trigger:** On disbursement of net salaries to employee bank accounts.
```
Debit:  2130 / Net Salaries Payable                                    [Disbursed Paiss]
Credit: 1112 / Operating Bank Account                                  [Disbursed Paiss]
```

### 3.3 Statutory Dues Settlement (PF / ESI / PT Remittance)
- **Voucher Type:** `JournalVoucherType.Payment`
- **Trigger:** Remittance to government challans.
```
Debit:  2131 / 2132 / 2133 / 2134 Statutory Liability Accounts         [Remitted Paiss]
Credit: 1112 / Operating Bank Account                                  [Remitted Paiss]
```

---

## 4. Service Management Posting Matrix

Service billing integrates with the existing `SalesService` and `PostingService`. When a completed service request is invoiced:

- **Voucher Type:** `JournalVoucherType.Sales`
```
Debit:  1111 / Cash OR 1112 / Bank OR 1201 / Trade Debtors (AR)        [Total Invoiced Paiss]
Credit: 4102 / Service & Repair Labor Revenue                          [Labor Paiss]
Credit: 4101 / Spare Parts Sales Revenue                               [Parts Paiss]
Credit: 2121 / Output CGST Payable                                     [CGST Paiss]
Credit: 2122 / Output SGST Payable (or 2123 Output IGST Payable)       [SGST/IGST Paiss]
```
*(Parts inventory movement is handled concurrently via `inventory.stock_movements`, ensuring 100% single-source stock consistency).*

---

## 5. Summary of Chart of Accounts Additions for Phase F

To support Phase F operations without conflicting with Phase 2/B/C/D/E accounts:

| Code | Account Name | Root Class | Group | Description |
|---|---|---|---|---|
| `1115` | Petty Cash Float Account | Asset | `1110` (Cash & Bank) | Main store petty cash custodian float |
| `2130` | Net Salaries Payable | Liability | `2100` (Current Liab) | Accrued net salaries due to staff |
| `2131` | Employee PF Deduction Payable | Liability | `2100` (Current Liab) | Employee Provident Fund deductions |
| `2132` | Employer PF Contribution Payable | Liability | `2100` (Current Liab) | Employer Provident Fund contribution |
| `2133` | ESI Dues Payable | Liability | `2100` (Current Liab) | Employee & Employer ESI contributions |
| `2134` | Professional Tax Payable | Liability | `2100` (Current Liab) | State Professional Tax deductions |
| `2135` | TDS (Salaries) Payable | Liability | `2100` (Current Liab) | Withholding tax u/s 192 |
| `4102` | Service & Labor Revenue | Revenue | `4100` (Operating Rev) | Repair labor and maintenance fees |
| `5210` | Staff Salaries & Wages | Expense | `5200` (Admin Exp) | Base salaries and allowances |
| `5211` | Employer Statutory Contributions | Expense | `5200` (Admin Exp) | Employer PF and ESI expenses |
| `5290` | Petty Cash Discrepancy / Loss | Expense | `5200` (Admin Exp) | Cash shortages in petty cash count |

