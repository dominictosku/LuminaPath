import { HttpHandler, HttpRequest } from '@angular/common/http';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { CookieInterceptor } from './CookieInterceptor.service';

describe('CookieInterceptor', () => {
  it('adds credentials to outgoing requests', (done) => {
    const router = createRouter();
    const interceptor = new CookieInterceptor(router);
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
    const router = createRouter();
    const interceptor = new CookieInterceptor(router);
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
    const router = createRouter();
    const interceptor = new CookieInterceptor(router);
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

function createRouter(): jasmine.SpyObj<Router> {
  return jasmine.createSpyObj<Router>('Router', ['navigate']);
}
