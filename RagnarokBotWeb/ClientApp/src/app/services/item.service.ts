import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ItemDto } from '../models/item.dto';
import { WEB_API } from '../api.const';
import { Page } from '../core/pagination/pager';

@Injectable({
  providedIn: 'root'
})
export class ItemService {

  constructor(private readonly http: HttpClient) { }

  getItems(pageSize: number, pageNumber: number, filter: string | null = null) {
    var url = `${WEB_API.baseUrl}/api/items?pageSize=${pageSize}&pageNumber=${pageNumber}`;
    if (filter) {
      url += `&filter=${filter}`;
    }
    return this.http.get<Page<ItemDto>>(url);
  }
}
