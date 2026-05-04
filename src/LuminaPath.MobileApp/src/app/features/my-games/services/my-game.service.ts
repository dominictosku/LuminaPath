import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApiService } from 'src/app/shared/services/api.service';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { MyGame } from '../../games/models/games.model';

type AddMyGameRequest = {
  id: number;
  gameId: number;
  status: number;
  timeSpend: number;
};

@Injectable({
  providedIn: 'root',
})
export class MyGameService extends ApiService<MyGame> {
  constructor(httpClient: HttpClient, apiEndpoint: ApiEndpointService) {
    super(httpClient, apiEndpoint, 'mygames');
  }

  addToLibrary(gameId: number) {
    const request: AddMyGameRequest = {
      id: 0,
      gameId,
      status: 1,
      timeSpend: 0,
    };

    return this.post(request as MyGame);
  }
}
