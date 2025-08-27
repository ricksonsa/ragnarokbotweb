import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormsModule, FormControl } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzPopoverModule } from 'ng-zorro-antd/popover';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzTableModule } from 'ng-zorro-antd/table';
import { BotService } from '../../../services/bot.service';
import { EventManager, EventWithContent } from '../../../services/event-manager.service';
import { Observable, of, BehaviorSubject, combineLatest, startWith, debounceTime, distinctUntilChanged, tap, switchMap, firstValueFrom, pipe } from 'rxjs';
import { Alert } from '../../../models/alert';
import { WarzoneDto } from '../../../models/warzone.dto';
import { BotState } from '../../../models/bot-state.dto';

@Component({
  selector: 'app-bots',
  templateUrl: './bots.component.html',
  styleUrls: ['./bots.component.scss'],
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
export class BotsComponent implements OnInit {
  dataSource: WarzoneDto[] = [];
  total = 0;
  pageIndex = 1;
  pageSize = 10;
  searchControl = new FormControl();
  data$: Observable<BotState[]> = of([]);
  isLoading = true;

  constructor(
    private readonly botService: BotService,
    private readonly eventManager: EventManager,
    private readonly router: Router) { }

  pageIndex$ = new BehaviorSubject<number>(1);
  pageSize$ = new BehaviorSubject<number>(10);

  ngOnInit() {
    this.data$ = this.botService.getBotTable().pipe(tap((_) => this.isLoading = false), switchMap(page => of(page)));
  }

  pageIndexChange(index: number) {
    this.pageIndex$.next(index);
  }

  pageSizeChange(size: number) {
    this.pageSize$.next(size);
  }

  sendReconnect(botId: string) {
    this.botService.reconnect(botId)
      .subscribe({
        next: (value) => {
          var message = `Reconnect signal sent to bot ${botId}`;
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('', message, 'success')));
        },
        error: (err) => {
          var msg = err.error?.details ?? 'One or more validation errors ocurred.';
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('', msg, 'error')));
        }
      });
  }


  cancelDelete() { }

}
