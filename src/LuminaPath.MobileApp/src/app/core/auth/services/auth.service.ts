import { Credentials, User } from '../models/user.model';
import { catchError, map, Observable, of, tap, throwError } from 'rxjs';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';

export interface LoginResult {
  authenticated: boolean;
  requiresTwoFactor: boolean;
}

interface IdentityLoginResponse {
  requiresTwoFactor?: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
  private apiEndpoint = inject(ApiEndpointService);

  private readonly authenticatedSignal = signal<boolean>(false);
  readonly isAuthenticated = this.authenticatedSignal.asReadonly();

  getUserInfo(): Observable<User> {
    return this.http.get<User>(this.apiEndpoint.url('manage/info'), {
      withCredentials: true,
    });
  }

  login(credentials: Credentials): Observable<LoginResult> {
    return this.http.post<IdentityLoginResponse | null>(`${this.apiEndpoint.url('login')}?useCookies=true&useSessionCookies=true`, this.toLoginPayload(credentials), {
      withCredentials: true,
    }).pipe(
      map((response) => {
        const requiresTwoFactor = response?.requiresTwoFactor === true;
        return {
          authenticated: !requiresTwoFactor,
          requiresTwoFactor,
        };
      }),
      catchError((error: unknown) => {
        if (this.isTwoFactorChallenge(error)) {
          return of({
            authenticated: false,
            requiresTwoFactor: true,
          });
        }

        this.authenticatedSignal.set(false);
        return throwError(() => error);
      }),
      tap((result) => this.authenticatedSignal.set(result.authenticated))
    );
  }

  logout() {
    return this.http.post(
      this.apiEndpoint.url('logout'),
      {},
      { withCredentials: true }
    ).pipe(
      tap(() => this.authenticatedSignal.set(false)),
      catchError((error) => {
        this.authenticatedSignal.set(false);
        throw error;
      })
    );
  }

  isLoggedIn(): Observable<boolean> {
    return this.getUserInfo().pipe(
      map(() => {
        this.authenticatedSignal.set(true);
        return true;
      }),
      catchError(() => {
        this.authenticatedSignal.set(false);
        return of(false);
      })
    );
  }

  refreshSession(): Observable<any> {
    return this.isLoggedIn();
  }

  clearSession() {
    this.authenticatedSignal.set(false);
  }

  private toLoginPayload(credentials: Credentials) {
    return {
      email: credentials.email,
      password: credentials.password,
      twoFactorCode: credentials.twoFactorCode || undefined,
      twoFactorRecoveryCode: credentials.twoFactorRecoveryCode || undefined,
    };
  }

  private isTwoFactorChallenge(error: unknown): boolean {
    if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
      return false;
    }

    const payload = error.error;
    if (typeof payload === 'string') {
      return payload.includes('RequiresTwoFactor');
    }

    if (!payload || typeof payload !== 'object') {
      return false;
    }

    const problem = payload as { detail?: unknown; title?: unknown; errors?: unknown };
    return problem.detail === 'RequiresTwoFactor'
      || problem.title === 'RequiresTwoFactor'
      || JSON.stringify(problem.errors ?? '').includes('RequiresTwoFactor');
  }
}
