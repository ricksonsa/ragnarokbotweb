import { Component, OnInit } from '@angular/core';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTableModule } from 'ng-zorro-antd/table';
import { PackageDto } from '../../../models/package.dto';
import { PackageService } from '../../../services/package.service';
import { CommonModule } from '@angular/common';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { Router, RouterModule } from '@angular/router';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { Observable, of, tap, switchMap, startWith, debounceTime, distinctUntilChanged } from 'rxjs';
import { NzPopoverModule } from 'ng-zorro-antd/popover';

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

  dataSource: PackageDto[] = [];
  total = 0;
  pageIndex = 1;
  pageSize = 10;
  searchControl = new FormControl();
  suggestions$: Observable<PackageDto[]> = of([]);
  isLoading = true;

  constructor(private readonly packageService: PackageService, private readonly router: Router) { }

  ngOnInit() {
    // this.loadPage();
    this.setUpFilter();
  }

  loadPage() {
    const query = this.searchControl.value; // Get value from the input field

    this.isLoading = true;
    this.suggestions$ = this.packageService.getPackages(this.pageSize, this.pageIndex, query)
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
        switchMap(value => this.packageService.getPackages(this.pageSize, this.pageIndex, value)
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
    this.packageService.deletePackage(id)
      .pipe(switchMap(() => {
        this.pageSizeChange(this.pageSize);
        this.pageIndexChange(this.pageIndex);
        return of();
      }));
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
