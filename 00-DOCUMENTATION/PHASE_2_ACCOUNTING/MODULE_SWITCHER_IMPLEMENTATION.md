# MAGIC ERP — MODULE SEPARATION & TOP-CENTRE WORKSPACE SWITCHER
## Technical Architecture, Navigation Isolation & Implementation Report

| Attribute | Value |
| :--- | :--- |
| **Project** | MAGIC – Universal Business Management ERP |
| **Component** | Application Header & Sidebar Navigation Architecture |
| **Workspaces** | Billing (`/billing/...`) & Accounting (`/accounting/...`) |
| **Design Language** | Deep Navy (`#0f172a`), Restrained Gold (`#f59e0b` / `#d97706`), Neutral White (`#ffffff`) |
| **Status** | Complete & Verified |
| **Date** | November 12, 2026 |

---

## 1. Executive Summary

This enhancement delivers complete UI/UX workspace separation for MAGIC ERP by introducing a top-centre segmented workspace switcher:

`[ 🛒 Billing ]  [ 📊 Accounting ]`

Operators can toggle seamlessly between **Billing** and **Accounting** from the application header without reloading or opening separate windows. When a workspace is active, the primary sidebar strictly displays that module's dedicated navigation hierarchy:
- In **Accounting**, all Phase 2 double-entry accounting screens are active and connected to PostgreSQL 18.6 APIs. No Billing menu items are shown.
- In **Billing**, the planned commercial operations menu (15 planned navigation entries across POS, Invoicing, Inventory, Procurement, and Cash Closing) is rendered with clear, honest "Coming Soon — Scheduled for Phase 3" architectural status cards. No Accounting menu items are shown.

---

## 2. Navigation Architecture & Header Design

### 2.1 Three-Column Header Layout
The top header (`shell.component.scss`) adopts a 3-column CSS Grid architecture (`1fr auto 1fr`):
1. **Left Column**: Official MAGIC emblem, brand wordmark (`MAGIC`), and tagline (`Universal Business Management ERP`), plus responsive mobile drawer toggle.
2. **Centre Column**: Horizontal segmented module switcher (`.module-switcher`) horizontally centered in the viewport.
3. **Right Column**: Enterprise context badge (`Universal Enterprises Ltd / Headquarters (MAIN) / FY 2026-27`), Strict Online LAN status pill, theme cycler, and sign out control.

### 2.2 Segmented Switcher Specification
- **Interaction**: Keyboard accessible via `role="tablist"` and `role="tab"`, with `aria-selected` tracking active state.
- **Visuals**: Embedded inside a dark translucent enclosure (`rgba(15, 23, 42, 0.7)`) with 1px border. The active tab transitions to a crisp `#f8fafc` surface with `#0f172a` high-contrast typography, while inactive tabs provide subtle hover illumination.
- **Focus Ring**: Accessible 2px gold outline (`#f59e0b`) on `:focus-visible`.
- **Responsive Layout**: On mobile/narrow screens (< 800px), the header converts to a 2-row layout with the module switcher spanning full width beneath the brand and controls.

---

## 3. Workspace Menu Structures

### 3.1 Accounting Workspace (`/accounting/...`)
When `Accounting` is active, the sidebar displays only financial accounting and platform governance entries:

| Section | Menu Item | Route | Status |
| :--- | :--- | :--- | :--- |
| **Financial Accounting** | Accounting Overview | `/accounting/dashboard` | **Active** (Live PostgreSQL KPIs) |
| | Chart of Accounts | `/accounting/chart-of-accounts` | **Active** (Live Group & Detail Accounts) |
| | Journal Entry | `/accounting/journal-entry` | **Active** (Live Double-Entry Posting) |
| | Voucher Registry | `/accounting/vouchers` | **Active** (Live Vouchers & Reversals) |
| | General Ledger | `/accounting/general-ledger` | **Active** (Live Statement Generation) |
| | Trial Balance | `/accounting/trial-balance` | **Active** (Live Equilibrium Verification) |
| | Fiscal Years & Periods | `/accounting/periods` | **Active** (Live Period Locking Controls) |
| | AR / AP Subledgers | `/accounting/subledgers` | **Active** (Live Control Account Balances) |
| **System & Governance** | Platform Status | `/system/status` | **Active** (LAN & Service Health) |
| | Display & Context | `/system/preferences` | **Active** (Theme & Display Settings) |

### 3.2 Billing Workspace (`/billing/...`)
When `Billing` is active, the sidebar displays the 15 planned commercial operations entries:

