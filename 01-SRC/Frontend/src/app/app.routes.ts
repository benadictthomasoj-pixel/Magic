import { Routes } from '@angular/router';
import { LoginPage } from './features/auth/login/login.page';
import { PlatformSettingsPage } from './features/platform-settings/platform-settings.page';
import { PlatformStatusPage } from './features/platform-status/platform-status.page';
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
      { path: 'system/status', component: PlatformStatusPage, title: 'MAGIC · Platform Status' },
      {
        path: 'system/preferences',
        component: PlatformSettingsPage,
        title: 'MAGIC · Display & Context',
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
