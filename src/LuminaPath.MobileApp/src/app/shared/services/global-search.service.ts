import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiEndpointService } from './api-endpoint.service';

export interface GlobalSearchResult {
  kind: 'library' | 'note' | 'quest' | string;
  title: string;
  subtitle: string;
  matchedText: string | null;
  route: string;
  icon: string;
  mediaKind: string | null;
  mediaId: number | null;
  libraryEntryId: number | null;
  questId: number | null;
}

@Injectable({ providedIn: 'root' })
export class GlobalSearchService {
  private readonly http = inject(HttpClient);
  private readonly apiEndpoint = inject(ApiEndpointService);
  private readonly openSignal = signal(false);

  readonly isOpen = this.openSignal.asReadonly();

  open(): void {
    this.openSignal.set(true);
  }

  close(): void {
    this.openSignal.set(false);
  }

  search(query: string, limit = 12): Observable<GlobalSearchResult[]> {
    const params = new HttpParams()
      .set('q', query)
      .set('limit', String(limit));

    return this.http.get<GlobalSearchResult[]>(this.apiEndpoint.url('search'), {
      withCredentials: true,
      params,
    });
  }
}
