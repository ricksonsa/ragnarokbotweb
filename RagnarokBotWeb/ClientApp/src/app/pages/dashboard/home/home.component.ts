import { DecimalPipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
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

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
  imports: [
    NzCardModule,
    NzStatisticModule,
    NzButtonModule,
    NzTypographyModule,
    NzBadgeModule,
    NzGridModule
  ]
})
export class HomeComponent implements OnInit {
  playerCount: any;
  battlemetricsData: any;
  account: AccountDto;
  botsOnline = 0;

  constructor(
    private readonly serverService: ServerService,
    private readonly accountService: AuthenticationService,
    private readonly battlemetrics: BattlemetricsService
  ) { }

  ngOnInit() {
    this.loadCount();
    this.loadAccount();
  }



  loadAccount() {
    this.accountService.account()
      .pipe(switchMap((account) => {
        this.account = account;
        if (account.server?.battleMetricsId)
          return this.battlemetrics.getBattlemetricsStatus(account.server!.battleMetricsId);
        return of(null)
      }))
      .subscribe((battlemetrics) => {
        if (battlemetrics) {
          this.battlemetricsData = battlemetrics;
        }
      });
  }

  getSlotsText() {
    if (this.battlemetricsData) {
      return {
        count: this.battlemetricsData.data.attributes.players,
        maxSlots: '/ ' + this.battlemetricsData.data.attributes.maxPlayers
      }
    }
    else {
      if (this.playerCount?.maxSlots) {
        return {
          count: this.playerCount.count,
          maxSlots: '/ ' + this.playerCount.maxSlots
        }
      }
    }

    return {
      count: 0
    };
  }

  loadCount() {
    this.serverService.getPlayerCount()
      .subscribe({
        next: (count) => {
          this.playerCount = count;
        }
      })
  }

}
