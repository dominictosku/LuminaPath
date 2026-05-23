import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';

import {
  ApiEndpointService,
  resolveApiEndpoint,
} from 'src/app/shared/services/api-endpoint.service';

import { TwoFactorService } from './two-factor.service';

describe('TwoFactorService', () => {
  let service: TwoFactorService;
  let http: HttpTestingController;
  let endpoint: string;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        TwoFactorService,
        ApiEndpointService,
      ],
    });
    service = TestBed.inject(TwoFactorService);
    http = TestBed.inject(HttpTestingController);
    endpoint = `${resolveApiEndpoint()}/manage/2fa`;
  });

  afterEach(() => http.verify());

  function expectPost(body: Record<string, unknown>): void {
    const req = http.expectOne(endpoint);
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBeTrue();
    expect(req.request.body).toEqual(body);
    req.flush({
      sharedKey: 'KEY',
      recoveryCodesLeft: 0,
      recoveryCodes: null,
      isTwoFactorEnabled: false,
      isMachineRemembered: false,
    });
  }

  it('load() posts an empty body', () => {
    service.load().subscribe();
    expectPost({});
  });

  it('enable(code) posts enable + twoFactorCode', () => {
    service.enable('123456').subscribe();
    expectPost({ enable: true, twoFactorCode: '123456' });
  });

  it('disable() posts enable: false', () => {
    service.disable().subscribe();
    expectPost({ enable: false });
  });

  it('resetSharedKey() posts resetSharedKey: true', () => {
    service.resetSharedKey().subscribe();
    expectPost({ resetSharedKey: true });
  });

  it('regenerateRecoveryCodes() posts resetRecoveryCodes: true', () => {
    service.regenerateRecoveryCodes().subscribe();
    expectPost({ resetRecoveryCodes: true });
  });

  it('forgetMachine() posts forgetMachine: true', () => {
    service.forgetMachine().subscribe();
    expectPost({ forgetMachine: true });
  });
});
