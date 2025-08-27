import { Routes } from '@angular/router';
import { PlayersComponent } from './players/players.component';
import { VipsComponent } from './vips/vips.component';
import { TicketsComponent } from './tickets/tickets.component';
import { PlayerComponent } from './players/player/player.component';
import { PlayerResolver } from './players/resolvers/player.resolver';
import { BotsComponent } from './bots/bots.component';

export const MANAGEMENT_ROUTES: Routes = [
  { path: 'players', component: PlayersComponent, pathMatch: 'full' },
  { path: 'players/:id', component: PlayerComponent, resolve: { player: PlayerResolver } },
  { path: 'vips', component: VipsComponent },
  { path: 'bots', component: BotsComponent },
  { path: 'tickets', component: TicketsComponent },
];
