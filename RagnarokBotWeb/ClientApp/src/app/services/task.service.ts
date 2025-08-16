import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { Page } from '../core/pagination/pager';
import { CustomTaskDto } from '../models/custom-task.dto';

@Injectable({
    providedIn: 'root'
})
export class TaskService {
    constructor(private readonly http: HttpClient) { }

    getPage(pageSize: number, pageNumber: number, filter: string | null = null) {
        var url = `${environment.apiUrl}/api/tasks/custom-tasks?pageSize=${pageSize}&pageNumber=${pageNumber}`;
        if (filter) url += `&filter=${filter}`;
        return this.http.get<Page<CustomTaskDto>>(url);
    }

    getById(id: number) {
        return this.http.get<CustomTaskDto>(`${environment.apiUrl}/api/tasks/custom-tasks/${id}`);
    }

    delete(id: number) {
        return this.http.delete(`${environment.apiUrl}/api/tasks/custom-tasks/${id}`);
    }

    save(packForm: any) {
        if (packForm.id) {
            return this.http.put<CustomTaskDto>(`${environment.apiUrl}/api/tasks/custom-tasks/${packForm.id}`, packForm);
        } else {
            return this.http.post<CustomTaskDto>(`${environment.apiUrl}/api/tasks/custom-tasks`, packForm);
        }
    }

}
