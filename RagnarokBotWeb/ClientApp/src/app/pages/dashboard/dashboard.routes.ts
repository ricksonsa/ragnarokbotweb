import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { ChatComponent } from './chat/chat.component';
import { ConsoleComponent } from './console/console.component';
import { LogsComponent } from './logs/logs.component';
import { PaymentsComponent } from './payments/payments.component';
import { PaymentSuccessComponent } from './payments/payment-success/payment-success.component';
import { PaymentCancelComponent } from './payments/payment-cancel/payment-cancel.component';

export const DASHBOARD_ROUTES: Routes = [
  { path: 'home', component: HomeComponent },
  { path: 'chat', component: ChatComponent },
  { path: 'console', component: ConsoleComponent },
  { path: 'logs', component: LogsComponent },
  { path: 'payments', component: PaymentsComponent },
  { path: 'payment-success', component: PaymentSuccessComponent },
  { path: 'payment-canceled', component: PaymentCancelComponent },
];
