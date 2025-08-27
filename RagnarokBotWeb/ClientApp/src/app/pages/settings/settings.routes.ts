import { Routes } from '@angular/router';
import { ServerComponent } from './server/server.component';
import { DiscordComponent } from './discord/discord.component';
import { KillFeedComponent } from './kill-feed/kill-feed.component';
import { RankingsComponent } from './rankings/rankings.component';

export const SETTINGS_ROUTES: Routes = [
  { path: 'server', component: ServerComponent },
  { path: 'discord', component: DiscordComponent },
  { path: 'rankings', component: RankingsComponent },
  { path: 'kill-feed', component: KillFeedComponent }
];
