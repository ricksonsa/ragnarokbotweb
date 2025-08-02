import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, ViewEncapsulation } from '@angular/core';
import { FormGroup, FormsModule, NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzSwitchModule } from 'ng-zorro-antd/switch';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzTypographyModule } from 'ng-zorro-antd/typography';
import { AccountDto } from '../../../models/account.dto';
import { AuthenticationService } from '../../../services/authentication.service';
import { ServerService } from '../../../services/server.service';
import { Alert } from '../../../models/alert';
import { EventManager, EventWithContent } from '../../../services/event-manager.service';

@Component({
  selector: 'app-kill-feed',
  templateUrl: './kill-feed.component.html',
  styleUrls: ['./kill-feed.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    NzCardModule,
    NzFormModule,
    NzSpaceModule,
    NzButtonModule,
    NzInputModule,
    NzCheckboxModule,
    NzSelectModule,
    NzPopconfirmModule,
    NzAlertModule,
    NzTypographyModule,
    NzDividerModule,
    NzSwitchModule,
    NzIconModule
  ]
})
export class KillFeedComponent implements OnInit {
  account?: AccountDto;
  private fb = inject(NonNullableFormBuilder);
  loading = false;
  killfeedForm!: FormGroup;

  constructor(
    private readonly authService: AuthenticationService,
    private readonly serverService: ServerService,
    private readonly eventManager: EventManager
  ) {
    this.killfeedForm = this.fb.group({
      useKillFeed: [false],
      showKillDistance: [false],
      showKillSector: [false],
      showKillWeapon: [false],
      showKillerName: [false],
      showMineKill: [false],
      showSameSquadKill: [false],
      showKillOnMap: [false],
      showKillCoordinates: [false],
      killAnnounceText: [null]
    });
  }

  ngOnInit() {
    this.loadAccount();
  }

  save() {
    this.loading = true;
    this.serverService.updateKillFeed(this.killfeedForm.value)
      .subscribe({
        next: (server) => {
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('Server', `Kill Feed Update`, 'success')));
          this.loading = false;
        },
        error: (err) => {
          this.loading = false;
        }
      })
  }

  loadAccount(force = false) {
    this.authService.account(force)
      .subscribe({
        next: (account) => {
          this.account = account;
          this.killfeedForm.patchValue(account.server);
        }
      });
  }
}
