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
import { PosCheckoutComponent } from './features/sales/pos/pos-checkout.component';
import { SalesInvoicesComponent } from './features/sales/invoices/sales-invoices.component';
import { SalesReturnsComponent } from './features/sales/returns/sales-returns.component';
import { CashManagementComponent } from './features/sales/cash-management/cash-management.component';
import { SalesReportsComponent } from './features/sales/reports/sales-reports.component';
import { ProductsListComponent } from './features/commercial/products/products-list.component';
import { CategoriesManagementComponent } from './features/commercial/categories/categories-management.component';
import { CustomersListComponent } from './features/commercial/customers/customers-list.component';
import { SuppliersListComponent } from './features/commercial/suppliers/suppliers-list.component';
import { WarehousesListComponent } from './features/inventory/warehouses/warehouses-list.component';
import { InventoryStatusComponent } from './features/inventory/stock/inventory-status.component';
import { StockAdjustmentsComponent } from './features/inventory/adjustments/stock-adjustments.component';
import { StockCountsComponent } from './features/inventory/counts/stock-counts.component';
import { StockTransfersComponent } from './features/inventory/transfers/stock-transfers.component';
import { ReorderManagementComponent } from './features/inventory/reorder/reorder-management.component';
import { PurchaseOrdersComponent } from './features/procurement/purchase-orders/purchase-orders.component';
import { GoodsReceiptsComponent } from './features/procurement/goods-receipts/goods-receipts.component';
import { PurchaseReturnsComponent } from './features/procurement/purchase-returns/purchase-returns.component';
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
        component: PosCheckoutComponent,
        title: 'MAGIC · Retail POS Terminal',
      },
      {
        path: 'billing/sales',
        component: SalesInvoicesComponent,
        title: 'MAGIC · Sales Invoices & Billing Register',
      },
      {
        path: 'billing/sales-returns',
        component: SalesReturnsComponent,
        title: 'MAGIC · Sales Returns & Credit Notes',
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
        path: 'billing/warehouses',
        component: WarehousesListComponent,
        title: 'MAGIC · Warehouse Management',
      },
      {
        path: 'billing/inventory',
        component: InventoryStatusComponent,
        title: 'MAGIC · Inventory Status & Ledger',
      },
      {
        path: 'billing/stock-adjustments',
        component: StockAdjustmentsComponent,
        title: 'MAGIC · Stock Adjustments',
      },
      {
        path: 'billing/stock-counts',
        component: StockCountsComponent,
        title: 'MAGIC · Stock Counting & Audits',
      },
      {
        path: 'billing/stock-transfers',
        component: StockTransfersComponent,
        title: 'MAGIC · Warehouse Transfers',
      },
      {
        path: 'billing/reorder-management',
        component: ReorderManagementComponent,
        title: 'MAGIC · Reorder Rules & Suggestions',
      },
      {
        path: 'billing/purchase-orders',
        component: PurchaseOrdersComponent,
        title: 'MAGIC · Purchase Orders',
      },
      {
        path: 'billing/goods-receipts',
        component: GoodsReceiptsComponent,
        title: 'MAGIC · Goods Receipt Notes (GRN)',
      },
      {
        path: 'billing/purchase-returns',
        component: PurchaseReturnsComponent,
        title: 'MAGIC · Purchase Returns',
      },
      {
        path: 'billing/stock-management',
        redirectTo: 'billing/stock-adjustments',
        pathMatch: 'full',
      },
      {
        path: 'billing/purchases',
        redirectTo: 'billing/purchase-orders',
        pathMatch: 'full',
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
        component: SalesReportsComponent,
        title: 'MAGIC · Sales Reports & Analytics',
      },
      {
        path: 'billing/cash-closing',
        component: CashManagementComponent,
        title: 'MAGIC · Cashier Shift & Cash Closing',
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
