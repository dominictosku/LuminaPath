import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApiService } from 'src/app/shared/services/api.service';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { MyMovie } from '../models/movies.model';
import { LibraryEntryDetails } from '../../media/models/media-item.model';

type AddMyMovieRequest = {
  id: number;
  movieId: number;
  status: number;
  timeSpend: number | null;
  rating?: number | null;
  startDate?: string | null;
  endDate?: string | null;
  currentWatchTimeMinutes?: number | null;
};

@Injectable({
  providedIn: 'root',
})
export class MyMovieService extends ApiService<MyMovie> {
  constructor(httpClient: HttpClient, apiEndpoint: ApiEndpointService) {
    super(httpClient, apiEndpoint, 'mymovies');
  }

  addToLibrary(movieId: number, details: LibraryEntryDetails) {
    return this.post(this.createRequest(0, movieId, details) as MyMovie);
  }

  updateLibraryEntry(myMovieId: number, movieId: number, details: LibraryEntryDetails) {
    return this.put(myMovieId, this.createRequest(myMovieId, movieId, details) as MyMovie);
  }

  private createRequest(id: number, movieId: number, details: LibraryEntryDetails): AddMyMovieRequest {
    return {
      id,
      movieId,
      status: details.status,
      timeSpend: details.timeSpend,
      rating: details.rating,
      startDate: details.startDate,
      endDate: details.endDate,
      currentWatchTimeMinutes: details.timeSpend == null ? null : Math.round(Number(details.timeSpend) * 60),
    };
  }
}
