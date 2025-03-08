import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { PackageDto } from '../models/package.dto';
import { WEB_API } from '../api.const';
import { Page } from '../core/pagination/pager';

@Injectable({
  providedIn: 'root'
})
export class PackageService {

  constructor(private readonly http: HttpClient) { }

  getPackages(pageSize: number, pageNumber: number, filter: string | null = null) {
    var url = `${WEB_API.baseUrl}/api/packs?pageSize=${pageSize}&pageNumber=${pageNumber}`;
    if (filter) url += `&filter=${filter}`;
    return this.http.get<Page<PackageDto>>(url);
  }

  getByPackageId(id: number) {
    return this.http.get<PackageDto>(`${WEB_API.baseUrl}/api/packs/${id}`);
  }

  deletePackage(id: number) {
    return this.http.delete<PackageDto>(`${WEB_API.baseUrl}/api/packs/${id}`);
  }

  savePackage(packForm: any) {
    if (packForm.id) {
      return this.http.put<PackageDto>(`${WEB_API.baseUrl}/api/packs/${packForm.id}`, packForm);
    } else {
      return this.http.post<PackageDto>(`${WEB_API.baseUrl}/api/packs`, packForm);
    }
  }

}
