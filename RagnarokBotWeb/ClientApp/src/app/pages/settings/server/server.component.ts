import { Component, inject, OnInit } from '@angular/core';
import { NzFormModule } from 'ng-zorro-antd/form';
import { FormControl, FormGroup, FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { CronEditorModule, CronOptions } from 'ngx-cron-editor';

import { CommonModule } from '@angular/common';

import cronstrue from 'cronstrue';

import { registerLocaleData } from '@angular/common';
import localePT from '@angular/common/locales/pt';
import localeES from '@angular/common/locales/es';
import localeDE from '@angular/common/locales/de';
import localeFR from '@angular/common/locales/fr';
import { ServerService } from '../../../services/server.service';
import { AuthenticationService } from '../../../services/authentication.service';
import { AccountDto } from '../../../models/account.dto';
import { EventManager, EventWithContent } from '../../../services/event-manager.service';
import { Alert } from '../../../models/alert';
import { NzTypographyModule } from 'ng-zorro-antd/typography';
import { ChannelDto } from '../../../models/channel.dto';
registerLocaleData(localePT);
registerLocaleData(localeES);
registerLocaleData(localeDE);
registerLocaleData(localeFR);

@Component({
  selector: 'app-server',
  templateUrl: './server.component.html',
  styleUrls: ['./server.component.scss', '../settings.scss'],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    NzCardModule,
    NzFormModule,
    NzButtonModule,
    NzInputModule,
    NzSelectModule,
    NzAlertModule,
    NzDividerModule,
    NzTypographyModule,
    NzIconModule,
    NzSpaceModule,
    CronEditorModule
  ]
})
export class ServerComponent implements OnInit {
  cronEditor = false;
  times: string[] = [];
  cron = new FormControl();
  ftpForm!: FormGroup;
  serverForm!: FormGroup;
  accountDto?: AccountDto;
  channels: ChannelDto[] = [];

  private fb = inject(NonNullableFormBuilder);

  public cronOptions: CronOptions = {
    defaultTime: "00:00:00",
    hideMinutesTab: false,
    hideHourlyTab: false,
    hideDailyTab: false,
    hideWeeklyTab: false,
    hideMonthlyTab: false,
    hideYearlyTab: false,
    hideAdvancedTab: true,
    hideSpecificWeekDayTab: false,
    hideSpecificMonthWeekTab: false,
    use24HourTime: true,
    hideSeconds: false,
    cronFlavor: "standard" //standard or quartz
  };
  savingFtp = false;

  constructor(
    private readonly serverService: ServerService,
    private readonly eventManager: EventManager,
    private readonly authService: AuthenticationService) {
    this.ftpForm = this.fb.group({
      userName: [null, [Validators.required]],
      password: [null, [Validators.required]],
      address: [null, [Validators.required]],
      port: [null, [Validators.required]]
    });
    this.serverForm = this.fb.group({
      coinAwardPeriodically: [0, [Validators.min(0)]],
      vipCoinAwardPeriodically: [0, [Validators.min(0)]]

    });
  }

  ngOnInit() {
    this.authService.account(true)
      .subscribe({
        next: (account) => {
          this.accountDto = account;
          if (account.server?.ftp) {
            this.ftpForm.patchValue(account.server.ftp);
          }
          account.server.restartTimes?.forEach(time => this.times.push(time));
          this.serverForm.patchValue(this.accountDto.server);
        }
      });
  }

  updateSettings() {
    const form = this.serverForm.value;
    form.restartTimes = this.times;
    this.serverService.updateSettings(form)
      .subscribe({
        next: (server) => {
          this.eventManager.broadcast(new EventWithContent<Alert>('alert', new Alert('', 'Server Settings Updated', 'success')));
        }
      });
  }

  loadDiscordChannels() {
    this.serverService.getDiscordServer()
      .subscribe({
        next: (guild) => {
          this.channels = guild.channels;
        }
      });
  }

  addTime() {
    var value = this.cron.value!.toString();
    if (!this.times.find(time => time === value)) this.times.push(value);
  }

  removeTime(index: number) {
    this.times.splice(index, 1);
  }

  getTimes() {
    return this.times.map(time => {
      return cronstrue.toString(time);
    });
  }

  saveFtp() {
    if (this.ftpForm.invalid) {
      Object.values(this.ftpForm.controls).forEach(control => {
        if (control.invalid) {
          control.markAsDirty();
          control.updateValueAndValidity({ onlySelf: true });
        }
      });
      return;
    }

    this.savingFtp = true;
    this.serverService.updateFtp(this.ftpForm.value)
      .subscribe({
        next: (value) => {
          this.accountDto!.server = value;
          this.ftpForm.patchValue(value.ftp!);
          this.savingFtp = false;
          this.eventManager.broadcast(new EventWithContent<Alert>('alert', new Alert('', 'Ftp Settings Success', 'success')));
        },
        error: (err) => {
          this.savingFtp = false;
          this.eventManager.broadcast(new EventWithContent<Alert>('alert', new Alert('Error', err.error.details, 'error')));
        }
      })
  }

}
