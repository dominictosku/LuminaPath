import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApiService } from 'src/app/shared/services/api.service';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { Series, SeriesSummary } from '../models/series.model';

@Injectable({
  providedIn: 'root',
})
export class SeriesService extends ApiService<Series> {
  private httpClient: HttpClient;

  constructor() {
    const httpClient = inject(HttpClient);
    const apiEndpoint = inject(ApiEndpointService);

    super(httpClient, apiEndpoint, 'series');
  
    this.httpClient = httpClient;
  }

  getSeasons(seriesId: number) {
    return this.httpClient.get<SeriesSummary[]>(`${this.apiUrl}/${seriesId}/seasons`, {
      withCredentials: true,
    });
  }
}
