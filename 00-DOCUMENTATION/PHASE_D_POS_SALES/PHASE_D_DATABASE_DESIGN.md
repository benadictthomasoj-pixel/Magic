# MAGIC ERP — Phase D: POS & Sales Billing Database Design

**Document:** `00-DOCUMENTATION/PHASE_D_POS_SALES/PHASE_D_DATABASE_DESIGN.md`  
**Target Database:** PostgreSQL 18  
**ORM:** EF Core 10.0 (Npgsql)  
**Schemas:** `sales` and `cashier`  

---

## 1. Schema: `sales`

### Table: `sales.sales_invoices`
| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `UUID` | PK | Primary Identifier |
| `CompanyId` | `UUID` | FK -> `platform.companies` | Multitenancy isolation |
| `BranchId` | `UUID` | FK -> `organization.branches` | Operating branch |
| `InvoiceNumber` | `VARCHAR(50)` | UNIQUE, NOT NULL | Deterministic sequence number (`INV-YYYYMM-XXXXX`) |
| `CustomerId` | `UUID` | NULLABLE, FK -> `commercial.customers` | Null for anonymous walk-in retail |
| `CashierSessionId` | `UUID` | NULLABLE, FK -> `cashier.cashier_sessions` | Associated active register session |
| `CashierUserId` | `UUID` | NOT NULL, FK -> `security.users` | Cashier user who transacted the sale |
| `InvoiceDateUtc` | `TIMESTAMPTZ` | NOT NULL | Transaction wall-clock timestamp |
| `Status` | `VARCHAR(30)` | NOT NULL | `Draft`, `Paid`, `PartiallyPaid`, `Cancelled`, `Refunded` |
| `SubtotalPaise` | `BIGINT` | NOT NULL | Pre-tax line item total |
| `DiscountPaise` | `BIGINT` | NOT NULL, DEFAULT 0 | Total invoice discounts |
| `CgstPaise` | `BIGINT` | NOT NULL, DEFAULT 0 | Central GST total in paise |
| `SgstPaise` | `BIGINT` | NOT NULL, DEFAULT 0 | State GST total in paise |
| `IgstPaise` | `BIGINT` | NOT NULL, DEFAULT 0 | Integrated GST total in paise |
| `TotalTaxPaise` | `BIGINT` | NOT NULL | `Cgst + Sgst + Igst` |
| `GrandTotalPaise` | `BIGINT` | NOT NULL | `Subtotal - Discount + TotalTax` |
| `AmountPaidPaise` | `BIGINT` | NOT NULL, DEFAULT 0 | Total tendered amount captured |
| `OutstandingBalancePaise` | `BIGINT` | NOT NULL, DEFAULT 0 | `GrandTotal - AmountPaid` |
| `PlaceOfSupplyStateCode` | `VARCHAR(10)` | NOT NULL | 2-digit Indian state code (e.g. `33`) |
| `CustomerGstin` | `VARCHAR(15)` | NULLABLE | Indian 15-char GSTIN if B2B |
| `SellerGstin` | `VARCHAR(15)` | NOT NULL | Company GSTIN |
| `Notes` | `VARCHAR(500)` | NULLABLE | Invoice remarks |
| `ConcurrencyVersion` | `INT` | NOT NULL, DEFAULT 1 | Optimistic concurrency token |
| `CreatedAtUtc` | `TIMESTAMPTZ` | NOT NULL | Audit creation timestamp |

### Table: `sales.sales_invoice_lines`
| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `UUID` | PK | Primary Key |
| `InvoiceId` | `UUID` | FK -> `sales.sales_invoices` (CASCADE) | Parent Invoice |
| `ProductId` | `UUID` | FK -> `commercial.products` | Product item |
| `ProductCode` | `VARCHAR(50)` | NOT NULL | Immutable SKU snapshot |
| `ProductName` | `VARCHAR(200)` | NOT NULL | Immutable Product name snapshot |
| `HsnCode` | `VARCHAR(20)` | NULLABLE | Statutory HSN/SAC code |
| `Quantity` | `NUMERIC(14,4)` | NOT NULL, CHECK > 0 | Billed unit quantity |
| `UnitPricePaise` | `BIGINT` | NOT NULL, CHECK >= 0 | Unit selling price in integer paise |
| `DiscountPercentage` | `NUMERIC(5,2)` | NOT NULL, DEFAULT 0 | Line discount rate |
| `DiscountPaise` | `BIGINT` | NOT NULL, DEFAULT 0 | Computed line discount in paise |
| `GstRatePercentage` | `NUMERIC(5,2)` | NOT NULL | GST rate (0, 5, 12, 18, 28) |
| `CgstPercentage` | `NUMERIC(5,2)` | NOT NULL, DEFAULT 0 | Rate / 2 if intra-state |
| `SgstPercentage` | `NUMERIC(5,2)` | NOT NULL, DEFAULT 0 | Rate / 2 if intra-state |
| `IgstPercentage` | `NUMERIC(5,2)` | NOT NULL, DEFAULT 0 | Rate if inter-state |
| `TaxAmountPaise` | `BIGINT` | NOT NULL | Total line tax in paise |
| `LineTotalPaise` | `BIGINT` | NOT NULL | `(Quantity * UnitPrice - Discount) + Tax` |
| `StorageLocationId` | `UUID` | NULLABLE | Pick location |
| `BatchNumber` | `VARCHAR(50)` | NULLABLE | Batch / Lot identifier |
| `SerialNumber` | `VARCHAR(100)` | NULLABLE | Unique item serial number |

