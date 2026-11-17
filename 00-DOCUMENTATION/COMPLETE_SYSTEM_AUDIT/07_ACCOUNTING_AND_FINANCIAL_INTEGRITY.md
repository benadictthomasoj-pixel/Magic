# MAGIC ERP — SYSTEM-WIDE ARCHITECTURE, CODEBASE & INTEGRATION AUDIT
## Document 07: Accounting Invariants, Financial Posting & Ledger Integrity Audit

**Document ID:** `AUDIT-07-FINANCE`  
**Audit Scope:** Double-Entry Integrity, Journal Vouchers, Subledgers, Tax Math, DEC-ACC-002 Provisional Invariant  
**Workspace:** `/Users/benadict/Documents/antigravity projects/magic`  
**Git Baseline Commit:** `2567c7d`  

---

### 1. The Core Double-Entry Invariant

In MAGIC ERP, financial integrity is rooted in the mathematical invariant that for every financial voucher, the total debit amount must equal the total credit amount to the exact cent:

$$\sum \text{DebitAmount} = \sum \text{CreditAmount} \quad \text{and} \quad \sum (\text{DebitAmount} - \text{CreditAmount}) = 0.00$$

#### Implementation Verification in `PostingService.cs`
The audit inspected `01-SRC/Backend/Magic.Infrastructure/Services/PostingService.cs`:
1. **Pre-Commit Invariant Check:**
   Before persisting any `JournalEntryRecord` to `accounting.journal_entries`, `PostingService` sums all line debits and credits:
   ```csharp
   decimal totalDebits = lines.Sum(l => l.DebitAmount);
   decimal totalCredits = lines.Sum(l => l.CreditAmount);
   if (Math.Round(totalDebits, 2) != Math.Round(totalCredits, 2))
   {
       throw new UnbalancedJournalException(
           $"Journal voucher {voucherNumber} is unbalanced. Debits: {totalDebits:F2}, Credits: {totalCredits:F2}");
   }
   ```
2. **Database Constraint:**
   PostgreSQL table `accounting.journal_entries` enforces check constraint:
   `CHECK (total_debit = total_credit)` and `CHECK (total_debit > 0)`.
3. **Database Audit Result:**
   Across both `magic_dev` and `magic_test` databases, query `SELECT COUNT(*) FROM accounting.journal_entries WHERE total_debit != total_credit;` returned **0**.

---

### 2. Operational Posting Matrix Across Business Workflows

The following table documents how operational events across all modules map into double-entry ledger entries via `IPostingService`:

| Source Event / Module | Voucher Type | Debit Account(s) | Credit Account(s) | Balancing Status |
|---|---|---|---|:---:|
| **POS Cash Checkout (Mod 02)** | `VoucherType.Sales` | Cash in Hand / Drawer (`1010`) | Sales Revenue (`4010`)<br>Output CGST (`2051`)<br>Output SGST (`2052`) | **BALANCED** |
| **POS Credit Checkout (Mod 02)** | `VoucherType.Sales` | Accounts Receivable (`1050`) | Sales Revenue (`4010`)<br>Output CGST (`2051`)<br>Output SGST (`2052`) | **BALANCED** |
| **Sales Return RMA (Mod 03)** | `VoucherType.CreditNote` | Sales Returns (`4020`)<br>Output CGST (`2051`)<br>Output SGST (`2052`) | Accounts Receivable (`1050`) | **BALANCED** |
| **Goods Receipt GRN (Mod 04)** | `VoucherType.Purchase` | Inventory Asset (`1080`)<br>Input CGST (`1091`)<br>Input SGST (`1092`) | GRN Clearing / AP Accrual (`2020`) | **BALANCED** |
| **Supplier Bill (Mod 07)** | `VoucherType.Journal` | GRN Clearing / AP Accrual (`2020`) | Accounts Payable (`2010`) | **BALANCED** |
| **Expense Reimbursement (Mod 11)**| `VoucherType.Payment` | Operating Expense Account (`5xxx`) | Bank Account (`1020`) or Cash (`1010`)| **BALANCED** |
| **Payroll Accrual (Mod 13)** | `VoucherType.Journal` | Salaries & Wages Expense (`5010`) | Salaries Payable (`2030`)<br>PF Payable (`2031`)<br>TDS Payable (`2032`) | **BALANCED** |
| **Salary Disbursement (Mod 13)**| `VoucherType.Payment` | Salaries Payable (`2030`) | Bank Account (`1020`) | **BALANCED** |
| **Fixed Asset Depreciation (Mod 07)**| `VoucherType.Journal` | Depreciation Expense (`5080`) | Accumulated Depreciation (`1590`) | **BALANCED** |
| **Inter-Branch Transfer (Mod 18)**| `VoucherType.Transfer`| In-Transit Stock (`1085`) | Source Warehouse Inventory (`1080`)| **BALANCED** |

---

### 3. Compliance with Architectural Decision DEC-ACC-002 (Provisional Vouchers)

#### Policy Definition
Under DEC-ACC-002, operational vouchers generated automatically by front-line transactional modules (POS, Orders, GRN, Expenses) are initially recorded with `JournalEntryStatus.Provisional`. They become `JournalEntryStatus.Posted` upon formal period closing or senior accountant approval, while maintaining real-time subledger visibility.

#### Code Audit Findings:
1. **Posting Status Enforcement:**
   `PostingService.PostOperationalVoucherAsync` assigns `status = JournalEntryStatus.Provisional` by default unless explicitly flagged as direct financial posting by an authorized accountant role.
2. **Immutability of Finalized Vouchers:**
   Once a voucher reaches `JournalEntryStatus.Posted`, EF Core entity validation and application logic reject any `UPDATE` or `DELETE` operations.
3. **Reversal Invariant:**
   To correct an error on a posted voucher, the system requires issuing an offsetting reversing voucher (`IPostingService.ReverseVoucherAsync`), preserving an immutable historical audit trail.

---

### 4. Statutory Tax Calculation (GST Math Engine)

Tax calculations were audited across `Magic.Domain/Tax/GstMath.cs`:
1. **Intra-State vs Inter-State Rule:**
   - When `SupplierStateCode == CustomerStateCode`: Total Tax is split exactly 50/50 between `CGST` and `SGST`.
   - When `SupplierStateCode != CustomerStateCode`: Total Tax is applied as `IGST`.
2. **Rounding Precision:**
   - Intermediate calculations use 4 decimal places (`numeric(18,4)`).
   - Final tax amounts per line item are rounded to 2 decimal places using `MidpointRounding.AwayFromZero`, perfectly matching Indian GST statutory regulations.

