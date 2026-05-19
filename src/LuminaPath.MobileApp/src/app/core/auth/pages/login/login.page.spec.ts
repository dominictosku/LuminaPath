import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';

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
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['login', 'clearSession']);

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

  function loginResult(overrides: Partial<LoginResult>): LoginResult {
    return {
      authenticated: false,
      requiresTwoFactor: false,
      ...overrides,
    };
  }
});
