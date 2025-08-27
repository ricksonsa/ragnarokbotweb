import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ScumServer } from '../models/scum-server';
import { environment } from '../../environments/environment';
import { AuthenticationService } from './authentication.service';
import { GuildDto } from '../models/guild';
import { UavDto } from '../models/uav.dto';
import { ExchangeDto } from '../models/exchange.dto';

@Injectable({
  providedIn: 'root'
})
export class ServerService {

  constructor(private readonly http: HttpClient, private readonly authService: AuthenticationService) { }

  updateFtp(updateForm: any) {
    return this.http.patch<ScumServer>(`${environment.apiUrl}/api/servers/ftp`, updateForm);
  }

  updateChannels(value: { key: string; value: any; }) {
    return this.http.put<ScumServer>(`${environment.apiUrl}/api/servers/discord/channels`, value);
  }

  updateDiscordSettings(updateForm: any) {
    return this.http.patch<GuildDto>(`${environment.apiUrl}/api/servers/discord/config`, updateForm);
  }

  updateUav(uav: UavDto) {
    return this.http.put<UavDto>(`${environment.apiUrl}/api/servers/uav`, uav);
  }

  updateExchange(exchange: ExchangeDto) {
    return this.http.put<ExchangeDto>(`${environment.apiUrl}/api/servers/exchange`, exchange);
  }

  getDiscordServer() {
    return this.http.get<GuildDto>(`${environment.apiUrl}/api/servers/discord`);
  }

  getPlayerCount() {
    return this.http.get<any>(`${environment.apiUrl}/api/servers/players`);
  }

  getDiscordChannels() {
    return this.http.get<{ key: string, value: string }[]>(`${environment.apiUrl}/api/servers/discord/channels`);
  }

  getDiscordRoles() {
    return this.http.get<{ discordId: string, name: string }[]>(`${environment.apiUrl}/api/servers/discord/roles`);
  }

  createDefaultChannels() {
    return this.http.patch<GuildDto>(`${environment.apiUrl}/api/servers/discord/channels/run-template`, null);
  }

  updateSettings(settings: any) {
    return this.http.put<ScumServer>(`${environment.apiUrl}/api/servers/settings`, settings);
  }

  updateKillFeed(settings: any) {
    return this.http.put<ScumServer>(`${environment.apiUrl}/api/servers/kill-feed`, settings);
  }

  updateRankAwards(awards: any) {
    return this.http.put<ScumServer>(`${environment.apiUrl}/api/servers/awards`, awards);
  }
}
