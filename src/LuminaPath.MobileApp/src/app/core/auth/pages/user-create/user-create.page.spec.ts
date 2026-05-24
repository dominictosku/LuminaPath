import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';

import { AuthService, RegisterResult } from '../../services/auth.service';
import { UserCreatePage } from './user-create.page';

describe('UserCreatePage', () => {
  let component: UserCreatePage;
  let fixture: ComponentFixture<UserCreatePage>;
  let authService: jasmine.SpyObj<AuthService>;
  let navigateSpy: jasmine.Spy;

  beforeEach(async () => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['register']);
    authService.register.and.returnValue(of(registerResult({ created: true })));

    await TestBed.configureTestingModule({
      imports: [UserCreatePage],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService },
      ],
    }).compileComponents();

    navigateSpy = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);

    fixture = TestBed.createComponent(UserCreatePage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('blocks submit and shows a message when passwords do not match', async () => {
    component.email = 'user@example.com';
    component.password = 'StrongP@ssw0rd';
    component.confirmPassword = 'something-else';

    await component.register();

    expect(authService.register).not.toHaveBeenCalled();
    expect(component.errorMessage).toContain('match');
    expect(component.registrationComplete).toBeFalse();
  });

  it('blocks submit when the password is shorter than the policy minimum', async () => {
    component.email = 'user@example.com';
    component.password = 'Short1!';
    component.confirmPassword = 'Short1!';

    await component.register();

    expect(authService.register).not.toHaveBeenCalled();
    expect(component.errorMessage).toContain('10');
  });

  it('switches to the success view when the backend reports the account was created', async () => {
    component.email = 'user@example.com';
    component.password = 'StrongP@ssw0rd';
    component.confirmPassword = 'StrongP@ssw0rd';

    await component.register();

    expect(authService.register).toHaveBeenCalledWith('user@example.com', 'StrongP@ssw0rd');
    expect(component.registrationComplete).toBeTrue();
    // Sensitive fields are wiped after a successful registration so the
    // bfcache can't leak them.
    expect(component.password).toBe('');
    expect(component.confirmPassword).toBe('');
    // We deliberately do NOT auto-navigate — the user has to click the
    // "Go to sign in" button so they actually see the approval-pending
    // copy.
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('surfaces backend validation errors on the form', async () => {
    authService.register.and.returnValue(of(registerResult({
      created: false,
      errors: [
        { field: 'Password', message: 'Passwords must be at least 10 characters.' },
        { field: 'Password', message: 'Passwords must have at least one non alphanumeric character.' },
      ],
    })));

    component.email = 'user@example.com';
    component.password = 'StrongP@ssw0rd';
    component.confirmPassword = 'StrongP@ssw0rd';

    await component.register();

    expect(component.registrationComplete).toBeFalse();
    expect(component.fieldErrors.length).toBe(2);
    expect(component.errorMessage).toContain('issues below');
  });

  it('navigates back to the login page from the success view', () => {
    component.goToLogin();
    expect(navigateSpy).toHaveBeenCalledWith(['/auth/login']);
  });

  function registerResult(overrides: Partial<RegisterResult>): RegisterResult {
    return {
      created: false,
      errors: [],
      ...overrides,
    };
  }
});
