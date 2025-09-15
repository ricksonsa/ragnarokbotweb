import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { Page } from '../core/pagination/pager';
import { TaxiDto } from '../models/taxi.dto';
import { IdsDto } from '../models/ids-dto';

@Injectable({
    providedIn: 'root'
})
export class TaxiService {
    constructor(private readonly http: HttpClient) { }

    getTaxis(pageSize: number, pageNumber: number, filter: string | null = null) {
        var url = `${environment.apiUrl}/api/taxis?pageSize=${pageSize}&pageNumber=${pageNumber}`;
        if (filter) url += `&filter=${filter}`;
        return this.http.get<Page<TaxiDto>>(url);
    }

    getTaxiIds() {
        var url = `${environment.apiUrl}/api/taxis/ids`;
        return this.http.get<IdsDto[]>(url);
    }

    getById(id: number) {
        return this.http.get<TaxiDto>(`${environment.apiUrl}/api/taxis/${id}`);
    }

    delete(id: number) {
        return this.http.delete(`${environment.apiUrl}/api/taxis/${id}`);
    }

    save(packForm: any) {
        if (packForm.id) {
            return this.http.put<TaxiDto>(`${environment.apiUrl}/api/taxis/${packForm.id}`, packForm);
        } else {
            return this.http.post<TaxiDto>(`${environment.apiUrl}/api/taxis`, packForm);
        }
    }

}
