import { Injectable } from '@angular/core';
import { idbAll, idbClear, idbDelete, idbSet } from './idb';

interface CacheEntry<T> {
  data: T;
  fetchedAt: number;
}

/**
 * View-level data snapshot cache with stale-while-revalidate semantics,
 * used by dashboard / statistic / library where the page orchestrates its
 * own fetching.
 *
 * Two tiers:
 *  - **In-memory `Map`** — synchronous reads (`get` / `isFresh`) so existing
 *    page code (`if (cached) apply() ...`) stays unchanged.
 *  - **IndexedDB** — write-through on every `set()`, hydrated into memory
 *    once at app bootstrap via `hydrate()` (called from `APP_INITIALIZER`).
 *    Lets the app paint the last-known good state on a cold launch — the
 *    foundation of read-only offline mode.
 *
 * IDB writes are fire-and-forget; failure to persist never affects the
 * in-memory result. `idb.ts` swallows IndexedDB errors so private-mode and
 * unsupported environments degrade to memory-only without throwing.
 */
@Injectable({ providedIn: 'root' })
export class RequestCache {
  /** Default freshness window. Override via `isFresh(key, ms)` per call site. */
  static readonly DEFAULT_TTL_MS = 60_000;

  private readonly entries = new Map<string, CacheEntry<unknown>>();
  /** Resolves once hydrate() has either completed or timed out. */
  private hydratePromise: Promise<void> | null = null;

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

  /** Store (or replace) a snapshot. Stamps fetchedAt to now and persists. */
  set<T>(key: string, data: T): void {
    const fetchedAt = Date.now();
    this.entries.set(key, { data, fetchedAt });
    // Persist async; never block the caller. Errors are swallowed in idb.ts.
    void idbSet(key, data, fetchedAt);
  }

  /** Drop one or many entries. String form matches by exact key or `prefix:*`. */
  invalidate(pattern: string | RegExp): void {
    const matches = typeof pattern === 'string'
      ? (key: string) => key === pattern || key.startsWith(pattern + ':')
      : (key: string) => pattern.test(key);
    for (const key of [...this.entries.keys()]) {
      if (matches(key)) {
        this.entries.delete(key);
        void idbDelete(key);
      }
    }
  }

  /** Wipe everything (memory + IDB). Call on sign-out and from settings. */
  clear(): void {
    this.entries.clear();
    void idbClear();
  }

  /**
   * Load all persisted snapshots into the in-memory Map. Idempotent —
   * subsequent calls return the same promise. Caller (APP_INITIALIZER)
   * is responsible for adding a timeout so a stuck IDB never blocks
   * bootstrap.
   */
  hydrate(): Promise<void> {
    if (this.hydratePromise) return this.hydratePromise;
    this.hydratePromise = (async () => {
      const entries = await idbAll();
      for (const { key, data, fetchedAt } of entries) {
        // Don't clobber anything that was written between bootstrap and
        // hydrate completing (unlikely but cheap to guard against).
        if (!this.entries.has(key)) {
          this.entries.set(key, { data, fetchedAt });
        }
      }
    })();
    return this.hydratePromise;
  }
}
