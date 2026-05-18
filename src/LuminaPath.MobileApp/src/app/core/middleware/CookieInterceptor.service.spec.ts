import { HttpHandler, HttpRequest } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { CookieInterceptor } from './CookieInterceptor.service';

function setup() {
  const router = jasmine.createSpyObj<Router>('Router', ['navigate']);
  TestBed.configureTestingModule({
    providers: [
      CookieInterceptor,
      { provide: Router, useValue: router },
    ],
  });
  const interceptor = TestBed.inject(CookieInterceptor);
  return { router, interceptor };
}

describe('CookieInterceptor', () => {
  it('adds credentials to outgoing requests', (done) => {
    const { interceptor } = setup();
    const request = new HttpRequest('GET', '/api/games');
    const next: HttpHandler = {
      handle: jasmine.createSpy('handle').and.callFake((handledRequest: HttpRequest<unknown>) => {
        expect(handledRequest.withCredentials).toBeTrue();
        return of({} as never);
      }),
    };

    interceptor.intercept(request, next).subscribe(() => {
      expect(next.handle).toHaveBeenCalled();
      done();
    });
  });

  it('redirects to login on protected 401 responses', (done) => {
    const { router, interceptor } = setup();
    const request = new HttpRequest('GET', '/api/games');
    const next: HttpHandler = {
      handle: jasmine.createSpy('handle').and.returnValue(throwError(() => ({ status: 401 }))),
    };

    interceptor.intercept(request, next).subscribe({
      error: () => {
        expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
        done();
      },
    });
  });

  it('does not redirect failed login requests', (done) => {
    const { router, interceptor } = setup();
    const request = new HttpRequest('POST', '/api/login?useCookies=true', {});
    const next: HttpHandler = {
      handle: jasmine.createSpy('handle').and.returnValue(throwError(() => ({ status: 401 }))),
    };

    interceptor.intercept(request, next).subscribe({
      error: () => {
        expect(router.navigate).not.toHaveBeenCalled();
        done();
      },
    });
  });
});
