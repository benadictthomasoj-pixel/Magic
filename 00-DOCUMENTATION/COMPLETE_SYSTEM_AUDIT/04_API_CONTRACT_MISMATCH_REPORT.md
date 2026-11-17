# MAGIC ERP — SYSTEM-WIDE ARCHITECTURE, CODEBASE & INTEGRATION AUDIT
## Document 04: Frontend-to-Backend API Contract & Route Mismatch Report

**Document ID:** `AUDIT-04-CONTRACTS`  
**Audit Scope:** HTTP Endpoints, Route Templates, DTO Serialization, Error Schemas, Frontend Angular API Services  
**Workspace:** `/Users/benadict/Documents/antigravity projects/magic`  
**Git Baseline Commit:** `2567c7d`  

---

### 1. Overview of Frontend API Service Architecture

The Angular 22 frontend encapsulates all backend communication inside dedicated injectable API services located in `01-SRC/Frontend/src/app/core/services/`:
1. `api-client.service.ts` — Base HTTP wrapper with JWT token injection, correlation headers, and error standardization.
2. `auth-boundary.service.ts` — Login, token refresh, and permission claims inspection.
3. `accounting-api.service.ts` — General ledger, journal vouchers, chart of accounts, and financial periods.
4. `commercial-api.service.ts` — Products, categories, customers, suppliers, and price tiers.
5. `sales-api.service.ts` — Invoices, POS checkout, cashier sessions, sales returns, and credit notes.
6. `inventory-api.service.ts` — Stock balances, warehouses, purchase orders, goods receipts, and batch lots.
7. `finance-api.service.ts` — Bank accounts, reconciliations, payables, receivables, and fixed assets.
8. `operations-api.service.ts` — Expenses, HR employees, leaves, payroll periods, pay slips, marketing campaigns, coupons, loyalty points, orders, backorders, and service desk tickets.
9. `enterprise-api.service.ts` — Business intelligence KPIs, multi-branch, terminal registration, enterprise admin, user governance, automation rules, AI insights, and system management health.

---

### 2. Critical Defect: Relative Path Prefixing in EnterpriseApiService (DEF-INT-001)

#### Root Cause Analysis
In `01-SRC/Frontend/src/app/core/services/enterprise-api.service.ts`:
All other API services (such as `operations-api.service.ts`, `finance-api.service.ts`, etc.) construct their request URLs using the configured `apiBaseUrl`:
```typescript
// Standard pattern used across Phase A - F services:
private readonly baseUrl = `${environment.apiBaseUrl}/v1/operations`;
this.http.get<...>(`${this.baseUrl}/expenses...`);
```
However, in `enterprise-api.service.ts`, lines 18–168 hardcode root-relative paths:
```typescript
// Defective pattern in enterprise-api.service.ts:
getExecutiveKpis(companyId: string): Observable<ExecutiveKpis> {
  return this.http.get<ExecutiveKpis>(`/api/v1/reporting/kpis?companyId=${companyId}`);
}
getBranches(companyId: string): Observable<Branch[]> {
  return this.http.get<Branch[]>(`/api/v1/branch-management/companies/${companyId}/branches`);
}
getSystemHealth(): Observable<SystemHealth> {
  return this.http.get<SystemHealth>('/api/v1/system-management/health');
}
```

#### Environmental Impact Breakdown
1. **Local Development Mode (`ng serve` on port 4200):**
   - In `environment.development.ts`, `apiBaseUrl` is configured as `http://127.0.0.1:5000/api`.
   - The Angular CLI development server does NOT have a default proxy configuration file (`proxy.conf.json`).
   - Consequently, when the browser issues a GET request to `/api/v1/reporting/kpis`, it sends the request to `http://localhost:4200/api/v1/reporting/kpis`.
   - Angular dev server catches this unmatched route and falls back to serving `index.html` (MIME type: `text/html`).
   - Angular's `HttpClient` attempts to parse the HTML document as JSON, immediately throwing:
     `HttpErrorResponse: SyntaxError: Unexpected token '<', "<!doctype "... is not valid JSON`.
   - **Result:** In development mode, the entire Phase G suite (BI Dashboard, Multi-Branch, Enterprise Admin, Automation Dashboard, and System Management) fails to fetch data from the backend.
2. **Production Mode (Reverse Proxy / Nginx / IIS / WebView2):**
   - If deployed behind a reverse proxy where `/api` is routed directly to the ASP.NET Core Kestrel backend on port 5000, relative paths resolve correctly.
   - However, in standalone local development or during direct API port testing, this divergence creates severe communication failures.

#### Recommended Remediation
Refactor `enterprise-api.service.ts` to utilize `environment.apiBaseUrl`:
```typescript
private readonly baseUrl = `${environment.apiBaseUrl}/v1`;
// Then invoke:
return this.http.get<ExecutiveKpis>(`${this.baseUrl}/reporting/kpis?companyId=${companyId}`);
```

