import { Routes } from '@angular/router';
import { PlayersComponent } from './players/players.component';
import { VipsComponent } from './vips/vips.component';
import { TicketsComponent } from './tickets/tickets.component';
import { PlayerComponent } from './players/player/player.component';

export const MANAGEMENT_ROUTES: Routes = [
  { path: 'players', component: PlayersComponent, pathMatch: 'full' },
  { path: 'players/:id', component: PlayerComponent },
  { path: 'vips', component: VipsComponent },
  { path: 'tickets', component: TicketsComponent },
];
