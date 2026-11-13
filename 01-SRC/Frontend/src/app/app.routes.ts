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
import { ReceivablesManagementComponent } from './features/accounting/receivables/receivables-management.component';
import { CustomerReceiptsComponent } from './features/accounting/customer-receipts/customer-receipts.component';
import { PayablesManagementComponent } from './features/accounting/payables/payables-management.component';
import { SupplierPaymentsComponent } from './features/accounting/supplier-payments/supplier-payments.component';
import { BankAccountsComponent } from './features/accounting/bank-accounts/bank-accounts.component';
import { TreasuryManagementComponent } from './features/accounting/treasury/treasury-management.component';
import { BankReconciliationComponent } from './features/accounting/bank-reconciliation/bank-reconciliation.component';
import { FinancialVouchersComponent } from './features/accounting/financial-vouchers/financial-vouchers.component';
import { FinancialReportsComponent } from './features/accounting/financial-reports/financial-reports.component';
import { ExpensesManagementComponent } from './features/billing/expenses/expenses-management.component';
import { HrManagementComponent } from './features/billing/hr/hr-management.component';
import { PayrollManagementComponent } from './features/billing/payroll/payroll-management.component';
import { MarketingManagementComponent } from './features/billing/marketing/marketing-management.component';
import { OrdersManagementComponent } from './features/billing/orders/orders-management.component';
import { ServiceDeskComponent } from './features/billing/service/service-desk.component';
import { EnterpriseReportsComponent } from './features/billing/reports/enterprise-reports.component';
import { BranchManagementComponent } from './features/billing/branches/branch-management.component';
import { EnterpriseAdministrationComponent } from './features/billing/administration/enterprise-administration.component';
import { AutomationAiComponent } from './features/billing/automation/automation-ai.component';
import { SystemManagementComponent } from './features/billing/system/system-management.component';
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
      {
        path: 'accounting/receivables',
        component: ReceivablesManagementComponent,
        title: 'MAGIC · Customer Receivables (AR)',
      },
      {
        path: 'accounting/customer-receipts',
        component: CustomerReceiptsComponent,
        title: 'MAGIC · Customer Receipts & Allocation',
      },
      {
        path: 'accounting/payables',
        component: PayablesManagementComponent,
        title: 'MAGIC · Supplier Payables (AP)',
      },
      {
        path: 'accounting/supplier-payments',
        component: SupplierPaymentsComponent,
        title: 'MAGIC · Supplier Payments & Settlement',
      },
      {
        path: 'accounting/bank-accounts',
        component: BankAccountsComponent,
        title: 'MAGIC · Bank & Cash Accounts',
      },
      {
        path: 'accounting/treasury',
        component: TreasuryManagementComponent,
        title: 'MAGIC · Treasury Management & Contra',
      },
      {
        path: 'accounting/bank-reconciliation',
        component: BankReconciliationComponent,
        title: 'MAGIC · Bank Reconciliation (BRS)',
      },
      {
        path: 'accounting/financial-vouchers',
        component: FinancialVouchersComponent,
        title: 'MAGIC · Financial Vouchers Explorer',
      },
      {
        path: 'accounting/financial-reports',
        component: FinancialReportsComponent,
        title: 'MAGIC · Financial Reports & Books',
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
        component: CustomerReceiptsComponent,
        title: 'MAGIC · Customer Payments & Receipts',
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
        component: ExpensesManagementComponent,
        title: 'MAGIC · Expenses & Petty Cash',
      },
      {
        path: 'billing/hr',
        component: HrManagementComponent,
        title: 'MAGIC · Human Resources',
      },
      {
        path: 'billing/payroll',
        component: PayrollManagementComponent,
        title: 'MAGIC · Payroll & Salaries',
      },
      {
        path: 'billing/marketing',
        component: MarketingManagementComponent,
        title: 'MAGIC · Marketing & Loyalty',
      },
      {
        path: 'billing/orders',
        component: OrdersManagementComponent,
        title: 'MAGIC · Sales Orders & Fulfillment',
      },
      {
        path: 'billing/service',
        component: ServiceDeskComponent,
        title: 'MAGIC · Service Desk & Repairs',
      },
      {
        path: 'billing/reports',
        component: EnterpriseReportsComponent,
        title: 'MAGIC · Enterprise Reports & Analytics',
      },
      {
        path: 'billing/branches',
        component: BranchManagementComponent,
        title: 'MAGIC · Multi-Branch & Terminals',
      },
      {
        path: 'billing/administration',
        component: EnterpriseAdministrationComponent,
        title: 'MAGIC · Enterprise Administration & Governance',
      },
      {
        path: 'billing/automation',
        component: AutomationAiComponent,
        title: 'MAGIC · Automation & AI Assistant',
      },
      {
        path: 'billing/system',
        component: SystemManagementComponent,
        title: 'MAGIC · System Operations & Backups',
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
