import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { ReleaseLibraryItem } from '../models/release-library.model';

@Injectable({
  providedIn: 'root',
})
export class ReleaseLibraryService {
  constructor(
    private readonly http: HttpClient,
    private readonly apiEndpoint: ApiEndpointService,
  ) {
  }

  getGameReleases(): Observable<ReleaseLibraryItem[]> {
    return this.http.get<ReleaseLibraryItem[]>(this.apiEndpoint.url('library/games/releases'), {
      withCredentials: true,
    });
  }

  getAnimeReleases(): Observable<ReleaseLibraryItem[]> {
    return this.http.get<ReleaseLibraryItem[]>(this.apiEndpoint.url('library/animes/releases'), {
      withCredentials: true,
    });
  }
}
