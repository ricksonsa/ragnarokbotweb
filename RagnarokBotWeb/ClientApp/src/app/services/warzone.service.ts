import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { WEB_API } from '../api.const';
import { Page } from '../core/pagination/pager';
import { WarzoneDto } from '../models/warzone.dto';

@Injectable({
    providedIn: 'root'
})
export class WarzoneService {
    constructor(private readonly http: HttpClient) { }

    getWarzones(pageSize: number, pageNumber: number, filter: string | null = null) {
        var url = `${WEB_API.baseUrl}/api/warzones?pageSize=${pageSize}&pageNumber=${pageNumber}`;
        if (filter) url += `&filter=${filter}`;
        return this.http.get<Page<WarzoneDto>>(url);
    }

    getByWarzoneId(id: number) {
        return this.http.get<WarzoneDto>(`${WEB_API.baseUrl}/api/warzones/${id}`);
    }

    deleteWarzone(id: number) {
        return this.http.delete<WarzoneDto>(`${WEB_API.baseUrl}/api/warzones/${id}`);
    }

    saveWarzone(warzone: WarzoneDto) {
        if (warzone.id) {
            return this.http.put<WarzoneDto>(`${WEB_API.baseUrl}/api/warzones/${warzone.id}`, warzone);
        } else {
            return this.http.post<WarzoneDto>(`${WEB_API.baseUrl}/api/warzones`, warzone);
        }
    }

}
