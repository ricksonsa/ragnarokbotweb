import { Injectable } from '@angular/core';
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { catchError, Observable, of, throwError } from 'rxjs';
import { PackageService } from '../../../../services/package.service';
import { EventManager, EventWithContent } from '../../../../services/event-manager.service';
import { Alert } from '../../../../models/alert';

@Injectable({
    providedIn: 'root',
})
export class PackageResolver implements Resolve<any> {
    constructor(
        private readonly packageService: PackageService,
        private readonly eventManager: EventManager) { }

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<any> {
        const id = route.paramMap.get('id');
        if (typeof id == 'number') {
            return this.packageService.getByPackageId(+id!)
                .pipe(catchError((err) => {
                    alert('back')
                    window.history.back();
                    setTimeout(() => this.eventManager.broadcast(new EventWithContent('alert', new Alert('Invalid package', 'Package not found', 'error'))), 1000);
                    return throwError(() => JSON.stringify(err));
                }));
        } else {
            if (id !== 'new') {
                    alert('back')
                    setTimeout(() => this.eventManager.broadcast(new EventWithContent('alert', new Alert('Invalid package', 'Package not found', 'error'))), 1000);
                    window.history.back();
            }
        }
        return of(null);


    }
}