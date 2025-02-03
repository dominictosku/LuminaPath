import { environment } from 'src/environments/environment';
import { User } from '../models/user';
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

  public isAuthenticated() { return false; };

  getUserInfo(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/user/info`, {
      withCredentials: true,
    });
  }

  login(credentials: { username: string; password: string }) {
    return this.http.post(`${this.apiUrl}/auth/login`, credentials, {
      withCredentials: true,
    });
  }

  logout() {
    return this.http.post(
      `${this.apiUrl}/auth/logout`,
      {},
      { withCredentials: true }
    );
  }

  // Check if the user is logged in
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
