import { Component, inject, OnInit } from '@angular/core';
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
    NzAlertModule,
    NzDividerModule,
    NzSwitchModule,
    NzIconModule
  ]
})
export class DiscordComponent implements OnInit {
  account?: AccountDto;
  private fb = inject(NonNullableFormBuilder);

  killfeedForm!: FormGroup;
  lockpickFeed!: FormGroup;
  discordForm!: FormGroup;
  tokenErrorMessage: string;

  isDiscordSettingsSaving = false;

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
      sideKillerName: [false],
      sideMineKill: [false],
      hideKillerName: [false],
      showSameSquadKill: [false]
    });

    this.lockpickFeed = this.fb.group({
      useLockpickFeed: [false],
      showLockpickSector: [false],
      showLockpickContainerName: [false],
      sendVipLockpickAlert: [false]
    });
  }

  ngOnInit() {
    this.authService.account()
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

  save() { }

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
