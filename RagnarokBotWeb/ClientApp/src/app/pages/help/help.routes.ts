import { Routes } from '@angular/router';
import { ContactComponent } from './contact/contact.component';

export const EVENTS_ROUTES: Routes = [
  { path: 'contact', component: ContactComponent, pathMatch: 'full' },
];
