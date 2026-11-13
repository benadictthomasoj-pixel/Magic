import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { LoginPage } from './features/auth/login/login.page';
import { AccessDeniedComponent } from './features/auth/access-denied/access-denied.component';
import { ChangePasswordComponent } from './features/auth/change-password/change-password.component';
import { PlatformSettingsPage } from './features/platform-settings/platform-settings.page';
import { PlatformStatusPage } from './features/platform-status/platform-status.page';
import { AccountingDashboardComponent } from './features/accounting/dashboard/accounting-dashboard.component';
import { ChartOfAccountsComponent } from './features/accounting/chart-of-accounts/chart-of-accounts.component';
import { JournalEntryComponent } from './features/accounting/journal-entry/journal-entry.component';
import { VouchersListComponent } from './features/accounting/vouchers/vouchers-list.component';
import { GeneralLedgerComponent } from './features/accounting/general-ledger/general-ledger.component';
import { TrialBalanceComponent } from './features/accounting/trial-balance/trial-balance.component';
import { AccountingPeriodsComponent } from './features/accounting/periods/accounting-periods.component';
import { SubledgersComponent } from './features/accounting/subledgers/subledgers.component';
import { BillingDashboardComponent } from './features/billing/billing-dashboard.component';
import { BillingFeaturePlaceholderComponent } from './features/billing/billing-feature-placeholder.component';
import { ProductsListComponent } from './features/commercial/products/products-list.component';
import { CategoriesManagementComponent } from './features/commercial/categories/categories-management.component';
import { CustomersListComponent } from './features/commercial/customers/customers-list.component';
import { SuppliersListComponent } from './features/commercial/suppliers/suppliers-list.component';
import { ShellComponent } from './layouts/shell/shell.component';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    component: LoginPage,
    title: 'MAGIC · Universal Business Management ERP',
  },
  {
    path: 'login',
    component: LoginPage,
    title: 'MAGIC · Universal Business Management ERP',
  },
  {
    path: 'access-denied',
    component: AccessDeniedComponent,
    title: 'MAGIC · Access Denied',
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      // Accounting Module
      {
        path: 'accounting',
        redirectTo: 'accounting/dashboard',
        pathMatch: 'full',
      },
      {
        path: 'accounting/dashboard',
        component: AccountingDashboardComponent,
        title: 'MAGIC · Accounting Dashboard',
      },
      {
        path: 'accounting/chart-of-accounts',
        component: ChartOfAccountsComponent,
        title: 'MAGIC · Chart of Accounts',
      },
      {
        path: 'accounting/journal-entry',
        component: JournalEntryComponent,
        title: 'MAGIC · Journal Voucher Entry',
      },
      {
        path: 'accounting/vouchers',
        component: VouchersListComponent,
        title: 'MAGIC · Voucher Registry',
      },
      {
        path: 'accounting/general-ledger',
        component: GeneralLedgerComponent,
        title: 'MAGIC · General Ledger',
      },
      {
        path: 'accounting/trial-balance',
        component: TrialBalanceComponent,
        title: 'MAGIC · Trial Balance',
      },
      {
        path: 'accounting/periods',
        component: AccountingPeriodsComponent,
        title: 'MAGIC · Fiscal Years & Periods',
      },
      {
        path: 'accounting/subledgers',
        component: SubledgersComponent,
        title: 'MAGIC · Subledgers (AR & AP)',
      },

      // Billing Module
      {
        path: 'billing',
        redirectTo: 'billing/dashboard',
        pathMatch: 'full',
      },
      {
        path: 'billing/dashboard',
        component: BillingDashboardComponent,
        title: 'MAGIC · Billing Overview',
      },
      {
        path: 'billing/pos',
        component: BillingFeaturePlaceholderComponent,
        title: 'MAGIC · POS / New Bill',
        data: {
          title: 'POS / New Bill',
          category: 'Retail Desk',
          icon: '💳',
          description: 'High-speed touch and barcode retail terminal with multi-pay split tenders, thermal receipt printing, and real-time inventory deduction.',
          scope: [
            'Instant item search via barcode scanner, SKU, or touchscreen product grid.',
            'Cash, Card, UPI, and Split-tender payment capture.',
            'Automatic double-entry voucher booking to General Ledger Cash & Bank accounts.',
            'Direct thermal ESC/POS receipt generation (80mm / 58mm).',
          ],
        },
      },
      {
        path: 'billing/sales',
        component: BillingFeaturePlaceholderComponent,
        title: 'MAGIC · Sales Invoices',
        data: {
          title: 'Sales & Invoices',
          category: 'Sales Operations',
          icon: '🧾',
          description: 'GST-compliant B2B and wholesale tax invoice generation, quotation conversions, delivery challans, and customer credit ledger integration.',
          scope: [
            'Deterministic CGST, SGST, and IGST tax calculation engines.',
            'Customer credit limit checks and outstanding payment status.',
            'E-Way bill and e-Invoicing integration payload generation.',
            'Direct link to Accounts Receivable control account (11300).',
          ],
        },
      },
      {
        path: 'billing/sales-returns',
        component: BillingFeaturePlaceholderComponent,
        title: 'MAGIC · Sales Returns',
        data: {
          title: 'Sales Returns (Credit Notes)',
          category: 'Sales Operations',
          icon: '↩️',
          description: 'Record returned merchandise against original sales invoices, credit note issuance, and inventory restock booking.',
          scope: [
            'Lookup original invoice line items with quantity limit validations.',
            'Credit note generation with statutory GST tax adjustments.',
            'Automated inventory restock into warehouse locations.',
          ],
        },
      },
      {
        path: 'billing/customers',
        component: CustomersListComponent,
        title: 'MAGIC · Customer Directory',
      },
      {
        path: 'billing/customer-payments',
        component: BillingFeaturePlaceholderComponent,
        title: 'MAGIC · Customer Payments',
        data: {
          title: 'Customer Payments & Receipts',
          category: 'Parties',
          icon: '💵',
          description: 'Record incoming customer payments, invoice allocations, partial remittances, and automatic Receipt Voucher (RV) generation.',
          scope: [
            'Multi-invoice payment distribution (FIFO or manual allocation).',
            'Cheque, NEFT/RTGS, UPI, and Bank transfer reconciliation.',
            'Automatic posting to GL Control Account 11300 and Bank ledgers.',
          ],
        },
      },
      {
        path: 'billing/products',
        component: ProductsListComponent,
        title: 'MAGIC · Products & Pricing Catalog',
      },
      {
        path: 'billing/categories',
        component: CategoriesManagementComponent,
        title: 'MAGIC · Categories & Taxonomy',
      },
      {
        path: 'billing/inventory',
        component: BillingFeaturePlaceholderComponent,
        title: 'MAGIC · Inventory Status',
        data: {
          title: 'Inventory & Warehouse Status',
          category: 'Inventory',
          icon: '📦',
          description: 'Real-time stock valuation across branches, reorder alerts, batch/lot tracking, expiry tracking, and serial number management.',
          scope: [
            'Multi-location stock on-hand, reserved, and available quantities.',
            'Batch-wise manufacturing and expiration date monitoring.',
            'Automated low-stock threshold triggers and reorder warnings.',
          ],
        },
      },
      {
        path: 'billing/stock-management',
        component: BillingFeaturePlaceholderComponent,
        title: 'MAGIC · Stock Management',
        data: {
          title: 'Stock Management & Adjustments',
          category: 'Inventory',
          icon: '🔄',
          description: 'Physical inventory audits, stock reconciliation, breakage/wastage write-offs, and inter-branch inventory transfer orders.',
          scope: [
            'Physical count discrepancy reconciliation worksheets.',
            'Stock adjustment vouchers booking to GL Inventory Adjustment account.',
            'Transfer orders with in-transit tracking between branch locations.',
          ],
        },
      },
      {
        path: 'billing/purchases',
        component: BillingFeaturePlaceholderComponent,
        title: 'MAGIC · Purchases',
        data: {
          title: 'Purchases & Inbound Orders',
          category: 'Procurement',
          icon: '🛒',
          description: 'Purchase orders, Goods Receipt Notes (GRN), vendor invoice booking, landed cost calculations, and input tax credit (ITC) tracking.',
          scope: [
            '3-way matching: Purchase Order vs. Goods Receipt vs. Vendor Bill.',
            'Input CGST/SGST/IGST tax credit booking into GL accounts 1131/1132.',
            'Direct posting to Accounts Payable control account (21100).',
          ],
        },
      },
      {
        path: 'billing/suppliers',
        component: SuppliersListComponent,
        title: 'MAGIC · Suppliers & Vendors',
      },
      {
        path: 'billing/expenses',
        component: BillingFeaturePlaceholderComponent,
        title: 'MAGIC · Expenses',
        data: {
          title: 'Direct Operating Expenses',
          category: 'Expenditure',
          icon: '📉',
          description: 'Daily operational expenses (rent, utilities, courier, store supplies), cash vouchers, and petty cash expense recording.',
          scope: [
            'Quick expense entry with petty cash and bank payment channels.',
            'Mapping to Chart of Accounts expense groups (Class 5 - Expenses).',
            'Vendor receipt attachment and tax deduction capture.',
          ],
        },
      },
      {
        path: 'billing/reports',
        component: BillingFeaturePlaceholderComponent,
        title: 'MAGIC · Billing Reports',
        data: {
          title: 'Billing & Commercial Reports',
          category: 'Analytics',
          icon: '📈',
          description: 'Daily sales summaries, product category velocity, cashier collections, item-wise profitability, and GST GSTR-1 preparation reports.',
          scope: [
            'Daily and monthly sales turnover dashboards.',
            'Cashier performance and tender collection breakdowns.',
            'Item sales velocity, slow-moving items, and stock aging.',
            'GSTR-1 outward supply statutory export tables.',
          ],
        },
      },
      {
        path: 'billing/cash-closing',
        component: BillingFeaturePlaceholderComponent,
        title: 'MAGIC · Cash Closing',
        data: {
          title: 'Cashier Day Closing & Register',
          category: 'Cash Desk',
          icon: '🔒',
          description: 'Cash drawer counting, shift handover, physical denomination audit, variance reconciliation, and daily cash closing lock.',
          scope: [
            'Opening float verification and mid-shift cash withdrawal logging.',
            'Denomination counter for physical cash reconciliation.',
            'Variance calculation (System Expected vs. Actual Count).',
            'Closing journal voucher posting directly to General Ledger.',
          ],
        },
      },
      {
        path: 'billing/settings',
        component: BillingFeaturePlaceholderComponent,
        title: 'MAGIC · Billing Settings',
        data: {
          title: 'Billing & POS Settings',
          category: 'Configuration',
          icon: '⚙️',
          description: 'Invoice numbering series, thermal printer configuration, barcode scanner settings, default tax slabs, and round-off rules.',
          scope: [
            'Branch-wise invoice prefix and sequential gapless numbering rules.',
            'Thermal printer page size (80mm / 58mm) and header/footer templates.',
            'Barcode scanner character delay and prefix/suffix configurations.',
          ],
        },
      },

      // System Foundation
      {
        path: 'system/status',
        component: PlatformStatusPage,
        title: 'MAGIC · Platform Status',
      },
      {
        path: 'system/preferences',
        component: PlatformSettingsPage,
        title: 'MAGIC · Display & Context',
      },
      {
        path: 'system/change-password',
        component: ChangePasswordComponent,
        title: 'MAGIC · Change Password',
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
