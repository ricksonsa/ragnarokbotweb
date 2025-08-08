import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { Page } from '../core/pagination/pager';
import { WarzoneDto } from '../models/warzone.dto';

@Injectable({
    providedIn: 'root'
})
export class WarzoneService {
    constructor(private readonly http: HttpClient) { }

    getWarzones(pageSize: number, pageNumber: number, filter: string | null = null) {
        var url = `${environment.apiUrl}/api/warzones?pageSize=${pageSize}&pageNumber=${pageNumber}`;
        if (filter) url += `&filter=${filter}`;
        return this.http.get<Page<WarzoneDto>>(url);
    }

    getByWarzoneId(id: number) {
        return this.http.get<WarzoneDto>(`${environment.apiUrl}/api/warzones/${id}`);
    }

    getRunningWarzone() {
        return this.http.get<WarzoneDto>(`${environment.apiUrl}/api/warzones/running`);
    }

    deleteWarzone(id: number) {
        return this.http.delete<WarzoneDto>(`${environment.apiUrl}/api/warzones/${id}`);
    }

    openWarzone() {
        return this.http.patch<WarzoneDto>(`${environment.apiUrl}/api/warzones/open?force=true`, null);
    }

    closeWarzone() {
        return this.http.patch<WarzoneDto>(`${environment.apiUrl}/api/warzones/close`, null);
    }

    saveWarzone(warzone: WarzoneDto) {
        if (warzone.id) {
            return this.http.put<WarzoneDto>(`${environment.apiUrl}/api/warzones/${warzone.id}`, warzone);
        } else {
            return this.http.post<WarzoneDto>(`${environment.apiUrl}/api/warzones`, warzone);
        }
    }

}
