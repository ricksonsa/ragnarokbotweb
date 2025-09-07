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
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzTypographyModule } from 'ng-zorro-antd/typography';

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
    NzDividerModule,
    NzTypographyModule,
    NzModalModule
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
  showCommandPrompt = false;
  botId: string;
  command: string;
  loadingCommand = false;

  constructor(
    private readonly botService: BotService,
    private readonly eventManager: EventManager,
    private readonly router: Router) { }

  pageIndex$ = new BehaviorSubject<number>(1);
  pageSize$ = new BehaviorSubject<number>(10);

  ngOnInit() {
    this.data$ = this.botService.getBotTable()
      .pipe(
        tap((bots) => {
          this.isLoading = false;
          this.total = bots.length;
        }),
        switchMap(page => of(page)));
  }

  pageIndexChange(index: number) {
    this.pageIndex$.next(index);
  }

  pageSizeChange(size: number) {
    this.pageSize$.next(size);
  }

  closeSendCommand() {
    this.botId = null;
    this.showCommandPrompt = false;
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

  openSendCommand(botId: string) {
    this.botId = botId;
    this.showCommandPrompt = true;
  }

  sendCommand() {
    if (this.botId == null) return;
    this.loadingCommand = true;
    this.botService.send(this.botId, { command: this.command })
      .subscribe({
        next: (value) => {
          var message = `Command sent to bot ${this.botId}`;
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('', message, 'success')));
          this.loadingCommand = false;
          this.botId = null;
          this.showCommandPrompt = false;
        },
        error: (err) => {
          var msg = err.error?.details ?? 'One or more validation errors ocurred.';
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('', msg, 'error')));
          this.loadingCommand = false;
        }
      });
  }

  cancelDelete() { }

}
