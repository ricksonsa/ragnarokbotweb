import { Injectable } from '@angular/core';
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { catchError, Observable, of, throwError } from 'rxjs';
import { EventManager, EventWithContent } from '../../../../services/event-manager.service';
import { Alert } from '../../../../models/alert';
import { isNumeric } from '../../../../core/functions/number.functions';
import { WarzoneService } from '../../../../services/warzone.service';

@Injectable({
    providedIn: 'root',
})
export class WarzoneResolver implements Resolve<any> {
    constructor(
        private readonly warzoneService: WarzoneService,
        private readonly eventManager: EventManager) { }

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<any> {
        const id = route.paramMap.get('id');
        if (isNumeric(id)) {
            return this.warzoneService.getByWarzoneId(+id!)
                .pipe(catchError((err) => {
                    window.history.back();
                    setTimeout(() => this.eventManager.broadcast(new EventWithContent('alert', new Alert('Invalid warzone', 'Warzone not found', 'error'))), 1000);
                    return throwError(() => JSON.stringify(err));
                }));
        } else {
            if (id !== 'new') {
                setTimeout(() => this.eventManager.broadcast(new EventWithContent('alert', new Alert('Invalid warzone', 'Warzone not found', 'error'))), 1000);
                window.history.back();
            }
        }
        return of(null);


    }
}