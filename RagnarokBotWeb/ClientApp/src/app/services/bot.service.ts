import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class BotService {
    constructor(private readonly http: HttpClient) { }

    getBots() {
        return this.http.get<any[]>(`${environment.apiUrl}/api/bots`);
    }
}