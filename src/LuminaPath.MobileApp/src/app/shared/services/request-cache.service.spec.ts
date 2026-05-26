import { _resetIdbForTests, idbAll, idbClear, idbGet } from './idb';
import { RequestCache } from './request-cache.service';

describe('RequestCache', () => {
  let cache: RequestCache;

  beforeEach(async () => {
    _resetIdbForTests();
    await idbClear();
    cache = new RequestCache();
  });

  describe('get / set', () => {
    it('returns undefined when key is absent', () => {
      expect(cache.get('missing')).toBeUndefined();
    });

    it('round-trips arbitrary values by reference', () => {
      const payload = { games: [1, 2, 3] };
      cache.set('home:v1', payload);
      expect(cache.get('home:v1')).toBe(payload);
    });

    it('overwrites prior entries on set', () => {
      cache.set('k', 'first');
      cache.set('k', 'second');
      expect(cache.get<string>('k')).toBe('second');
    });
  });

  describe('isFresh', () => {
    beforeEach(() => {
      jasmine.clock().install();
      jasmine.clock().mockDate(new Date(2026, 0, 1));
    });

    afterEach(() => jasmine.clock().uninstall());

    it('is false for unknown keys', () => {
      expect(cache.isFresh('nope')).toBeFalse();
    });

    it('is true within the default TTL window', () => {
      cache.set('k', 'v');
      jasmine.clock().tick(RequestCache.DEFAULT_TTL_MS - 1);
      expect(cache.isFresh('k')).toBeTrue();
    });

    it('flips to false at the TTL boundary', () => {
      cache.set('k', 'v');
      jasmine.clock().tick(RequestCache.DEFAULT_TTL_MS);
      expect(cache.isFresh('k')).toBeFalse();
    });

    it('honors a per-call maxAgeMs override', () => {
      cache.set('k', 'v');
      jasmine.clock().tick(500);
      expect(cache.isFresh('k', 200)).toBeFalse();
      expect(cache.isFresh('k', 1_000)).toBeTrue();
    });

    it('does NOT remove the entry once stale — get still returns the value', () => {
      cache.set('k', 'v');
      jasmine.clock().tick(RequestCache.DEFAULT_TTL_MS + 1);
      expect(cache.isFresh('k')).toBeFalse();
      expect(cache.get<string>('k')).toBe('v');
    });
  });

  describe('invalidate', () => {
    beforeEach(() => {
      cache.set('home:v1', 1);
      cache.set('home:metrics', 2);
      cache.set('statistic:v1', 3);
    });

    it('drops a single key matched exactly', () => {
      cache.invalidate('statistic:v1');
      expect(cache.get('statistic:v1')).toBeUndefined();
      expect(cache.get('home:v1')).toBe(1);
    });

    it('drops a whole prefix family when the string has no colon match', () => {
      cache.invalidate('home');
      expect(cache.get('home:v1')).toBeUndefined();
      expect(cache.get('home:metrics')).toBeUndefined();
      expect(cache.get('statistic:v1')).toBe(3);
    });

    it('drops by regex', () => {
      cache.invalidate(/^home:/);
      expect(cache.get('home:v1')).toBeUndefined();
      expect(cache.get('home:metrics')).toBeUndefined();
      expect(cache.get('statistic:v1')).toBe(3);
    });
  });

  describe('clear', () => {
    it('empties everything', () => {
      cache.set('a', 1);
      cache.set('b', 2);
      cache.clear();
      expect(cache.get('a')).toBeUndefined();
      expect(cache.get('b')).toBeUndefined();
    });
  });

  describe('IndexedDB persistence', () => {
    /** Poll until IDB has the expected entry. `set()` is fire-and-forget so
     *  the test has to wait for the write to settle — bounded retry rather
     *  than a hardcoded timeout. */
    async function waitForIdbValue<T>(key: string, expected: T, attempts = 20): Promise<T | null> {
      for (let i = 0; i < attempts; i++) {
        const record = await idbGet<T>(key);
        if (record && JSON.stringify(record.data) === JSON.stringify(expected)) {
          return record.data;
        }
        await new Promise((resolve) => setTimeout(resolve, 10));
      }
      return null;
    }

    it('writes through to IDB on set()', async () => {
      const payload = { items: [1, 2, 3] };
      cache.set('home:v1', payload);
      const stored = await waitForIdbValue('home:v1', payload);
      expect(stored).toEqual(payload);
    });

    it('hydrate() loads persisted entries into memory', async () => {
      cache.set('home:v1', { hello: 'world' });
      await waitForIdbValue('home:v1', { hello: 'world' });

      // Fresh instance — simulates a cold app boot.
      _resetIdbForTests();
      const next = new RequestCache();
      expect(next.get('home:v1')).toBeUndefined();
      await next.hydrate();
      expect(next.get<{ hello: string }>('home:v1')).toEqual({ hello: 'world' });
    });

    it('hydrate() does not clobber a value written between bootstrap and hydrate', async () => {
      cache.set('home:v1', 'persisted');
      await waitForIdbValue('home:v1', 'persisted');

      _resetIdbForTests();
      const next = new RequestCache();
      next.set('home:v1', 'live');
      await next.hydrate();
      expect(next.get<string>('home:v1')).toBe('live');
    });

    it('clear() wipes the IDB store', async () => {
      cache.set('a', 1);
      cache.set('b', 2);
      await waitForIdbValue('a', 1);
      await waitForIdbValue('b', 2);

      cache.clear();
      // Bounded wait for the async wipe; verify both keys are gone.
      let remaining = await idbAll();
      for (let i = 0; i < 20 && remaining.length > 0; i++) {
        await new Promise((resolve) => setTimeout(resolve, 10));
        remaining = await idbAll();
      }
      expect(remaining.length).toBe(0);
    });

    it('invalidate(prefix) removes the matching IDB entries', async () => {
      cache.set('home:v1', 1);
      cache.set('home:metrics', 2);
      cache.set('statistic:v1', 3);
      await waitForIdbValue('home:v1', 1);
      await waitForIdbValue('home:metrics', 2);
      await waitForIdbValue('statistic:v1', 3);

      cache.invalidate('home');
      let homeV1 = await idbGet('home:v1');
      let homeMetrics = await idbGet('home:metrics');
      for (let i = 0; i < 20 && (homeV1 !== null || homeMetrics !== null); i++) {
        await new Promise((resolve) => setTimeout(resolve, 10));
        homeV1 = await idbGet('home:v1');
        homeMetrics = await idbGet('home:metrics');
      }
      expect(homeV1).toBeNull();
      expect(homeMetrics).toBeNull();
      expect((await idbGet<number>('statistic:v1'))?.data).toBe(3);
    });
  });
});
