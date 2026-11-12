import { Routes } from '@angular/router';
import { LoginPage } from './features/auth/login/login.page';
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
    path: '',
    component: ShellComponent,
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
    ],
  },
  { path: '**', redirectTo: '' },
];