### Table: `sales.sales_payments`
| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `UUID` | PK | Primary Key |
| `CompanyId` | `UUID` | NOT NULL | Tenant isolation |
| `BranchId` | `UUID` | NOT NULL | Operating branch |
| `InvoiceId` | `UUID` | FK -> `sales.sales_invoices` | Target invoice |
| `CashierSessionId` | `UUID` | NULLABLE, FK -> `cashier.cashier_sessions` | Associated drawer session |
| `PaymentMethod` | `VARCHAR(30)` | NOT NULL | `Cash`, `Upi`, `Card`, `BankTransfer`, `CustomerCredit` |
| `AmountPaise` | `BIGINT` | NOT NULL, CHECK > 0 | Payment allocation in paise |
| `ReferenceNumber` | `VARCHAR(100)` | NULLABLE | UPI UTR, Card auth code, or Cheque number |
| `PaymentDateUtc` | `TIMESTAMPTZ` | NOT NULL | Transaction timestamp |
| `CashierUserId` | `UUID` | NOT NULL | Staff user who accepted payment |
| `Status` | `VARCHAR(20)` | NOT NULL | `Completed`, `Voided`, `Refunded` |

### Table: `sales.sales_returns` & `sales.sales_return_lines`
- `sales_returns`: Tracks return session against invoice, refund amount, reason code, and issued Credit Note reference.
- `sales_return_lines`: Tracks returned quantity, unit price, tax credit, whether item is resalable (triggers restock movement) or damaged.

### Table: `sales.credit_notes`
- Tracks issued statutory credit notes with sequential reference numbering (`CN-YYYYMM-XXXXX`).

---

## 2. Schema: `cashier`

### Table: `cashier.cashier_sessions`
| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `UUID` | PK | Primary Key |
| `CompanyId` | `UUID` | NOT NULL | Tenant isolation |
| `BranchId` | `UUID` | NOT NULL | Operating branch |
| `TerminalId` | `UUID` | NULLABLE | Hardware POS terminal |
| `CashierUserId` | `UUID` | NOT NULL | Cashier staff |
| `CashierUsername`| `VARCHAR(100)` | NOT NULL | Username snapshot |
| `OpenedAtUtc` | `TIMESTAMPTZ` | NOT NULL | Shift opening timestamp |
| `ClosedAtUtc` | `TIMESTAMPTZ` | NULLABLE | Shift closing timestamp |
| `Status` | `VARCHAR(20)` | NOT NULL | `Open`, `Closed`, `Reconciled` |
| `OpeningCashPaise` | `BIGINT` | NOT NULL, DEFAULT 0 | Starting register float |
| `CashSalesPaise` | `BIGINT` | NOT NULL, DEFAULT 0 | Cumulative cash sales |
| `CashRefundsPaise` | `BIGINT` | NOT NULL, DEFAULT 0 | Cumulative cash returns |
| `CashDepositsPaise`| `BIGINT` | NOT NULL, DEFAULT 0 | Cash additions into drawer |
| `CashWithdrawalsPaise`| `BIGINT` | NOT NULL, DEFAULT 0 | Cash removals / drops |
| `ExpectedCashPaise`| `BIGINT` | NOT NULL, DEFAULT 0 | `Opening + Sales - Refunds + Deposits - Withdrawals` |
| `ActualClosingCashPaise`| `BIGINT` | NULLABLE | Physical count entered by cashier |
| `VariancePaise` | `BIGINT` | NULLABLE | `Actual - Expected` |
| `ClosingNotes` | `VARCHAR(500)` | NULLABLE | Shift notes |
| `SupervisorUserId`| `UUID` | NULLABLE | Supervisor approver |
| `ApprovedAtUtc` | `TIMESTAMPTZ` | NULLABLE | Supervisor reconciliation timestamp |

### Table: `cashier.cash_movements`
- Individual cash drops, floats, and drawer adjustments with user attribution and reason notes.
