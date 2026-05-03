import { HttpClient } from '@angular/common/http';
import { ApiEndpointService } from './api-endpoint.service';
import { ApiService } from './api.service';

class TestApiService extends ApiService<unknown> {
  get url() {
    return this.apiUrl;
  }
}

describe('ApiService', () => {
  let service: TestApiService;

  beforeEach(() => {
    service = new TestApiService({} as HttpClient, new ApiEndpointService(), 'games');
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('builds endpoint URLs through ApiEndpointService', () => {
    expect(service.url).toBe('https://localhost:7013/api/games');
  });
});
