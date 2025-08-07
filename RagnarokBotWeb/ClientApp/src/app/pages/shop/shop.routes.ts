import { Routes } from '@angular/router';
import { BankComponent } from './bank/bank.component';
import { ItemsComponent } from './items/items.component';
import { PackagesComponent } from './packages/packages.component';
import { OrdersComponent } from './orders/orders.component';
import { PackageResolver } from './packages/resolvers/package.resolver';
import { PackageComponent } from './packages/package/package.component';
import { ItemComponent } from './items/item/item.component';
import { ItemResolver } from './items/resolvers/item.resolver';
import { AdminGuard } from '../../core/guards/admin.guard';
import { WelcomePackComponent } from './welcome-pack/welcome-pack.component';
import { WelcomePackResolver } from './packages/resolvers/welcome-pack.resolver';
import { UavComponent } from './uav/uav.component';

export const SHOP_ROUTES: Routes = [
  { path: 'bank', component: BankComponent },
  { path: 'items', component: ItemsComponent, canActivate: [AdminGuard] },
  { path: 'items/:id', component: ItemComponent, resolve: { item: ItemResolver }, canActivate: [AdminGuard] },
  { path: 'packages', component: PackagesComponent, pathMatch: 'full' },
  { path: 'packages/:id', component: PackageComponent, resolve: { package: PackageResolver } },
  { path: 'orders', component: OrdersComponent },
  { path: 'uav', component: UavComponent },
  { path: 'welcome-pack', component: WelcomePackComponent, resolve: { package: WelcomePackResolver } },
];
