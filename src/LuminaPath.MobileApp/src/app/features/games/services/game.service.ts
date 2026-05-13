import { Injectable } from '@angular/core';
import { Game, GameNewsItem, GameSummary } from '../models/games.model';
import { ApiService } from '../../../shared/services/api.service';
import { HttpClient, HttpParams } from '@angular/common/http';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';

@Injectable({
  providedIn: 'root',
})
export class GameService extends ApiService<Game> {
  constructor(
    private httpClient: HttpClient,
    apiEndpoint: ApiEndpointService,
  ) {
    super(httpClient, apiEndpoint, 'games');
  }

  public labels = ['Title', 'Description', 'Status', 'Release'];
  Id: string = 'games';

  createMedia(media: Game, endPoint: string) {
    if (media.id == 0) {
      this.post(media);
    } else {
      this.put(media.id, media);
    }
    return;
  }

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
