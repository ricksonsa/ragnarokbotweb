import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { ChatComponent } from './chat/chat.component';
import { ConsoleComponent } from './console/console.component';
import { LogsComponent } from './logs/logs.component';

export const DASHBOARD_ROUTES: Routes = [
  { path: 'home', component: HomeComponent },
  { path: 'chat', component: ChatComponent },
  { path: 'console', component: ConsoleComponent },
  { path: 'logs', component: LogsComponent },
];
