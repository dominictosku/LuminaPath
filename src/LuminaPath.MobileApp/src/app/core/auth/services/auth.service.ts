import { canEditCatalog, Credentials, hasAdminRole, User } from '../models/user.model';
import { catchError, map, Observable, of, switchMap, tap, throwError } from 'rxjs';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';

export interface LoginResult {
  authenticated: boolean;
  requiresTwoFactor: boolean;
}

/**
 * Specific block reasons the backend can signal on a 401 from /api/login
 * via the `X-Login-Blocked-Reason` response header. Stays a string union
 * so unknown reasons surface as `null` and the UI falls back to its
 * generic message — that way a future backend reason can ship without
 * coordinated SPA changes.
 */
export type LoginBlockedReason = 'InactiveAccount';

const LOGIN_BLOCKED_REASON_HEADER = 'X-Login-Blocked-Reason';
const KNOWN_LOGIN_BLOCKED_REASONS: readonly LoginBlockedReason[] = ['InactiveAccount'];

export interface RegisterResult {
  /**
   * True when the server confirmed the account was created. The backend's
   * /api/register returns 200 OK on success regardless of whether the
   * admin-approval gate is on — callers should still show the
   * "awaiting approval" copy when they know that flag is enabled (we
   * can't detect it from the response).
   */
  created: boolean;
  /**
   * Validation errors keyed by field — surfaced verbatim from
   * Identity's ValidationProblemDetails (e.g. `Email`, `Password`).
   * Empty array when the call succeeded.
   */
  errors: RegisterFieldError[];
}

export interface RegisterFieldError {
  field: string;
  message: string;
}

interface IdentityLoginResponse {
  requiresTwoFactor?: boolean;
}

/** Shape Identity's /register returns for validation problems. */
interface IdentityValidationProblem {
  errors?: Record<string, string[]>;
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

  /**
   * POSTs to Identity's /api/register. Returns a normalized
   * {created, errors} result so the page doesn't have to know about
   * Identity's ValidationProblemDetails shape. We deliberately do NOT
   * auto-sign-in afterwards — if the admin-approval gate is enabled,
   * the user can't sign in yet, and even when it's off we want the
   * user to confirm their credentials work via the login screen.
   */
  register(email: string, password: string): Observable<RegisterResult> {
    return this.http.post(
      this.apiEndpoint.url('register'),
      { email, password },
      { withCredentials: true },
    ).pipe(
      map(() => ({ created: true, errors: [] as RegisterFieldError[] })),
      catchError((error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status >= 400 && error.status < 500) {
          return of({
            created: false,
            errors: this.parseRegisterErrors(error.error),
          });
        }
        return throwError(() => error);
      }),
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

  /**
   * Extracts the backend's block reason from a failed-login error so
   * the login page can show a tailored message (e.g. "awaiting admin
   * approval") instead of the generic "invalid credentials".
   *
   * <p>Returns null when the header is missing, the value isn't a
   * recognized reason, or the input isn't an HttpErrorResponse — all
   * of which collapse to "show the generic message" upstream.</p>
   *
   * <p>The header is only readable cross-origin because the backend's
   * CORS policy adds it to `Access-Control-Expose-Headers` — see
   * <c>AddCors</c> in the .NET DependencyInjection.cs.</p>
   */
  getLoginBlockedReason(error: unknown): LoginBlockedReason | null {
    if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
      return null;
    }
    const raw = error.headers?.get(LOGIN_BLOCKED_REASON_HEADER);
    if (!raw) {
      return null;
    }
    const trimmed = raw.trim();
    return (KNOWN_LOGIN_BLOCKED_REASONS as readonly string[]).includes(trimmed)
      ? (trimmed as LoginBlockedReason)
      : null;
  }

  /**
   * Flattens Identity's `{errors: {Email: ['msg'], Password: ['msg1','msg2']}}`
   * shape into the flat array our register page renders. Falls back to a
   * single generic error when the payload doesn't match the expected
   * shape (e.g. the backend returned a plain string).
   */
  private parseRegisterErrors(payload: unknown): RegisterFieldError[] {
    if (typeof payload === 'string' && payload.trim().length > 0) {
      return [{ field: '', message: payload }];
    }

    const problem = payload as IdentityValidationProblem | null;
    const errors = problem?.errors;
    if (!errors || typeof errors !== 'object') {
      return [{ field: '', message: 'Registration failed. Please try again.' }];
    }

    const flat: RegisterFieldError[] = [];
    for (const [field, messages] of Object.entries(errors)) {
      if (!Array.isArray(messages)) continue;
      for (const message of messages) {
        flat.push({ field, message: String(message) });
      }
    }
    return flat.length > 0
      ? flat
      : [{ field: '', message: 'Registration failed. Please try again.' }];
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
