import { Routes } from '@angular/router';
import { CustomTasksComponent } from './custom-tasks/custom-tasks.component';
import { DefaultTasksComponent } from './default-tasks/default-tasks.component';

export const SCHEDULEDTASKS_ROUTES: Routes = [
  { path: 'custom', component: CustomTasksComponent },
  { path: 'default', component: DefaultTasksComponent },
];
