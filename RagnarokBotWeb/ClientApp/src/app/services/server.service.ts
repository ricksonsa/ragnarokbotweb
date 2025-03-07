import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ScumServer } from '../models/scum-server';
import { WEB_API } from '../api.const';
import { AuthenticationService } from './authentication.service';

@Injectable({
  providedIn: 'root'
})
export class ServerService {

  constructor(private readonly http: HttpClient, private readonly authService: AuthenticationService) { }

  updateFtp(updateForm: any) {
    return this.http.patch<ScumServer>(`${WEB_API.baseUrl}/api/servers/settings/ftp`, updateForm);
  }

}