---

### 3. Comprehensive Route & Contract Mapping Matrix

| Frontend Service Method | Angular Endpoint Called | Backend Controller & Action | Route Match | Request/Response DTO Alignment | Status |
|---|---|---|:---:|:---:|:---:|
| `AuthBoundary.login` | `POST ${apiBaseUrl}/v1/auth/login` | `AuthenticationController.Login` | **EXACT** | `LoginRequest` ──► `AuthenticationResult` | **VERIFIED** |
| `CommercialApi.getProducts` | `GET ${apiBaseUrl}/v1/commercial/products` | `ProductsController.GetProducts` | **EXACT** | Query params ──► `List<ProductDto>` | **VERIFIED** |
| `SalesApi.checkout` | `POST ${apiBaseUrl}/v1/pos/checkout` | `PosController.Checkout` | **EXACT** | `PosCheckoutRequest` ──► `PosCheckoutResponse` | **VERIFIED** |
| `SalesApi.openSession` | `POST ${apiBaseUrl}/v1/cashier-sessions/open` | `CashierSessionsController.OpenSession` | **EXACT** | `OpenSessionRequest` ──► `CashierSessionDto` | **VERIFIED** |
| `InventoryApi.getBalances` | `GET ${apiBaseUrl}/v1/inventory/stock-overview` | `StockOverviewController.GetBalances` | **EXACT** | Query params ──► `StockOverviewDto` | **VERIFIED** |
| `InventoryApi.createGrn` | `POST ${apiBaseUrl}/v1/inventory/goods-receipt` | `GoodsReceiptController.CreateGrn` | **EXACT** | `CreateGrnRequest` ──► `GoodsReceiptDto` | **VERIFIED** |
| `AccountingApi.getAccounts`| `GET ${apiBaseUrl}/v1/accounting/accounts` | `AccountsController.GetAccounts` | **EXACT** | Query params ──► `List<AccountDto>` | **VERIFIED** |
| `FinanceApi.getBankAccounts`| `GET ${apiBaseUrl}/v1/finance/bank-accounts` | `BankAccountsController.GetBankAccounts` | **EXACT** | Query params ──► `List<BankAccountDto>`| **VERIFIED** |
| `OperationsApi.getExpenses` | `GET ${apiBaseUrl}/v1/operations/expenses` | `ExpensesController.GetExpenses` | **EXACT** | Query params ──► `List<ExpenseDto>` | **VERIFIED** |
| `OperationsApi.getEmployees`| `GET ${apiBaseUrl}/v1/operations/hr/employees` | `HrController.GetEmployees` | **EXACT** | Query params ──► `List<EmployeeDto>` | **VERIFIED** |
| `OperationsApi.createOrder` | `POST ${apiBaseUrl}/v1/operations/orders` | `OrdersController.CreateOrder` | **EXACT** | `CreateOrderRequest` ──► `OrderDto` | **VERIFIED** |
| `OperationsApi.getTickets` | `GET ${apiBaseUrl}/v1/operations/services/tickets`| `ServiceDeskController.GetTickets` | **EXACT** | Query params ──► `List<ServiceTicketDto>`| **VERIFIED** |
| `EnterpriseApi.getKpis` | `GET /api/v1/reporting/kpis` | `BusinessIntelligenceController.GetKpis` | **MISMATCH (Dev)**| `ExecutiveKpisDto` (Path relative) | **DEFECT (DEF-INT-001)** |
| `EnterpriseApi.getBranches`| `GET /api/v1/branch-management/...` | `BranchManagementController.GetBranches` | **MISMATCH (Dev)**| `List<BranchDto>` (Path relative) | **DEFECT (DEF-INT-001)** |
| `EnterpriseApi.getHealth` | `GET /api/v1/system-management/health` | `SystemManagementController.GetHealth` | **MISMATCH (Dev)**| `SystemHealthDto` (Path relative) | **DEFECT (DEF-INT-001)** |

---

### 4. DTO Serialization & Data Type Compatibility

All backend controllers configure System.Text.Json with:
- `JsonNamingPolicy.CamelCase`
- `JsonStringEnumConverter` for string-based enum representations.
- UTC ISO-8601 timestamp serialization (`yyyy-MM-ddTHH:mm:ss.fffZ`).

Field-level inspection between C# DTO records and TypeScript interfaces confirmed:
1. **Identifiers (GUIDs):** All C# `Guid` properties deserialize cleanly into TypeScript `string` GUIDs.
2. **Decimals & Monetary Precision:** C# `decimal` fields serialize as JSON numbers; Angular parses them into JavaScript 64-bit IEEE 754 floats. For normal business transactions (< 9 trillion currency units with 2 decimal places), this maintains precision without floating-point degradation.
3. **Enum Values:** Enums match casing exactly (e.g., `PaymentStatus.Paid` serializes as `"Paid"` or `"PAID"` depending on converter, and TypeScript interfaces handle string unions).

