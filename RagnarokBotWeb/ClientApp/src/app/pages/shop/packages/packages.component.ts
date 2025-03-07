import { Component, OnInit } from '@angular/core';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTableModule } from 'ng-zorro-antd/table';
import { PackageDto } from '../../../models/package.dto';
import { PackageService } from '../../../services/package.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-packages',
  templateUrl: './packages.component.html',
  styleUrls: ['./packages.component.scss'],
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    NzCardModule,
    NzIconModule,
    NzTableModule,
    NzButtonModule,
    NzDividerModule
  ]
})
export class PackagesComponent implements OnInit {

  dataSource: PackageDto[] = [];
  total = 0;
  pageIndex = 1;
  pageSize = 10;

  constructor(private readonly packageService: PackageService, private readonly router: Router) { }

  ngOnInit() {
    this.loadPackages();
  }


  loadPackages() {
    this.packageService.getPackages(this.pageSize, this.pageIndex)
      .subscribe({
        next: (page) => {
          this.dataSource = page.content;
          this.total = page.totalElements;
          this.pageIndex = page.number;
          this.pageSize = page.size;
        }
      });
  }

  addNewPack() {
    // this.router.navigateByUrl('/').;
  }

  pageIndexChange(index: number) {
    this.pageIndex = index;
    this.loadPackages();
  }

  pageSizeChange(size: number) {
    this.pageSize = size;
    this.loadPackages();
  }

}
