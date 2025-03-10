import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { Observable, of, switchMap } from 'rxjs';
import { AuthenticationService } from '../../services/authentication.service';

@Injectable({
    providedIn: 'root'
})
export class AdminGuard implements CanActivate {

    constructor(private authService: AuthenticationService, private router: Router) { }

    canActivate(
        route: ActivatedRouteSnapshot,
        state: RouterStateSnapshot
    ): boolean | Observable<boolean> | Promise<boolean> {
        return this.authService.account()
            .pipe(switchMap(account => {
                if (account.accessLevel > 0) {
                    return of(true);
                }
                return of(false);
            }));
    }
}
