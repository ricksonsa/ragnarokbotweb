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
import { NzTypographyModule } from 'ng-zorro-antd/typography';
import { AccountDto } from '../../models/account.dto';
import { Alert } from '../../models/alert';
import { AuthenticationService } from '../../services/authentication.service';
import { EventManager, EventWithContent } from '../../services/event-manager.service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss'],
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
    NzPopconfirmModule,
    NzButtonModule
  ]
})
export class ProfileComponent implements OnInit {
  account?: AccountDto;
  profileForm!: FormGroup;
  private fb = inject(NonNullableFormBuilder);

  constructor(
    private readonly authService: AuthenticationService,
    private readonly eventManager: EventManager,
    private route: ActivatedRoute,
    private readonly router: Router) {
    this.profileForm = this.fb.group({
      id: [null],
      name: [null, [Validators.required]],
      email: [null, [Validators.required]],
      password: [null],
      confirmPassword: [null]
    });
  }

  ngOnInit() {
    this.loadAccount();
  }

  loadAccount() {
    this.authService.account()
      .subscribe({
        next: (account) => {
          this.profileForm.patchValue(account);
        }
      });
  }

  save() {
    if (this.profileForm.invalid) {
      Object.values(this.profileForm.controls).forEach(control => {
        if (control.invalid) {
          control.markAsDirty();
          control.updateValueAndValidity({ onlySelf: true });
        }
      });
      return;
    }

    // this.itemService.saveItem(this.profileForm.value)
    //   .subscribe({
    //     next: (value) => {
    //       setTimeout(() => this.eventManager.broadcast(new EventWithContent('alert', new Alert('', `Profile updated successfully.`))), 800);
    //     }
    //   });
  }

}
