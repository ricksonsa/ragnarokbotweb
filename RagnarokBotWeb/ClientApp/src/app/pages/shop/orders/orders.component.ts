import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzTableModule } from 'ng-zorro-antd/table';
import { OrderDto } from '../../../models/order.dto';
import { BehaviorSubject, combineLatest, debounceTime, distinctUntilChanged, Observable, of, startWith, switchMap, tap } from 'rxjs';
import { OrderService } from '../../../services/order.service';
import { EventManager, EventWithContent } from '../../../services/event-manager.service';
import { Alert } from '../../../models/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';

@Component({
  selector: 'app-orders',
  templateUrl: './orders.component.html',
  styleUrls: ['./orders.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    NzSpaceModule,
    NzInputModule,
    NzIconModule,
    NzCardModule,
    NzTableModule,
    NzButtonModule
  ]
})
export class OrdersComponent implements OnInit {
  dataSource: OrderDto[] = [];
  total = 0;
  pageIndex = 1;
  pageSize = 10;
  searchControl = new FormControl();
  suggestions$: Observable<OrderDto[]> = of([]);
  isLoading = false;
  cancelLoading = false;
  requeLoading = false;

  constructor(
    private readonly orderService: OrderService,
    private readonly router: Router,
    private readonly eventManager: EventManager) { }

  pageIndex$ = new BehaviorSubject<number>(1);
  pageSize$ = new BehaviorSubject<number>(10);

  ngOnInit() {
    this.load();
  }

  load() {
    this.suggestions$ = combineLatest([
      this.searchControl.valueChanges.pipe(startWith(''), debounceTime(300), distinctUntilChanged()),
      this.pageIndex$,
      this.pageSize$
    ]).pipe(
      tap(() => {
        this.isLoading = true;
      }),
      switchMap(([query, pageIndex, pageSize]) => this.orderService.getOrders(pageSize, pageIndex, query)
      ),
      tap(page => {
        if (page.totalPages > 0 && this.pageIndex > page.totalPages && this.pageIndex !== 1) {
          this.pageIndex = 1;
          this.pageIndex$.next(1);
        } else {
          this.dataSource = page.content;
          this.total = page.totalElements;
          this.pageIndex = page.number;
          this.pageSize = page.size;
        }
        this.isLoading = false;
      }),
      switchMap(page => of(page.content))
    );
  }

  pageIndexChange(index: number) {
    this.pageIndex$.next(index);
  }

  pageSizeChange(size: number) {
    this.pageSize$.next(size);
  }

  cancel(order: OrderDto) {
    this.cancelLoading = true;
    this.orderService.cancelOrder(order.id)
      .subscribe({
        next: (order) => {
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('', `Order number ${order.id} canceled.`, 'success')));
          this.cancelLoading = false;
          this.load();
        },
        error: (err) => {
          this.cancelLoading = false;
          let msg = '';
          if (err.error?.details) msg = err.error.details;
          if (err?.details) msg = err.details;
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('Error', msg, 'error')));
        }
      });
  }

  requeue(order: OrderDto) {
    this.requeLoading = true;
    this.orderService.requeueOrder(order.id)
      .subscribe({
        next: (order) => {
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('', `Order number ${order.id} requeued.`, 'success')));
          this.load();
          this.requeLoading = false;
        },
        error: (err) => {
          this.requeLoading = false;
          let msg = '';
          if (err.error?.details) msg = err.error.details;
          if (err?.details) msg = err.details;
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('Error', msg, 'error')));
        }
      });
  }

  resolveOrderType(type: number) {
    switch (type) {
      case 0: return 'Shop Pack';
      case 1: return 'Warzone Teleport';
      case 2: return 'UAV';
      case 3: return 'Taxi';
      case 4: return 'Exchange';
      default: return type.toString();
    }
  }

  getName(data: OrderDto) {
    switch (data.orderType) {
      case 0: return data.pack?.name ?? '{error}';
      case 1: return data.warzone?.name ?? '{error}';
      case 2: return data.scumServer.uav?.name ?? '{error}';
      case 3: return data.taxi?.name ?? '{error}';
      case 4: return this.getExchangeTypeName(data) ?? '{error}';
      default: return 'Unknown';
    }
  }

  getExchangeTypeName(data: OrderDto) {
    switch (data.exchangeType) {
      case 0: return `Transfer[${data.exchangeAmount}]`;
      case 1: return `Withdraw[${data.exchangeAmount}]`;
      case 2: return `Deposit[${data.exchangeAmount}]`;
      default: return 'Unknown';
    }
  }

  resolveOrderStatus(type: number) {
    switch (type) {
      case 0: return 'Queued';
      case 1: return 'Processing';
      case 2: return 'Completed';
      case 3: return 'Canceled';
      default: return type.toString();
    }
  }

}
