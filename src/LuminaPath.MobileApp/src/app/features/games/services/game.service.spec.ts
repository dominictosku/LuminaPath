import { HttpClient } from '@angular/common/http';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { GameService } from './game.service';

describe('GameService', () => {
  let service: GameService;

  beforeEach(() => {
    service = new GameService({} as HttpClient, new ApiEndpointService());
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
