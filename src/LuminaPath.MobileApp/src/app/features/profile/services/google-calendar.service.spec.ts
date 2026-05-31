import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';

import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import {
  GoogleCalendarService,
  GoogleCalendarStatus,
  GoogleCalendarSyncResult,
} from './google-calendar.service';

describe('GoogleCalendarService', () => {
  let service: GoogleCalendarService;
  let httpMock: HttpTestingController;
  let api: ApiEndpointService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        GoogleCalendarService,
        ApiEndpointService,
      ],
    });

    service = TestBed.inject(GoogleCalendarService);
    httpMock = TestBed.inject(HttpTestingController);
    api = TestBed.inject(ApiEndpointService);
  });

  afterEach(() => httpMock.verify());

  it('getStatus() GETs the status endpoint with credentials', () => {
    let result: GoogleCalendarStatus | undefined;
    service.getStatus().subscribe((value) => (result = value));

    const req = httpMock.expectOne(api.url('integrations/google/status'));
    expect(req.request.method).toBe('GET');
    expect(req.request.withCredentials).toBeTrue();

    const payload: GoogleCalendarStatus = {
      configured: true,
      connected: true,
      email: 'a@b.com',
      lastSyncedAt: null,
    };
    req.flush(payload);
    expect(result).toEqual(payload);
  });

  it('sync() POSTs the sync endpoint', () => {
    let result: GoogleCalendarSyncResult | undefined;
    service.sync().subscribe((value) => (result = value));

    const req = httpMock.expectOne(api.url('integrations/google/sync'));
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBeTrue();

    const payload: GoogleCalendarSyncResult = {
      releaseEvents: 2,
      questEvents: 1,
      deleted: 0,
      message: 'Synced 2 release(s) and 1 quest(s).',
    };
    req.flush(payload);
    expect(result).toEqual(payload);
  });

  it('disconnect() DELETEs the integration endpoint', () => {
    service.disconnect().subscribe();

    const req = httpMock.expectOne(api.url('integrations/google'));
    expect(req.request.method).toBe('DELETE');
    expect(req.request.withCredentials).toBeTrue();
    req.flush(null);
  });

  it('connectUrl() points at the backend connect endpoint', () => {
    expect(service.connectUrl()).toBe(api.url('integrations/google/connect'));
  });
});
