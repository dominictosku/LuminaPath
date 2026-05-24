import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';

/**
 * Per-user PSN trophy totals, mirrored from PSN's tier names (the same
 * vocabulary the system displays on profile cards). All four counts are
 * always present — the server returns 0 instead of null when a tier is
 * empty, so the UI can render without null-checks.
 */
export interface PsnTrophyTotals {
  bronze: number;
  silver: number;
  gold: number;
  platinum: number;
  total: number;
}

/**
 * Tiny dedicated service for the statistic page's aggregate endpoints.
 * Kept separate from the per-media services because these queries don't
 * fit a CRUD pattern — they're read-only summaries over multiple tables.
 */
@Injectable({ providedIn: 'root' })
export class StatisticService {
  private readonly http = inject(HttpClient);
  private readonly apiEndpoint = inject(ApiEndpointService);

  getPsnTrophyTotals(): Observable<PsnTrophyTotals> {
    return this.http.get<PsnTrophyTotals>(
      this.apiEndpoint.url('statistics/psn-trophies'),
      { withCredentials: true },
    );
  }
}
