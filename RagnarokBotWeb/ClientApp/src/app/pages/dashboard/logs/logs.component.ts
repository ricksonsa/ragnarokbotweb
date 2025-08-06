import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzSegmentedModule } from 'ng-zorro-antd/segmented';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { debounceTime, distinctUntilChanged, Observable, of, startWith, Subscription, switchMap, tap } from 'rxjs';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSkeletonModule } from 'ng-zorro-antd/skeleton';
import { LogService } from '../../../services/logs.service';
import { GameKillData } from '../../../models/game-kill-data';
import { LockpickLog } from '../../../models/lockpick';
import { GenericLogValue } from '../../../models/generic-log-value';

@Component({
  selector: 'app-logs',
  templateUrl: './logs.component.html',
  styleUrls: ['./logs.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    NzCardModule,
    NzFormModule,
    NzSpaceModule,
    NzSegmentedModule,
    NzInputModule,
    NzDatePickerModule,
    NzDividerModule,
    NzIconModule,
    NzButtonModule,
    NzSkeletonModule,
    NzEmptyModule
  ]
})
export class LogsComponent implements OnInit, OnDestroy {
  options = ['Login', 'Lockpick', 'Kills', 'Economy', 'Chests', 'Vehicles', "Violations"];
  selectedDateFrom = new Date();
  selectedDateTo = new Date();
  selectedSegment = 'Login';
  filter$: Observable<string[]> = of([]);
  searchControl = new FormControl();
  loading = false;
  subs: Subscription;
  kills: GameKillData[] = [];
  lockpicks: LockpickLog[] = [];
  economy: GenericLogValue[] = [];
  vehicles: GenericLogValue[] = [];
  logins: GenericLogValue[] = [];
  buriedChests: GenericLogValue[] = [];
  violations: GenericLogValue[] = [];
  constructor(private readonly logService: LogService) {
  }

  ngOnInit() {
    this.selectedDateFrom.setDate(new Date().getDate() - 5);
    this.loadLogs();
  }

  ngOnDestroy(): void {
    this.subs?.unsubscribe();
  }

  changeSegment(segment: string) {
    this.searchControl.patchValue('');
    this.loadLogs();
  }

  loadLogs() {
    switch (this.selectedSegment) {
      case 'Kills':
        this.loadKills(this.selectedDateFrom, this.selectedDateTo);
        break;
      case 'Lockpick':
        this.loadLockpicks(this.selectedDateFrom, this.selectedDateTo);
        break;
      case 'Economy':
        this.loadEconomy(this.selectedDateFrom, this.selectedDateTo);
        break;
      case 'Vehicles':
        this.loadVehicles(this.selectedDateFrom, this.selectedDateTo);
        break;
      case 'Login':
        this.loadLogins(this.selectedDateFrom, this.selectedDateTo);
        break;
      case 'Chests':
        this.loadBuriedChests(this.selectedDateFrom, this.selectedDateTo);
        break;
      case 'Violations':
        this.loadViolations(this.selectedDateFrom, this.selectedDateTo);
        break;
    }
  }

  getDate(dateString: string) {
    return new Date(dateString);
  }

  getVehicles() {
    if (this.searchControl.value === '' || this.searchControl.value === ' ' || this.searchControl.value == null)
      return this.vehicles;
    else
      return this.vehicles.filter(x => x.line.toLowerCase().includes(this.searchControl.value.toLowerCase()));
  }

  getEconomy() {
    if (this.searchControl.value === '' || this.searchControl.value === ' ' || this.searchControl.value == null)
      return this.economy;
    else
      return this.economy.filter(x => x.line.toLowerCase().includes(this.searchControl.value.toLowerCase()));
  }

  getKills() {
    if (this.searchControl.value === '' || this.searchControl.value === ' ' || this.searchControl.value == null)
      return this.kills;
    else
      return this.kills.filter(x => x.line.toLowerCase().includes(this.searchControl.value.toLowerCase()));
  }

