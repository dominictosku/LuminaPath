import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { Credentials } from '../../models/user.model';
import { AuthService, LoginResult } from '../../services/auth.service';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { LoginPage } from './login.page';

describe('LoginPage', () => {
  let component: LoginPage;
  let fixture: ComponentFixture<LoginPage>;
  let authService: jasmine.SpyObj<AuthService>;
  let navigateSpy: jasmine.Spy;

  beforeEach(async () => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', [
      'login',
      'clearSession',
      'getLoginBlockedReason',
    ]);
    // Default: no special block reason — the inactive-account test
    // overrides this. Keeps existing happy-path tests untouched.
    authService.getLoginBlockedReason.and.returnValue(null);

    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService },
        {
          provide: ApiEndpointService,
          useValue: {
            endpoint: () => '/api',
            setEndpoint: (value: string) => value,
            resetEndpoint: () => '/api',
          },
        },
      ],
    }).compileComponents();

    authService.login.and.returnValue(of(loginResult({ authenticated: true })));
    navigateSpy = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);

    fixture = TestBed.createComponent(LoginPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('keeps the user on the login page when 2FA is required', async () => {
    authService.login.and.returnValue(of(loginResult({ requiresTwoFactor: true })));
    component.credentials.email = 'user@example.com';
    component.credentials.password = 'Secret123!';

    await component.login();

    expect(component.twoFactorRequired).toBeTrue();
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('submits a normalized authenticator code for the second step', async () => {
    component.twoFactorRequired = true;
    component.credentials.email = 'user@example.com';
    component.credentials.password = 'Secret123!';
    component.twoFactorCode = '123 456';

    await component.login();

    const payload = authService.login.calls.mostRecent().args[0] as Credentials;
    expect(payload.twoFactorCode).toBe('123456');
    expect(payload.twoFactorRecoveryCode).toBeUndefined();
    expect(navigateSpy).toHaveBeenCalledWith(['/home']);
  });

  it('submits a normalized recovery code when recovery mode is selected', async () => {
    component.twoFactorRequired = true;
    component.useRecoveryCode = true;
    component.credentials.email = 'user@example.com';
    component.credentials.password = 'Secret123!';
    component.recoveryCode = 'abcd efgh';

    await component.login();

    const payload = authService.login.calls.mostRecent().args[0] as Credentials;
    expect(payload.twoFactorCode).toBeUndefined();
    expect(payload.twoFactorRecoveryCode).toBe('abcdefgh');
  });

  it('shows the awaiting-approval message when the backend reports an inactive account', async () => {
    const inactiveError = new HttpErrorResponse({
      status: 401,
      statusText: 'Unauthorized',
      headers: new HttpHeaders({ 'X-Login-Blocked-Reason': 'InactiveAccount' }),
    });
    authService.login.and.returnValue(throwError(() => inactiveError));
    authService.getLoginBlockedReason.and.returnValue('InactiveAccount');

    component.credentials.email = 'pending@example.com';
    component.credentials.password = 'StrongP@ssw0rd';

    await component.login();

    expect(component.errorMessage).toContain('awaiting administrator approval');
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('falls back to the generic message when no block reason is signalled', async () => {
    authService.login.and.returnValue(throwError(() => new HttpErrorResponse({ status: 401 })));
    authService.getLoginBlockedReason.and.returnValue(null);

    component.credentials.email = 'someone@example.com';
    component.credentials.password = 'wrong-password';

    await component.login();

    expect(component.errorMessage).toContain('Login failed');
  });

  function loginResult(overrides: Partial<LoginResult>): LoginResult {
    return {
      authenticated: false,
      requiresTwoFactor: false,
      ...overrides,
    };
  }
});
