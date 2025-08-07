import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { PackageDto } from '../models/package.dto';
import { environment } from '../../environments/environment';
import { Page } from '../core/pagination/pager';

@Injectable({
  providedIn: 'root'
})
export class PackageService {
  constructor(private readonly http: HttpClient) { }

  getPackages(pageSize: number, pageNumber: number, filter: string | null = null) {
    var url = `${environment.apiUrl}/api/packs?pageSize=${pageSize}&pageNumber=${pageNumber}`;
    if (filter) url += `&filter=${filter}`;
    return this.http.get<Page<PackageDto>>(url);
  }

  getWelcomePack() {
    return this.http.get<PackageDto>(`${environment.apiUrl}/api/packs/welcome-pack`);
  }

  getByPackageId(id: number) {
    return this.http.get<PackageDto>(`${environment.apiUrl}/api/packs/${id}`);
  }

  deletePackage(id: number) {
    return this.http.delete(`${environment.apiUrl}/api/packs/${id}`);
  }

  savePackage(packForm: any) {
    if (packForm.id) {
      return this.http.put<PackageDto>(`${environment.apiUrl}/api/packs/${packForm.id}`, packForm);
    } else {
      return this.http.post<PackageDto>(`${environment.apiUrl}/api/packs`, packForm);
    }
  }

}
