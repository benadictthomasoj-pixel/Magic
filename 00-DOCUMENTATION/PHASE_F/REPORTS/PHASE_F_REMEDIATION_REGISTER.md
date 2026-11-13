# MAGIC ERP — PHASE F REMEDIATION REGISTER

**Audit & Remediation Scope:** Phase F Modules & Integration Test Suite  
**Date:** 2026-11-13  
**Status:** **ALL DEFECTS REMEDIATED AND CLOSED**

---

## 1. Remediation Items Register

### DEF-01: OutboxProcessorTests Suite Concurrency Contention
- **Severity:** High
- **Description:** `dotnet test Magic.sln` exited with code 1 because `OutboxProcessorTests.ProcessPendingBatchDispatchesToConsumerAndMarksProcessed` expected its test message to be picked up in a single batch of `LIMIT 25`. When run as part of the full solution suite, unhandled outbox messages from previous tests filled the first batch.
- **Root Cause:** Single batch invocation assumption without batch looping.
- **Remediation:** Replaced single `ProcessPendingBatchAsync` invocation with a bounded loop (up to 10 batches) until the target event appears in `_consumer.HandledEventIds`.
- **Status:** **CLOSED / VERIFIED PASS**

### DEF-02: Missing Expense Cancellation Endpoint & Reversal Voucher Integration
- **Severity:** High
- **Description:** Scenario 1 required cancelling an approved/paid expense claim and issuing a non-destructive accounting reversal voucher in the General Ledger. The cancellation endpoint and request contract were missing, and `ExpenseService.CancelExpenseAsync` had an improper call sequence.
- **Root Cause:** Missing `POST /api/v1/expenses/{id}/cancel` endpoint and direct call to `PostVoucherAsync` on an already posted reversal voucher instead of `ReverseVoucherAsync`.
- **Remediation:**
  1. Added `CancelExpenseRequest(string Reason)` contract to `ExpenseContracts.cs`.
  2. Implemented `CancelExpense` controller action in `ExpensesController.cs`.
  3. Refactored `ExpenseService.CancelExpenseAsync` to call `postingService.ReverseVoucherAsync(...)` which handles sequential reversal voucher number generation, outbox message creation, and system audit logging.
- **Status:** **CLOSED / VERIFIED PASS**

### DEF-03: Missing Inventory Overselling Prevention in Order Fulfillment
- **Severity:** High
- **Description:** `OrderService.FulfillOrderAsync` deducted stock without checking if `stockBalance.QuantityOnHand` was sufficient, allowing negative inventory on oversold backorders.
- **Root Cause:** Missing guard check against physical warehouse balance before dispatching.
- **Remediation:** Added validation in `OrderService.FulfillOrderAsync`:
  ```csharp
  if (prevQty < lineReq.QuantityDispatched)
  {
      throw new DomainException("INSUFFICIENT_STOCK", $"Cannot dispatch {lineReq.QuantityDispatched} units of product {line.ProductId}. Only {prevQty} units available in warehouse.", StatusCodes.Status400BadRequest);
  }
  ```
- **Status:** **CLOSED / VERIFIED PASS**

### DEF-04: Foreign Key Violation on Service Labor Line Billing
- **Severity:** Medium
- **Description:** Invoicing service requests created labor invoice lines with `Guid.Empty` for `ProductId`, causing a foreign key violation against `catalog.products`.
- **Root Cause:** Labor was not mapped to an active catalog product ID for the company.
- **Remediation:** Updated `ServiceDeskService.InvoiceServiceRequestAsync` to resolve an active company general/service product ID as a fallback when creating labor invoice lines.
- **Status:** **CLOSED / VERIFIED PASS**

### DEF-05: Sales Order Status Naming Mismatch
- **Severity:** Low
- **Description:** Scenario 8 & 9 tests asserted `"PartiallyShipped"` and `"Shipped"`, while domain enum and frontend use `"PartiallyFulfilled"` and `"Fulfilled"`.
- **Root Cause:** Status naming convention discrepancy in test assertions.
- **Remediation:** Aligned `PhaseFExtendedScenariosTests.cs` to assert canonical domain statuses `"PartiallyFulfilled"` and `"Fulfilled"`.
- **Status:** **CLOSED / VERIFIED PASS**

---

## 2. Verification Sign-Off

All 5 defects have been corrected and verified across both isolated tests and full solution regression.

