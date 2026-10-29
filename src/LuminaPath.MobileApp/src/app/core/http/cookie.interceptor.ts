import { Injectable, inject } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpHandlerFn,
  HttpEvent,
  HttpInterceptor,
  HttpInterceptorFn,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';

const LUMINA_CLIENT_HEADER = 'X-Lumina-Client';
const LUMINA_CLIENT_HEADER_VALUE = 'web';
const UNSAFE_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

function handleCookieInterception(
  request: HttpRequest<unknown>,
  next: (req: HttpRequest<unknown>) => Observable<HttpEvent<unknown>>,
  router: Router
): Observable<HttpEvent<unknown>> {
  const isAuthRequest =
    request.url.includes('/login') || request.url.includes('/logout');
  let credentialsRequest = request.withCredentials
    ? request
    : request.clone({ withCredentials: true });

  if (shouldAddClientHeader(credentialsRequest)) {
    credentialsRequest = credentialsRequest.clone({
      setHeaders: { [LUMINA_CLIENT_HEADER]: LUMINA_CLIENT_HEADER_VALUE },
    });
  }

  return next(credentialsRequest).pipe(
    catchError((error) => {
      if (error.status === 401 && !isAuthRequest) {
        router.navigate(['/auth/login']);
      }
      return throwError(() => error);
    })
  );
}

export const cookieInterceptor: HttpInterceptorFn = (
  request: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const router = inject(Router);
  return handleCookieInterception(request, next, router);
};

@Injectable()
export class CookieInterceptor implements HttpInterceptor {
  private readonly router = inject(Router);

  intercept(
    request: HttpRequest<unknown>,
    next: HttpHandler
  ): Observable<HttpEvent<unknown>> {
    return handleCookieInterception(request, (req) => next.handle(req), this.router);
  }
}

function shouldAddClientHeader(request: HttpRequest<unknown>): boolean {
  return UNSAFE_METHODS.has(request.method.toUpperCase())
    && isApiRequest(request.url)
    && !request.headers.has(LUMINA_CLIENT_HEADER);
}

function isApiRequest(url: string): boolean {
  try {
    const baseUrl = globalThis.location?.origin ?? 'http://localhost';
    const path = new URL(url, baseUrl).pathname;
    return path === '/api' || path.endsWith('/api') || path.includes('/api/');
  } catch {
    return url === '/api' || url.startsWith('/api/') || url.includes('/api/');
  }
}
