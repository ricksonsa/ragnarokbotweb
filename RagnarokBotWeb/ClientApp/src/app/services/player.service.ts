import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { PlayerDto } from '../models/player.dto';
import { environment } from '../../environments/environment';
import { Page } from '../core/pagination/pager';
import { GraphDto } from '../models/order.dto';
import { LockpickStatsDto, PlayerStatsDto } from '../models/player-stats.dto';

@Injectable({
  providedIn: 'root'
})
export class PlayerService {

  constructor(private readonly http: HttpClient) { }

  getPlayers(pageSize: number, pageNumber: number, filter: string = null) {
    var url = `${environment.apiUrl}/api/players?pageSize=${pageSize}&pageNumber=${pageNumber}`;
    if (filter) {
      url += `&filter=${filter}`;
    }
    return this.http.get<Page<PlayerDto>>(url);
  }

  getPlayerById(id: number) {
    return this.http.get<PlayerDto>(`${environment.apiUrl}/api/players/${id}`);
  }

  getNewPlayerStatistics() {
    return this.http.get<GraphDto[]>(`${environment.apiUrl}/api/players/statistics/monthly-registers`);
  }

  getPlayerStatisticsKills() {
    return this.http.get<PlayerStatsDto[]>(`${environment.apiUrl}/api/players/statistics/kills`);
  }

  getPlayerStatisticsLockpicks() {
    return this.http.get<LockpickStatsDto[]>(`${environment.apiUrl}/api/players/statistics/lockpicks`);
  }

  removeBan(id: number) {
    return this.http.delete<PlayerDto>(`${environment.apiUrl}/api/players/${id}/ban`);
  }

  removeVip(id: number) {
    return this.http.delete<PlayerDto>(`${environment.apiUrl}/api/players/${id}/vip`);
  }

  removeSilence(id: number) {
    return this.http.delete<PlayerDto>(`${environment.apiUrl}/api/players/${id}/silence`);
  }

  silence(id: any, dto: any) {
    return this.http.post<PlayerDto>(`${environment.apiUrl}/api/players/${id}/silence`, dto);
  }

  ban(id: any, dto: any) {
    return this.http.post<PlayerDto>(`${environment.apiUrl}/api/players/${id}/ban`, dto);
  }

  vip(id: any, dto: any) {
    return this.http.post<PlayerDto>(`${environment.apiUrl}/api/players/${id}/vip`, dto);
  }

  updateCoins(id: number, value: number) {
    return this.http.patch<PlayerDto>(`${environment.apiUrl}/api/players/${id}/coins`, { amount: value });
  }

  updateFame(id: number, value: number) {
    return this.http.patch<PlayerDto>(`${environment.apiUrl}/api/players/${id}/fame`, { amount: value });
  }

  updateMoney(id: number, value: number) {
    return this.http.patch<PlayerDto>(`${environment.apiUrl}/api/players/${id}/money`, { amount: value });
  }

  updateGold(id: number, value: number) {
    return this.http.patch<PlayerDto>(`${environment.apiUrl}/api/players/${id}/gold`, { amount: value });
  }

}
