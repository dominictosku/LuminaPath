import { Injectable } from '@angular/core';

interface CacheEntry<T> {
  data: T;
  fetchedAt: number;
}

/**
 * Tiny in-memory cache for view-level data snapshots, used for the
 * stale-while-revalidate pattern on pages that do a lot of upfront fetching
 * (dashboard, statistic).
 *
 * Intended usage from a page that already orchestrates its own requests:
 *
 *   const cached = this.cache.get<MySnapshot>('home:v1');
 *   if (cached) {
 *     this.apply(cached);
 *     this.isLoading = false;
 *     if (this.cache.isFresh('home:v1')) return; // skip refetch
 *   }
 *   // …fire the real request and call cache.set() on success
 *
 * No Observable wrapping by design — pages stay in control of how they paint
 * cached vs. fresh data. The cache survives navigation but is cleared on
 * full page reload (it's just a Map).
 */
@Injectable({ providedIn: 'root' })
export class RequestCache {
  /** Default freshness window. Override via `isFresh(key, ms)` per call site. */
  static readonly DEFAULT_TTL_MS = 60_000;

  private readonly entries = new Map<string, CacheEntry<unknown>>();

  /** Sync read. Returns the cached value regardless of age, or undefined. */
  get<T>(key: string): T | undefined {
    return this.entries.get(key)?.data as T | undefined;
  }

  /**
   * True if `key` is cached AND was fetched within the last `maxAgeMs`.
   * Callers use this to decide whether to skip a background refetch when
   * pull-to-refresh wasn't explicitly requested.
   */
  isFresh(key: string, maxAgeMs: number = RequestCache.DEFAULT_TTL_MS): boolean {
    const entry = this.entries.get(key);
    if (!entry) return false;
    return Date.now() - entry.fetchedAt < maxAgeMs;
  }

  /** Store (or replace) a snapshot. Stamps fetchedAt to now. */
  set<T>(key: string, data: T): void {
    this.entries.set(key, { data, fetchedAt: Date.now() });
  }

  /** Drop one or many entries. String form matches by exact key or `prefix:*`. */
  invalidate(pattern: string | RegExp): void {
    const matches = typeof pattern === 'string'
      ? (key: string) => key === pattern || key.startsWith(pattern + ':')
      : (key: string) => pattern.test(key);
    for (const key of [...this.entries.keys()]) {
      if (matches(key)) this.entries.delete(key);
    }
  }

  /** Wipe everything. Call on sign-out. */
  clear(): void {
    this.entries.clear();
  }
}
