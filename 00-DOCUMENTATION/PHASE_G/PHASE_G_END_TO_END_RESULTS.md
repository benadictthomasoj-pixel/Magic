# MAGIC ERP — PHASE G: END-TO-END WORKFLOW RESULTS

**Subsystems Tested:** Full Phase G (Modules 17–21) + Pre-existing Core Modules  

---

### 1. Workflow 1: Multi-Branch Store Setup & POS Billing

1. **Company Profile Maintenance:** Legal profile updated with GSTIN `29ABCDE1234F1Z5`.
2. **Branch Provisioning:** New retail branch (`BR-BLR-02`) registered and linked to Central Warehouse.
3. **POS Terminal Authorization:** Terminal `POS-02` registered on macOS host, authorized by admin, and assigned cashier.
4. **Billing Execution:** Terminal successfully issues sales invoice, posting debits to Cash/AR and credits to Sales and GST Output.

---

### 2. Workflow 2: Inter-Branch Inventory Rebalancing

1. **Requisition Created:** Transfer request submitted for 10 units of SKU from Bangalore HQ to Koramangala Store.
2. **Approval:** Originating branch supervisor signs off on the transfer.
3. **Dispatch:** System verifies stock availability in source warehouse, deducts 10 units, and records `TransferOut` stock movement.
4. **Receipt:** Destination warehouse confirms physical intake; system adds 10 units to destination warehouse stock balance and records `TransferIn` stock movement.

---

### 3. Workflow 3: Multi-Tier Approval with Separation of Duties

1. **Threshold Configuration:** Procurement workflow configured for PO amounts >= ₹10,000.
2. **Requisition Submission:** Operator submits Purchase Order for ₹15,000; status transitions to `PendingApproval`.
3. **Self-Approval Attempt:** Requestor attempts to approve their own PO; system rejects with `400 Bad Request` citing separation of duties violation.
4. **Independent Approval:** Designated approver logs in, reviews audit trail, and approves PO with reason notes. PO transitions to `Approved` and releases for GRN receipt.

---

### 4. Workflow 4: Grounded AI Assistant Operational Advisory

1. **Executive Inquiry:** User prompts: *"Summarize today's sales and revenue across all active branches"*.
2. **Intent Matching:** Engine executes typed LINQ aggregation over authenticated tenant's `SalesInvoices`.
3. **Structured Advisory:** Returns factual revenue figures, top product categories, deep-link navigation to `/billing/sales`, and operational recommendations for evening cashier shift closure.

---

### 5. Workflow 5: Disaster Recovery & Isolated Restore Verification

1. **Backup Triggered:** Administrator triggers on-demand database backup. System archives schema and records SHA-256 hash.
2. **Unsafe Restore Intercepted:** User attempts restore directly targeting live `magic_dev` database. System blocks operation with `400 Bad Request`.
3. **Safe Isolated Simulation:** Restore executed targeting `magic_restore_sandbox_*` with confirmation token. Schema validation, table foreign keys, and checksum integrity verified successfully.
