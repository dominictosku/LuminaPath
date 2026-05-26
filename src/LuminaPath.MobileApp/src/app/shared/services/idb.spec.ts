import { _resetIdbForTests, idbAll, idbClear, idbDelete, idbGet, idbSet } from './idb';

/**
 * Karma runs against real Chrome, so IndexedDB is genuinely available
 * here — we round-trip through the actual API rather than mocking it.
 * The wrapper is small enough that mocking would just re-test the mock.
 */
describe('idb', () => {
  beforeEach(async () => {
    _resetIdbForTests();
    // Each spec starts from a clean store; otherwise tests that ran in a
    // previous file in the same browser session would leak entries.
    await idbClear();
  });

  it('returns null for missing keys', async () => {
    const result = await idbGet('does-not-exist');
    expect(result).toBeNull();
  });

  it('round-trips a JSON payload', async () => {
    const payload = { items: [1, 2, 3], when: '2026-01-01' };
    await idbSet('home:v1', payload);
    const result = await idbGet<typeof payload>('home:v1');
    expect(result).not.toBeNull();
    expect(result?.data).toEqual(payload);
    expect(typeof result?.fetchedAt).toBe('number');
  });

  it('overwrites an existing key on set', async () => {
    await idbSet('k', 'first');
    await idbSet('k', 'second');
    const result = await idbGet<string>('k');
    expect(result?.data).toBe('second');
  });

  it('preserves the supplied fetchedAt stamp', async () => {
    const stamp = 1_700_000_000_000;
    await idbSet('k', 'v', stamp);
    const result = await idbGet<string>('k');
    expect(result?.fetchedAt).toBe(stamp);
  });

  it('deletes a single key', async () => {
    await idbSet('a', 1);
    await idbSet('b', 2);
    await idbDelete('a');
    expect(await idbGet('a')).toBeNull();
    expect((await idbGet<number>('b'))?.data).toBe(2);
  });

  it('clears everything', async () => {
    await idbSet('a', 1);
    await idbSet('b', 2);
    await idbClear();
    expect(await idbGet('a')).toBeNull();
    expect(await idbGet('b')).toBeNull();
  });

  it('returns all entries with their keys and stamps', async () => {
    await idbSet('home:v1', { type: 'home' }, 100);
    await idbSet('statistic:v1', { type: 'statistic' }, 200);

    const all = await idbAll();
    const byKey = new Map(all.map((entry) => [entry.key, entry]));

    expect(byKey.size).toBe(2);
    expect(byKey.get('home:v1')?.data).toEqual({ type: 'home' });
    expect(byKey.get('home:v1')?.fetchedAt).toBe(100);
    expect(byKey.get('statistic:v1')?.data).toEqual({ type: 'statistic' });
    expect(byKey.get('statistic:v1')?.fetchedAt).toBe(200);
  });
});
