import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';

@Injectable()
export class CookieInterceptor implements HttpInterceptor {
  constructor(private router: Router) {}

  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    const isAuthRequest = request.url.includes('/login') || request.url.includes('/logout');
    const credentialsRequest = request.withCredentials
      ? request
      : request.clone({ withCredentials: true });

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
