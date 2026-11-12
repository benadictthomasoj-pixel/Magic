import { Routes } from '@angular/router';
import { PlatformSettingsPage } from './features/platform-settings/platform-settings.page';
import { PlatformStatusPage } from './features/platform-status/platform-status.page';
import { ShellComponent } from './layouts/shell/shell.component';

export const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
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
