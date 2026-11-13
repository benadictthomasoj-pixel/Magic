# MAGIC ERP — MODULE 15: ADVANCED ORDER MANAGEMENT SPECIFICATION
## Sales Orders, Backorders, Warehouses & Delivery Fulfillment

**Route:** `/billing/orders`  
**API Controller:** `OrdersController` (`/api/v1/orders`)  
**Database Schema:** `orders`

---

## 1. Functional Scope

1. **Sales Order Processing:**
   - Multi-line customer sales orders with price tiering, delivery addresses, and customer terms.
   - States: `Draft` $\rightarrow$ `Submitted` $\rightarrow$ `Confirmed` $\rightarrow$ `PartiallyFulfilled` $\rightarrow$ `Fulfilled` $\rightarrow$ `Invoiced` $\rightarrow$ `Cancelled`.
2. **Inventory Integration & Fulfillment:**
   - On confirmation, commits available inventory without double-decrementing stock.
   - Dispatches fulfillment shipments tracking carrier name, tracking code, and dispatched quantities.
   - Backorder generation for out-of-stock items, fulfilled upon replenishment.
3. **Invoicing & Accounting Integration:**
   - Converts fulfilled or confirmed sales orders directly into Sales Tax Invoices via existing `SalesService`.
   - Decrements stock from `inventory.stock_balances` exactly once upon invoicing/dispatch.
   - Revenue and tax recognition are executed via existing `PostingService`.

