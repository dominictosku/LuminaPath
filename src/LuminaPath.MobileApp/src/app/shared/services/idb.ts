/**
 * Tiny hand-rolled IndexedDB wrapper used for offline persistence of view
 * snapshots and library items. One object store (`snapshots`) keyed by an
 * opaque string. Values are arbitrary JSON-serializable shapes — we trust
 * the caller to round-trip them through `structuredClone` semantics.
 *
 * Why not localStorage: ~5MB cap, synchronous, blocks the main thread on
 * larger writes. A modest library + dashboard snapshot can exceed that.
 *
 * Why not @capacitor/preferences or SQLite: they add native plugins for
 * what is already a solved problem in the WebView. IndexedDB works in the
 * browser and on Android WebView identically.
 *
 * Everything is best-effort. If IndexedDB is unavailable (private mode in
 * some browsers, very old WebViews), helpers resolve to `undefined` / no-op
 * so callers can treat persistence as opportunistic.
 */

const DB_NAME = 'luminapath-offline';
const STORE = 'snapshots';
const DB_VERSION = 1;

type SnapshotRecord = {
  data: unknown;
  fetchedAt: number;
};

let dbPromise: Promise<IDBDatabase | null> | null = null;

function isSupported(): boolean {
  return typeof globalThis !== 'undefined' && 'indexedDB' in globalThis;
}

function openDb(): Promise<IDBDatabase | null> {
  if (!isSupported()) return Promise.resolve(null);
  if (dbPromise) return dbPromise;

  dbPromise = new Promise<IDBDatabase | null>((resolve) => {
    let request: IDBOpenDBRequest;
    try {
      request = indexedDB.open(DB_NAME, DB_VERSION);
    } catch {
      resolve(null);
      return;
    }

    request.onupgradeneeded = () => {
      const db = request.result;
      if (!db.objectStoreNames.contains(STORE)) {
        db.createObjectStore(STORE);
      }
    };
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => resolve(null);
    request.onblocked = () => resolve(null);
  });

  return dbPromise;
}

function tx(db: IDBDatabase, mode: IDBTransactionMode): IDBObjectStore {
  return db.transaction(STORE, mode).objectStore(STORE);
}

function promisifyRequest<T>(request: IDBRequest<T>): Promise<T | null> {
  return new Promise((resolve) => {
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => resolve(null);
  });
}

/** Read a single snapshot. Returns null when missing or IDB is unavailable. */
export async function idbGet<T>(key: string): Promise<{ data: T; fetchedAt: number } | null> {
  const db = await openDb();
  if (!db) return null;
  const record = (await promisifyRequest<SnapshotRecord>(tx(db, 'readonly').get(key))) ?? null;
  if (!record || typeof record !== 'object' || !('data' in record)) {
    return null;
  }
  return { data: record.data as T, fetchedAt: Number(record.fetchedAt) || 0 };
}

/** Persist a snapshot. No-op on unsupported / failed environments. */
export async function idbSet<T>(key: string, data: T, fetchedAt: number = Date.now()): Promise<void> {
  const db = await openDb();
  if (!db) return;
  const record: SnapshotRecord = { data, fetchedAt };
  await promisifyRequest(tx(db, 'readwrite').put(record, key));
}

/** Delete a single key. */
export async function idbDelete(key: string): Promise<void> {
  const db = await openDb();
  if (!db) return;
  await promisifyRequest(tx(db, 'readwrite').delete(key));
}

/** Wipe the entire store. Used on sign-out and from the Settings "clear" action. */
export async function idbClear(): Promise<void> {
  const db = await openDb();
  if (!db) return;
  await promisifyRequest(tx(db, 'readwrite').clear());
}

/** Return all entries. Used to hydrate the in-memory cache on startup. */
export async function idbAll(): Promise<{ key: string; data: unknown; fetchedAt: number }[]> {
  const db = await openDb();
  if (!db) return [];

  const store = tx(db, 'readonly');
  const keysReq = store.getAllKeys();
  const valuesReq = store.getAll();

  const [keys, values] = await Promise.all([
    promisifyRequest<IDBValidKey[]>(keysReq),
    promisifyRequest<SnapshotRecord[]>(valuesReq),
  ]);

  if (!keys || !values || keys.length !== values.length) return [];

  const entries: { key: string; data: unknown; fetchedAt: number }[] = [];
  for (let i = 0; i < keys.length; i++) {
    const key = keys[i];
    const value = values[i];
    if (typeof key !== 'string' || !value || typeof value !== 'object') continue;
    entries.push({ key, data: value.data, fetchedAt: Number(value.fetchedAt) || 0 });
  }
  return entries;
}

/** Internal: reset the cached connection. Exposed for tests. */
export function _resetIdbForTests(): void {
  dbPromise = null;
}
