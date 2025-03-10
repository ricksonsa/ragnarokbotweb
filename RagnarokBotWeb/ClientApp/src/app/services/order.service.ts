import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { WEB_API } from '../api.const';
import { Page } from '../core/pagination/pager';
import { OrderDto } from '../models/order.dto';

@Injectable({
  providedIn: 'root'
})
export class OrderService {

  constructor(private readonly http: HttpClient) { }

  getOrders(pageSize: number, pageNumber: number, filter: string | null = null) {
    var url = `${WEB_API.baseUrl}/api/orders?pageSize=${pageSize}&pageNumber=${pageNumber}`;
    if (filter) url += `&filter=${filter}`;
    return this.http.get<Page<OrderDto>>(url);
  }

  getByOrderId(id: number) {
    return this.http.get<OrderDto>(`${WEB_API.baseUrl}/api/orders/${id}`);
  }
}