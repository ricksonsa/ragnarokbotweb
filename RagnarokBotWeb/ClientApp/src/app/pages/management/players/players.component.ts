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
import { debounceTime, distinctUntilChanged, Observable, of, startWith, switchMap, tap } from 'rxjs';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzInputModule } from 'ng-zorro-antd/input';

@Component({
  selector: 'app-players',
  templateUrl: './players.component.html',
  styleUrls: ['./players.component.scss'],
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
    NzTableModule
  ]
})
export class PlayersComponent implements OnInit {
  dataSource: PlayerDto[] = [];
  searchControl = new FormControl();
  suggestions$: Observable<PlayerDto[]> = of([]);
  isLoading = false;
  total = 0;
  pageIndex = 1;
  pageSize = 10;

  constructor(private readonly playerService: PlayerService) { }

  ngOnInit() {
    this.loadPlayers();
    this.setUpFilter();
  }

  loadPage() {
    const query = this.searchControl.value; // Get value from the input field

    this.isLoading = true;
    this.suggestions$ = this.playerService.getPlayers(this.pageSize, this.pageIndex, query)
      .pipe(
        tap(() => (this.isLoading = false)),
        switchMap((page) => {
          this.dataSource = page.content;
          this.total = page.totalElements;
          this.pageIndex = page.number;
          this.pageSize = page.size;
          return of(page.content);
        })
      );
  }

  setUpFilter() {
    this.suggestions$ = this.searchControl.valueChanges
      .pipe(
        startWith(''), // Triggers API call on page load with an empty value
        debounceTime(300), // Wait 300ms after the last input
        distinctUntilChanged(), // Ignore same consecutive values
        tap(() => (this.isLoading = true)), // Show loading indicator
        switchMap(value => this.playerService.getPlayers(this.pageSize, this.pageIndex, value)
        ),
        tap((page) => {
          if (page) {
            this.dataSource = page.content;
            this.total = page.totalElements;
            this.pageIndex = page.number;
            this.pageSize = page.size;
          }
          this.isLoading = false
        }),
        switchMap((page) => {
          return of(page.content);
        })
      ); // Hide loading indicator
  }


  loadPlayers() {
    this.playerService.getPlayers(this.pageSize, this.pageIndex)
      .subscribe({
        next: (page) => {
          this.dataSource = page.content;
          this.total = page.totalElements;
          this.pageIndex = page.number;
          this.pageSize = page.size;
        }
      });
  }

  pageIndexChange(index: number) {
    this.pageIndex = index;
    this.loadPage();
    this.setUpFilter();
  }

  pageSizeChange(size: number) {
    this.pageSize = size;
    this.loadPage();
    this.setUpFilter();
  }
}
