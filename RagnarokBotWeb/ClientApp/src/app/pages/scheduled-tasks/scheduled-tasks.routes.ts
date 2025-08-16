import { Routes } from '@angular/router';
import { CustomTasksComponent } from './custom-tasks/custom-tasks.component';
import { DefaultTasksComponent } from './default-tasks/default-tasks.component';
import { CustomTaskComponent } from './custom-tasks/custom-task/custom-task.component';
import { CustomTaskResolver } from './resolvers/custom-task.resolver';

export const SCHEDULEDTASKS_ROUTES: Routes = [
  { path: 'custom-tasks', component: CustomTasksComponent },
  { path: 'custom-tasks/:id', component: CustomTaskComponent, resolve: { customTask: CustomTaskResolver } },
  { path: 'default', component: DefaultTasksComponent },
];
