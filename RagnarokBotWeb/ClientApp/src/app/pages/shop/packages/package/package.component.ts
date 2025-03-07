import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormGroup, FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { AuthenticationService } from '../../../../services/authentication.service';
import { AccountDto } from '../../../../models/account.dto';
import { Router } from '@angular/router';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzListModule } from 'ng-zorro-antd/list';
import { NzTypographyModule } from 'ng-zorro-antd/typography';
import { PackageService } from '../../../../services/package.service';
import { EventManager, EventWithContent } from '../../../../services/event-manager.service';
import { Alert } from '../../../../models/alert';

@Component({
  selector: 'app-package',
  templateUrl: './package.component.html',
  styleUrls: ['./package.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    NzCardModule,
    NzFormModule,
    NzInputModule,
    NzSelectModule,
    NzSpaceModule,
    NzIconModule,
    NzCheckboxModule,
    NzListModule,
    NzTypographyModule,
    NzButtonModule
  ]
})
export class PackageComponent implements OnInit {
  account?: AccountDto;
  packageForm!: FormGroup;
  private fb = inject(NonNullableFormBuilder);
  commands: string[] = [];

  constructor(
    private readonly authService: AuthenticationService,
    private readonly packageService: PackageService,
    private readonly eventManager: EventManager,
    private readonly router: Router) {
    this.packageForm = this.fb.group({
      id: [null],
      name: [null, [Validators.required]],
      description: [null],
      price: [null],
      vipPrice: [null],
      isVipOnly: [false],
      discordChannelId: [null],
      purchaseCooldownSeconds: [null],
      stockPerPlayer: [null],
      enabled: [true],
      isBlockPurchaseRaidTime: [false],
      deliveryText: ['{playerName} your order #{orderId} of {packageName} was delivered.']
    });
  }

  ngOnInit() {
    this.loadAccount();
  }

  loadAccount() {
    this.authService.account()
      .subscribe({
        next: (account) => {
          this.account = account;
        }
      });
  }

  goBack() {
    this.router.navigate(['servers', this.account!.serverId, 'shop', 'packages']);
  }

  addComand(command: string) {
    if (command?.length === 0) return;
    this.commands.push(command);
  }

  removeCommand(index: number) {
    this.commands.splice(index, 1);
  }

  save() {
    if (this.packageForm.invalid) {
      Object.values(this.packageForm.controls).forEach(control => {
        if (control.invalid) {
          control.markAsDirty();
          control.updateValueAndValidity({ onlySelf: true });
        }
      });
      return;
    }

    this.packageService.savePackage(this.packageForm.value)
      .subscribe({
        next: (value) => {
          setTimeout(() => this.eventManager.broadcast(new EventWithContent('alert', new Alert('', `Package ${value.name} successfully created.`))), 800);
          this.goBack();
        }
      });
  }

}
