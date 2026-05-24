import { Injectable, inject } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';

const LUMINA_CLIENT_HEADER = 'X-Lumina-Client';
const LUMINA_CLIENT_HEADER_VALUE = 'web';
const UNSAFE_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

@Injectable()
export class CookieInterceptor implements HttpInterceptor {
  private router = inject(Router);


  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
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

    return next.handle(credentialsRequest).pipe(
      catchError((error) => {
        if (error.status === 401 && !isAuthRequest) {
          this.router.navigate(['/auth/login']);
        }
        return throwError(() => error);
      })
    );
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
