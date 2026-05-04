import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApiService } from 'src/app/shared/services/api.service';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { MyGame } from '../../games/models/games.model';

type AddMyGameRequest = {
  id: number;
  gameId: number;
  status: number;
  timeSpend: number | null;
  rating?: number | null;
  startDate?: string | null;
  endDate?: string | null;
};

@Injectable({
  providedIn: 'root',
})
export class MyGameService extends ApiService<MyGame> {
  constructor(httpClient: HttpClient, apiEndpoint: ApiEndpointService) {
    super(httpClient, apiEndpoint, 'mygames');
  }

  addToLibrary(gameId: number, details: Omit<AddMyGameRequest, 'id' | 'gameId'>) {
    const request: AddMyGameRequest = {
      id: 0,
      gameId,
      ...details,
    };

    return this.post(request as MyGame);
  }

  updateLibraryEntry(myGameId: number, gameId: number, details: Omit<AddMyGameRequest, 'id' | 'gameId'>) {
    const request: AddMyGameRequest = {
      id: myGameId,
      gameId,
      ...details,
    };

    return this.put(myGameId, request as MyGame);
  }
}
