import { Routes } from '@angular/router';
import { WarzoneComponent } from './warzones/warzone/warzone.component';
import { WarzonesComponent } from './warzones/warzones.component';

export const EVENTS_ROUTES: Routes = [
  { path: 'warzones', component: WarzonesComponent, pathMatch: 'full' },
  { path: 'warzones/:id', component: WarzoneComponent },
];
