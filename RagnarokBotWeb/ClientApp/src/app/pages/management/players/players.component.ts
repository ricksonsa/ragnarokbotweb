import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzTableModule } from 'ng-zorro-antd/table';
import { PlayerService } from '../../../services/player.service';
import { PlayerDto } from '../../../models/player.dto';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { RouterModule } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { BehaviorSubject, combineLatest, debounceTime, distinctUntilChanged, Observable, of, startWith, switchMap, tap } from 'rxjs';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzPopoverModule } from 'ng-zorro-antd/popover';
import { getDaysBetweenDates } from '../../../core/functions/date.functions';
import { NzTypographyModule } from 'ng-zorro-antd/typography';
import { NzListModule } from 'ng-zorro-antd/list';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { Alert } from '../../../models/alert';
import { EventManager, EventWithContent } from '../../../services/event-manager.service';
import { BotService } from '../../../services/bot.service';

@Component({
  selector: 'app-players',
  templateUrl: './players.component.html',
  styleUrls: ['./players.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    NzInputModule,
    RouterModule,
    FormsModule,
    NzSpaceModule,
    NzButtonModule,
    NzIconModule,
    NzCardModule,
    NzDividerModule,
    NzTableModule,
    NzTypographyModule,
    NzDividerModule,
    NzPopoverModule,
    NzListModule,
    NzModalModule
  ]
})
export class PlayersComponent implements OnInit {
  dataSource: PlayerDto[] = [];
  searchControl = new FormControl();
  suggestions$: Observable<PlayerDto[]> = of([]);
  isLoading = true;
  total = 0;
  pageIndex = 1;
  pageSize = 10;

  addingGoldAll = false;
  addingGold = false;
  addGoldValue = 0;

  addingMoneyAll = false;
  addingMoney = false;
  addMoneyValue = 0;

  addingFame = false;
  addFameValue = 0;

  addingLoader = false;

  addingCoins = false;
  addCoinsValue = 0;

  addingCoinsOnline = false;

  onlinePlayersOptionsvisible = false;

  constructor(
    private readonly playerService: PlayerService,
    private readonly eventManager: EventManager,
    private readonly botService: BotService
  ) {
    this.searchControl.patchValue('');
  }

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
        this.playerService.getPlayers(pageSize, pageIndex, query)
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

  getDate(date: Date) {
    const remaining = getDaysBetweenDates(new Date(date));
    return `${remaining} days to expire`;
  }

  addGold(online: boolean) {
    this.addingLoader = true;
    const cmd = online ? `!give_gold_online:${this.addGoldValue}` : `!give_gold_all:${this.addGoldValue}`;
    this.botService.sendCommand({ command: cmd })
      .subscribe({
        next: (player) => {
          this.addingGold = false;
          this.addingLoader = false;
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('Player', `You have successfully added ${this.addGoldValue} gold to all players online`, 'success')));
          this.addGoldValue = 0;
        },
        error: (err) => {
          this.addingLoader = false;
          this.addingGold = false;
          this.eventManager.broadcast(new EventWithContent<Alert>('alert', new Alert('Error', err?.error?.details ?? `Something went wrong, please try again later.`, 'error')));
        }
      });
  }

  addMoney(online: boolean) {
    this.addingLoader = true;
    const cmd = online ? `!give_money_online:${this.addMoneyValue}` : `!give_money_all:${this.addMoneyValue}`;
    this.botService.sendCommand({ command: cmd })
      .subscribe({
        next: (player) => {
          this.addingMoney = false;
          this.addingLoader = false;
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('Player', `You have successfully added ${this.addMoneyValue} money to all players online`, 'success')));
          this.addMoneyValue = 0;
        },
        error: (err) => {
          this.addingLoader = false;
          this.addingMoney = false;
          this.eventManager.broadcast(new EventWithContent<Alert>('alert', new Alert('Error', err?.error?.details ?? `Something went wrong, please try again later.`, 'error')));
        }
      });
  }


  addFame() {
    this.addingLoader = true;
    this.botService.sendCommand({ command: `!give_fame_online:${this.addFameValue}` })
      .subscribe({
        next: (player) => {
          this.addingFame = false;
          this.addingLoader = false;
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('Player', `You have successfully added ${this.addFameValue} fame to all players online`, 'success')));
          this.addFameValue = 0;
        },
        error: (err) => {
          this.addingLoader = false;
          this.addingFame = false;
          this.eventManager.broadcast(new EventWithContent<Alert>('alert', new Alert('Error', err?.error?.details ?? `Something went wrong, please try again later.`, 'error')));
        }
      });
  }

  addCoins(online: boolean) {
    this.addingLoader = true;
    this.playerService.updateCoinsToAll(online, this.addCoinsValue)
      .subscribe({
        next: () => {
          this.addingCoins = false;
          this.addingCoinsOnline = false;
          this.addingLoader = false;
          if (online) {
            this.eventManager.broadcast(new EventWithContent('alert', new Alert('Player', `You have successfully added ${this.addCoinsValue} coins to all players online`, 'success')));
          } else {
            this.eventManager.broadcast(new EventWithContent('alert', new Alert('Player', `You have successfully added ${this.addCoinsValue} coins to all players`, 'success')));
          }
          this.addCoinsValue = 0;
        },
        error: (err) => {
          this.addingLoader = false;
          this.addingCoins = false;
          this.addingCoinsOnline = false;
          this.eventManager.broadcast(new EventWithContent<Alert>('alert', new Alert('Error', err?.error?.details ?? `Something went wrong, please try again later.`, 'error')));
        }
      });
  }
}
