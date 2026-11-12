# MAGIC — DOMAIN EVENTS & TRANSACTIONAL OUTBOX SPECIFICATION

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  

---

## 1. Event-Driven Decoupling for Secondary Asynchronous Work

To preserve strict module boundaries without introducing the operational complexity of external message brokers (e.g., RabbitMQ, Kafka), MAGIC employs an **In-Process Transactional Outbox Pattern** backed by PostgreSQL for **secondary asynchronous work**.

> [!IMPORTANT]
> **Critical Financial & Stock Boundary Rule:** Core commercial transactions (Sales Document + Physical Stock Deduction + Payment + Accounting Double-Entry Voucher + Customer/Supplier Balance) execute **synchronously inside one PostgreSQL ACID transaction** via approved Application Service interfaces (`Contracts/`). Post-commit Outbox events handle ONLY secondary non-critical side effects (BI snapshot updates, loyalty notifications, marketing attribution, audit projection indexes).

### Principles
1. **Atomic State & Event Persistence:** When an aggregate root mutates state (e.g. Sales Invoice posted), the core domain entity changes (invoice, stock movement, accounting voucher) and the secondary `DomainEvent` are committed to PostgreSQL within the **exact same database transaction**.
2. **Zero Lost Events:** If the process crashes immediately after a commit, the event remains safely stored in the `platform.outbox_messages` table and will be dispatched upon recovery.
3. **In-Process High Velocity:** Under normal operation, events are dispatched to an asynchronous background worker channel (`System.Threading.Channels.Channel<T>`) for sub-millisecond execution.
4. **Idempotent Consumers:** Every subscriber tracks processed message IDs to guarantee that duplicated dispatches do not result in repeated actions.

---

## 2. PostgreSQL Outbox Table Design

```sql
CREATE TABLE platform.outbox_messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    occurred_on_utc TIMESTAMPTZ NOT NULL DEFAULT CLOCK_TIMESTAMP(),
    aggregate_type VARCHAR(100) NOT NULL,      -- e.g. 'SalesInvoice', 'PurchaseOrder'
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(150) NOT NULL,          -- e.g. 'magic.sales.invoice_posted.v1'
    payload_json JSONB NOT NULL,
    trace_correlation_id UUID NOT NULL,
    processed_at_utc TIMESTAMPTZ,
    error_message TEXT,
    retry_count INT NOT NULL DEFAULT 0,
    lock_token UUID,                           -- For distributed consumer claiming
    lock_expires_at_utc TIMESTAMPTZ
);

CREATE INDEX ix_outbox_unprocessed 
    ON platform.outbox_messages(occurred_on_utc) 
    WHERE processed_at_utc IS NULL;
```

---

## 3. Core Domain Event Catalogue

### 3.1 Event: `magic.sales.invoice_posted.v1`
- **Emitted By:** `SalesModule` / `POSModule`
- **When:** A sales invoice is finalized and committed.
- **Payload Schema:**
  ```json
  {
    "invoiceId": "019318f2-39c2-7489-8d5a-5264b3c95972",
    "invoiceNumber": "INV-2026-001429",
    "branchId": "019318f2-39c2-7489-8d5a-5264b3c90001",
    "terminalId": "019318f2-39c2-7489-8d5a-5264b3c90002",
    "customerId": "019318f2-39c2-7489-8d5a-5264b3c90050",
    "postingDate": "2026-11-11",
    "isRetail": true,
    "taxableAmountPaise": 84746,
    "cgstPaise": 7627,
    "sgstPaise": 7627,
    "igstPaise": 0,
    "roundOffPaise": 0,
    "grandTotalPaise": 100000,
    "lineItems": [
      {
        "itemId": "019318f2-39c2-7489-8d5a-5264b3c90100",
        "warehouseId": "019318f2-39c2-7489-8d5a-5264b3c90200",
        "batchNumber": "B2611-01",
        "quantity": 2.0,
        "unitPricePaise": 50000
      }
    ]
  }
  ```
- **Subscribers (Secondary Asynchronous Work Only):**
  - `BiModule`: Updates real-time sales velocity read-model snapshots.
  - `CrmModule`: Updates customer analytics and accrues loyalty reward points.
  - `MarketingModule`: Records campaign and coupon attribution metrics.
  *(Note: Physical stock deduction and Accounting double-entry posting run synchronously inside the primary Sales ACID transaction before this event is emitted.)*

---

### 3.2 Event: `magic.inventory.stock_deficit_detected.v1`
- **Emitted By:** `InventoryModule` (during offline terminal sync or force-issue).
- **When:** Stock is deducted below zero due to concurrent offline sales.
- **Payload Schema:**
  ```json
  {
    "alertId": "019318f2-39c2-7489-8d5a-5264b3c95988",
    "itemId": "019318f2-39c2-7489-8d5a-5264b3c90100",
    "warehouseId": "019318f2-39c2-7489-8d5a-5264b3c90200",
    "deficitQuantity": -3.0,
    "triggerSource": "OFFLINE_TERMINAL_SYNC",
    "sourceTerminalId": "TRM-01",
    "detectedAtUtc": "2026-11-11T12:45:00.000Z"
  }
  ```
- **Subscribers:**
  - `BiModule`: Triggers high-priority banner in Store Manager dashboard.
  - `AuditModule`: Records immutable security alert for physical inventory count reconciliation.

---

### 3.3 Event: `magic.purchases.invoice_verified.v1`
- **Emitted By:** `PurchasesModule` / `AutomationModule` (Auto Stock Update Commit)
- **When:** A wholesale purchase invoice is verified and approved.
- **Subscribers (Secondary Asynchronous Work Only):**
  - `BiModule`: Refreshes procurement spend and supplier price-trend analytics.
  - `AuditModule`: Archives structured parsing provenance metadata.
  *(Note: Inventory batch inward, valuation recalculation, and Accounting purchase voucher posting run synchronously inside the primary Purchase/Auto-Stock ACID transaction.)*

---

## 4. In-Process Dispatcher Architecture

```
[Application Service Transaction (e.g. PostSalesInvoiceCommandHandler)]
                       |
         (Within Single PostgreSQL ACID Transaction)
         1. Write sales.invoices
         2. Deduct inventory.stock_balances & write stock_ledger (Sync Contract)
         3. Post accounting.vouchers & journal_lines (Sync Contract)
         4. Write platform.outbox_messages (Secondary Domain Event)
                       |
               (Commit Transaction)
                       |
                       v
[In-Memory Channel Dispatcher (System.Threading.Channels)]
                       |
                       +---> [BiMetricsSnapshotSubscriber]
                       |
                       +---> [CrmLoyaltyAccrualSubscriber]
                       |
                       +---> [MarketingCampaignAttributionSubscriber]
```

Secondary subscribers run asynchronously without blocking the primary HTTP response to the client. If an in-process subscriber throws an unhandled exception, the event remains in `platform.outbox_messages` where a resilient fallback background service (`OutboxPollerService`) retries processing with exponential backoff.

