# MAGIC ERP — PHASE 2, STAGE 4 UI IMPLEMENTATION
## Angular Architecture, Screen Design & User Experience Workflows

| Attribute | Value |
| :--- | :--- |
| **Framework** | Angular 22 / TypeScript 5.8 / Angular CDK 22 |
| **Component Architecture** | Standalone Components with Signals & ChangeDetectionStrategy.OnPush |
| **Design System** | Custom High-Contrast ERP System (Navy `#0f172a`, Gold `#d97706`, Pure White `#ffffff`) |
| **Responsive Grid** | Desktop-first ERP layout with responsive mobile sliding drawer navigation |

---

## 1. Application Layout & Navigation

### 1.1 Header Context Display
The primary header (`shell.component.html`) provides persistent enterprise context:
- **Logo & Wordmark**: Official MAGIC emblem and typography.
- **Enterprise Badge**: Displays active legal entity (`Universal Enterprises Ltd`), operating branch (`Headquarters (MAIN)`), and active accounting year (`FY 2026-27`).
- **Strict Online LAN Indicator**: Integrates with `ConnectivityMonitorService` to indicate real-time local network reachability (`Online LAN`, `LAN Degraded`, `LAN Offline`).
- **Theme Cycler**: Switches between Light, Dark, and System color preferences.
- **Sign Out**: Clears session and returns to login.

### 1.2 Sidebar Navigation Grouping
The sidebar partitions application modules into clear visual sections:
1. **Financial Accounting**:
   - `Overview / KPI` (`/accounting/dashboard`)
   - `Chart of Accounts` (`/accounting/chart-of-accounts`)
   - `Journal Entry` (`/accounting/journal-entry`)
   - `Voucher Registry` (`/accounting/vouchers`)
   - `General Ledger` (`/accounting/general-ledger`)
   - `Trial Balance` (`/accounting/trial-balance`)
   - `Accounting Periods` (`/accounting/periods`)
   - `AR / AP Subledgers` (`/accounting/subledgers`)
2. **Upcoming Modules**:
   - `Financial Statements` (Soon badge, disabled)
   - `Sales & Invoicing` (Phase 3 badge, disabled)
   - `Purchases & AP` (Phase 3 badge, disabled)
   - `Point of Sale (POS)` (Soon badge, disabled)
3. **System & Governance**:
   - `Platform Status` (`/system/status`)
   - `Display & Context` (`/system/preferences`)

---

## 2. Screen Specifications & Features

### 2.1 Overview / KPI Dashboard (`accounting-dashboard.component`)
- **Key Metrics**: Total Debit Movements, Total Credit Movements, Posted Vouchers Count, Draft Vouchers Count.
- **Fiscal Governance Card**: Displays current active fiscal year and period status (Open, Locked) with quick link to period controls.
- **Double-Entry Status Banner**: Dynamically evaluates $\sum Dr = \sum Cr$ across the database and displays equilibrium confirmation (`System is in Balance`).
- **Recent Vouchers Feed**: Tabular stream of recent transactions with status badges and amount formatting.

### 2.2 Chart of Accounts (`chart-of-accounts.component`)
- **Category Tabs**: Filter by Root Class (All, Assets, Liabilities, Equity, Revenue, Expenses).
- **Two-Column Explorer**: Left column displays hierarchical Account Groups with badge count of detail accounts; right column displays tabular accounts under the selected group.
- **Detail Account Management**: New Account modal dialog allowing creation of detail accounts under any group, validation of unique code rules, and updating account display names.

### 2.3 Journal Voucher Entry (`journal-entry.component`)
- **Dynamic Multi-line Table**: FormArray-based line editor with account picker, narration, debit amount, and credit amount.
- **Line Invariants**: Entering a debit automatically clears credit on that line and vice versa.
- **Live Equilibrium Status Bar**: Real-time summary showing total Dr, total Cr, difference, and a green balanced badge when $Dr = Cr$.
- **Validation Engine**: "Validate Invariants" button queries backend domain validator (`/vouchers/validate`) to verify period lock state, account active states, and double-entry balance prior to submission.
- **Post to GL**: Single-click posting directly to PostgreSQL with immediate routing to Voucher Registry.

### 2.4 Voucher Registry (`vouchers-list.component`)
- **Filter Toolbar**: Text search (voucher #, narration, reference #) and status filter (Draft, Posted, Reversed).
- **Interactive Data Table**: Chronological list of vouchers with currency formatting in Indian Rupee format (`₹`).
- **Flyout Detail Drawer**: Sliding drawer revealing all journal distribution lines, audit metadata, and one-click "Reverse This Voucher" action with reason prompt.

### 2.5 General Ledger Statement (`general-ledger.component`)
- **Account & Date Selection**: Searchable account dropdown and from/to date pickers.
- **Summary Metrics**: Opening Balance brought forward, Total Debits, Total Credits, and Net Closing Balance.
- **Statement Grid**: Date, Voucher Number, Narration, Debit, Credit, and running balance column calculating cumulative position after each transaction.

### 2.6 Trial Balance (`trial-balance.component`)
- **As-of Date Picker**: Historical and current trial balance execution.
- **Equilibrium Banner**: Visual audit badge certifying whether the ledger is in equilibrium.
- **Account Class Aggregations**: Color-coded classification pills (Asset, Liability, Equity, Income, Expense) with debit and credit totals.

### 2.7 Fiscal Governance (`accounting-periods.component`)
- **Fiscal Year Selector**: Left panel showing financial years (FY 2026-27).
- **12 Calendar Periods Grid**: Listing of periods (M01-APR through M12-MAR) with states (`Open`, `Closing`, `Locked`).
- **Lifecycle Action Buttons**: Lock period, Close period, and Reopen period with confirmation dialogs.

### 2.8 Subledgers (`subledgers.component`)
- **AR / AP Dual Tabs**: Switches between Customer Receivables (`11300`) and Vendor Payables (`21100`).
- **Architectural Scope Notice**: Informs the operator that ledger control account totals are live from PostgreSQL, while individual customer/vendor open invoice tracking will link directly during Phase 3 Sales & Purchasing modules.
