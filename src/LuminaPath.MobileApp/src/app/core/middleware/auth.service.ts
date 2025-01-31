import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private refreshInProgress = false;
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getUserInfo(): Observable<MLPUser> {
    return this.http.get<MLPUser>(`${this.apiUrl}/user/info`, {
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
