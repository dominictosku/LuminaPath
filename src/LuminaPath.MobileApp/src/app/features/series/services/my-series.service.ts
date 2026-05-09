import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApiService } from 'src/app/shared/services/api.service';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { MySeries } from '../models/series.model';
import { LibraryEntryDetails } from '../../library/models/media-item.model';

type AddMySeriesRequest = {
  id: number;
  seriesId: number;
  status: number;
  timeSpend: number | null;
  rating?: number | null;
  startDate?: string | null;
  endDate?: string | null;
  currentWatchTimeMinutes?: number | null;
  currentEpisode?: number | null;
};

@Injectable({
  providedIn: 'root',
})
export class MySeriesService extends ApiService<MySeries> {
  constructor(httpClient: HttpClient, apiEndpoint: ApiEndpointService) {
    super(httpClient, apiEndpoint, 'myseries');
  }

  addToLibrary(seriesId: number, details: LibraryEntryDetails) {
    return this.post(this.createRequest(0, seriesId, details) as MySeries);
  }

  updateLibraryEntry(mySeriesId: number, seriesId: number, details: LibraryEntryDetails) {
    return this.put(mySeriesId, this.createRequest(mySeriesId, seriesId, details) as MySeries);
  }

  private createRequest(id: number, seriesId: number, details: LibraryEntryDetails): AddMySeriesRequest {
    return {
      id,
      seriesId,
      status: details.status,
      timeSpend: details.timeSpend,
      rating: details.rating,
      startDate: details.startDate,
      endDate: details.endDate,
      currentWatchTimeMinutes: details.timeSpend == null ? null : Math.round(Number(details.timeSpend) * 60),
      currentEpisode: details.currentEpisode ?? null,
    };
  }
}
