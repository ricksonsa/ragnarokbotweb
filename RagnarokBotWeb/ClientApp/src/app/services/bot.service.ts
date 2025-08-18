import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class BotService {
    constructor(private readonly http: HttpClient) { }

    getBots() {
        return this.http.get<{ value: number }>(`${environment.apiUrl}/api/bots`);
    }

    getBotCount() {
        return this.http.get<{ value: number }>(`${environment.apiUrl}/api/bots/count`);
    }
}