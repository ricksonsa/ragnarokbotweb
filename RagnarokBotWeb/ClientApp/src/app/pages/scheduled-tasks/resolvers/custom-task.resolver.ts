import { Injectable } from '@angular/core';
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { catchError, Observable, of, throwError } from 'rxjs';
import { TaskService } from '../../../services/task.service';
import { EventManager, EventWithContent } from '../../../services/event-manager.service';
import { isNumeric } from '../../../core/functions/number.functions';
import { Alert } from '../../../models/alert';

@Injectable({
    providedIn: 'root',
})
export class CustomTaskResolver implements Resolve<any> {
    constructor(
        private readonly taskService: TaskService,
        private readonly eventManager: EventManager) { }

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<any> {
        const id = route.paramMap.get('id');
        if (isNumeric(id)) {
            return this.taskService.getById(+id!)
                .pipe(catchError((err) => {
                    window.history.back();
                    setTimeout(() => this.eventManager.broadcast(new EventWithContent('alert', new Alert('Invalid custom-task', 'CustomTask not found', 'error'))), 1000);
                    return throwError(() => JSON.stringify(err));
                }));
        } else {
            if (id !== 'new') {
                setTimeout(() => this.eventManager.broadcast(new EventWithContent('alert', new Alert('Invalid custom-task', 'CustomTask not found', 'error'))), 1000);
                window.history.back();
            }
        }
        return of(null);


    }
}