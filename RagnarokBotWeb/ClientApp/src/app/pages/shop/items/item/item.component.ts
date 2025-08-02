import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormGroup, FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzListModule } from 'ng-zorro-antd/list';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzTypographyModule } from 'ng-zorro-antd/typography';
import { AccountDto } from '../../../../models/account.dto';
import { AuthenticationService } from '../../../../services/authentication.service';
import { ActivatedRoute, Router } from '@angular/router';
import { ItemService } from '../../../../services/item.service';
import { EventManager, EventWithContent } from '../../../../services/event-manager.service';
import { Alert } from '../../../../models/alert';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';

@Component({
  selector: 'app-item',
  templateUrl: './item.component.html',
  styleUrls: ['./item.component.scss'],
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
export class ItemComponent implements OnInit {
  account?: AccountDto;
  itemForm!: FormGroup;
  private fb = inject(NonNullableFormBuilder);

  constructor(
    private readonly authService: AuthenticationService,
    private readonly eventManager: EventManager,
    private readonly itemService: ItemService,
    private route: ActivatedRoute,
    private readonly router: Router) {
    this.itemForm = this.fb.group({
      id: [null],
      name: [null, [Validators.required]],
      code: [null, [Validators.required]]
    });
  }

  ngOnInit() {
    this.loadAccount();

    this.route.data.subscribe(data => {
      var item = data['item'];
      if (item) {
        this.itemForm.patchValue(item);
      }
    });
  }

  confirmDelete(id: number) {
    this.itemService.deleteItem(id)
      .subscribe({
        next: () => {
          this.goBack();
        }
      });
  }

  cancelDelete() { }

  loadAccount() {
    this.authService.account()
      .subscribe({
        next: (account) => {
          this.account = account;
        }
      });
  }

  goBack() {
    this.router.navigate(['shop', 'items']);
  }

  save() {
    if (this.itemForm.invalid) {
      Object.values(this.itemForm.controls).forEach(control => {
        if (control.invalid) {
          control.markAsDirty();
          control.updateValueAndValidity({ onlySelf: true });
        }
      });
      return;
    }

    this.itemService.saveItem(this.itemForm.value)
      .subscribe({
        next: (value) => {
          setTimeout(() => this.eventManager.broadcast(new EventWithContent('alert', new Alert('', `Package ${value.name} successfully created.`))), 800);
          this.goBack();
        }
      });
  }
}
