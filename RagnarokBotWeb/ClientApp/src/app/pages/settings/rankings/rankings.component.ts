import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormGroup, FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzListModule } from 'ng-zorro-antd/list';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzTypographyModule } from 'ng-zorro-antd/typography';
import { EventManager, EventWithContent } from '../../../services/event-manager.service';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { ServerService } from '../../../services/server.service';
import { Alert } from '../../../models/alert';
import { AuthenticationService } from '../../../services/authentication.service';
import { take } from 'rxjs';

@Component({
  selector: 'app-rankings',
  templateUrl: './rankings.component.html',
  styleUrls: ['./rankings.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    NzCardModule,
    NzFormModule,
    NzInputModule,
    NzSelectModule,
    NzSpaceModule,
    NzDividerModule,
    NzIconModule,
    NzCheckboxModule,
    NzListModule,
    NzTypographyModule,
    NzPopconfirmModule,
    NzButtonModule,
    NzAlertModule
  ]
})
export class RankingsComponent implements OnInit {
  form!: FormGroup;
  private fb = inject(NonNullableFormBuilder);
  loading = false;

  constructor(
    private readonly eventManager: EventManager,
    private readonly authService: AuthenticationService,
    private readonly serverService: ServerService
  ) {
    this.form = this.fb.group({
      rankEnabled: [false],
      rankVipOnly: [false],
      killRankDailyTop1Award: [null],
      killRankDailyTop2Award: [null],
      killRankDailyTop3Award: [null],
      killRankDailyTop4Award: [null],
      killRankDailyTop5Award: [null],
      killRankWeeklyTop1Award: [null],
      killRankWeeklyTop2Award: [null],
      killRankWeeklyTop3Award: [null],
      killRankWeeklyTop4Award: [null],
      killRankWeeklyTop5Award: [null],
      killRankMonthlyTop1Award: [null],
      killRankMonthlyTop2Award: [null],
      killRankMonthlyTop3Award: [null],
      killRankMonthlyTop4Award: [null],
      killRankMonthlyTop5Award: [null],
    });
  }

  ngOnInit() {
    this.loadAccount();
  }

  loadAccount() {
    this.authService.account()
      .pipe(take(1))
      .subscribe({
        next: (account) => {
          this.form.patchValue(account.server);
        }
      });
  }

  save() {
    this.loading = true;
    const value = this.form.value;

    for (let attr in value) {
      if (value[attr] != null && value[attr] !== '') {
        if (typeof value[attr] === 'number') {
          value[attr] = Math.abs(value[attr]);
        }
      }
    }

    this.form.patchValue(value);
    this.serverService.updateRankAwards(value)
      .subscribe({
        next: (value) => {
          const message = `Rank Awards updated.`;
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('', message, 'success')));
          this.loading = false;
        },
        error: (err) => {
          var msg = err.error?.details ?? 'One or more validation errors ocurred.';
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('', msg, 'error')));
          this.loading = false;
        }
      })

  }
}
