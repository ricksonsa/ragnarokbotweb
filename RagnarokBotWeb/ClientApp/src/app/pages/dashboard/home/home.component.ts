import { CommonModule, DecimalPipe } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzGridModule } from 'ng-zorro-antd/grid';
import { NzBadgeModule } from 'ng-zorro-antd/badge';
import { NzStatisticModule } from 'ng-zorro-antd/statistic';
import { ServerService } from '../../../services/server.service';
import { AuthenticationService } from '../../../services/authentication.service';
import { AccountDto } from '../../../models/account.dto';
import { BattlemetricsService } from '../../../services/battlemetrics.service';
import { of, switchMap } from 'rxjs';
import { NzTypographyModule } from 'ng-zorro-antd/typography';
import { BotService } from '../../../services/bot.service';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzListModule } from 'ng-zorro-antd/list';
import { NzDescriptionsModule } from 'ng-zorro-antd/descriptions';
import { ReactiveFormsModule } from '@angular/forms';
import { Chart, registerables } from 'chart.js';
import { WarzoneService } from '../../../services/warzone.service';
import { WarzoneDto } from '../../../models/warzone.dto';
import { OrderService } from '../../../services/order.service';
import { GraphDto } from '../../../models/order.dto';
import { PlayerService } from '../../../services/player.service';
import { PlayerStatsDto, LockpickStatsDto } from '../../../models/player-stats.dto';
import { EventManager, EventWithContent } from '../../../services/event-manager.service';
import { Alert } from '../../../models/alert';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
Chart.register(...registerables);

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NzCardModule,
    NzStatisticModule,
    NzButtonModule,
    NzTypographyModule,
    NzBadgeModule,
    NzGridModule,
    NzTableModule,
    NzListModule,
    NzDescriptionsModule,
    NzEmptyModule
  ]
})
export class HomeComponent implements OnInit, OnDestroy {

  players: any = [];
  battlemetricsData?: any;
  account: AccountDto;
  botsOnline = 0;
  loadInterval: any;
  maxSlots = 0;
  playerCount = 0;
  maxSlotsBattleMetrics = 0;
  playerCountBattleMetrics = 0;
  loading = false;

  warzones: WarzoneDto[] = [];
  bestSellers: GraphDto[] = [];
  kills: PlayerStatsDto[];
  lockpicks: LockpickStatsDto[];
  playerStatistics: GraphDto[] = [];

  constructor(
    private readonly serverService: ServerService,
    private readonly eventManager: EventManager,
    private readonly accountService: AuthenticationService,
    private readonly botService: BotService,
    private readonly warzoneService: WarzoneService,
    private readonly battlemetrics: BattlemetricsService,
    private readonly playerService: PlayerService,
    private readonly orderService: OrderService
  ) { }

  ngOnInit() {
    this.loadAll();
    this.loadInterval = setInterval(() => this.loadAll(), 120000);

  }

  ngOnDestroy(): void {
    clearInterval(this.loadInterval);
  }

  getLockType(value: string) {
    switch (value.toLowerCase()) {
      case 'basic': return 'Iron';
      case 'medium': return 'Silver';
      case 'advanced': return 'Gold';
      default: return value;
    }
  }

  getPercentage(value: number) {
    return Math.round(value);
  }

  renderChart(id: string, type: any, labeldata: any, valuedata: any, colordata?: any) {
    const chart = new Chart(id, {
      type: type,
      data: {
        labels: labeldata,
        datasets: [
          {
            label: 'Monthly',
            data: valuedata,
            backgroundColor: colordata
          }
        ]
      },
      options: {

      }
    });
  }

  loadAll() {
    this.loadCount();
    this.loadAccount();
    this.loadBots();
    this.loadWarzone();
    this.loadBestSellers();
    this.loadPlayerStatistics();
    this.loadPlayersKills();
    this.loadPlayersLockpicks();
    this.loadBattlemetricsData();
  }

  isRunningWarzone() {
    return this.warzones.some(w => w.isRunning);
  }

  getRunningWarzone() {
    return this.warzones.filter(w => w.isRunning)[0];
  }

  loadPlayersKills() {
    this.playerService.getPlayerStatisticsKills()
      .subscribe({
        next: (kills) => {
          this.kills = kills;
        }
      });
  }

  loadPlayersLockpicks() {
    this.playerService.getPlayerStatisticsLockpicks()
      .subscribe({
        next: (lockpicks) => {
          this.lockpicks = lockpicks;
        }
      });
  }

  loadPlayerStatistics() {
    this.playerService.getNewPlayerStatistics()
      .subscribe({
        next: (graph) => {
          this.playerStatistics = graph;
          setTimeout(() => {
            this.renderChart('newPlayers', 'bar', graph.map(x => x.name), graph.map(x => x.amount), graph.map(x => x.color));
          }, 500);
        }
      })
  }

  loadBestSellers() {
    this.orderService.getBestSellersOrders()
      .subscribe({
        next: (bestSellers) => {
          this.bestSellers = bestSellers;
          setTimeout(() => {
            this.renderChart('orders', 'doughnut', bestSellers.map(x => x.name), bestSellers.map(x => x.amount), bestSellers.map(x => x.color))
          }, 500);
        }
      });
  }

  loadWarzone() {
    this.warzoneService.getWarzones(100, 1)
      .subscribe({
        next: (warzone) => {
          this.warzones = warzone.content;
        }
      });
  }

  loadBots() {
    this.botService.getBots()
      .subscribe({
        next: (bots) => {
          this.botsOnline = bots.length;
        }
      });
  }

  startWarzone() {
    this.loading = true;
    this.warzoneService.openWarzone()
      .subscribe({
        next: (warzone) => {
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('', `${warzone.name} started.`, 'success')));
          this.warzones.find(w => w.id == warzone.id).isRunning = true;
          this.loading = false;
        },
        error: (err) => {
          this.loading = false;
          this.eventManager.broadcast(new EventWithContent<Alert>('alert', new Alert('Error', err.error.details, 'error')));
        }
      });
  }

  stopWarzone() {
    this.loading = true;
    this.warzoneService.closeWarzone()
      .subscribe({
        next: (warzone) => {
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('', `${warzone.name} stopped.`, 'success')));
          this.warzones.find(w => w.id == warzone.id).isRunning = false;
          this.loading = false;
        },
        error: (err) => {
          this.loading = false;
          this.eventManager.broadcast(new EventWithContent<Alert>('alert', new Alert('Error', err.error.details, 'error')));
        }
      });
  }

  loadAccount() {
    this.accountService.account()
      .subscribe((account) => {
        this.account = account;
        this.maxSlots = account.server?.slots ?? 0;
      });
  }

  loadBattlemetricsData() {
    if (this.account?.server.battleMetricsId) {
      this.battlemetrics.getBattlemetricsStatus(this.account.server.battleMetricsId)
        .subscribe({
          next: (battlemetrics) => {
            this.battlemetricsData = battlemetrics;
            this.maxSlotsBattleMetrics = battlemetrics.data?.attributes?.maxPlayers ?? 0;
            this.playerCountBattleMetrics = battlemetrics.data?.attributes?.players ?? 0;
          }
        });
    }
  }

  loadCount() {
    this.serverService.getPlayerCount()
      .subscribe({
        next: (players) => {
          this.players = players;
          if (this.battlemetrics == null)
            this.playerCount = players.length;
        }
      })
  }

}
