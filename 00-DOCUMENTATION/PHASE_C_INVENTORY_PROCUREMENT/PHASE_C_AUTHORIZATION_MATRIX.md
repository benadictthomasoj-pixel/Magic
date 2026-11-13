# MAGIC ERP — Phase C: RBAC Authorization Matrix

| Permission Code | Module | Description | Admin | Accountant | Cashier | Warehouse Manager |
| :--- | :--- | :--- | :---: | :---: | :---: | :---: |
| `inventory:warehouse:view` | Inventory | View warehouses, layouts, and storage bins | ✅ | ✅ | ❌ | ✅ |
| `inventory:warehouse:manage` | Inventory | Create, edit, and configure warehouses | ✅ | ❌ | ❌ | ✅ |
| `inventory:stock:view` | Inventory | View stock balances and movement history | ✅ | ✅ | ✅ | ✅ |
| `inventory:stock:manage` | Inventory | Record opening stock and manual transfers | ✅ | ❌ | ❌ | ✅ |
| `inventory:adjust:create` | Inventory | Create draft stock adjustment requests | ✅ | ✅ | ❌ | ✅ |
| `inventory:adjust:approve` | Inventory | Approve high-value stock adjustments | ✅ | ✅ | ❌ | ❌ |
| `inventory:count:view` | Inventory | View physical stock counting sessions | ✅ | ✅ | ❌ | ✅ |
| `inventory:count:manage` | Inventory | Create counts, record audits, reconcile | ✅ | ✅ | ❌ | ✅ |
| `procurement:po:view` | Procurement | View purchase orders and vendor quotes | ✅ | ✅ | ❌ | ✅ |
| `procurement:po:create` | Procurement | Create and edit purchase orders | ✅ | ✅ | ❌ | ✅ |
| `procurement:po:approve` | Procurement | Approve and authorize purchase orders | ✅ | ✅ | ❌ | ❌ |
| `procurement:grn:view` | Procurement | View goods receipt notes | ✅ | ✅ | ❌ | ✅ |
| `procurement:grn:create` | Procurement | Record goods receipts and quality checks | ✅ | ❌ | ❌ | ✅ |
| `procurement:grn:confirm` | Procurement | Confirm GRN and update physical stock | ✅ | ✅ | ❌ | ✅ |
| `procurement:return:manage` | Procurement | Create and confirm purchase returns | ✅ | ✅ | ❌ | ✅ |
| `inventory:transfer:create` | Inventory | Create inter-warehouse transfer requests | ✅ | ❌ | ❌ | ✅ |
| `inventory:transfer:dispatch`| Inventory | Dispatch stock from source warehouse | ✅ | ❌ | ❌ | ✅ |
| `inventory:transfer:receive` | Inventory | Confirm receipt at destination warehouse | ✅ | ❌ | ❌ | ✅ |
| `inventory:reorder:view` | Inventory | View min/max stock rules and suggestions | ✅ | ✅ | ❌ | ✅ |
| `inventory:reorder:manage` | Inventory | Configure reorder points and lead times | ✅ | ❌ | ❌ | ✅ |

