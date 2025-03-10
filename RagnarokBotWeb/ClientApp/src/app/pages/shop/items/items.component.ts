import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzTableModule } from 'ng-zorro-antd/table';
import { ItemDto } from '../../../models/item.dto';
import { ItemService } from '../../../services/item.service';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { debounceTime, distinctUntilChanged, Observable, of, startWith, switchMap, tap } from 'rxjs';
import { Page } from '../../../core/pagination/pager';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';

@Component({
  selector: 'app-items',
  templateUrl: './items.component.html',
  styleUrls: ['./items.component.scss'],
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    ReactiveFormsModule,
    NzCardModule,
    NzInputModule,
    NzSpaceModule,
    NzIconModule,
    NzPopconfirmModule,
    NzTableModule,
    NzButtonModule,
    NzDividerModule
  ]
})
export class ItemsComponent implements OnInit {

  dataSource: ItemDto[] = [];
  total = 0;
  pageIndex = 1;
  pageSize = 10;
  searchControl = new FormControl();
  suggestions$: Observable<ItemDto[]> = of([]);
  isLoading = false;

  constructor(private readonly itemService: ItemService, private readonly router: Router) { }

  ngOnInit() {
    // this.loadPage();
    this.setUpFilter();
  }

  loadPage() {
    const query = this.searchControl.value; // Get value from the input field

    this.isLoading = true;
    this.suggestions$ = this.itemService.getItems(this.pageSize, this.pageIndex, query)
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
        switchMap(value => this.itemService.getItems(this.pageSize, this.pageIndex, value)
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
    this.itemService.deleteItem(id)
      .subscribe({
        next: () => {
          this.pageSizeChange(this.pageSize);
        }
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
