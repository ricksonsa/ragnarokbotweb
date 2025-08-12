import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzPopoverModule } from 'ng-zorro-antd/popover';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzTableModule } from 'ng-zorro-antd/table';
import { debounceTime, distinctUntilChanged, Observable, of, startWith, switchMap, tap } from 'rxjs';
import { PaymentDto } from '../../../models/payment.dto';
import { PaymentService } from '../../../services/payment.service';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { EventManager, EventWithContent } from '../../../services/event-manager.service';
import { Alert } from '../../../models/alert';
import { NzBadgeModule } from 'ng-zorro-antd/badge';

@Component({
  selector: 'app-payments',
  templateUrl: './payments.component.html',
  styleUrls: ['./payments.component.scss'],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    NzCardModule,
    NzTableModule,
    NzSpaceModule,
    NzInputModule,
    NzIconModule,
    NzButtonModule,
    NzPopoverModule,
    NzBadgeModule
  ]
})
export class PaymentsComponent implements OnInit {

  dataSource: PaymentDto[] = [];
  total = 0;
  pageIndex = 1;
  pageSize = 10;
  searchControl = new FormControl();
  suggestions$: Observable<PaymentDto[]> = of([]);
  isLoading = true;
  loading = false;

  constructor(private readonly paymentService: PaymentService, private readonly eventManager: EventManager) { }

  ngOnInit() {
    this.setUpFilter();
  }

  loadPage() {
    this.isLoading = true;
    this.suggestions$ = this.paymentService.getPayments(this.pageSize, this.pageIndex)
      .pipe(
        tap(() => (this.isLoading = false)),
        switchMap((page) => {
          this.dataSource = page.content;
          this.total = page.totalElements;
          this.pageIndex = page.number;
          this.pageSize = page.size;
          return of(page.content);
        })
      );
  }

  addPayment() {
    this.loading = true;
    this.paymentService.addPayment()
      .subscribe({
        next: (response) => {
          window.open("https://www.paypal.com/ncp/payment/ZJ2UNRCF4W2EL");
          this.loading = false;
          this.loadPage();
        },
        error: (err) => {
          this.loading = false;
          this.eventManager.broadcast(new EventWithContent<Alert>('alert', new Alert('Error', err.error.details, 'error')));
        }
      });
  }

  pay(link: string) {
    window.open("https://www.paypal.com/ncp/payment/ZJ2UNRCF4W2EL");
  }

  setUpFilter() {
    this.suggestions$ = this.searchControl.valueChanges
      .pipe(
        startWith(''), // Triggers API call on page load with an empty value
        debounceTime(300), // Wait 300ms after the last input
        distinctUntilChanged(), // Ignore same consecutive values
        tap(() => (this.isLoading = true)), // Show loading indicator
        switchMap(value => this.paymentService.getPayments(this.pageSize, this.pageIndex)
        ),
        tap((page) => {
          if (page) {
            this.dataSource = page.content;
            this.total = page.totalElements;
            this.pageIndex = page.number;
            this.pageSize = page.size;
          }
          this.isLoading = false
        }),
        switchMap((page) => {
          return of(page.content);
        })
      ); // Hide loading indicator
  }

  pageIndexChange(index: number) {
    this.pageIndex = index;
    this.loadPage();
    this.setUpFilter();

  }

  pageSizeChange(size: number) {
    this.pageSize = size;
    this.loadPage();
    this.setUpFilter();

  }

  getStatus(payment: PaymentDto) {
    switch (payment.status) {
      case 0: return 'Waiting Payment';
      case 1: return payment.isExpired ? 'Subscription Expired' : 'Payment Confirmed';
      case 2: return 'Canceled';
      default: return 'Unknown';
    }
  }

  getStatusColor(payment: PaymentDto) {
    switch (payment.status) {
      case 0: return 'processing';
      case 1: return payment.isExpired ? 'default' : 'success';
      case 2: return 'error';
      default: return 'warning';
    }
  }

}
