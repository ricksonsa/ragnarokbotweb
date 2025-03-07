import { Routes } from '@angular/router';
import { BankComponent } from './bank/bank.component';
import { ItemsComponent } from './items/items.component';
import { PackagesComponent } from './packages/packages.component';
import { OrdersComponent } from './orders/orders.component';
import { PackageResolver } from './packages/resolvers/package.resolver';
import { PackageComponent } from './packages/package/package.component';

export const SHOP_ROUTES: Routes = [
  { path: 'bank', component: BankComponent },
  { path: 'items', component: ItemsComponent },
  { path: 'packages', component: PackagesComponent, pathMatch: 'full' },
  { path: 'packages/:id', component: PackageComponent, resolve: { package: PackageResolver } },
  { path: 'orders', component: OrdersComponent },
];
