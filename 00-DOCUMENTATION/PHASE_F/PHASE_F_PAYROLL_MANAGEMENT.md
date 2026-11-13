# MAGIC ERP — MODULE 13: PAYROLL MANAGEMENT SPECIFICATION
## Salary Structures, Monthly Calculations, Payslips & Statutory Compliance

**Route:** `/billing/payroll`  
**API Controller:** `PayrollController` (`/api/v1/payroll`)  
**Database Schema:** `payroll`

---

## 1. Functional Scope

1. **Salary Structures & Rules:**
   - Modular salary components: Basic, HRA, Allowances, Performance Bonus, Overtime.
   - Statutory deduction components: Employee PF (12% of basic), ESI (0.75%), Professional Tax (state slab), TDS (u/s 192).
   - Employer contribution components: Employer PF (12%), Employer ESI (3.25%).
   - All rules and rates are dynamically configurable per structure—never hardcoded.
2. **Monthly Payroll Runs:**
   - Pulls real calendar days, attendance records, and approved unpaid leaves.
   - Pro-rates earnings according to days worked ($WorkingDays / TotalDaysInMonth$).
   - Computes gross earnings, individual deductions, and net payable in integer paise.
   - Strict preview state allowing authorized adjustments before lock.
3. **Approval & Accounting Finalization:**
   - Multi-tier authorization: `Draft` $\rightarrow$ `Calculated` $\rightarrow$ `Approved` $\rightarrow$ `Finalized` $\rightarrow$ `Paid`.
   - On finalization: Generates Journal Voucher debiting Salary Expense & Employer Contribution and crediting Salary Payable & Statutory Liabilities.
4. **Disbursement & Payslips:**
   - Direct bank disbursement recording voucher debiting Salary Payable and crediting Bank Account.
   - Generates digital payslips for employee self-service.

