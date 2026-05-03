import { Credentials, User } from '../models/user.model';
import { BehaviorSubject, catchError, map, Observable, of, tap } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private authenticated = false;
  private authenticatedSubject = new BehaviorSubject<boolean>(false);
  readonly authenticated$ = this.authenticatedSubject.asObservable();

  constructor(private http: HttpClient, private apiEndpoint: ApiEndpointService) {}

  public isAuthenticated() {
    return this.authenticated;
  }

  getUserInfo(): Observable<User> {
    return this.http.get<User>(this.apiEndpoint.url('manage/info'), {
      withCredentials: true,
    });
  }

  login(credentials: Credentials) {
    return this.http.post(`${this.apiEndpoint.url('login')}?useCookies=true`, credentials, {
      withCredentials: true,
    }).pipe(tap(() => this.setAuthenticated(true)));
  }

  logout() {
    return this.http.post(
      this.apiEndpoint.url('logout'),
      {},
      { withCredentials: true }
    ).pipe(
      tap(() => this.setAuthenticated(false)),
      catchError((error) => {
        this.setAuthenticated(false);
        throw error;
      })
    );
  }

  isLoggedIn(): Observable<boolean> {
    return this.getUserInfo().pipe(
      map(() => {
        this.setAuthenticated(true);
        return true;
      }),
      catchError(() => {
        this.setAuthenticated(false);
        return of(false);
      })
    );
  }

  refreshSession(): Observable<any> {
    return this.isLoggedIn();
  }

  clearSession() {
    this.setAuthenticated(false);
  }

  private setAuthenticated(value: boolean) {
    this.authenticated = value;
    this.authenticatedSubject.next(value);
  }
}
