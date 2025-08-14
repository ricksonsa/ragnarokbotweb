import { Component, OnDestroy, OnInit } from '@angular/core';
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
import { Observable, of, tap, switchMap, startWith, debounceTime, distinctUntilChanged, firstValueFrom, Subscription } from 'rxjs';
import { NzPopoverModule } from 'ng-zorro-antd/popover';
import { EventManager, EventWithContent } from '../../../services/event-manager.service';
import { Alert } from '../../../models/alert';
import { ServerService } from '../../../services/server.service';

@Component({
  selector: 'app-packages',
  templateUrl: './packages.component.html',
  styleUrls: ['./packages.component.scss'],
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
export class PackagesComponent implements OnInit, OnDestroy {

  dataSource: PackageDto[] = [];
  total = 0;
  pageIndex = 1;
  pageSize = 10;
  searchControl = new FormControl();
  suggestions$: Observable<PackageDto[]> = of([]);
  isLoading = true;
  discord$: Subscription;
  channels: { key: string; value: string; }[];

  constructor(
    private readonly packageService: PackageService,
    private readonly serverService: ServerService,
    private readonly router: Router,
    private readonly eventManager: EventManager) { }

  ngOnInit() {
    // this.loadPage();
    this.setUpFilter();
    this.loadDiscordChannels();
  }

  ngOnDestroy(): void {
    this.discord$?.unsubscribe();
  }

  loadDiscordChannels() {
    this.discord$ = this.serverService.getDiscordChannels()
      .subscribe({
        next: (channels => {
          this.channels = channels;
        })
      });
  }

  getDiscordName(discordId: any) {
    return this.channels.filter(channel => channel.key == discordId)[0];
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
            this.pageIndex = 1;
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
    firstValueFrom(this.packageService.deletePackage(id))
      .then(() => {
        this.pageSizeChange(this.pageSize);
        this.pageIndexChange(this.pageIndex);
        this.eventManager.broadcast(new EventWithContent('alert', new Alert('', `Pack number ${id} deleted.`, 'success')));
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
