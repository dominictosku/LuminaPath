import { environment } from 'src/environments/environment';
import { Credentials, User } from '../models/user.model';
import { Observable } from 'rxjs/internal/Observable';
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private refreshInProgress = false;
  private apiUrl = environment.endpoint;

  constructor(private http: HttpClient) {}

  public isAuthenticated() {
    return false;
  }

  getUserInfo(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/user/info`, {
      withCredentials: true,
    });
  }

  login(credentials: Credentials) {
    return this.http.post(`${this.apiUrl}/login?useCookies=true`, credentials, {
      withCredentials: true,
    });
  }

  logout() {
    return this.http.post(
      `${this.apiUrl}/logout`,
      {},
      { withCredentials: true }
    );
  }

  isLoggedIn(): Observable<boolean> {
    return this.http.get<boolean>(`${this.apiUrl}/auth/check`, {
      withCredentials: true,
    });
  }

  refreshSession(): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/auth/refresh`,
      {},
      { withCredentials: true }
    );
  }
}
