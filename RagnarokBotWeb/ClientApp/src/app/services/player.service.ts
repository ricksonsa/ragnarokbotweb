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

  getPlayers(pageSize: number, pageNumber: number) {
    return this.http.get<Page<PlayerDto>>(`${WEB_API.baseUrl}/api/players?pageSize=${pageSize}&pageNumber=${pageNumber}`);
  }

}
