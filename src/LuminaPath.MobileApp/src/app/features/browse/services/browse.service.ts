import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { BrowseItem } from '../models/browse.model';

@Injectable({
  providedIn: 'root',
})
export class BrowseService {
  private readonly http = inject(HttpClient);
  private readonly apiEndpoint = inject(ApiEndpointService);


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
