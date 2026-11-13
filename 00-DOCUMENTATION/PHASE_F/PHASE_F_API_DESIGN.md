# MAGIC ERP — PHASE F API DESIGN SPECIFICATION
## RESTful Endpoints, Contracts, Payloads & Error Handling

**Project:** MAGIC — Universal Business Management ERP  
**Phase:** F — Extended Business Modules  
**Protocol:** REST over HTTPS, JSON payloads, Tenant isolation via Claims & Headers.

---

## 1. Module 11 — Expense Management API (`/api/v1/expenses`)

### Categories & Accounts
- `GET /api/v1/expenses/categories` — List active expense categories with mapped GL accounts.
- `POST /api/v1/expenses/categories` — Create new expense category.
  - Requires: `expenses:create`
  - Body: `{ code, name, defaultGlAccountId, description }`

### Expenses Lifecycle
- `GET /api/v1/expenses` — Query expenses with pagination, status, category, date filters.
  - Requires: `expenses:view`
- `GET /api/v1/expenses/{id}` — Get single expense details with voucher linkages.
  - Requires: `expenses:view`
- `POST /api/v1/expenses` — Record new operational expense (Draft / Submitted).
  - Requires: `expenses:create`
  - Body: `{ expenseCategoryId, glAccountId, vendorId, paymentMethod, bankCashAccountId, expenseDate, amountPaise, taxAmountPaise, costCenter, referenceNumber, description }`
- `POST /api/v1/expenses/{id}/approve` — Approve pending expense.
  - Requires: `expenses:approve`
- `POST /api/v1/expenses/{id}/reject` — Reject pending expense.
  - Requires: `expenses:approve`
  - Body: `{ reason }`
- `POST /api/v1/expenses/{id}/pay` — Disburse payment for approved expense and trigger General Ledger posting.
  - Requires: `expenses:pay`
  - Body: `{ paymentMethod, bankCashAccountId, paymentDate, referenceNumber }`
- `POST /api/v1/expenses/{id}/cancel` — Cancel expense and post counter-reversal voucher if already posted.
  - Requires: `expenses:create`
  - Body: `{ reason }`

### Petty Cash Operations
- `GET /api/v1/expenses/petty-cash` — List petty cash floats and custodians.
  - Requires: `expenses:pettycash:manage`
- `POST /api/v1/expenses/petty-cash` — Register new petty cash float.
  - Body: `{ accountName, glAccountId, custodianUserId, floatLimitPaise }`
- `POST /api/v1/expenses/petty-cash/{id}/replenish` — Replenish float from bank account.
  - Body: `{ sourceBankAccountId, amountPaise, notes }`
- `POST /api/v1/expenses/petty-cash/{id}/reconcile` — Record actual cash count, recognizing shortage or excess.
  - Body: `{ actualCountPaise, notes }`

---

## 2. Module 12 — Human Resource Management API (`/api/v1/hr`)

### Designations & Staff Directory
- `GET /api/v1/hr/designations` — List designations and job roles.
- `POST /api/v1/hr/designations` — Create new designation.
- `GET /api/v1/hr/employees` — Search employee roster with department, branch, and status filters.
  - Requires: `hr:employees:view`
- `GET /api/v1/hr/employees/{id}` — Get employee detailed profile.
  - Requires: `hr:employees:view`
- `POST /api/v1/hr/employees` — Register new employee.
  - Requires: `hr:employees:manage`
  - Body: `{ employeeCode, firstName, lastName, email, phone, address, joiningDate, employmentType, departmentId, designationId, reportingManagerId, bankAccountNumber, bankIfsc, bankName, panNumber, linkedUserId }`
- `PUT /api/v1/hr/employees/{id}` — Update employee details.
  - Requires: `hr:employees:manage`

### Attendance Tracking
- `GET /api/v1/hr/attendance` — Query daily attendance logs for branch or employee.
  - Requires: `hr:attendance:record`
- `POST /api/v1/hr/attendance/punch` — Record employee check-in or check-out (Self-service or Terminal).
  - Body: `{ employeeId, punchType: "CheckIn"|"CheckOut", punchTimeUtc, notes }`
- `POST /api/v1/hr/attendance/correct` — Supervisor attendance correction with audit justification.
  - Requires: `hr:attendance:correct`
  - Body: `{ employeeId, attendanceDate, status, checkInUtc, checkOutUtc, notes }`

### Leave Management
- `GET /api/v1/hr/leave-types` — List statutory and company leave types.
- `GET /api/v1/hr/leave-balances/{employeeId}` — Get remaining balances for employee.
- `GET /api/v1/hr/leave-applications` — List applications with pending/approved status.
- `POST /api/v1/hr/leave-applications` — Submit leave application.
  - Requires: `hr:leave:apply`
  - Body: `{ leaveTypeId, startDate, endDate, reason }`
- `POST /api/v1/hr/leave-applications/{id}/approve` — Approve leave request.
  - Requires: `hr:leave:approve`
- `POST /api/v1/hr/leave-applications/{id}/reject` — Reject leave request.
  - Requires: `hr:leave:approve`
  - Body: `{ rejectionReason }`

---

## 3. Module 13 — Payroll Management API (`/api/v1/payroll`)

### Salary Structures & Assignments
- `GET /api/v1/payroll/structures` — List configured salary structures and deduction rules.
- `POST /api/v1/payroll/structures` — Create or update salary structure.
  - Requires: `payroll:structures:manage`
  - Body: `{ name, description, components: [...] }`
- `POST /api/v1/payroll/assignments` — Assign salary structure to employee.
  - Requires: `payroll:structures:manage`
  - Body: `{ employeeId, salaryStructureId, monthlyGrossPaise, effectiveFrom }`