  getLockpicks() {
    if (this.searchControl.value === '' || this.searchControl.value === ' ' || this.searchControl.value == null)
      return this.lockpicks;
    else
      return this.lockpicks.filter(x => x.line.toLowerCase().includes(this.searchControl.value.toLowerCase()));
  }

  getLogins() {
    if (this.searchControl.value === '' || this.searchControl.value === ' ' || this.searchControl.value == null)
      return this.logins;
    else
      return this.logins.filter(x => x.line.toLowerCase().includes(this.searchControl.value.toLowerCase()));
  }

  getBuriedChests() {
    if (this.searchControl.value === '' || this.searchControl.value === ' ' || this.searchControl.value == null)
      return this.buriedChests;
    else
      return this.buriedChests.filter(x => x.line.toLowerCase().includes(this.searchControl.value.toLowerCase()));
  }

  getViolations() {
    if (this.searchControl.value === '' || this.searchControl.value === ' ' || this.searchControl.value == null)
      return this.violations;
    else
      return this.violations.filter(x => x.line.toLowerCase().includes(this.searchControl.value.toLowerCase()));
  }

  loadLogins(from: Date, to: Date) {
    this.loading = true;
    this.subs?.unsubscribe();
    this.subs = this.logService.getLogins(from, to)
      .subscribe({
        next: (logins) => {
          this.loading = false;
          logins.forEach(e => e.line = e.line.substring(21));
          this.logins = logins;
        },
        error: (err) => {
          this.loading = false;
        }
      })
  }

  loadVehicles(from: Date, to: Date) {
    this.loading = true;
    this.subs?.unsubscribe();
    this.subs = this.logService.getVehicles(from, to)
      .subscribe({
        next: (vehicles) => {
          this.loading = false;
          vehicles.forEach(e => e.line = e.line.substring(21));
          this.vehicles = vehicles;
        },
        error: (err) => {
          this.loading = false;
        }
      })
  }

  loadEconomy(from: Date, to: Date) {
    this.loading = true;
    this.subs?.unsubscribe();
    this.subs = this.logService.getEconomy(from, to)
      .subscribe({
        next: (economy) => {
          this.loading = false;
          economy.forEach(e => e.line = e.line.substring(21));
          this.economy = economy;
        },
        error: (err) => {
          this.loading = false;
        }
      })
  }

  loadLockpicks(from: Date, to: Date) {
    this.loading = true;
    this.subs?.unsubscribe();
    this.subs = this.logService.getLockpickLog(from, to)
      .subscribe({
        next: (lockpicks) => {
          this.loading = false;
          this.lockpicks = lockpicks;
        },
        error: (err) => {
          this.loading = false;
        }
      })
  }

  loadKills(from: Date, to: Date) {
    this.loading = true;
    this.subs?.unsubscribe();
    this.subs = this.logService.getKillsLog(from, to)
      .subscribe({
        next: (kills) => {
          this.loading = false;
          this.kills = kills;
        },
        error: (err) => {
          this.loading = false;
        }
      })
  }

  loadBuriedChests(from: Date, to: Date) {
    this.loading = true;
    this.subs?.unsubscribe();
    this.subs = this.logService.getBuriedChests(from, to)
      .subscribe({
        next: (buriedChests) => {
          this.loading = false;
          buriedChests.forEach(e => e.line = e.line.substring(21));
          this.buriedChests = buriedChests;
        },
        error: (err) => {
          this.loading = false;
        }
      })
  }

  loadViolations(from: Date, to: Date) {
    this.loading = true;
    this.subs?.unsubscribe();
    this.subs = this.logService.getViolations(from, to)
      .subscribe({
        next: (violations) => {
          this.loading = false;
          violations.forEach(e => e.line = e.line.substring(21));
          this.violations = violations;
        },
        error: (err) => {
          this.loading = false;
        }
      })
  }
}

