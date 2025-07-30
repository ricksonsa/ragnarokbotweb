import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ScumServer } from '../models/scum-server';
import { WEB_API } from '../api.const';
import { AuthenticationService } from './authentication.service';
import { GuildDto } from '../models/guild';

@Injectable({
  providedIn: 'root'
})
export class ServerService {

  constructor(private readonly http: HttpClient, private readonly authService: AuthenticationService) { }

  updateFtp(updateForm: any) {
    return this.http.patch<ScumServer>(`${WEB_API.baseUrl}/api/servers/ftp`, updateForm);
  }

  updateChannels(value: { key: string; value: any; }) {
    return this.http.put<ScumServer>(`${WEB_API.baseUrl}/api/servers/discord/channels`, value);
  }

  updateDiscordSettings(updateForm: any) {
    return this.http.patch<GuildDto>(`${WEB_API.baseUrl}/api/servers/discord/config`, updateForm);
  }

  getDiscordServer() {
    return this.http.get<GuildDto>(`${WEB_API.baseUrl}/api/servers/discord`);
  }

  getDiscordChannels() {
    return this.http.get<{ key: string, value: string }[]>(`${WEB_API.baseUrl}/api/servers/discord/channels`);
  }

  getDiscordRoles() {
    return this.http.get<{ discordId: string, name: string }[]>(`${WEB_API.baseUrl}/api/servers/discord/roles`);
  }

  createDefaultChannels() {
    return this.http.patch<GuildDto>(`${WEB_API.baseUrl}/api/servers/discord/channels/run-template`, null);
  }

  updateSettings(settings: any) {
    return this.http.put<ScumServer>(`${WEB_API.baseUrl}/api/servers/settings`, settings);
  }
}
