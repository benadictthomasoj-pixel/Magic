# MAGIC ERP — MODULE 16: SERVICE MANAGEMENT SPECIFICATION
## Repair Workflows, Labor Catalog, Spare Parts & Service Billing

**Route:** `/billing/service`  
**API Controller:** `ServiceController` (`/api/v1/service`)  
**Database Schema:** `services`

---

## 1. Functional Scope

1. **Service Catalog & Pricing:**
   - Catalog of standard maintenance and repair operations with standard labor charges and durations.
2. **Service Request Ticketing:**
   - Ticket intake: item description, serial numbers, customer complaints, priority, and assigned technician.
   - States: `NewRequest` $\rightarrow$ `Inspection` $\rightarrow$ `Quotation` $\rightarrow$ `Approved` $\rightarrow$ `InProgress` $\rightarrow$ `Completed` $\rightarrow$ `Delivered` (or `Cancelled`).
3. **Service Quotations:**
   - Itemized labor charges + replacement spare parts.
   - Customer approval gate before technician commences repair work.
4. **Parts Consumption & Inventory Integration:**
   - Consumes spare parts from warehouses, immediately recording standard `inventory.stock_movements`.
   - Prevents negative inventory unless authorized by company policy.
5. **Billing Integration:**
   - Converts completed service tickets into Sales Tax Invoices via `SalesService`.
   - Recognizes labor revenue (`4102`) and spare parts revenue (`4101`) along with applicable output GST.

