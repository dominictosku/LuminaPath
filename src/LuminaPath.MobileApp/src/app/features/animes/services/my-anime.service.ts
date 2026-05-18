import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApiService } from 'src/app/shared/services/api.service';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { MyAnime } from '../models/animes.model';
import { LibraryEntryDetails } from '../../library/models/media-item.model';

type AddMyAnimeRequest = {
  id: number;
  animeId: number;
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
export class MyAnimeService extends ApiService<MyAnime> {
  constructor() {
    const httpClient = inject(HttpClient);
    const apiEndpoint = inject(ApiEndpointService);

    super(httpClient, apiEndpoint, 'myanimes');
  }

  addToLibrary(animeId: number, details: LibraryEntryDetails) {
    return this.post(this.createRequest(0, animeId, details) as MyAnime);
  }

  updateLibraryEntry(myAnimeId: number, animeId: number, details: LibraryEntryDetails) {
    return this.put(myAnimeId, this.createRequest(myAnimeId, animeId, details) as MyAnime);
  }

  private createRequest(id: number, animeId: number, details: LibraryEntryDetails): AddMyAnimeRequest {
    return {
      id,
      animeId,
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
