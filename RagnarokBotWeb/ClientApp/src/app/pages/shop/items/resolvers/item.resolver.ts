import { Injectable } from '@angular/core';
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { catchError, Observable, of, throwError } from 'rxjs';
import { EventManager, EventWithContent } from '../../../../services/event-manager.service';
import { Alert } from '../../../../models/alert';
import { ItemService } from '../../../../services/item.service';
import { isNumeric } from '../../../../core/functions/number.functions';

@Injectable({
    providedIn: 'root',
})
export class ItemResolver implements Resolve<any> {
    constructor(
        private readonly itemService: ItemService,
        private readonly eventManager: EventManager) { }

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<any> {
        const id = route.paramMap.get('id');
        if (isNumeric(id)) {
            return this.itemService.getItemById(+id!)
                .pipe(catchError((err) => {
                    window.history.back();
                    setTimeout(() => this.eventManager.broadcast(new EventWithContent('alert', new Alert('Invalid package', 'Item not found', 'error'))), 1000);
                    return throwError(() => JSON.stringify(err));
                }));
        } else {
            if (id !== 'new') {
                setTimeout(() => this.eventManager.broadcast(new EventWithContent('alert', new Alert('Invalid package', 'Item not found', 'error'))), 1000);
                window.history.back();
            }
        }
        return of(null);


    }
}