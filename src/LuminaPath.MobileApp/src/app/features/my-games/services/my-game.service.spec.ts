import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';

import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { MyGameService } from './my-game.service';
import { MyGame } from '../../games/models/games.model';

describe('MyGameService', () => {
  let service: MyGameService;
  let httpMock: HttpTestingController;
  let endpoint: string;

  beforeEach(() => {
    localStorage.removeItem('luminapath.mediaMode');
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        MyGameService,
        ApiEndpointService,
      ],
    });

    service = TestBed.inject(MyGameService);
    httpMock = TestBed.inject(HttpTestingController);
    endpoint = TestBed.inject(ApiEndpointService).url('mygames');
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('addToLibrary POSTs the wrapped request payload', () => {
    const details = {
      status: 1,
      timeSpend: 12,
      rating: 8,
      startDate: '2024-01-01',
      endDate: null,
    };

    let response: MyGame | undefined;
    service.addToLibrary(42, details).subscribe((value) => (response = value));

    const req = httpMock.expectOne(endpoint);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      id: 0,
      gameId: 42,
      ...details,
    });
    expect(req.request.withCredentials).toBeTrue();

    const fakeResponse = { id: 7, gameId: 42, status: 1 } as unknown as MyGame;
    req.flush(fakeResponse);
    expect(response).toEqual(fakeResponse);
  });

  it('addToLibrary surfaces backend errors to the subscriber', () => {
    let errorPayload: unknown;
    service.addToLibrary(1, { status: 1, timeSpend: 0 }).subscribe({
      next: () => fail('expected an error'),
      error: (error) => (errorPayload = error.error),
    });

    const req = httpMock.expectOne(endpoint);
    req.flush('This is game already added', { status: 400, statusText: 'Bad Request' });

    expect(errorPayload).toBe('This is game already added');
  });

  it('updateLibraryEntry PUTs to the entry id with the existing myGameId', () => {
    const details = { status: 2, timeSpend: 3 };
    service.updateLibraryEntry(7, 42, details).subscribe();

    const req = httpMock.expectOne(`${endpoint}/7`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({
      id: 7,
      gameId: 42,
      ...details,
    });

    req.flush({ id: 7, gameId: 42, status: 2 });
  });

  it('getAchievements fetches earned trophies for a library entry', () => {
    service.getAchievements(7).subscribe((achievements) => {
      expect(achievements[0].title).toBe('First jump');
      expect(achievements[0].providerName).toBe('PlayStation');
    });

    const req = httpMock.expectOne(`${endpoint}/7/achievements`);
    expect(req.request.method).toBe('GET');
    expect(req.request.withCredentials).toBeTrue();

    req.flush([
      {
        id: 1,
        gameAchievementId: 2,
        provider: 2,
        providerName: 'PlayStation',
        sourceAchievementId: 'default:1',
        title: 'First jump',
        description: 'Jump once',
        iconUrl: null,
        isHidden: false,
        trophyType: 'bronze',
        unlockedAt: '2026-05-10T00:00:00.000Z',
        syncedAt: '2026-05-11T00:00:00.000Z',
      },
    ]);
  });

  it('does not issue a request until subscribed', () => {
    service.addToLibrary(1, { status: 1, timeSpend: 0 });
    httpMock.expectNone(endpoint);
    expect().nothing();
  });
});
