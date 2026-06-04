import { Injectable, inject } from '@angular/core';
import { Game, GameNewsItem, GameSummary } from '../models/games.model';
import { ApiService } from '../../../shared/services/api.service';
import { HttpClient, HttpParams } from '@angular/common/http';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';

@Injectable({
  providedIn: 'root',
})
export class GameService extends ApiService<Game> {
  private httpClient: HttpClient;

  constructor() {
    const httpClient = inject(HttpClient);
    const apiEndpoint = inject(ApiEndpointService);

    super(httpClient, apiEndpoint, 'games');

    this.httpClient = httpClient;
  }

  public labels = ['Title', 'Description', 'Status', 'Release'];
  Id: string = 'games';

  getNews(gameId: number, refresh = false) {
    const params = refresh ? new HttpParams().set('refresh', 'true') : undefined;
    return this.httpClient.get<GameNewsItem[]>(`${this.apiUrl}/${gameId}/news`, {
      withCredentials: true,
      params,
    });
  }

  getDlcs(gameId: number) {
    return this.httpClient.get<GameSummary[]>(`${this.apiUrl}/${gameId}/dlcs`, {
      withCredentials: true,
    });
  }
}
