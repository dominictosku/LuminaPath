import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApiService } from 'src/app/shared/services/api.service';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { Series } from '../models/series.model';

@Injectable({
  providedIn: 'root',
})
export class SeriesService extends ApiService<Series> {
  constructor(httpClient: HttpClient, apiEndpoint: ApiEndpointService) {
    super(httpClient, apiEndpoint, 'series');
  }
}
