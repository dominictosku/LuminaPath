import { canEditCatalog, Credentials, hasAdminRole, User } from '../models/user.model';
import { catchError, map, Observable, of, switchMap, tap, throwError } from 'rxjs';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';

export interface LoginResult {
  authenticated: boolean;
  requiresTwoFactor: boolean;
}

interface IdentityLoginResponse {
  requiresTwoFactor?: boolean;
}

/**
 * Loose shape of the `/manage/info` response. ASP.NET Core Identity's
 * default `InfoResponse` exposes email + isEmailConfirmed; we accept any
 * extras the server may add.
 */
interface UserInfoResponse {
  userName?: string;
  email?: string;
  age?: number;
  [key: string]: unknown;
}

/**
 * Shape of `/api/manage/roles`. The backend returns a `Roles` array
 * (System.Text.Json camel-cases this to `roles` by default but accept
 * both for safety in case the server is reconfigured).
 */
interface UserRolesResponse {
  roles?: string[];
  Roles?: string[];
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
  private apiEndpoint = inject(ApiEndpointService);

  private readonly authenticatedSignal = signal<boolean>(false);
  readonly isAuthenticated = this.authenticatedSignal.asReadonly();

  /**
   * Roles for the current session, populated from `/api/manage/roles`
   * after a successful auth check. Stays empty when the call fails or
   * returns nothing — admin-gated UI just hides in that case.
   */
  private readonly rolesSignal = signal<readonly string[]>([]);
  readonly roles = this.rolesSignal.asReadonly();
  readonly isAdmin = computed(() => hasAdminRole(this.rolesSignal()));
  readonly canEditCatalog = computed(() => canEditCatalog(this.rolesSignal()));

  getUserInfo(): Observable<User> {
    return this.http.get<UserInfoResponse>(this.apiEndpoint.url('manage/info'), {
      withCredentials: true,
    }).pipe(
      map((response) => response as unknown as User),
    );
  }

  /**
   * Fetches the current user's roles from the dedicated backend endpoint
   * and writes them into `rolesSignal`. Swallows errors (returns []) so a
   * missing endpoint or transient failure can't break the auth flow —
   * the UI just falls back to "no admin access".
   */
  refreshRoles(): Observable<readonly string[]> {
    return this.http.get<UserRolesResponse>(this.apiEndpoint.url('manage/roles'), {
      withCredentials: true,
    }).pipe(
      map((response) => response?.roles ?? response?.Roles ?? []),
      tap((roles) => this.rolesSignal.set([...roles])),
      catchError(() => {
        this.rolesSignal.set([]);
        return of<readonly string[]>([]);
      }),
    );
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
      tap(() => {
        this.authenticatedSignal.set(false);
        this.rolesSignal.set([]);
      }),
      catchError((error) => {
        this.authenticatedSignal.set(false);
        this.rolesSignal.set([]);
        throw error;
      })
    );
  }

  isLoggedIn(): Observable<boolean> {
    return this.getUserInfo().pipe(
      switchMap(() => {
        this.authenticatedSignal.set(true);
        // Hydrate roles alongside the auth check so admin-only UI is
        // available immediately. `refreshRoles` swallows its own errors,
        // so a missing endpoint can't make a logged-in user look logged
        // out.
        return this.refreshRoles().pipe(map(() => true));
      }),
      catchError(() => {
        this.authenticatedSignal.set(false);
        this.rolesSignal.set([]);
        return of(false);
      })
    );
  }

  refreshSession(): Observable<any> {
    return this.isLoggedIn();
  }

  clearSession() {
    this.authenticatedSignal.set(false);
    this.rolesSignal.set([]);
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
