import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';

import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { GameService } from './game.service';
import { Game } from '../models/games.model';
import { MediaFilter } from 'src/app/core/entities/mediaFilter';
import { PaginateResult } from 'src/app/core/entities/paginatedResult';

describe('GameService', () => {
  let service: GameService;
  let httpMock: HttpTestingController;
  let endpoint: string;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        GameService,
        ApiEndpointService,
      ],
    });

    service = TestBed.inject(GameService);
    httpMock = TestBed.inject(HttpTestingController);
    endpoint = TestBed.inject(ApiEndpointService).url('games');
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getAll() requests the games endpoint without filter params', () => {
    let response: PaginateResult<Game> | undefined;
    service.getAll().subscribe((value) => (response = value));

    const req = httpMock.expectOne((r) => r.url === endpoint);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().length).toBe(0);
    expect(req.request.withCredentials).toBeTrue();

    const fake = new PaginateResult<Game>();
    fake.data = [Object.assign(new Game(), { id: 1, name: 'Apex' })];
    req.flush(fake);

    expect(response?.data.length).toBe(1);
    expect(response?.data[0].name).toBe('Apex');
  });

  it('getAll(filter) sends paging and search parameters', () => {
    const filter = new MediaFilter();
    filter.MyMedia = true;
    filter.SearchString = 'cele';
    filter.Paging.PageIndex = 2;
    filter.Paging.Count = 10;

    service.getAll(filter).subscribe();

    const req = httpMock.expectOne((r) => r.url === endpoint);
    expect(req.request.params.get('myMedia')).toBe('true');
    expect(req.request.params.get('searchString')).toBe('cele');
    expect(req.request.params.get('paging.pageIndex')).toBe('2');
    expect(req.request.params.get('paging.count')).toBe('10');
    req.flush(new PaginateResult<Game>());
  });

  it('post() POSTs to the games endpoint', () => {
    const game = Object.assign(new Game(), { name: 'New' });
    service.post(game).subscribe();

    const req = httpMock.expectOne(endpoint);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(game);
    req.flush(game);
  });

  it('put(id, game) PUTs to /games/{id}', () => {
    const game = Object.assign(new Game(), { id: 5, name: 'Old' });
    service.put(5, game).subscribe();

    const req = httpMock.expectOne(`${endpoint}/5`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(game);
    req.flush(game);
  });

  it('delete(id) DELETEs /games/{id}', () => {
    service.delete(9).subscribe();

    const req = httpMock.expectOne(`${endpoint}/9`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('getNews(gameId) requests the game news endpoint with credentials', () => {
    service.getNews(42).subscribe((items) => {
      expect(items[0].title).toBe('Patch notes');
    });

    const req = httpMock.expectOne(`${endpoint}/42/news`);
    expect(req.request.method).toBe('GET');
    expect(req.request.withCredentials).toBeTrue();
    expect(req.request.params.keys().length).toBe(0);

    req.flush([
      {
        title: 'Patch notes',
        summary: 'New update',
        url: 'https://example.com/news',
        source: 'Steam',
        provider: 'Steam',
        publishedAt: '2026-05-08T00:00:00.000Z',
      },
    ]);
  });

  it('getNews(gameId, true) sends the refresh flag', () => {
    service.getNews(42, true).subscribe();

    const req = httpMock.expectOne((request) => request.url === `${endpoint}/42/news`);
    expect(req.request.params.get('refresh')).toBe('true');
    req.flush([]);
  });
});
