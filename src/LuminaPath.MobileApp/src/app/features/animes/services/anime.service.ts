import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApiService } from 'src/app/shared/services/api.service';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { Anime, AnimeSummary } from '../models/animes.model';

@Injectable({
  providedIn: 'root',
})
export class AnimeService extends ApiService<Anime> {
  constructor(private httpClient: HttpClient, apiEndpoint: ApiEndpointService) {
    super(httpClient, apiEndpoint, 'animes');
  }

  getSeasons(animeId: number) {
    return this.httpClient.get<AnimeSummary[]>(`${this.apiUrl}/${animeId}/seasons`, {
      withCredentials: true,
    });
  }
}
