import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { Credentials } from '../models/user.model';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ApiEndpointService,
          useValue: {
            url: (path: string) => `/api/${path}`,
          },
        },
      ],
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('does not authenticate the client when the API returns the Identity 2FA challenge', () => {
    let authenticated = true;
    let requiresTwoFactor = false;

    service.login(createCredentials()).subscribe((result) => {
      authenticated = result.authenticated;
      requiresTwoFactor = result.requiresTwoFactor;
    });

    const request = httpMock.expectOne('/api/login?useCookies=true&useSessionCookies=true');
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBeTrue();

    request.flush(
      {
        title: 'Unauthorized',
        status: 401,
        detail: 'RequiresTwoFactor',
      },
      {
        status: 401,
        statusText: 'Unauthorized',
      }
    );

    expect(authenticated).toBeFalse();
    expect(requiresTwoFactor).toBeTrue();
    expect(service.isAuthenticated()).toBeFalse();
  });

  it('keeps ordinary unauthorized login responses as failures', () => {
    let failed = false;

    service.login(createCredentials()).subscribe({
      next: () => fail('Expected login to fail.'),
      error: () => {
        failed = true;
      },
    });

    const request = httpMock.expectOne('/api/login?useCookies=true&useSessionCookies=true');
    request.flush(
      {
        title: 'Unauthorized',
        status: 401,
        detail: 'Failed',
      },
      {
        status: 401,
        statusText: 'Unauthorized',
      }
    );

    expect(failed).toBeTrue();
    expect(service.isAuthenticated()).toBeFalse();
  });

  it('authenticates the client only after a completed login response', () => {
    let authenticated = false;
    let requiresTwoFactor = true;

    service.login(createCredentials({ twoFactorCode: '123456' })).subscribe((result) => {
      authenticated = result.authenticated;
      requiresTwoFactor = result.requiresTwoFactor;
    });

    const request = httpMock.expectOne('/api/login?useCookies=true&useSessionCookies=true');
    expect(request.request.body).toEqual(jasmine.objectContaining({
      email: 'user@example.com',
      password: 'Secret123!',
      twoFactorCode: '123456',
      twoFactorRecoveryCode: undefined,
    }));

    request.flush(null);

    expect(authenticated).toBeTrue();
    expect(requiresTwoFactor).toBeFalse();
    expect(service.isAuthenticated()).toBeTrue();
  });

  function createCredentials(overrides: Partial<Credentials> = {}): Credentials {
    const credentials = new Credentials();
    credentials.email = 'user@example.com';
    credentials.password = 'Secret123!';
    Object.assign(credentials, overrides);
    return credentials;
  }
});
