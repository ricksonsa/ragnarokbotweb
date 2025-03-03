import { Component, OnInit } from '@angular/core';
import { NzFormModule } from 'ng-zorro-antd/form';
import { FormsModule } from '@angular/forms';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzSwitchModule } from 'ng-zorro-antd/switch';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-pvp',
  templateUrl: './pvp.component.html',
  styleUrls: ['./pvp.component.scss', '../settings.scss'],
    imports: [
      FormsModule,
      RouterLink,
      NzSwitchModule,
      NzCardModule,
      NzFormModule,
      NzButtonModule,
      NzInputModule,
      NzSelectModule,
      NzAlertModule,
      NzDividerModule,
      NzIconModule
    ]
})
export class PvpComponent implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
