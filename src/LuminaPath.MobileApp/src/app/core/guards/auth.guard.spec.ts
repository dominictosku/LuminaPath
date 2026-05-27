import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';

import { AuthService } from '../auth/services/auth.service';
import { AuthGuard } from './auth.guard';

describe('AuthGuard', () => {
  let service: AuthGuard;
  let auth: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['isLoggedIn']);
    router = jasmine.createSpyObj<Router>('Router', ['createUrlTree']);
    router.createUrlTree.and.returnValue({} as never);

    TestBed.configureTestingModule({
      providers: [
        AuthGuard,
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
      ],
    });
    service = TestBed.inject(AuthGuard);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('allows authenticated users', (done) => {
    auth.isLoggedIn.and.returnValue(of(true));

    service.canActivate().subscribe((result) => {
      expect(result).toBeTrue();
      expect(router.createUrlTree).not.toHaveBeenCalled();
      done();
    });
  });

  it('returns a login UrlTree for anonymous users', (done) => {
    const loginTree = {} as never;
    auth.isLoggedIn.and.returnValue(of(false));
    router.createUrlTree.and.returnValue(loginTree);

    service.canActivateChild().subscribe((result) => {
      expect(result).toBe(loginTree);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/auth/login']);
      done();
    });
  });
});
