import { Component, OnInit } from '@angular/core';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { Squad } from '../../../../models/squad.dto';
import { NzTableModule } from 'ng-zorro-antd/table';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';

@Component({
  selector: 'app-squad',
  templateUrl: './squad.component.html',
  styleUrls: ['./squad.component.scss'],
  imports: [
    RouterModule,
    NzCardModule,
    NzIconModule,
    NzSpaceModule,
    NzFormModule,
    NzTableModule,
    NzButtonModule
  ]
})
export class SquadComponent implements OnInit {

  squad?: Squad;

  constructor(private readonly route: ActivatedRoute, private readonly router: Router) { }

  ngOnInit() {
    this.route.data.subscribe(data => {
      var item = data['squad'];
      if (item) {
        this.squad = item;
      }
    });
  }

  goBack() {
    this.router.navigate(['management', 'squads']);
  }

}
