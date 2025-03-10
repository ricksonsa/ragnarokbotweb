import { Component, inject, OnInit } from '@angular/core';
import { NzFormModule } from 'ng-zorro-antd/form';
import { FormGroup, FormsModule, NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
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

  constructor(private readonly authService: AuthenticationService) {
    this.killfeedForm = this.fb.group({
      useKillFeed: [false],
      showKillDistance: [false],
      showKillSector: [false],
      showKillWeapon: [false],
      sideKillerName: [false],
      sideMineKill: [false],
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
        }
      });
  }

  save() { }

}
