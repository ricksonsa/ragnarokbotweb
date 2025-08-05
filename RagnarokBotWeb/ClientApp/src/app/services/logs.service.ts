import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { WEB_API } from '../api.const';
import { GameKillData } from '../models/game-kill-data';
import { LockpickLog } from '../models/lockpick';
import { GenericLogValue } from '../models/generic-log-value';

@Injectable({
    providedIn: 'root'
})
export class LogService {

    constructor(private readonly http: HttpClient) { }

    getKillsLog(from: Date, to: Date) {
        let url = `${WEB_API.baseUrl}/api/logs/kills?from=${from.toUTCString()}&to=${to.toUTCString()}`;
        return this.http.get<GameKillData[]>(url);
    }

    getLockpickLog(from: Date, to: Date) {
        let url = `${WEB_API.baseUrl}/api/logs/lockpicks?from=${from.toUTCString()}&to=${to.toUTCString()}`;
        return this.http.get<LockpickLog[]>(url);
    }

    getEconomy(from: Date, to: Date) {
        let url = `${WEB_API.baseUrl}/api/logs/economy?from=${from.toUTCString()}&to=${to.toUTCString()}`;
        return this.http.get<GenericLogValue[]>(url);
    }

    getVehicles(from: Date, to: Date) {
        let url = `${WEB_API.baseUrl}/api/logs/vehicles?from=${from.toUTCString()}&to=${to.toUTCString()}`;
        return this.http.get<GenericLogValue[]>(url);
    }

    getLogins(from: Date, to: Date) {
        let url = `${WEB_API.baseUrl}/api/logs/login?from=${from.toUTCString()}&to=${to.toUTCString()}`;
        return this.http.get<GenericLogValue[]>(url);
    }

    getBuriedChests(from: Date, to: Date) {
        let url = `${WEB_API.baseUrl}/api/logs/buried-chests?from=${from.toUTCString()}&to=${to.toUTCString()}`;
        return this.http.get<GenericLogValue[]>(url);
    }

    getViolations(from: Date, to: Date) {
        let url = `${WEB_API.baseUrl}/api/logs/violations?from=${from.toUTCString()}&to=${to.toUTCString()}`;
        return this.http.get<GenericLogValue[]>(url);
    }
}