| Section | Menu Item | Route | Status |
| :--- | :--- | :--- | :--- |
| **POS & Desk Operations** | Billing Dashboard | `/billing/dashboard` | **Phase 3 Blueprint** |
| | POS / New Bill | `/billing/pos` | **Phase 3 Architecture** (Coming Soon) |
| | Cash Closing | `/billing/cash-closing` | **Phase 3 Architecture** (Coming Soon) |
| **Sales & Parties** | Sales | `/billing/sales` | **Phase 3 Architecture** (Coming Soon) |
| | Sales Returns | `/billing/sales-returns` | **Phase 3 Architecture** (Coming Soon) |
| | Customers | `/billing/customers` | **Phase 3 Architecture** (Coming Soon) |
| | Customer Payments | `/billing/customer-payments` | **Phase 3 Architecture** (Coming Soon) |
| **Catalog & Inventory** | Products | `/billing/products` | **Phase 3 Architecture** (Coming Soon) |
| | Inventory | `/billing/inventory` | **Phase 3 Architecture** (Coming Soon) |
| | Stock Management | `/billing/stock-management` | **Phase 3 Architecture** (Coming Soon) |
| **Procurement & Costs** | Purchases | `/billing/purchases` | **Phase 3 Architecture** (Coming Soon) |
| | Suppliers | `/billing/suppliers` | **Phase 3 Architecture** (Coming Soon) |
| | Expenses | `/billing/expenses` | **Phase 3 Architecture** (Coming Soon) |
| **Reports & Settings** | Billing Reports | `/billing/reports` | **Phase 3 Architecture** (Coming Soon) |
| | Billing Settings | `/billing/settings` | **Phase 3 Architecture** (Coming Soon) |
| **System & Governance** | Platform Status | `/system/status` | **Active** (LAN & Service Health) |
| | Display & Context | `/system/preferences` | **Active** (Theme & Display Settings) |

---

## 4. Route Changes & Route Synchronization

### 4.1 Route Table Updates in `app.routes.ts`
- Added `/billing` redirecting to `/billing/dashboard`.
- Registered `BillingDashboardComponent` for `/billing/dashboard`.
- Registered `BillingFeaturePlaceholderComponent` for all 14 individual commercial operation routes, passing feature metadata (`title`, `category`, `icon`, `description`, `scope`) via Angular Route `data`.

### 4.2 Two-Way Route & Switcher Synchronization
In `shell.component.ts`:
- An Angular router `NavigationEnd` subscription checks `event.urlAfterRedirects || event.url`.
- If the URL starts with `/billing`, `currentModule` is set to `'billing'`.
- If the URL starts with `/accounting`, `currentModule` is set to `'accounting'`.
- If the user directly types or navigates to `/accounting/trial-balance`, the header switcher automatically highlights `[ Accounting ]` and shows the Accounting menu.
- If the user refreshes their browser, the URL determines the workspace immediately upon initialization.
- If the operator clicks `[ Billing ]`, the router navigates to `/billing/dashboard`. If they click `[ Accounting ]`, it navigates to `/accounting/dashboard`.

---

## 5. Files Changed

| File | Change Type | Description |
| :--- | :--- | :--- |
| `01-SRC/Frontend/src/app/layouts/shell/shell.component.ts` | Modified | Added `currentModule` signal, reactive route sync, module switcher methods, and dynamic `navSections` computation. |
| `01-SRC/Frontend/src/app/layouts/shell/shell.component.html` | Modified | Updated header to 3-column layout with centered segmented switcher, plus workspace badge in sidebar. |
| `01-SRC/Frontend/src/app/layouts/shell/shell.component.scss` | Modified | Styled 3-column header, segmented switcher tab styles, focus states, and mobile responsiveness. |
| `01-SRC/Frontend/src/app/layouts/shell/shell.component.spec.ts` | Modified | Added unit test suite verifying tab switching, sidebar menu isolation, and accessibility attributes. |
| `01-SRC/Frontend/src/app/app.routes.ts` | Modified | Added 15 Billing routes and configured metadata for placeholder screens. |
| `01-SRC/Frontend/src/app/features/billing/billing-dashboard.component.ts` | Created | Billing overview component summarizing planned commercial capabilities. |
| `01-SRC/Frontend/src/app/features/billing/billing-dashboard.component.html` | Created | Billing overview template with Phase 3 status banner. |
| `01-SRC/Frontend/src/app/features/billing/billing-dashboard.component.scss` | Created | Billing overview styles using MAGIC design tokens. |
| `01-SRC/Frontend/src/app/features/billing/billing-feature-placeholder.component.ts` | Created | Reusable feature placeholder extracting route data. |
| `01-SRC/Frontend/src/app/features/billing/billing-feature-placeholder.component.html` | Created | Clean coming soon card with planned specifications checklist. |
| `01-SRC/Frontend/src/app/features/billing/billing-feature-placeholder.component.scss` | Created | High-contrast placeholder styling. |

---

## 6. Verification & Test Results

### 6.1 Frontend Production Build
```bash
npm run build --prefix "01-SRC/Frontend"
```
- **Exit Code**: `0`
- **Result**: Initial total `531.55 kB` (Transfer size `120.61 kB`). Zero errors, zero warnings.

### 6.2 Frontend Unit Tests (Vitest)
```bash
npm test --prefix "01-SRC/Frontend" -- --watch=false
```
- **Exit Code**: `0`
- **Result**: 11 Test Files Passed (100%), 18 Tests Passed (100%).
- **Key Test Verified**: `renders top-centre module switcher and toggles navigation between Billing and Accounting` passed in 128 ms.

### 6.3 Backend Solution Tests (.NET 10)
```bash
dotnet test Magic.sln
```
- **Exit Code**: `0`
- **Result**: 71 Tests Passed (36 Domain, 4 Architecture, 31 Integration). Zero regressions.

---

## 7. Known Limitations & Architectural Boundaries

1. **Billing Backend**: In accordance with the prompt instructions, no mock backend endpoints, fake transactions, or fake invoice numbers were invented. Billing features display architectural specifications and are scheduled for implementation in Phase 3.
2. **Security Boundary (`OPEN-003`)**: Module switching is strictly navigational and does not bypass or alter authentication checks. Financial mutations remain fail-closed.
