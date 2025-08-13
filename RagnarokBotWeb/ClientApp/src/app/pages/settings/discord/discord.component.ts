import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { NzFormModule } from 'ng-zorro-antd/form';
import { FormGroup, FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzSwitchModule } from 'ng-zorro-antd/switch';
import { CommonModule } from '@angular/common';
import { AuthenticationService } from '../../../services/authentication.service';
import { AccountDto } from '../../../models/account.dto';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { ServerService } from '../../../services/server.service';
import { EventManager, EventWithContent } from '../../../services/event-manager.service';
import { Alert } from '../../../models/alert';
import { ChannelDto } from '../../../models/channel.dto';
import { NzTypographyModule } from 'ng-zorro-antd/typography';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { Observable, pipe, Subscription, switchMap, take } from 'rxjs';

@Component({
  selector: 'app-discord',
  templateUrl: './discord.component.html',
  styleUrls: ['./discord.component.scss', '../settings.scss'],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    NzCardModule,
    NzFormModule,
    NzSpaceModule,
    NzButtonModule,
    NzInputModule,
    NzSelectModule,
    NzPopconfirmModule,
    NzAlertModule,
    NzTypographyModule,
    NzDividerModule,
    NzSwitchModule,
    NzIconModule
  ]
})
export class DiscordComponent implements OnInit, OnDestroy {
  account?: AccountDto;
  private fb = inject(NonNullableFormBuilder);

  killfeedForm!: FormGroup;
  lockpickFeed!: FormGroup;
  discordForm!: FormGroup;
  channelsForm!: FormGroup;

  tokenErrorMessage: string;
  channels: ChannelDto[] = [];
  subs: Subscription[] = [];

  isDiscordSettingsSaving = false;
  runTemplate = false;

  constructor(
    private readonly authService: AuthenticationService,
    private readonly eventManager: EventManager,
    private readonly serverService: ServerService
  ) {
    this.discordForm = this.fb.group({
      token: [null, [Validators.required]],
      discordLink: [null],
    });

    this.killfeedForm = this.fb.group({
      useKillFeed: [false],
      showKillDistance: [false],
      showKillSector: [false],
      showKillWeapon: [false],
      hideKillerName: [false],
      hideMineKill: [false],
      showSameSquadKill: [false]
    });

    this.lockpickFeed = this.fb.group({
      useLockpickFeed: [false],
      showLockpickSector: [false],
      showLockpickContainerName: [false],
      sendVipLockpickAlert: [false]
    });

    this.channelsForm = this.fb.group({
      "game-chat": [],
      "no-admin-abuse-public": [],
      "kill-feed": [],
      "bunker-states": [],
      "register": [],
      "taxi": [],
      "kill-rank": [],
      "sniper-rank": [],
      "lockpick-rank": [],
      "login": [],
      "buried-chest": [],
      "mine-kill": [],
      "lockpick-alert": [],
      "admin-alert": [],
      "lockpick-admin": []
    });
  }
  ngOnDestroy(): void {
    this.subs.forEach(sub => sub.unsubscribe());
  }

  ngOnInit() {
    this.loadAccount();
    // this.loadDiscordChannels();
    this.loadTemplateDiscordChannels();
  }

  loadAccount(force = false) {
    this.authService.account(force)
      .pipe(take(1))
      .subscribe({
        next: (account) => {
          this.account = account;
          if (this.account.server.discord) {
            this.discordForm.controls['token'].disable();
            this.discordForm.patchValue(this.account.server.discord);
          }
        }
      });
  }

  loadTemplateDiscordChannels() {
    this.serverService.getDiscordServer()
      .pipe(
        switchMap((guild) => {
          this.runTemplate = guild.runTemplate;
          this.channels = guild.channels;
          return this.serverService.getDiscordChannels()
        })
      )
      .subscribe({
        next: (response) => {
          response.forEach(item => {
            this.channelsForm.controls[item.key].patchValue(item.value);
            this.channels.find(channel => channel.discordId === item.value).disabled = true;
          });

          this.registerFormChanges();
        }
      });
  }

  private unregisterFormChanges() {
    this.subs.forEach(sub => sub.unsubscribe());
  }

  private registerFormChanges() {
    for (const control in this.channelsForm.controls) {
      this.subs.push(
        this.channelsForm.controls[control].valueChanges
          .subscribe(
            {
              next: (value) => {
                this.save({ key: control, value });
              }
            }
          )
      );
    }
  }

  loadDiscordChannels() {
    this.serverService.getDiscordServer()
      .subscribe({
        next: (guild) => {
          this.runTemplate = guild.runTemplate;
          this.channels = guild.channels;
        }
      });
  }

  save(value: any) {
    this.unregisterFormChanges();
    this.serverService.updateChannels(value)
      .pipe(
        switchMap(response => {
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('Server', `Channel updated`, 'success')));
          return this.serverService.getDiscordChannels();
        })
      ).subscribe({
        next: (response) => {
          this.channels.forEach(channel => channel.disabled = false);
          response.forEach(item => {
            this.channelsForm.controls[item.key].patchValue(item.value);
            this.channels.find(channel => channel.discordId === item.value).disabled = true;
          });
        },
        complete: () => this.registerFormChanges()
      });
  }

  createTemplate() {
    this.serverService.createDefaultChannels()
      .subscribe({
        next: (guild) => {
          this.runTemplate = guild.runTemplate;
          this.channels = guild.channels;
        }
      });
  }

  saveDiscordData() {
    if (this.discordForm.invalid) {
      Object.values(this.discordForm.controls).forEach(control => {
        if (control.invalid) {
          this.tokenErrorMessage = 'The confirmation token is required to setup discord settings!';
          control.markAsDirty();
          control.updateValueAndValidity({ onlySelf: true });
        }
      });
      return;
    }

    this.isDiscordSettingsSaving = true;
    this.serverService.updateDiscordSettings(this.discordForm.value)
      .subscribe({
        next: (value) => {
          this.isDiscordSettingsSaving = false;
          this.discordForm.patchValue(value);
          this.discordForm.controls['token'].disable();
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('Discord Settings', 'Discord server successfully configured.', 'success')));
          setTimeout(() => (this.loadAccount(true), location.reload()), 1000);
        },
        error: (err) => {
          this.isDiscordSettingsSaving = false;
          if (err.error?.details) this.tokenErrorMessage = err.error.details;
          if (err?.details) this.tokenErrorMessage = err.details;
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('Discord Settings', this.tokenErrorMessage, 'error')));
        }
      });
  }

}
