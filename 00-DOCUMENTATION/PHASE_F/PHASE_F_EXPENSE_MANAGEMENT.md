# MAGIC ERP — MODULE 11: EXPENSE MANAGEMENT SPECIFICATION
## Direct Expenses, Petty Cash & Accounting Integration

**Route:** `/billing/expenses`  
**API Controller:** `ExpensesController` (`/api/v1/expenses`)  
**Database Schema:** `expenses`

---

## 1. Functional Scope

1. **Expense Recording:**
   - Multi-channel expenses: Cash, Bank, Petty Cash, or Accounts Payable (Credit).
   - Category mapping to Chart of Accounts expense groups (Class 5 - Expenses).
   - Tax capture (Input CGST/SGST/IGST credit).
   - Cost center tracking and vendor association (linking to `commercial.suppliers`).
2. **Approval Workflow:**
   - Multi-state lifecycle: `Draft` $\rightarrow$ `Submitted` $\rightarrow$ `Approved` $\rightarrow$ `Paid` (or `Rejected` / `Cancelled`).
   - Approval amounts limit validation.
3. **Petty Cash Floats:**
   - Float accounts per branch with custodian assignment.
   - Float replenishment from bank account via Contra voucher.
   - Daily cash reconciliation with automatic shortage/excess ledger entries.
4. **Accounting Integration:**
   - Generates double-entry vouchers via existing `PostingService`.
   - Immutable once paid; cancellations create counter-reversal vouchers.

