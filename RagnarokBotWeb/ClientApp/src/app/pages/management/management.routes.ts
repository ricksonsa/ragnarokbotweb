import { Routes } from '@angular/router';
import { PlayersComponent } from './players/players.component';
import { VipsComponent } from './vips/vips.component';
import { TicketsComponent } from './tickets/tickets.component';

export const MANAGEMENT_ROUTES: Routes = [
  { path: 'players', component: PlayersComponent },
  { path: 'vips', component: VipsComponent },
  { path: 'tickets', component: TicketsComponent },
];
