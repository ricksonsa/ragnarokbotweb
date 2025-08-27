import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzTableModule } from 'ng-zorro-antd/table';
import { PlayerService } from '../../../services/player.service';
import { PlayerDto } from '../../../models/player.dto';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { RouterModule } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { BehaviorSubject, combineLatest, debounceTime, distinctUntilChanged, Observable, of, startWith, switchMap, take, tap } from 'rxjs';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzPopoverModule } from 'ng-zorro-antd/popover';
import { getDaysBetweenDates } from '../../../core/functions/date.functions';

@Component({
  selector: 'app-players',
  templateUrl: './vips.component.html',
  styleUrls: ['./vips.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    NzInputModule,
    RouterModule,
    FormsModule,
    NzSpaceModule,
    NzButtonModule,
    NzIconModule,
    NzCardModule,
    NzDividerModule,
    NzTableModule,
    NzPopoverModule
  ]
})
export class VipsComponent implements OnInit {
  dataSource: PlayerDto[] = [];
  searchControl = new FormControl();
  suggestions$: Observable<PlayerDto[]> = of([]);
  isLoading = true;
  total = 0;
  pageIndex = 1;
  pageSize = 10;

  constructor(private readonly playerService: PlayerService) {
    this.searchControl.patchValue('');
  }

  pageIndex$ = new BehaviorSubject<number>(1);
  pageSize$ = new BehaviorSubject<number>(10);

  ngOnInit() {
    this.suggestions$ = combineLatest([
      this.searchControl.valueChanges.pipe(startWith(''), debounceTime(300), distinctUntilChanged()),
      this.pageIndex$,
      this.pageSize$
    ]).pipe(
      tap(() => {
        this.isLoading = true;
      }),
      switchMap(([query, pageIndex, pageSize]) =>
        this.playerService.getVipPlayers(pageSize, pageIndex, query)
      ),
      tap(page => {
        if (page.totalPages > 0 && this.pageIndex > page.totalPages && this.pageIndex !== 1) {
          this.pageIndex = 1;
          this.pageIndex$.next(1);
        } else {
          this.dataSource = page.content;
          this.total = page.totalElements;
          this.pageIndex = page.number;
          this.pageSize = page.size;
        }
        this.isLoading = false;
      }),

      switchMap(page => of(page.content))
    );
  }

  pageIndexChange(index: number) {
    this.pageIndex$.next(index);
  }

  pageSizeChange(size: number) {
    this.pageSize$.next(size);
  }

  getDate(date: Date) {
    const remaining = getDaysBetweenDates(new Date(date));
    return `${remaining} days to expire`;
  }
}
