import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApiService } from 'src/app/shared/services/api.service';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { MyGame } from '../../games/models/games.model';
import { MediaModeService } from 'src/app/shared/services/media-mode.service';

type AddMyGameRequest = {
  id: number;
  gameId: number;
  animeId?: number;
  movieId?: number;
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
export class MyGameService extends ApiService<MyGame> {
  constructor(
    httpClient: HttpClient,
    apiEndpoint: ApiEndpointService,
    private mediaMode: MediaModeService,
  ) {
    super(httpClient, apiEndpoint, 'mygames');
  }

  protected override get apiUrl(): string {
    return this.apiEndpoint.url(this.mediaMode.current.libraryEndpoint);
  }

  addToLibrary(gameId: number, details: Omit<AddMyGameRequest, 'id' | 'gameId'>) {
    const request: AddMyGameRequest = {
      id: 0,
      gameId,
      ...details,
    };
    this.assignMediaId(request, gameId);

    return this.post(request as MyGame);
  }

  updateLibraryEntry(myGameId: number, gameId: number, details: Omit<AddMyGameRequest, 'id' | 'gameId'>) {
    const request: AddMyGameRequest = {
      id: myGameId,
      gameId,
      ...details,
    };
    this.assignMediaId(request, gameId);

    return this.put(myGameId, request as MyGame);
  }

  private assignMediaId(request: AddMyGameRequest, mediaId: number): void {
    const mediaKey = this.mediaMode.current.libraryIdKey;
    (request as AddMyGameRequest & Record<string, number>)[mediaKey] = mediaId;

    if (this.mediaMode.current.id !== 'games') {
      request.currentWatchTimeMinutes = request.timeSpend == null ? null : Math.round(Number(request.timeSpend) * 60);
    }
  }
}
