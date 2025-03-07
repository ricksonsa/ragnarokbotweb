import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { WEB_API } from '../../api.const';


@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor() {}

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const serverApiUrl = WEB_API.baseUrl;

    if (!request.url || (request.url.startsWith('http') && !(serverApiUrl && request.url.startsWith(serverApiUrl)))) {
      return next.handle(request);
    }

    const idToken: string | null = localStorage.getItem("id_token");
    const accessToken: string | null = localStorage.getItem("access_token");
    if (idToken || accessToken) {
      request = request.clone({
        setHeaders: {
          Authorization: `Bearer ${accessToken ? accessToken : idToken}`,
        },
      });
    }
    return next.handle(request);
  }
}
