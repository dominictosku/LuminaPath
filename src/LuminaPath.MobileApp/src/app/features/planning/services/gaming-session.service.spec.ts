import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { GameForecast, GamingSessionService } from './gaming-session.service';

describe('GamingSessionService', () => {
  let service: GamingSessionService;
  let httpMock: HttpTestingController;
  let endpoint: string;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        GamingSessionService,
        ApiEndpointService,
      ],
    });

    service = TestBed.inject(GamingSessionService);
    httpMock = TestBed.inject(HttpTestingController);
    endpoint = TestBed.inject(ApiEndpointService).url('gamingsessions');
  });

  afterEach(() => httpMock.verify());

  it('list passes myGameId, from, and to as query params', () => {
    const from = new Date('2026-06-01T00:00:00.000Z');
    const to = new Date('2026-07-01T00:00:00.000Z');

    service.list({ myGameId: 7, from, to }).subscribe();

    const req = httpMock.expectOne((r) => r.url === endpoint);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('myGameId')).toBe('7');
    expect(req.request.params.get('from')).toBe(from.toISOString());
    expect(req.request.params.get('to')).toBe(to.toISOString());
    expect(req.request.withCredentials).toBeTrue();
    req.flush([]);
  });

  it('create POSTs the wrapped payload with id zero', () => {
    service
      .create({
        myGameId: 5,
        scheduledAt: '2026-06-04T19:00:00.000Z',
        durationMinutes: 90,
        completed: false,
        notes: 'Boss fight',
      })
      .subscribe();

    const req = httpMock.expectOne(endpoint);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      id: 0,
      myGameId: 5,
      scheduledAt: '2026-06-04T19:00:00.000Z',
      durationMinutes: 90,
      completed: false,
      completedAt: null,
      notes: 'Boss fight',
    });
    req.flush({});
  });

  it('update PUTs to /gamingsessions/{id} and forces id into body', () => {
    service
      .update(11, {
        myGameId: null,
        scheduledAt: '2026-06-10T20:00:00.000Z',
        durationMinutes: 60,
        completed: true,
        notes: null,
        completedAt: '2026-06-10T21:00:00.000Z',
      })
      .subscribe();

    const req = httpMock.expectOne(`${endpoint}/11`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.id).toBe(11);
    expect(req.request.body.myGameId).toBeNull();
    expect(req.request.body.completed).toBeTrue();
    expect(req.request.body.completedAt).toBe('2026-06-10T21:00:00.000Z');
    req.flush({});
  });

  it('remove DELETEs /gamingsessions/{id}', () => {
    service.remove(3).subscribe();
    const req = httpMock.expectOne(`${endpoint}/3`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('forecast GETs /gamingsessions/forecast/{myGameId}', () => {
    let received: GameForecast | undefined;
    service.forecast(42).subscribe((value) => (received = value));

    const req = httpMock.expectOne(`${endpoint}/forecast/42`);
    expect(req.request.method).toBe('GET');

    const fake: GameForecast = {
      myGameId: 42,
      gameName: 'Hades',
      playtimeEstimateHours: 20,
      playedHours: 5,
      remainingHours: 15,
      scheduledHours: 6,
      upcomingSessionCount: 3,
      sessionsToCompletion: null,
      projectedCompletionDate: null,
      weeklyHours: 1.5,
      additionalHoursNeeded: 9,
      weeksAtCurrentPace: 6,
    };
    req.flush(fake);

    expect(received).toEqual(fake);
  });
});
