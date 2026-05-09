import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { BrowseItem } from '../models/browse.model';

@Injectable({
  providedIn: 'root',
})
export class BrowseService {
  constructor(
    private readonly http: HttpClient,
    private readonly apiEndpoint: ApiEndpointService,
  ) {
  }

  getGameReleases(): Observable<BrowseItem[]> {
    return this.http.get<BrowseItem[]>(this.apiEndpoint.url('browse/games/releases'), {
      withCredentials: true,
    });
  }

  getAnimeReleases(): Observable<BrowseItem[]> {
    return this.http.get<BrowseItem[]>(this.apiEndpoint.url('browse/animes/releases'), {
      withCredentials: true,
    });
  }
}
