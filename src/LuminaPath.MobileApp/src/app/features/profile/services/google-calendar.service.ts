import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';

export interface GoogleCalendarStatus {
  /** Whether the server has Google OAuth credentials configured at all. */
  configured: boolean;
  connected: boolean;
  email: string | null;
  lastSyncedAt: string | null;
}

export interface GoogleCalendarSyncResult {
  releaseEvents: number;
  questEvents: number;
  deleted: number;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class GoogleCalendarService {
  private readonly http = inject(HttpClient);
  private readonly apiEndpoint = inject(ApiEndpointService);
  private readonly httpConfig = { withCredentials: true };

  getStatus(): Observable<GoogleCalendarStatus> {
    return this.http.get<GoogleCalendarStatus>(
      this.apiEndpoint.url('integrations/google/status'),
      this.httpConfig,
    );
  }

  sync(): Observable<GoogleCalendarSyncResult> {
    return this.http.post<GoogleCalendarSyncResult>(
      this.apiEndpoint.url('integrations/google/sync'),
      {},
      this.httpConfig,
    );
  }

  disconnect(): Observable<void> {
    return this.http.delete<void>(
      this.apiEndpoint.url('integrations/google'),
      this.httpConfig,
    );
  }

  /**
   * Absolute URL of the backend connect endpoint. Linking is a full-page
   * navigation (not XHR) so the browser follows the 302 to Google's consent
   * screen and back; the session cookie rides along on the top-level request.
   */
  connectUrl(): string {
    return this.apiEndpoint.url('integrations/google/connect');
  }
}
