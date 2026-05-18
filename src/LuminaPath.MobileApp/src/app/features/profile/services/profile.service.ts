import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { User } from 'src/app/core/auth/models/user.model';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';

export interface ProfileUpdate {
  userName?: string;
  newEmail?: string;
  age?: number;
  oldPassword?: string;
}

export interface PasswordChange {
  oldPassword: string;
  newPassword: string;
}

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private http = inject(HttpClient);
  private apiEndpoint = inject(ApiEndpointService);

  private readonly httpConfig = { withCredentials: true };

  updateProfile(update: ProfileUpdate): Observable<User> {
    return this.http.post<User>(
      this.apiEndpoint.url('manage/info'),
      update,
      this.httpConfig,
    );
  }

  changePassword(change: PasswordChange): Observable<void> {
    return this.http.post<void>(
      this.apiEndpoint.url('manage/info'),
      change,
      this.httpConfig,
    );
  }
}
