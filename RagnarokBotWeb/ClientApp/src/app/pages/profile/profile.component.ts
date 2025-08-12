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
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { COUNTRIES } from '../../constants';
import { Observable, take } from 'rxjs';
import { ApplicationService } from '../../services/application.service';

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
    NzButtonModule,
    NzAlertModule
  ]
})
export class ProfileComponent implements OnInit {
  account?: AccountDto;
  profileForm!: FormGroup;
  private fb = inject(NonNullableFormBuilder);
  mismatchPass: boolean;
  loading = false;
  countries = COUNTRIES;

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
      confirmPassword: [null],
      country: [null, [Validators.required]]
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

    this.mismatchPass = this.profileForm.value.password !== this.profileForm.value.confirmPassword;
    if (this.mismatchPass) return;

    this.loading = true;
    this.authService.update(this.profileForm.value)
      .subscribe({
        next: (account => {
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('', 'Profile successfully updated.', 'success')));
          this.loading = false;
        }),
        error: (err) => {
          this.eventManager.broadcast(new EventWithContent<Alert>('alert', new Alert('Error', err.error.details, 'error')));
          this.loading = false;
        }
      });
  }

}
