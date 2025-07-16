import { Injectable } from '@angular/core';
import { Resolve, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { catchError, Observable, of } from 'rxjs';
import { PackageService } from '../../../../services/package.service';

@Injectable({
    providedIn: 'root',
})
export class WelcomePackResolver implements Resolve<any> {
    constructor(
        private readonly packService: PackageService) { }

    resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<any> {
        return this.packService.getWelcomePack()
            .pipe(catchError((err) => {
                return of(null);
            }));;
    }
}