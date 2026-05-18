import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';

export type GamingSession = {
  id: number;
  myGameId: number | null;
  gameName: string | null;
  scheduledAt: string;
  durationMinutes: number;
  completed: boolean;
  completedAt: string | null;
  notes: string | null;
  createdAt: string;
};

export type GamingSessionDraft = Omit<GamingSession, 'id' | 'gameName' | 'createdAt' | 'completedAt'> & {
  id?: number;
  completedAt?: string | null;
};

export type GameForecast = {
  myGameId: number;
  gameName: string;
  playtimeEstimateHours: number | null;
  playedHours: number;
  remainingHours: number | null;
  scheduledHours: number;
  upcomingSessionCount: number;
  sessionsToCompletion: number | null;
  projectedCompletionDate: string | null;
  weeklyHours: number;
  additionalHoursNeeded: number;
  weeksAtCurrentPace: number | null;
};

@Injectable({ providedIn: 'root' })
export class GamingSessionService {
  private http = inject(HttpClient);
  private apiEndpoint = inject(ApiEndpointService);

  private readonly httpConfig = { withCredentials: true };

  list(options: { myGameId?: number; from?: Date | string; to?: Date | string } = {}): Observable<GamingSession[]> {
    let params = new HttpParams();
    if (options.myGameId != null) {
      params = params.set('myGameId', String(options.myGameId));
    }
    if (options.from) {
      params = params.set('from', this.toIso(options.from));
    }
    if (options.to) {
      params = params.set('to', this.toIso(options.to));
    }
    return this.http.get<GamingSession[]>(this.apiEndpoint.url('gamingsessions'), { ...this.httpConfig, params });
  }

  create(draft: GamingSessionDraft): Observable<GamingSession> {
    return this.http.post<GamingSession>(this.apiEndpoint.url('gamingsessions'), this.toApi(draft), this.httpConfig);
  }

  update(id: number, draft: GamingSessionDraft): Observable<GamingSession> {
    return this.http.put<GamingSession>(this.apiEndpoint.url(`gamingsessions/${id}`), this.toApi({ ...draft, id }), this.httpConfig);
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(this.apiEndpoint.url(`gamingsessions/${id}`), this.httpConfig);
  }

  forecast(myGameId: number): Observable<GameForecast> {
    return this.http.get<GameForecast>(this.apiEndpoint.url(`gamingsessions/forecast/${myGameId}`), this.httpConfig);
  }

  private toApi(draft: GamingSessionDraft) {
    return {
      id: draft.id ?? 0,
      myGameId: draft.myGameId ?? null,
      scheduledAt: draft.scheduledAt,
      durationMinutes: draft.durationMinutes,
      completed: draft.completed,
      completedAt: draft.completedAt ?? null,
      notes: draft.notes ?? null,
    };
  }

  private toIso(value: Date | string): string {
    return value instanceof Date ? value.toISOString() : value;
  }
}
