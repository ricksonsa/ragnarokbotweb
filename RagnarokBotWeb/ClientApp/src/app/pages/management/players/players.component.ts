import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzTableModule } from 'ng-zorro-antd/table';
import { PlayerService } from '../../../services/player.service';
import { PlayerDto } from '../../../models/player.dto';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { RouterModule } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';

@Component({
  selector: 'app-players',
  templateUrl: './players.component.html',
  styleUrls: ['./players.component.scss'],
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    NzButtonModule,
    NzIconModule,
    NzCardModule,
    NzDividerModule,
    NzTableModule
  ]
})
export class PlayersComponent implements OnInit {
  dataSource: PlayerDto[] = [];
  total = 0;
  pageIndex = 1;
  pageSize = 10;

  constructor(private readonly playerService: PlayerService) { }

  ngOnInit() {
    this.loadPlayers();
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
    this.loadPlayers();
  }

  pageSizeChange(size: number) {
    this.pageSize = size;
    this.loadPlayers();
  }
}
