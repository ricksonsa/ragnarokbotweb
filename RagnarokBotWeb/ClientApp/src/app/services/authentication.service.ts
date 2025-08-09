import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, Observable, of, Subject, switchMap, tap, throwError } from 'rxjs';
import { AuthResponse } from '../models/auth-response';
import { AccountDto } from '../models/account.dto';
import { decodeJwt } from "jose";
import { environment } from '../../environments/environment';


@Injectable({
  providedIn: 'root'
})
export class AuthenticationService {

  private accountSubject: Subject<AccountDto> = new Subject();
  public logoutEvent: Subject<boolean> = new Subject();
  accountValue?: AccountDto;

  constructor(private readonly http: HttpClient) { }

  isTokenExpired = (token: string): boolean => {
    try {
      const decoded = decodeJwt(token);
      if (!decoded.exp) return true;

      const currentTime = Math.floor(Date.now() / 1000);
      return decoded.exp < currentTime;
    } catch (error: any) {
      console.error("Error decoding JWT:", error.message);
      return true;
    }
  };

  authenticate(loginForm: any) {
    localStorage.removeItem('id_token');
    localStorage.removeItem('access_token');
    return this.http.post<AuthResponse>(`${environment.apiUrl}/api/authenticate`, loginForm)
      .pipe(tap(response => {
        localStorage.setItem('id_token', response.idToken!);
        localStorage.setItem('scum_servers', JSON.stringify(response.scumServers));
      }));
  }

  login(serverId: number) {
    return this.http.get<AuthResponse>(`${environment.apiUrl}/api/login?serverId=${serverId}`)
      .pipe(
        tap(response => {
          localStorage.setItem('access_token', response.accessToken!);
        }),
        switchMap((value) => {
          return this.account(true);
        }),
        tap((account) => {
          this.accountSubject.next(account);
        })
      );
  }


  account(force = false, cachedOnly = false) {
    const token = localStorage.getItem('access_token');
    if (force || !token || this.isTokenExpired(token)) {
      if (cachedOnly) return of(null);
      return this.http.get<AccountDto>(`${environment.apiUrl}/api/account`)
        .pipe(
          catchError(err => {
            this.logout();
            return throwError(() => new Error('Oops! Something went wrong. Please try again later.'));
          }),
          tap(response => {
            this.accountValue = response;
            this.accountSubject.next(response);
          })
        );
    }

    if (this.accountValue) setTimeout(() => this.accountSubject.next(this.accountValue!), 400);
    return this.accountSubject.asObservable();
  }

  register(registerForm: any) {
    return this.http.post(`${environment.apiUrl}/api/register`, registerForm);
  }

  update(updateForm: any) {
    return this.http.put(`${environment.apiUrl}/api/account`, updateForm);
  }

  isAuthenticated() {
    var accessToken = localStorage.getItem('access_token');
    return accessToken !== undefined || accessToken !== null;
  }

  logout() {
    localStorage.removeItem('id_token');
    localStorage.removeItem('access_token');
    this.logoutEvent.next(true);
  }

}
