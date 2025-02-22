import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { AuthService } from '../auth/services/auth.service';
import { Router } from '@angular/router';

@Injectable()
export class CookieInterceptor implements HttpInterceptor {
  constructor(private authService: AuthService, private router: Router) {}

  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    return next.handle(request).pipe(
      catchError((error) => {
        if (error.status === 401) {
          return this.authService.refreshSession().pipe(
            switchMap(() => {
              return next.handle(request);
            }),
            catchError((refreshError) => {
              this.authService.logout();
              this.router.navigate(['/auth/login']);
              return throwError(() => new Error(refreshError));
            })
          );
        }
        return throwError(() => new Error(error));
      })
    );
  }
}
