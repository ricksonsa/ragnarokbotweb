import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { BotState } from '../models/bot-state.dto';

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

    getBotTable() {
        return this.http.get<BotState[]>(`${environment.apiUrl}/api/bots/table`);
    }

    reconnect(botId: string) {
        return this.http.post<any>(`${environment.apiUrl}/api/bots/${botId}/reconnect`, null);
    }

    send(botId: string, command: any) {
        return this.http.post<any>(`${environment.apiUrl}/api/bots/${botId}/command`, command);
    }

    sendCommand(command: any) {
        return this.http.post<any>(`${environment.apiUrl}/api/bots/command`, command);
    }
}