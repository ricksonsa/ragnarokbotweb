import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormGroup, FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
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
import { NzPopoverModule } from 'ng-zorro-antd/popover';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzTypographyModule } from 'ng-zorro-antd/typography';
import { AuthenticationService } from '../../../../services/authentication.service';
import { AccountDto } from '../../../../models/account.dto';
import { EventManager, EventWithContent } from '../../../../services/event-manager.service';
import { Alert } from '../../../../models/alert';
import { getDaysBetweenDates } from '../../../../core/functions/date.functions';

@Component({
  selector: 'app-player',
  templateUrl: './player.component.html',
  styleUrls: ['./player.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    NzCardModule,
    NzFormModule,
    NzInputModule,
    NzSelectModule,
    NzModalModule,
    NzSpaceModule,
    NzIconModule,
    NzCheckboxModule,
    NzPopoverModule,
    NzListModule,
    NzTypographyModule,
    NzPopconfirmModule,
    NzButtonModule
  ]
})
export class PlayerComponent implements OnInit {
  playerForm!: FormGroup;
  private fb = inject(NonNullableFormBuilder);
  account?: AccountDto;
  addingCoins = false;
  addingGold = false;
  addingMoney = false;
  addingFame = false;
  addingCoinsLoader = false;

  constructor(
    private readonly authService: AuthenticationService,
    private readonly eventManager: EventManager,
    private route: ActivatedRoute,
    private readonly router: Router) {
    this.playerForm = this.fb.group({
      id: [null],
      name: [null],
      steamName: [null],
      steamId64: [null],
      discordName: [null],
      discordId: [null],
      money: [null],
      gold: [null],
      fame: [null],
      coin: [null],
      isVip: [false],
      vipExpiresAt: [null]
    });
  }

  ngOnInit() {
    this.loadAccount();

    this.route.data.subscribe(data => {
      var player = data['player'];
      if (player) {
        this.playerForm.patchValue(player);
      }
    });
  }

  confirmVipRemove(id: number) {

  }
  getDate(date: Date) {
    const remaining = getDaysBetweenDates(new Date(date));
    return `${remaining} days to expire`;
  }

  loadAccount() {
    this.authService.account()
      .subscribe({
        next: (account) => {
          this.account = account;
        }
      });
  }

  confirmBan(id: number) {

  }

  confirmSilence(id: number) {
  }

  addCoinsValue = 0;
  addCoins() {
    this.addingCoins = false;
    this.playerForm.controls['coin'].patchValue(+this.playerForm.controls['coin'].value + this.addCoinsValue);
    this.eventManager.broadcast(new EventWithContent('alert', new Alert('Player', `You have successfully added ${this.addCoinsValue} bot coins to the player ${this.playerForm.value.name}`, 'success')));
    this.addCoinsValue = 0;
  }

  addGoldValue = 0;
  addGold() {
    this.addingGold = false;
    this.playerForm.controls['gold'].patchValue(+this.playerForm.controls['gold'].value + this.addGoldValue);
    this.eventManager.broadcast(new EventWithContent('alert', new Alert('Player', `You have successfully added ${this.addGoldValue} ingame gold to the player ${this.playerForm.value.name}`, 'success')));
    this.addGoldValue = 0;
  }

  addFameValue = 0;
  addFame() {
    this.addingGold = false;
    this.playerForm.controls['fame'].patchValue(+this.playerForm.controls['fame'].value + this.addFameValue);
    this.eventManager.broadcast(new EventWithContent('alert', new Alert('Player', `You have successfully added ${this.addFameValue} ingame fame to the player ${this.playerForm.value.name}`, 'success')));
    this.addFameValue = 0;
  }

  addMoneyValue = 0;
  addMoney() {
    this.addingMoney = false;
    this.playerForm.controls['money'].patchValue(+this.playerForm.controls['money'].value + this.addMoneyValue);
    this.eventManager.broadcast(new EventWithContent('alert', new Alert('Player', `You have successfully added ${this.addMoneyValue} ingame money to the player ${this.playerForm.value.name}`, 'success')));
    this.addMoneyValue = 0;
  }


  goBack() {
    this.router.navigate(['servers', this.account!.serverId, 'management', 'players']);
  }

  save() {
    if (this.playerForm.invalid) {
      Object.values(this.playerForm.controls).forEach(control => {
        if (control.invalid) {
          control.markAsDirty();
          control.updateValueAndValidity({ onlySelf: true });
        }
      });
      return;
    }

  }
}
