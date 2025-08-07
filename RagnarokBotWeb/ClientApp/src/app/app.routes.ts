import { Routes } from '@angular/router';
import { ProfileComponent } from './pages/profile/profile.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard/home' },
  { path: 'profile', component: ProfileComponent },
  { path: 'dashboard', loadChildren: () => import('./pages/dashboard/dashboard.routes').then(m => m.DASHBOARD_ROUTES) },
  { path: 'settings', loadChildren: () => import('./pages/settings/settings.routes').then(m => m.SETTINGS_ROUTES) },
  { path: 'management', loadChildren: () => import('./pages/management/management.routes').then(m => m.MANAGEMENT_ROUTES) },
  { path: 'shop', loadChildren: () => import('./pages/shop/shop.routes').then(m => m.SHOP_ROUTES) },
  { path: 'events', loadChildren: () => import('./pages/events/events.routes').then(m => m.EVENTS_ROUTES) },
  { path: 'scheduled-tasks', loadChildren: () => import('./pages/scheduled-tasks/scheduled-tasks.routes').then(m => m.SCHEDULEDTASKS_ROUTES) },
];
