import { RequestCache } from './request-cache.service';

describe('RequestCache', () => {
  let cache: RequestCache;

  beforeEach(() => {
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
});
