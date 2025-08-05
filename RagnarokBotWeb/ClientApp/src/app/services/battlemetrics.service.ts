import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class BattlemetricsService {

    constructor(private readonly http: HttpClient) { }

    getBattlemetricsStatus(id: string) {
        return this.http.get<any>(`https://api.battlemetrics.com/servers/${id}`);
    }
}
