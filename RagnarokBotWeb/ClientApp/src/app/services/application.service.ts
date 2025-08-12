import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class ApplicationService {
    constructor(private readonly http: HttpClient) { }

    getTimeZones() {
        return this.http.get<{ id: string, displayName: string }[]>(`${environment.apiUrl}/api/application/timezones`);
    }
}