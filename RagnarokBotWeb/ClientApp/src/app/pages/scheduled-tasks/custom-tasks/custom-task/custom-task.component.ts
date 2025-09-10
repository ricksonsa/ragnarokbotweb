import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormGroup, NonNullableFormBuilder, FormControl, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
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
    CronEditorModule
  ]
})
export class CustomTaskComponent implements OnInit {
  taskForm!: FormGroup;
  commandsForm!: FormGroup;
  serverSettingsForm!: FormGroup;
  private fb = inject(NonNullableFormBuilder);
  cronEditor = false;
  cron: any;
  times: string[] = [];
  conditions: string[] = [];
  commands: string;
  loading = false;

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

  constructor(
    private readonly router: Router,
    private route: ActivatedRoute,
    private readonly taskService: TaskService,
    private readonly eventManager: EventManager) {
    this.taskForm = this.fb.group({
      id: [0],
      name: [null, [Validators.required]],
      description: [null],
      taskType: [0, [Validators.required]],
      enabled: [true, [Validators.required]],
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

  }

  ngOnInit() {
    this.route.data.subscribe(data => {
      var item = data['customTask'];
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
      default: return undefined;
    }
  }

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
    this.loading = true;

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
