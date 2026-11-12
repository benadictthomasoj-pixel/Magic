import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { LoginPage } from './features/auth/login/login.page';
import { PlatformSettingsPage } from './features/platform-settings/platform-settings.page';
import { PlatformStatusPage } from './features/platform-status/platform-status.page';
import { ShellComponent } from './layouts/shell/shell.component';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginPage,
    title: 'MAGIC · Sign In',
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'system/status' },
      { path: 'system/status', component: PlatformStatusPage, title: 'MAGIC · Platform Status' },
      {
        path: 'system/preferences',
        component: PlatformSettingsPage,
        title: 'MAGIC · Display & Context',
      },
    ],
  },
  { path: '**', redirectTo: 'system/status' },
];
