import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { PlayerDto } from '../models/player.dto';
import { WEB_API } from '../api.const';
import { Page } from '../core/pagination/pager';

@Injectable({
  providedIn: 'root'
})
export class PlayerService {

  constructor(private readonly http: HttpClient) { }

  getPlayers(pageSize: number, pageNumber: number, filter: string = null) {
    var url = `${WEB_API.baseUrl}/api/players?pageSize=${pageSize}&pageNumber=${pageNumber}`;
    if (filter) {
      url += `&filter=${filter}`;
    }
    return this.http.get<Page<PlayerDto>>(url);
  }

  getPlayerById(id: number) {
    return this.http.get<PlayerDto>(`${WEB_API.baseUrl}/api/players/${id}`);
  }

}
