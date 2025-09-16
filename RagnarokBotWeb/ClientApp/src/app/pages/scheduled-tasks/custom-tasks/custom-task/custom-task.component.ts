import { CommonModule } from '@angular/common';
import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { FormGroup, NonNullableFormBuilder, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
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
import { CronEditorModule, CronOptions } from 'ngx-cron-editor';
import { TaskService } from '../../../../services/task.service';
import { Alert } from '../../../../models/alert';
import { EventManager, EventWithContent } from '../../../../services/event-manager.service';
import { CustomTaskDto } from '../../../../models/custom-task.dto';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { AuthenticationService } from '../../../../services/authentication.service';
import { AccountDto } from '../../../../models/account.dto';
import { firstValueFrom, Subscription } from 'rxjs';
import { PackageService } from '../../../../services/package.service';
import { TaxiService } from '../../../../services/taxi.service';
import { IdsDto } from '../../../../models/ids-dto';

@Component({
  selector: 'app-custom-task',
  templateUrl: './custom-task.component.html',
  styleUrls: ['./custom-task.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
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
    CronEditorModule,
    NzDatePickerModule
  ]
})
export class CustomTaskComponent implements OnInit, OnDestroy {
  taskForm!: FormGroup;
  commandsForm!: FormGroup;
  serverSettingsForm!: FormGroup;
  switchForm!: FormGroup;
  private fb = inject(NonNullableFormBuilder);
  cronEditor = false;
  cron: any;
  times: string[] = [];
  conditions: string[] = [];
  commands: string;
  loading = false;
  items: IdsDto[] = [];

  public cronOptions: CronOptions = {
    defaultTime: "00:00:00",
    hideMinutesTab: false,
    hideHourlyTab: false,
    hideDailyTab: false,
    hideWeeklyTab: false,
    hideMonthlyTab: false,
    hideYearlyTab: false,
    hideAdvancedTab: false,
    hideSpecificWeekDayTab: false,
    hideSpecificMonthWeekTab: false,
    use24HourTime: true,
    hideSeconds: false,
    cronFlavor: "standard" //standard or quartz
  };
  account: AccountDto;
  switchType$: Subscription;

  constructor(
    private readonly authService: AuthenticationService,
    private readonly router: Router,
    private route: ActivatedRoute,
    private readonly taskService: TaskService,
    private readonly packageService: PackageService,
    private readonly taxiService: TaxiService,
    private readonly eventManager: EventManager) {
    this.taskForm = this.fb.group({
      id: [0],
      name: [null, [Validators.required]],
      description: [null],
      taskType: [0, [Validators.required]],
      expireAt: [],
      enabled: [true, [Validators.required]],
      deleteExpired: [false, [Validators.required]],
      isBlockPurchaseRaidTime: [false, [Validators.required]],
      minPlayerOnline: [null],
      cron: [null, [Validators.required]],
      commands: [null],
      startMessage: [null]
    });

    this.commandsForm = this.fb.group({
      commands: [null, Validators.required]
    });

    this.serverSettingsForm = this.fb.group({
      key: [null, [Validators.required]],
      value: [null, [Validators.required]],
    });

    this.switchForm = this.fb.group({
      switchType: [null, [Validators.required]],
      switchValue: [null, [Validators.required]],
      value: [null, [Validators.required]],
    });


  }
  ngOnDestroy(): void {
    this.switchType$?.unsubscribe();
  }

  ngOnInit() {
    this.route.data.subscribe(data => {
      var item = data['customTask'];
      var account = data['account'];
      this.account = account;
      if (item) {
        const task = item as CustomTaskDto;
        this.cron = task.cron;
        this.commands = task.commands;
        this.taskForm.patchValue(task);

        if (task.taskType === 2) {
          const key = task.commands.split('=')[0];
          const value = task.commands.split('=')[1];
          this.serverSettingsForm.patchValue({
            key,
            value
          });
        }

        if (task.taskType === 3) {
          var switchType = task.commands.substring(0, task.commands.indexOf(':'));

          switch (switchType) {
            case 'taxi':
              firstValueFrom(this.taxiService.getTaxiIds())
                .then(items => {
                  this.items = items;
                  this.setSwitchForm(task, switchType);
                });
              break;

            case 'package':
              firstValueFrom(this.packageService.getPackIds())
                .then(items => {
                  this.items = items;
                  this.setSwitchForm(task, switchType);
                });
              break;

            case 'uav':
              this.items.push({ id: this.account.server.uav.id, name: this.account.server.uav.name });
              this.switchForm.controls['switchValue'].patchValue(this.account.server.uav.id);
              this.setSwitchForm(task, switchType);
              break;

            case 'exchange':
              this.items.push({ id: this.account.server.exchange.id, name: this.account.server.exchange.name });
              this.switchForm.controls['switchValue'].patchValue(this.account.server.exchange.id);
              this.setSwitchForm(task, switchType);
              break;

            case 'shop':
              this.items.push({ id: this.account.server.id, name: 'Shop' });
              this.switchForm.controls['switchValue'].patchValue(this.account.server.id);
              this.setSwitchForm(task, switchType);
              break;

            case 'rank':
              this.items.push({ id: this.account.server.id, name: 'Ranking Rewards' });
              this.switchForm.controls['switchValue'].patchValue(this.account.server.id);
              this.setSwitchForm(task, switchType);
              break;

            case 'task':
              firstValueFrom(this.taskService.getTaskIds())
                .then(items => {
                  this.items = items;
                  this.setSwitchForm(task, switchType);
                });
              break;
          }
        }
      }
    });

    this.switchType$ = this.switchForm.controls['switchType'].valueChanges
      .subscribe({
        next: (value) => {
          this.items = [];
          this.switchForm.controls['switchValue'].patchValue(null);
          switch (value) {
            case 'taxi':
              firstValueFrom(this.taxiService.getTaxiIds())
                .then(items => {
                  this.items = items;
                });
              break;

            case 'package':
              firstValueFrom(this.packageService.getPackIds())
                .then(items => {
                  this.items = items;
                });
              break;

            case 'uav':
              this.items.push({ id: this.account.server.uav.id, name: this.account.server.uav.name });
              this.switchForm.controls['switchValue'].patchValue(this.account.server.uav.id);
              break;

            case 'exchange':
              this.items.push({ id: this.account.server.exchange.id, name: this.account.server.exchange.name });
              this.switchForm.controls['switchValue'].patchValue(this.account.server.exchange.id);
              break;

            case 'shop':
              console.log('account', this.account);
              this.items.push({ id: this.account.server.id, name: 'Shop' });
              this.switchForm.controls['switchValue'].patchValue(this.account.serverId);
              break;

            case 'rank':
              this.items.push({ id: this.account.server.id, name: 'Ranking Rewards' });
              this.switchForm.controls['switchValue'].patchValue(this.account.server.id);
              break;

            case 'task':
              firstValueFrom(this.taskService.getTaskIds())
                .then(items => {
                  this.items = items;
                });
              break;
          }
        }
      })
  }

  private setSwitchForm(task: CustomTaskDto, switchType: string) {
    var switchValue = task.commands.split("=")[0].substring(task.commands.indexOf(':') + 1);
    var value = task.commands.substring(task.commands.indexOf('=') + 1);
    this.switchForm.patchValue({
      switchType,
      switchValue: +switchValue,
      value: +value
    });
  }

  loadAccount() {
    this.authService.account()
      .subscribe({
        next: (account) => {
          this.account = account;

          if (this.account?.server?.exchange)

            if (this.account?.server?.uav)
              this.items.push({ id: this.account.server.uav.id, name: this.account.server.uav.name });
        }
      });
  }

  goBack() {
    this.router.navigate(['scheduled-tasks', 'custom-tasks']);
  }
  onCronChange(cron: string) {
    this.taskForm.controls['cron'].patchValue(cron);
  }

  onCommandsChange(commands: string) {
    this.taskForm.controls['commands'].patchValue(commands);
  }

  GetCommandValuesTitle() {
    switch (this.taskForm.value.taskType) {
      case 0: return 'Commands';
      case 1: return 'Commands';
      case 2: return 'Server Settings';
      case 3: return 'Enable/Disable';
      default: return undefined;
    }
  }

  disabledDate = (current: Date): boolean => {
    // Can not select days before today
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return current && current < today;
  };

  save() {
    if (this.taskForm.invalid) {
      Object.values(this.taskForm.controls).forEach(control => {
        if (control.invalid) {
          control.markAsDirty();
          control.updateValueAndValidity({ onlySelf: true });
        }
      });
      return;
    }

    const task = this.taskForm.value;
    if (task.taskType === 2) {
      if (this.serverSettingsForm.invalid) {
        Object.values(this.serverSettingsForm.controls).forEach(control => {
          if (control.invalid) {
            control.markAsDirty();
            control.updateValueAndValidity({ onlySelf: true });
          }
        });
        return;
      }
      task.commands = `${this.serverSettingsForm.value.key}=${this.serverSettingsForm.value.value}`;
    }
    if (task.taskType === 3) {
      if (this.switchForm.invalid) {
        Object.values(this.switchForm.controls).forEach(control => {
          if (control.invalid) {
            control.markAsDirty();
            control.updateValueAndValidity({ onlySelf: true });
          }
        });
        return;
      }
      task.commands = `${this.switchForm.value.switchType}:${this.switchForm.value.switchValue}=${this.switchForm.value.value}`;
    }
    this.loading = true;


    if (this.switchForm.value.expireAt) {
      this.switchForm.value.expireAt = (this.switchForm.value.expireAt as Date).toISOString();
    }

    this.taskService.save(task)
      .subscribe({
        next: (value) => {
          this.loading = false;
          var message = `Task ${value.name} successfully created.`;
          if (this.taskForm.value.id != 0) {
            message = `Task ${value.name} successfully updated.`;
          }
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('', message, 'success')));
          this.goBack();

        },
        error: (err) => {
          var msg = err.error?.details ?? 'One or more validation errors ocurred.';
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('', msg, 'error')));
          this.loading = false;
        }
      });
  }

}
