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
import { Observable, of, tap, switchMap, startWith, debounceTime, distinctUntilChanged, firstValueFrom, Subscription, combineLatest, BehaviorSubject } from 'rxjs';
import { NzPopoverModule } from 'ng-zorro-antd/popover';
import { EventManager, EventWithContent } from '../../../services/event-manager.service';
import { Alert } from '../../../models/alert';
import { ServerService } from '../../../services/server.service';
import { ChannelDto } from '../../../models/channel.dto';

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
  channels: ChannelDto[] = [];

  constructor(
    private readonly packageService: PackageService,
    private readonly serverService: ServerService,
    private readonly router: Router,
    private readonly eventManager: EventManager) { }

  ngOnDestroy(): void {
    this.discord$?.unsubscribe();
  }

  pageIndex$ = new BehaviorSubject<number>(1);
  pageSize$ = new BehaviorSubject<number>(10);
  ngOnInit() {
    this.loadDiscordChannels();
    this.suggestions$ = combineLatest([
      this.searchControl.valueChanges.pipe(startWith(''), debounceTime(300), distinctUntilChanged()),
      this.pageIndex$,
      this.pageSize$
    ]).pipe(
      tap(() => {
        this.isLoading = true;
      }),
      switchMap(([query, pageIndex, pageSize]) =>
        this.packageService.getPackages(pageSize, pageIndex, query)
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

  loadDiscordChannels() {
    this.serverService.getDiscordServer()
      .subscribe({
        next: (guild) => {
          this.channels = guild.channels;
        }
      });
  }

  getDiscordName(discordId: any) {
    return this.channels.filter(channel => channel.discordId == discordId)[0]?.name;
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

}
