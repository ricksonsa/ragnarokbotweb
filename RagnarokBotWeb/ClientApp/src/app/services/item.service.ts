import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ItemDto } from '../models/item.dto';
import { Page } from '../core/pagination/pager';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ItemService {

  constructor(private readonly http: HttpClient) { }

  getItems(pageSize: number, pageNumber: number, filter: string | null = null) {
    var url = `${environment.apiUrl}/api/items?pageSize=${pageSize}&pageNumber=${pageNumber}`;
    if (filter) {
      url += `&filter=${filter}`;
    }
    return this.http.get<Page<ItemDto>>(url);
  }

  getItemById(id: number) {
    return this.http.get<ItemDto>(`${environment.apiUrl}/api/items/${id}`);
  }

  deleteItem(id: number) {
    return this.http.delete<ItemDto>(`${environment.apiUrl}/api/items/${id}`);
  }

  saveItem(itemForm: any) {
    if (itemForm.id) {
      return this.http.put<ItemDto>(`${environment.apiUrl}/api/items/${itemForm.id}`, itemForm);
    } else {
      return this.http.post<ItemDto>(`${environment.apiUrl}/api/items`, itemForm);
    }
  }
}
