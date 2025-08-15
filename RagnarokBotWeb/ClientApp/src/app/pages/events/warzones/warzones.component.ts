import { Component, OnInit } from '@angular/core';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTableModule } from 'ng-zorro-antd/table';
import { CommonModule } from '@angular/common';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { Router, RouterModule } from '@angular/router';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { Observable, of, tap, switchMap, startWith, debounceTime, distinctUntilChanged, firstValueFrom, BehaviorSubject, combineLatest } from 'rxjs';
import { NzPopoverModule } from 'ng-zorro-antd/popover';
import { WarzoneService } from '../../../services/warzone.service';
import { WarzoneDto } from '../../../models/warzone.dto';
import { EventManager, EventWithContent } from '../../../services/event-manager.service';
import { Alert } from '../../../models/alert';

@Component({
  selector: 'app-warzones',
  templateUrl: './warzones.component.html',
  styleUrls: ['./warzones.component.scss'],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    FormsModule,
    NzPopoverModule,
    NzCardModule,
    NzIconModule,
    NzInputModule,
    NzPopconfirmModule,
    NzTableModule,
    NzButtonModule,
    NzSpaceModule,
    NzDividerModule
  ]
})
export class WarzonesComponent implements OnInit {

  dataSource: WarzoneDto[] = [];
  total = 0;
  pageIndex = 1;
  pageSize = 10;
  searchControl = new FormControl();
  suggestions$: Observable<WarzoneDto[]> = of([]);
  isLoading = true;

  constructor(
    private readonly warzoneService: WarzoneService,
    private readonly eventManager: EventManager,
    private readonly router: Router) { }

  pageIndex$ = new BehaviorSubject<number>(1);
  pageSize$ = new BehaviorSubject<number>(10);

  ngOnInit() {
    this.suggestions$ = combineLatest([
      this.searchControl.valueChanges.pipe(startWith(''), debounceTime(300), distinctUntilChanged()),
      this.pageIndex$,
      this.pageSize$
    ]).pipe(
      tap(() => {
        this.isLoading = true;
      }),
      switchMap(([query, pageIndex, pageSize]) =>
        this.warzoneService.getWarzones(pageSize, pageIndex, query)
      ),
      tap(page => {
        if (this.pageIndex > page.totalPages) {
          this.pageIndex = 1;
          this.pageIndex$.next(1);
        }
        this.dataSource = page.content;
        this.total = page.totalElements;
        this.pageIndex = page.number;
        this.pageSize = page.size;
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

  confirmDelete(id: number) {
    firstValueFrom(this.warzoneService.deleteWarzone(id))
      .then(() => {
        this.pageSizeChange(this.pageSize);
        this.pageIndexChange(this.pageIndex);
        this.eventManager.broadcast(new EventWithContent('alert', new Alert('', `Warzone number ${id} deleted.`, 'success')));
      });
  }

  cancelDelete() { }

}
