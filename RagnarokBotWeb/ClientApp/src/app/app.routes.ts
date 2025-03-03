import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: '/welcome' },
  { path: 'welcome', loadChildren: () => import('./pages/dashboard/welcome/welcome.routes').then(m => m.WELCOME_ROUTES) },
  { path: 'servers/:id/settings', loadChildren: () => import('./pages/settings/settings.routes').then(m => m.SETTINGS_ROUTES) },
];