### Monthly Payroll Runs
- `GET /api/v1/payroll/periods` — List payroll cycles and processing states.
  - Requires: `payroll:process`
- `POST /api/v1/payroll/periods/calculate` — Generate monthly payroll calculation from attendance & leave data.
  - Requires: `payroll:process`
  - Body: `{ month, year, fiscalYearId }`
- `GET /api/v1/payroll/periods/{id}/preview` — Detailed payroll breakdown per employee for review.
  - Requires: `payroll:process`
- `POST /api/v1/payroll/periods/{id}/approve` — Authorize payroll preview.
  - Requires: `payroll:approve`
- `POST /api/v1/payroll/periods/{id}/finalize` — Finalize payroll, post accrual voucher to General Ledger.
  - Requires: `payroll:finalize`
- `POST /api/v1/payroll/periods/{id}/disburse` — Record disbursement from bank, post payment voucher.
  - Requires: `payroll:disburse`
  - Body: `{ bankAccountId, paymentReference, disbursementDate }`
- `GET /api/v1/payroll/payslips/{payrollItemId}` — Retrieve payslip summary / PDF generation data.
  - Requires: `payroll:payslip:view`

---

## 4. Module 14 — Marketing & Customer Growth API (`/api/v1/marketing`)

### Segmentation & Campaigns
- `GET /api/v1/marketing/segments` — List customer segments and membership criteria.
- `POST /api/v1/marketing/segments` — Create segment.
  - Requires: `marketing:segments:manage`
- `GET /api/v1/marketing/campaigns` — List marketing campaigns.
- `POST /api/v1/marketing/campaigns` — Create campaign with budget and target segment.
  - Requires: `marketing:campaigns:manage`
  - Body: `{ name, description, targetSegmentId, startDate, endDate, budgetPaise }`

### Coupons & Loyalty
- `GET /api/v1/marketing/coupons` — List coupons, validity, and usage stats.
- `POST /api/v1/marketing/coupons` — Issue promotional discount coupon.
  - Requires: `marketing:coupons:manage`
  - Body: `{ code, campaignId, discountType, discountValue, minOrderAmountPaise, maxDiscountAmountPaise, validFrom, validTo, usageLimitTotal }`
- `POST /api/v1/marketing/coupons/validate` — Validate coupon eligibility for a cart / customer before POS tender.
  - Body: `{ code, customerId, cartSubtotalPaise }`
- `GET /api/v1/marketing/loyalty/balance/{customerId}` — Get customer points balance and redemption value.
- `POST /api/v1/marketing/loyalty/redeem` — Redeem loyalty points for a sale.
  - Body: `{ customerId, pointsToRedeem, salesInvoiceId }`

---

## 5. Module 15 — Advanced Order Management API (`/api/v1/orders`)

### Sales Orders
- `GET /api/v1/orders` — Search sales orders with status, date, customer filters.
  - Requires: `orders:view`
- `GET /api/v1/orders/{id}` — Order details, line items, fulfillment progress.
  - Requires: `orders:view`
- `POST /api/v1/orders` — Create new sales order (Draft / Submitted).
  - Requires: `orders:create`
  - Body: `{ customerId, warehouseId, expectedDeliveryDate, deliveryAddress, notes, lines: [...] }`
- `POST /api/v1/orders/{id}/confirm` — Confirm sales order and commit inventory.
  - Requires: `orders:approve`
- `POST /api/v1/orders/{id}/fulfill` — Dispatch fulfillment shipment against order lines.
  - Requires: `orders:fulfill`
  - Body: `{ carrierName, trackingNumber, lines: [{ salesOrderLineId, quantityDispatched }] }`
- `POST /api/v1/orders/{id}/invoice` — Convert fulfilled/confirmed order into Sales Tax Invoice via `SalesService`.
  - Requires: `orders:approve`
- `POST /api/v1/orders/{id}/cancel` — Cancel sales order and release committed stock.
  - Requires: `orders:cancel`

---

## 6. Module 16 — Service Management API (`/api/v1/service`)

### Service Catalog & Requests
- `GET /api/v1/service/catalog` — List repair services, standard labor rates, and duration.
- `POST /api/v1/service/catalog` — Add service catalog entry.
  - Requires: `service:catalog:manage`
- `GET /api/v1/service/requests` — Search service desk tickets.
  - Requires: `service:requests:view`
- `GET /api/v1/service/requests/{id}` — Ticket detail, inspection history, parts consumed.
  - Requires: `service:requests:view`
- `POST /api/v1/service/requests` — Log new service request.
  - Requires: `service:requests:manage`
  - Body: `{ customerId, serviceCatalogId, itemDescription, serialNumber, reportedIssue, priority, targetCompletionDate }`
- `PUT /api/v1/service/requests/{id}/status` — Advance status (`Inspection`, `InProgress`, `Completed`, `Delivered`).
  - Requires: `service:requests:manage`
  - Body: `{ status, assignedTechnicianId, technicianNotes }`

### Quotations & Parts Consumption
- `POST /api/v1/service/requests/{id}/quotations` — Issue service estimate for labor + parts.
  - Requires: `service:quotations:manage`
  - Body: `{ validUntil, lines: [...] }`
- `POST /api/v1/service/quotations/{id}/approve` — Customer approves quotation.
  - Requires: `service:quotations:manage`
- `POST /api/v1/service/requests/{id}/parts` — Record spare parts consumed from inventory (deducts stock).
  - Requires: `service:parts:consume`
  - Body: `{ productId, warehouseId, quantity, notes }`
- `POST /api/v1/service/requests/{id}/invoice` — Convert completed service job into Sales Invoice.
  - Requires: `service:billing:create`
  - Body: `{ paymentMethod, bankCashAccountId }`

