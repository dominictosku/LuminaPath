import { buildPageFilter, LibraryFilterState, releaseDateRange } from './library-filter.helpers';

function makeState(overrides: Partial<LibraryFilterState> = {}): LibraryFilterState {
  return {
    searchTerm: '',
    ownershipFilter: 'all',
    statusFilter: 'all',
    platformFilter: 'all',
    releaseDateFilter: 'all',
    releaseDateFrom: '',
    releaseDateTo: '',
    sortMode: 'title',
    smartFilter: 'none',
    ...overrides,
  };
}

const NOW = new Date(2026, 4, 22); // 22 May 2026 local

describe('releaseDateRange', () => {
  it('"all" returns nulls (no date constraint)', () => {
    expect(releaseDateRange(makeState({ releaseDateFilter: 'all' }), NOW)).toEqual({ from: null, to: null });
  });

  it('"released" caps the `to` at tomorrow (exclusive upper bound on today)', () => {
    const range = releaseDateRange(makeState({ releaseDateFilter: 'released' }), NOW);
    expect(range.from).toBeNull();
    expect(range.to).toBe('2026-05-23');
  });

  it('"upcoming" anchors `from` at today (inclusive of today)', () => {
    const range = releaseDateRange(makeState({ releaseDateFilter: 'upcoming' }), NOW);
    expect(range.from).toBe('2026-05-22');
    expect(range.to).toBeNull();
  });

  it('"this-year" spans Jan 1 → Jan 1 of next year', () => {
    expect(releaseDateRange(makeState({ releaseDateFilter: 'this-year' }), NOW)).toEqual({
      from: '2026-01-01',
      to: '2027-01-01',
    });
  });

  it('"last-year" spans the previous calendar year', () => {
    expect(releaseDateRange(makeState({ releaseDateFilter: 'last-year' }), NOW)).toEqual({
      from: '2025-01-01',
      to: '2026-01-01',
    });
  });

  it('"custom" passes through `from` and bumps `to` by a day for inclusive end-date semantics', () => {
    const range = releaseDateRange(
      makeState({
        releaseDateFilter: 'custom',
        releaseDateFrom: '2026-03-01',
        releaseDateTo: '2026-03-31',
      }),
      NOW,
    );
    expect(range.from).toBe('2026-03-01');
    expect(range.to).toBe('2026-04-01'); // +1 day from 2026-03-31
  });

  it('"custom" with missing endpoints leaves the unset side null', () => {
    const range = releaseDateRange(
      makeState({ releaseDateFilter: 'custom', releaseDateFrom: '2026-03-01', releaseDateTo: '' }),
      NOW,
    );
    expect(range.from).toBe('2026-03-01');
    expect(range.to).toBeNull();
  });
});

describe('buildPageFilter', () => {
  it('translates the state into a MediaFilter with paging applied', () => {
    const filter = buildPageFilter(makeState(), {
      pageIndex: 2,
      pageSize: 24,
      isGamesMode: true,
    }, NOW);
    expect(filter.Paging.PageIndex).toBe(2);
    expect(filter.Paging.Count).toBe(24);
    expect(filter.SearchString).toBe('');
    expect(filter.From).toBeNull();
    expect(filter.To).toBeNull();
  });

  it('trims the search term', () => {
    const filter = buildPageFilter(makeState({ searchTerm: '  hades  ' }), {
      pageIndex: 1,
      pageSize: 24,
      isGamesMode: true,
    }, NOW);
    expect(filter.SearchString).toBe('hades');
  });

  it('writes Status when in games-mode and a status is selected', () => {
    const filter = buildPageFilter(makeState({ statusFilter: '2' }), {
      pageIndex: 1,
      pageSize: 24,
      isGamesMode: true,
    }, NOW);
    expect(filter.Status).toBe(2);
    expect(filter.MediaStatus).toBeNull();
  });

  it('writes MediaStatus when NOT in games-mode and a status is selected', () => {
    const filter = buildPageFilter(makeState({ statusFilter: '3' }), {
      pageIndex: 1,
      pageSize: 24,
      isGamesMode: false,
    }, NOW);
    expect(filter.MediaStatus).toBe(3);
    expect(filter.Status).toBeNull();
  });

  it('writes Platform only in games-mode', () => {
    const gamesFilter = buildPageFilter(makeState({ platformFilter: '4' }), {
      pageIndex: 1, pageSize: 24, isGamesMode: true,
    }, NOW);
    expect(gamesFilter.Platform).toBe(4);

    const otherFilter = buildPageFilter(makeState({ platformFilter: '4' }), {
      pageIndex: 1, pageSize: 24, isGamesMode: false,
    }, NOW);
    expect(otherFilter.Platform).toBeNull();
  });

  it('threads the release-date range into From/To', () => {
    const filter = buildPageFilter(
      makeState({ releaseDateFilter: 'upcoming' }),
      { pageIndex: 1, pageSize: 24, isGamesMode: true },
      NOW,
    );
    expect(filter.From).toBe('2026-05-22');
    expect(filter.To).toBeNull();
  });
});
