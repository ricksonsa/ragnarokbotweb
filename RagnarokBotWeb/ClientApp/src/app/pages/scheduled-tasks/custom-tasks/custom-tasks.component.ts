import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormControl, FormGroup, FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
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

import cronstrue from 'cronstrue';
import { CronEditorModule, CronOptions } from 'ngx-cron-editor';

@Component({
  selector: 'app-custom-tasks',
  templateUrl: './custom-tasks.component.html',
  styleUrls: ['./custom-tasks.component.scss'],
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
    CronEditorModule
  ]
})
export class CustomTasksComponent implements OnInit {
  taskForm!: FormGroup;
  private fb = inject(NonNullableFormBuilder);
  cronEditor = false;
  cron = new FormControl();
  times: string[] = [];
  conditions: string[] = [];

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

  constructor() {
    this.taskForm = this.fb.group({
      id: [null],
      name: [null, [Validators.required]],
      scheduledTaskType: ["0", [Validators.required]],
      isActive: [true, [Validators.required]],
      blockedRaidTimes: [false, [Validators.required]],
    });

  }

  ngOnInit() {
  }

  addCondition(type: string, condition: string, value: string) {
    if (value?.length === 0 
      || type?.length === 0 
      || condition?.length === 0 ) return;

      if (this.conditions.some(c => c.startsWith(type))) {
        this.removeCondition(this.conditions.findIndex(c => c.startsWith(type)));
      }

    this.conditions.push(type + condition + value);
  }

  removeCondition(index: number) {
    this.conditions.splice(index, 1);
  }

  addTime() {
    var value = this.cron.value!.toString();
    if (!this.times.find(time => time === value)) this.times.push(value);
  }

  removeTime(index: number) {
    this.times.splice(index, 1);
  }

  getTimes() {
    return this.times.map(time => {
      return cronstrue.toString(time);
    });
  }

  GetCommandValuesTitle() {
    switch (this.taskForm.value.scheduledTaskType) {
      case '0': return 'Commands';
      case '1': return 'Server Settings';
      default: return undefined;
    }
  }

  save() {

  }
}
