# MAGIC ERP — PHASE F TEST STRATEGY & VERIFICATION PLAN
## Rigorous Automated Testing for Extended Business Modules

**Project:** MAGIC — Universal Business Management ERP  
**Phase:** F — Extended Business Modules  
**Environments:** macOS / .NET 10 / PostgreSQL 18 (`magic_test`) / Angular 22  
**Target:** 100% Pass Rate across all unit, integration, and UI regression suites.

---

## 1. Testing Pyramid & Boundaries

```
                 / \
                /   \     E2E Workflow Scenarios (10 Core Workflows)
               /-----\
              /       \    API Integration Tests (PostgreSQL magic_test)
             /---------\
            /           \   Frontend Karma / Jasmine Unit Tests
           /-------------\
          /               \  Domain Unit & Accounting Invariant Tests
         +-----------------+
```

### 1.1 Backend Testing Rules
1. **Isolated Integration Database:** All database-mutating integration tests execute strictly against `magic_test` via `MagicTestFixture`. The development database `magic_dev` is never mutated.
2. **Double-Entry Balance Verification:** Every test asserting a posted accounting voucher must assert:
   $$\sum \text{Debits} == \sum \text{Credits}$$
3. **Paise Precision Invariant:** All calculations assert integer paise without float round-off errors.
4. **Tenant Isolation Verification:** Every query must enforce `CompanyId` and `BranchId` checks. Multi-branch access by unauthorized users must return `403 Forbidden` or `404 Not Found`.

---

## 2. Core End-to-End Workflow Test Scenarios

The integration test suite will verify the 10 mandatory workflows:

1. **Staff Onboarding & Attendance Flow:**
   Register Employee $\rightarrow$ Punch In/Out $\rightarrow$ Supervisor Attendance Correction $\rightarrow$ Leave Application $\rightarrow$ Manager Approval $\rightarrow$ Verify Leave Balance Depletion.
2. **Attendance to Monthly Payroll Accrual:**
   Create Salary Structure $\rightarrow$ Assign to Employee $\rightarrow$ Calculate Payroll Period from Attendance $\rightarrow$ Preview $\rightarrow$ Approve $\rightarrow$ Finalize $\rightarrow$ Verify Accrual Voucher Debits/Credits.
3. **Payroll Disbursement Flow:**
   Finalized Payroll Period $\rightarrow$ Disburse via Bank Account $\rightarrow$ Verify Payment Voucher $\rightarrow$ Assert `Salary Payable` (2130) balance reduced to zero.
4. **Expense Lifecycle Flow:**
   Create Draft Expense $\rightarrow$ Submit $\rightarrow$ Approve $\rightarrow$ Pay via Bank/Cash $\rightarrow$ Verify General Ledger Voucher Debits Expense (52xx) and Credits Cash/Bank.
5. **Petty Cash Float & Reconciliation:**
   Allocate Float from Bank $\rightarrow$ Record Petty Cash Expense $\rightarrow$ Replenish Float $\rightarrow$ Reconcile Cash with Shortage $\rightarrow$ Verify Shortage Expense Voucher (5290).
6. **Marketing Campaign & Coupon Redemption at POS:**
   Create Campaign $\rightarrow$ Issue 10% Coupon $\rightarrow$ Validate at POS $\rightarrow$ Complete Invoice $\rightarrow$ Verify Discount Applied $\rightarrow$ Verify Usage Limit Increment $\rightarrow$ Verify Second Use Rejection if Single-Use.
7. **Customer Loyalty Points Accrual & Redemption:**
   Process Sales Invoice $\rightarrow$ Award Loyalty Points $\rightarrow$ Check Balance $\rightarrow$ Redeem Points on Subsequent Sale $\rightarrow$ Verify Balance Deduction.
8. **Sales Order to Delivery & Invoicing Flow:**
   Create Sales Order $\rightarrow$ Confirm Order $\rightarrow$ Verify Stock Commitment $\rightarrow$ Dispatch Fulfillment $\rightarrow$ Generate Sales Invoice $\rightarrow$ Verify Stock Decremented Exactly Once $\rightarrow$ Verify No Double Revenue Posting.
9. **Service Request to Parts Consumption & Billing:**
   Log Service Request $\rightarrow$ Inspect $\rightarrow$ Issue Quotation $\rightarrow$ Approve $\rightarrow$ Consume Repair Part from Inventory $\rightarrow$ Verify Stock Movement $\rightarrow$ Convert to Sales Invoice $\rightarrow$ Verify Revenue & Tax Posting.
10. **Multi-Branch Isolation & Reversals:**
    Attempt to view or pay Branch A's expenses or payroll from Branch B user session $\rightarrow$ Assert Rejection $\rightarrow$ Post Expense Reversal $\rightarrow$ Verify Counter-Reversal Journal Voucher.

