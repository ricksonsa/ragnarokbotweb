import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { Page } from '../core/pagination/pager';
import { PaymentDto } from '../models/payment.dto';

@Injectable({
    providedIn: 'root'
})
export class PaymentService {

    constructor(private readonly http: HttpClient) { }

    getPayments(pageSize: number, pageNumber: number) {
        var url = `${environment.apiUrl}/api/payments?pageSize=${pageSize}&pageNumber=${pageNumber}`;
        return this.http.get<Page<PaymentDto>>(url);
    }

    getPayment(id: number) {
        var url = `${environment.apiUrl}/api/payments/${id}`;
        return this.http.get<PaymentDto>(url);
    }

    getPaymentByToken(token: string) {
        var url = `${environment.apiUrl}/api/payments/token/${token}`;
        return this.http.get<PaymentDto>(url);
    }

    confirmPayment(token: string) {
        var url = `${environment.apiUrl}/api/payments/success?token=${token}&payerID=${token}`;
        return this.http.get(url);
    }

    addPayment() {
        var url = `${environment.apiUrl}/api/payments`;
        return this.http.post<PaymentDto>(url, null);
    }
}