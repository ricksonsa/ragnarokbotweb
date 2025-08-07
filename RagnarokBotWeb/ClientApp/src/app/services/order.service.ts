import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { Page } from '../core/pagination/pager';
import { OrderDto } from '../models/order.dto';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  constructor(private readonly http: HttpClient) { }

  getOrders(pageSize: number, pageNumber: number, filter: string | null = null) {
    var url = `${environment.apiUrl}/api/orders?pageSize=${pageSize}&pageNumber=${pageNumber}`;
    if (filter) url += `&filter=${filter}`;
    return this.http.get<Page<OrderDto>>(url);
  }

  deliverWelcomePack(id: any) {
    return this.http.patch<OrderDto>(`${environment.apiUrl}/api/orders/players/${id}/welcomepack`, null);
  }

  getByOrderId(id: number) {
    return this.http.get<OrderDto>(`${environment.apiUrl}/api/orders/${id}`);
  }
}