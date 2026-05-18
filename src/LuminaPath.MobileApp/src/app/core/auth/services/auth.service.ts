import { Credentials, User } from '../models/user.model';
import { catchError, map, Observable, of, tap } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';

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

  login(credentials: Credentials) {
    return this.http.post(`${this.apiEndpoint.url('login')}?useCookies=true`, credentials, {
      withCredentials: true,
    }).pipe(tap(() => this.authenticatedSignal.set(true)));
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
}
