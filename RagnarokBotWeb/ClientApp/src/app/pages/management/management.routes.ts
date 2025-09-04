import { Routes } from '@angular/router';
import { PlayersComponent } from './players/players.component';
import { VipsComponent } from './vips/vips.component';
import { TicketsComponent } from './tickets/tickets.component';
import { PlayerComponent } from './players/player/player.component';
import { PlayerResolver } from './players/resolvers/player.resolver';
import { BotsComponent } from './bots/bots.component';
import { SquadsComponent } from './squads/squads.component';
import { SquadResolver } from './squads/resolvers/squad.resolver';
import { SquadComponent } from './squads/squad/squad.component';

export const MANAGEMENT_ROUTES: Routes = [
  { path: 'players', component: PlayersComponent, pathMatch: 'full' },
  { path: 'players/:id', component: PlayerComponent, resolve: { player: PlayerResolver } },
  { path: 'vips', component: VipsComponent },
  { path: 'bots', component: BotsComponent },
  { path: 'squads', component: SquadsComponent, pathMatch: 'full' },
  { path: 'squads/:id', component: SquadComponent, resolve: { squad: SquadResolver } },
  { path: 'tickets', component: TicketsComponent },
];
