import { Routes } from '@angular/router';
import { ProfileComponent } from './pages/profile/profile.component';

export const routes: Routes = [
  // { path: '', pathMatch: 'full' },
  { path: 'profile', component: ProfileComponent },
  { path: 'servers/:id/dashboard', loadChildren: () => import('./pages/dashboard/dashboard.routes').then(m => m.DASHBOARD_ROUTES) },
  { path: 'servers/:id/settings', loadChildren: () => import('./pages/settings/settings.routes').then(m => m.SETTINGS_ROUTES) },
  { path: 'servers/:id/management', loadChildren: () => import('./pages/management/management.routes').then(m => m.MANAGEMENT_ROUTES) },
  { path: 'servers/:id/shop', loadChildren: () => import('./pages/shop/shop.routes').then(m => m.SHOP_ROUTES) },
  { path: 'servers/:id/events', loadChildren: () => import('./pages/events/events.routes').then(m => m.EVENTS_ROUTES) },
  { path: 'servers/:id/scheduled-tasks', loadChildren: () => import('./pages/scheduled-tasks/scheduled-tasks.routes').then(m => m.SCHEDULEDTASKS_ROUTES) },
];
