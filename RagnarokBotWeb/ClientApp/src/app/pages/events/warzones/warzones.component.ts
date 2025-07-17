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
import { Observable, of, tap, switchMap, startWith, debounceTime, distinctUntilChanged, firstValueFrom } from 'rxjs';
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

  ngOnInit() {
    // this.loadPage();
    this.setUpFilter();
  }

  loadPage() {
    const query = this.searchControl.value; // Get value from the input field

    this.isLoading = true;
    this.suggestions$ = this.warzoneService.getWarzones(this.pageSize, this.pageIndex, query)
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

  setUpFilter() {
    this.suggestions$ = this.searchControl.valueChanges
      .pipe(
        startWith(''), // Triggers API call on page load with an empty value
        debounceTime(300), // Wait 300ms after the last input
        distinctUntilChanged(), // Ignore same consecutive values
        tap(() => (this.isLoading = true)), // Show loading indicator
        switchMap(value => this.warzoneService.getWarzones(this.pageSize, this.pageIndex, value)
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

  confirmDelete(id: number) {
    firstValueFrom(this.warzoneService.deleteWarzone(id))
      .then(() => {
        this.pageSizeChange(this.pageSize);
        this.pageIndexChange(this.pageIndex);
        this.eventManager.broadcast(new EventWithContent('alert', new Alert('', `Warzone number ${id} deleted.`, 'success')));
      });
  }

  cancelDelete() { }

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

}
