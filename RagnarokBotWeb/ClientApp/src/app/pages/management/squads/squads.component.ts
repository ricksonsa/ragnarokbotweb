import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormsModule, FormControl } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzPopoverModule } from 'ng-zorro-antd/popover';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzTableModule } from 'ng-zorro-antd/table';
import { EventManager } from '../../../services/event-manager.service';
import { Observable, of, BehaviorSubject, tap, switchMap } from 'rxjs';
import { WarzoneDto } from '../../../models/warzone.dto';
import { ServerService } from '../../../services/server.service';
import { Squad } from '../../../models/squad.dto';

@Component({
  selector: 'app-squads',
  templateUrl: './squads.component.html',
  styleUrls: ['./squads.component.scss'],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    FormsModule,
    NzPopoverModule,
    NzCardModule,
    NzIconModule,
    NzInputModule,
    NzPopconfirmModule,
    NzTableModule,
    NzButtonModule,
    NzSpaceModule,
    NzDividerModule
  ]
})
export class SquadsComponent implements OnInit {
  dataSource: WarzoneDto[] = [];
  total = 0;
  pageIndex = 1;
  pageSize = 10;
  searchControl = new FormControl();
  data$: Observable<Squad[]> = of([]);
  isLoading = true;

  constructor(
    private readonly serverService: ServerService,
    private readonly eventManager: EventManager,
    private readonly router: Router) { }

  pageIndex$ = new BehaviorSubject<number>(1);
  pageSize$ = new BehaviorSubject<number>(10);

  ngOnInit() {
    this.data$ = this.serverService.getSquads()
      .pipe(tap((squads) => {
        this.isLoading = false;
        this.total = squads.length;
      }),
        switchMap(page => of(page)));
  }

  pageIndexChange(index: number) {
    this.pageIndex$.next(index);
  }

  pageSizeChange(size: number) {
    this.pageSize$.next(size);
  }

}
