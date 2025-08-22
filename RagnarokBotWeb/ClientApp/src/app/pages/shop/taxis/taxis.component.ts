import { Component, OnDestroy, OnInit } from '@angular/core';
import { TaxiService } from '../../../services/taxi.service';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { Observable, of, Subscription, BehaviorSubject, combineLatest, startWith, debounceTime, distinctUntilChanged, tap, switchMap, firstValueFrom } from 'rxjs';
import { Alert } from '../../../models/alert';
import { EventManager, EventWithContent } from '../../../services/event-manager.service';
import { CommonModule } from '@angular/common';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzPopoverModule } from 'ng-zorro-antd/popover';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzTableModule } from 'ng-zorro-antd/table';
import { TaxiDto } from '../../../models/taxi.dto';
import { ChannelDto } from '../../../models/channel.dto';
import { ServerService } from '../../../services/server.service';

@Component({
  selector: 'app-taxis',
  templateUrl: './taxis.component.html',
  styleUrls: ['./taxis.component.scss'],
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
export class TaxisComponent implements OnInit, OnDestroy {

  dataSource: TaxiDto[] = [];
  total = 0;
  pageIndex = 1;
  pageSize = 10;
  searchControl = new FormControl();
  suggestions$: Observable<TaxiDto[]> = of([]);
  isLoading = true;
  discord$: Subscription;
  channels: ChannelDto[] = [];

  constructor(
    private readonly serverService: ServerService,
    private readonly taxiService: TaxiService,
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
        this.taxiService.getTaxis(pageSize, pageIndex, query)
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

  loadDiscordChannels() {
    this.serverService.getDiscordServer()
      .subscribe({
        next: (guild) => {
          this.channels = guild.channels;
        }
      });
  }


  pageIndexChange(index: number) {
    this.pageIndex$.next(index);
  }

  getDiscordName(discordId: any) {
    return this.channels.filter(channel => channel.discordId == discordId)[0].name;
  }

  pageSizeChange(size: number) {
    this.pageSize$.next(size);
  }

  confirmDelete(id: number) {
    firstValueFrom(this.taxiService.delete(id))
      .then(() => {
        this.pageSizeChange(this.pageSize);
        this.pageIndexChange(this.pageIndex);
        this.eventManager.broadcast(new EventWithContent('alert', new Alert('', `Pack number ${id} deleted.`, 'success')));
      });
  }

  cancelDelete() { }

}
