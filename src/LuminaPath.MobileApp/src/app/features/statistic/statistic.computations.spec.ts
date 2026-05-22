import {
  computeBreakdown,
  computeDonut,
  computeFlags,
  computeGenres,
  computeHealth,
  computeMetrics,
  computeRatingBuckets,
  computeStatusSlices,
  computeTimeBars,
  computeTrend,
  formatHours,
  statusClass,
} from './statistic.computations';
import {
  BacklogItem,
  DONUT_CIRCUMFERENCE,
  GenreSlice,
  KIND_COLORS,
  KIND_ICONS,
  KindBreakdown,
  StatisticKind,
  TREND_MONTHS,
} from './models/statistic.model';

/** Minimal factory so tests stay focused on the field(s) under test. */
function makeItem(overrides: Partial<BacklogItem> = {}): BacklogItem {
  return {
    id: overrides.id ?? 1,
    kind: overrides.kind ?? 'Games',
    name: overrides.name ?? 'Item',
    status: overrides.status ?? 1,
    statusLabel: overrides.statusLabel ?? 'Planned',
    estimatedHours: overrides.estimatedHours ?? 0,
    consumedHours: overrides.consumedHours ?? 0,
    remainingHours: overrides.remainingHours ?? 0,
    completed: overrides.completed ?? false,
    active: overrides.active ?? false,
    dropped: overrides.dropped ?? false,
    rating: overrides.rating ?? null,
    genre: overrides.genre ?? '',
    startDate: overrides.startDate ?? null,
    endDate: overrides.endDate ?? null,
  };
}

function makeBreakdown(overrides: Partial<KindBreakdown> = {}): KindBreakdown {
  const kind: StatisticKind = overrides.kind ?? 'Games';
  return {
    kind,
    owned: overrides.owned ?? 0,
    active: overrides.active ?? 0,
    completed: overrides.completed ?? 0,
    remainingHours: overrides.remainingHours ?? 0,
    consumedHours: overrides.consumedHours ?? 0,
    completionRate: overrides.completionRate ?? 0,
    color: overrides.color ?? KIND_COLORS[kind],
    icon: overrides.icon ?? KIND_ICONS[kind],
  };
}

describe('statistic.computations', () => {
  describe('formatHours', () => {
    it('rounds and appends h', () => {
      expect(formatHours(0)).toBe('0h');
      expect(formatHours(12.4)).toBe('12h');
      expect(formatHours(12.6)).toBe('13h');
    });
  });

  describe('statusClass', () => {
    it('lowercases and hyphenates spaces', () => {
      expect(statusClass('On hold')).toBe('on-hold');
      expect(statusClass('Story Complete')).toBe('story-complete');
      expect(statusClass('Active')).toBe('active');
    });
  });

  describe('computeHealth', () => {
    it('returns 100/good on an empty board', () => {
      const result = computeHealth([], [], [], [], 0, 0);
      expect(result.score).toBe(100);
      expect(result.tone).toBe('good');
    });

    it('penalises a crowded active list (capped at 28)', () => {
      const activeFive = Array.from({ length: 5 }, (_, i) =>
        makeItem({ id: i, active: true }),
      );
      // active.length - 3 = 2, × 7 = 14 penalty → 86
      const small = computeHealth([], activeFive, [], [], 0, 0);
      expect(small.score).toBe(86);

      const activeTwenty = Array.from({ length: 20 }, (_, i) =>
        makeItem({ id: i, active: true }),
      );
      // (20-3)*7 = 119, capped at 28 → 72
      const big = computeHealth([], activeTwenty, [], [], 0, 0);
      expect(big.score).toBe(72);
    });

    it('penalises remaining hours above 120 (capped at 30)', () => {
      // (200-120)/10 = 8 penalty → 92
      const eight = computeHealth([], [], [], [], 200, 100);
      expect(eight.score).toBe(92);

      // (1000-120)/10 = 88, capped at 30 → 70
      const capped = computeHealth([], [], [], [], 1000, 100);
      expect(capped.score).toBe(70);
    });

    it('penalises low completion when owned >= 5', () => {
      const owned = Array.from({ length: 5 }, (_, i) => makeItem({ id: i }));
      const low = computeHealth(owned, [], [], [], 0, 20);
      expect(low.score).toBe(82); // -18

      // Same owned, 30+ completion → no penalty
      const ok = computeHealth(owned, [], [], [], 0, 30);
      expect(ok.score).toBe(100);
    });

    it('penalises empty quick-wins / empty active when backlog exists', () => {
      const backlog = [makeItem({ id: 1 })];
      // no quick wins (-8), no active (-8) → 84
      const result = computeHealth([], [], backlog, [], 0, 100);
      expect(result.score).toBe(84);
    });

    it('clamps to [0, 100]', () => {
      // Pile on every penalty
      const owned = Array.from({ length: 10 }, (_, i) => makeItem({ id: i }));
      const active = Array.from({ length: 20 }, (_, i) =>
        makeItem({ id: 100 + i, active: true }),
      );
      const result = computeHealth(owned, active, [makeItem()], [], 5000, 10);
      expect(result.score).toBeGreaterThanOrEqual(0);
      expect(result.score).toBeLessThanOrEqual(100);
    });

    it('maps score bands to tones', () => {
      expect(computeHealth([], [], [], [], 0, 0).tone).toBe('good'); // 100
      // build a 60 score
      const owned = Array.from({ length: 5 }, (_, i) => makeItem({ id: i }));
      const sixty = computeHealth(owned, [], [], [], 320, 10);
      // -18 (low completion) - 20 (hours capped piece) = 62
      expect(sixty.tone).toBe('steady');

      // build a sub-50 score
      const active = Array.from({ length: 12 }, (_, i) =>
        makeItem({ id: 200 + i, active: true }),
      );
      const risky = computeHealth(owned, active, [makeItem()], [], 1000, 5);
      expect(risky.tone).toBe('risk');
    });
  });

  describe('computeBreakdown', () => {
    it('emits one row per kind in fixed order', () => {
      const rows = computeBreakdown([]);
      expect(rows.map((r) => r.kind)).toEqual(['Games', 'Anime', 'Movies', 'Series']);
      for (const row of rows) {
        expect(row.owned).toBe(0);
        expect(row.completionRate).toBe(0);
        expect(row.color).toBe(KIND_COLORS[row.kind]);
        expect(row.icon).toBe(KIND_ICONS[row.kind]);
      }
    });

    it('aggregates owned, active, completed, hours, and rounds completionRate', () => {
      const items: BacklogItem[] = [
        makeItem({ id: 1, kind: 'Games', active: true, consumedHours: 4, remainingHours: 6 }),
        makeItem({ id: 2, kind: 'Games', completed: true, consumedHours: 10 }),
        makeItem({ id: 3, kind: 'Games', dropped: true, consumedHours: 2, remainingHours: 8 }),
        makeItem({ id: 4, kind: 'Anime', completed: true }),
      ];
      const rows = computeBreakdown(items);
      const games = rows.find((r) => r.kind === 'Games')!;
      expect(games.owned).toBe(3);
      expect(games.active).toBe(1);
      expect(games.completed).toBe(1);
      // remainingHours skips completed AND dropped: only the active row contributes
      expect(games.remainingHours).toBe(6);
      expect(games.consumedHours).toBe(16);
      expect(games.completionRate).toBe(33); // 1/3 = 33.33 → 33

      const anime = rows.find((r) => r.kind === 'Anime')!;
      expect(anime.owned).toBe(1);
      expect(anime.completionRate).toBe(100);
    });
  });

  describe('computeDonut', () => {
    it('returns empty result when total is zero', () => {
      const result = computeDonut([makeBreakdown({ owned: 3 })], 0);
      expect(result.segments).toEqual([]);
      expect(result.total).toBe(0);
    });

    it('skips kinds with zero owned and advances offset by previous arc length', () => {
      const breakdown: KindBreakdown[] = [
        makeBreakdown({ kind: 'Games', owned: 6 }),
        makeBreakdown({ kind: 'Anime', owned: 0 }),
        makeBreakdown({ kind: 'Movies', owned: 4 }),
      ];
      const { segments, total } = computeDonut(breakdown, 10);
      expect(total).toBe(10);
      expect(segments.length).toBe(2);
      expect(segments[0].kind).toBe('Games');
      expect(segments[1].kind).toBe('Movies');

      expect(segments[0].percent).toBe(60);
      expect(segments[1].percent).toBe(40);

      expect(segments[0].length).toBeCloseTo(DONUT_CIRCUMFERENCE * 0.6, 5);
      expect(segments[0].gap).toBeCloseTo(DONUT_CIRCUMFERENCE * 0.4, 5);
      expect(segments[0].offset).toBe(0);
      expect(segments[1].offset).toBeCloseTo(-DONUT_CIRCUMFERENCE * 0.6, 5);
    });
  });

  describe('computeRatingBuckets', () => {
    it('produces 10 buckets with x/y geometry even when nothing is rated', () => {
      const { buckets, count, average } = computeRatingBuckets([
        makeItem({ rating: null }),
        makeItem({ rating: 0 }),
      ]);
      expect(buckets.length).toBe(10);
      expect(count).toBe(0);
      expect(average).toBe(0);
      // All bars zero-height, all sitting on the chart baseline (chartHeight + 4 - 0 = 74)
      for (const bucket of buckets) {
        expect(bucket.count).toBe(0);
        expect(bucket.height).toBe(0);
        expect(bucket.y).toBe(74);
      }
      expect(buckets[0].x).toBe(4);
      expect(buckets[1].x).toBe(4 + (18 + 6));
    });

    it('rounds ratings into the correct bucket and clamps to 1–10', () => {
      const { buckets, count, average } = computeRatingBuckets([
        makeItem({ rating: 7.4 }),
        makeItem({ rating: 7.6 }),
        makeItem({ rating: 10 }),
        makeItem({ rating: 0.4 }), // clamps to 1
      ]);
      expect(count).toBe(4);
      expect(buckets[6].count).toBe(1); // 7 → index 6
      expect(buckets[7].count).toBe(1); // 8 → index 7
      expect(buckets[9].count).toBe(1); // 10 → index 9
      expect(buckets[0].count).toBe(1); // clamped to 1 → index 0
      expect(average).toBeCloseTo((7.4 + 7.6 + 10 + 0.4) / 4, 5);
    });

    it('scales bar height against the tallest bucket', () => {
      const items = [
        makeItem({ id: 1, rating: 5 }),
        makeItem({ id: 2, rating: 5 }),
        makeItem({ id: 3, rating: 7 }),
      ];
      const { buckets } = computeRatingBuckets(items);
      const five = buckets[4];
      const seven = buckets[6];
      // 5★ has 2 hits, 7★ has 1 → ratio 1:2
      expect(five.height).toBeCloseTo(70, 5);
      expect(seven.height).toBeCloseTo(35, 5);
    });
  });

  describe('computeTrend', () => {
    beforeEach(() => {
      jasmine.clock().install();
      jasmine.clock().mockDate(new Date(2026, 4, 15)); // 15 May 2026
    });

    afterEach(() => {
      jasmine.clock().uninstall();
    });

    it('emits 12 month points with this-month at the tail', () => {
      const trend = computeTrend([]);
      expect(trend.points.length).toBe(TREND_MONTHS);
      expect(trend.total).toBe(0);
      expect(trend.thisMonth).toBe(0);
      // Last label is whatever the host locale renders for May 2026 — don't
      // hard-code the English name. Compare against the same call.
      const expected = new Date(2026, 4, 1).toLocaleString(undefined, { month: 'short' });
      expect(trend.points[trend.points.length - 1].shortLabel).toBe(expected);
    });

    it('counts items by completion month and emits a polyline + area path', () => {
      const items: BacklogItem[] = [
        // Two items this month
        makeItem({ id: 1, completed: true, endDate: new Date(2026, 4, 1) }),
        makeItem({ id: 2, completed: true, endDate: new Date(2026, 4, 28) }),
        // One last month
        makeItem({ id: 3, completed: true, endDate: new Date(2026, 3, 10) }),
        // Falls back to startDate when no endDate
        makeItem({ id: 4, completed: true, startDate: new Date(2026, 2, 20) }),
        // Outside the 12-month window — should be ignored in totals
        makeItem({ id: 5, completed: true, endDate: new Date(2024, 0, 1) }),
      ];
      const trend = computeTrend(items);

      expect(trend.thisMonth).toBe(2);
      expect(trend.total).toBe(4); // 2 + 1 + 1 (the 2024 one is outside the window)
      expect(trend.max).toBe(2);

      // 12 coordinate pairs separated by spaces
      expect(trend.polyline.split(' ').length).toBe(TREND_MONTHS);
      // Area path is a closed shape
      expect(trend.area.startsWith('M ')).toBe(true);
      expect(trend.area.endsWith(' Z')).toBe(true);
    });
  });

  describe('computeGenres', () => {
    it('returns nothing when no genres are tagged', () => {
      expect(computeGenres([makeItem({ genre: '' })])).toEqual([]);
    });

    it('splits on comma/semicolon/pipe/slash and title-cases', () => {
      const items: BacklogItem[] = [
        makeItem({ id: 1, genre: 'rpg, action' }),
        makeItem({ id: 2, genre: 'RPG; horror' }),
        makeItem({ id: 3, genre: 'horror | action' }),
        makeItem({ id: 4, genre: 'action / strategy' }),
      ];
      const genres = computeGenres(items);
      const byName = new Map(genres.map((g) => [g.name, g]));
      expect(byName.get('Action')?.count).toBe(3);
      expect(byName.get('Rpg')?.count).toBe(2);
      expect(byName.get('Horror')?.count).toBe(2);
      expect(byName.get('Strategy')?.count).toBe(1);
    });

    it('sorts by count descending, caps at 5, and percent is relative to top', () => {
      const items: BacklogItem[] = [];
      const seed: Array<[string, number]> = [
        ['a', 5], ['b', 4], ['c', 3], ['d', 2], ['e', 1], ['f', 1],
      ];
      let id = 1;
      for (const [name, n] of seed) {
        for (let i = 0; i < n; i++) {
          items.push(makeItem({ id: id++, genre: name }));
        }
      }
      const genres = computeGenres(items);
      expect(genres.length).toBe(5);
      expect(genres.map((g) => g.name)).toEqual(['A', 'B', 'C', 'D', 'E']);
      expect(genres[0].percent).toBe(100);
      expect(genres[1].percent).toBe(80); // 4/5
    });
  });

  describe('computeTimeBars', () => {
    it('drops kinds with zero owned', () => {
      const bars = computeTimeBars([
        makeBreakdown({ kind: 'Games', owned: 0, consumedHours: 100 }),
      ]);
      expect(bars).toEqual([]);
    });

    it('scales consumed/remaining percent against the largest total across bars', () => {
      const bars = computeTimeBars([
        makeBreakdown({ kind: 'Games', owned: 3, consumedHours: 50, remainingHours: 50 }),
        makeBreakdown({ kind: 'Anime', owned: 2, consumedHours: 25, remainingHours: 25 }),
      ]);
      const games = bars.find((b) => b.kind === 'Games')!;
      const anime = bars.find((b) => b.kind === 'Anime')!;

      expect(games.total).toBe(100);
      expect(anime.total).toBe(50);
      expect(games.consumedPercent).toBe(50);
      expect(games.remainingPercent).toBe(50);
      expect(anime.consumedPercent).toBe(25);
      expect(anime.remainingPercent).toBe(25);
    });
  });

  describe('computeStatusSlices', () => {
    it('normalises raw labels and follows STATUS_ORDER', () => {
      const items: BacklogItem[] = [
        makeItem({ id: 1, statusLabel: 'Playing' }),
        makeItem({ id: 2, statusLabel: 'Watching' }),
        makeItem({ id: 3, statusLabel: 'Story complete' }),
        makeItem({ id: 4, statusLabel: 'Main game' }),
        makeItem({ id: 5, statusLabel: 'Completed' }),
        makeItem({ id: 6, statusLabel: 'Planned' }),
        makeItem({ id: 7, statusLabel: 'On hold' }),
        makeItem({ id: 8, statusLabel: 'Dropped' }),
      ];
      const slices = computeStatusSlices(items);
      const order = slices.map((s) => s.label);
      expect(order).toEqual(['Planned', 'Active', 'On hold', 'Completed', 'Dropped']);

      const active = slices.find((s) => s.label === 'Active')!;
      const completed = slices.find((s) => s.label === 'Completed')!;
      expect(active.count).toBe(2);
      expect(completed.count).toBe(3);
      // 2/8 = 25%, 3/8 = 37.5 → rounded 38
      expect(active.percent).toBe(25);
      expect(completed.percent).toBe(38);
    });

    it('omits unseen statuses entirely', () => {
      const slices = computeStatusSlices([makeItem({ statusLabel: 'Planned' })]);
      expect(slices.map((s) => s.label)).toEqual(['Planned']);
      expect(slices[0].percent).toBe(100);
    });
  });

  describe('computeFlags', () => {
    it('returns four flags with thresholded titles/tones', () => {
      const active = Array.from({ length: 5 }, (_, i) =>
        makeItem({ id: i, active: true }),
      );
      const backlog = [makeItem()];
      const quickWins: BacklogItem[] = [];
      const flags = computeFlags(active, backlog, quickWins, 200, 30);
      expect(flags.length).toBe(4);
      expect(flags[0].tone).toBe('amber'); // 5 active > 3
      expect(flags[1].tone).toBe('risk'); // 200 > 180
      expect(flags[2].tone).toBe('steady'); // no quick wins
      expect(flags[3].tone).toBe('steady'); // 30% < 40
    });

    it('flips to good when thresholds are happy', () => {
      const flags = computeFlags(
        [makeItem({ active: true })],
        [],
        [makeItem({ id: 99 })],
        50,
        80,
      );
      expect(flags.every((f) => f.tone === 'good')).toBe(true);
    });

    it('pluralises the active flag detail', () => {
      const one = computeFlags([makeItem({ active: true })], [], [], 0, 100);
      expect(one[0].detail).toBe('1 active item');

      const two = computeFlags(
        [makeItem({ id: 1, active: true }), makeItem({ id: 2, active: true })],
        [],
        [],
        0,
        100,
      );
      expect(two[0].detail).toBe('2 active items');
    });
  });

  describe('computeMetrics', () => {
    const baseInput = {
      healthScore: 80,
      remainingHours: 120,
      consumedHours: 40,
      estimatedHours: 200,
      completionRate: 55,
      ownedCount: 20,
      completedCount: 11,
      activeCount: 3,
      backlogCount: 6,
      topGenre: undefined as GenreSlice | undefined,
      ratedCount: 0,
      averageRating: 0,
      trendThisMonth: 0,
    };

    it('emits the eight expected cards in fixed order', () => {
      const cards = computeMetrics(baseInput);
      expect(cards.map((c) => c.label)).toEqual([
        'Health', 'Remaining', 'Active', 'Complete', 'Logged', 'Avg Rating', 'Top Genre', 'This Month',
      ]);
    });

    it('substitutes em-dash when there is no top genre or no ratings', () => {
      const cards = computeMetrics(baseInput);
      const avg = cards.find((c) => c.label === 'Avg Rating')!;
      const top = cards.find((c) => c.label === 'Top Genre')!;
      expect(avg.value).toBe('—');
      expect(avg.detail).toBe('no ratings yet');
      expect(top.value).toBe('—');
      expect(top.detail).toBe('tag your library');
    });

    it('formats hours, percentages, and rating averages', () => {
      const cards = computeMetrics({
        ...baseInput,
        ratedCount: 4,
        averageRating: 7.25,
        topGenre: { name: 'Action', count: 3, percent: 100 },
        trendThisMonth: 2,
      });
      expect(cards.find((c) => c.label === 'Remaining')!.value).toBe('120h');
      expect(cards.find((c) => c.label === 'Logged')!.value).toBe('40h');
      expect(cards.find((c) => c.label === 'Logged')!.detail).toBe('200h tracked estimate');
      expect(cards.find((c) => c.label === 'Complete')!.value).toBe('55%');
      expect(cards.find((c) => c.label === 'Complete')!.detail).toBe('11/20 finished');
      expect(cards.find((c) => c.label === 'Avg Rating')!.value).toBe('7.3');
      expect(cards.find((c) => c.label === 'Avg Rating')!.detail).toBe('4 rated items');
      expect(cards.find((c) => c.label === 'Top Genre')!.value).toBe('Action');
      expect(cards.find((c) => c.label === 'Top Genre')!.detail).toBe('3 items');
      expect(cards.find((c) => c.label === 'This Month')!.detail).toBe('completed recently');
    });

    it('uses singular forms when counts are 1', () => {
      const cards = computeMetrics({
        ...baseInput,
        backlogCount: 1,
        ratedCount: 1,
        averageRating: 8,
        topGenre: { name: 'Rpg', count: 1, percent: 100 },
      });
      expect(cards.find((c) => c.label === 'Remaining')!.detail).toBe('1 open commitment');
      expect(cards.find((c) => c.label === 'Avg Rating')!.detail).toBe('1 rated item');
      expect(cards.find((c) => c.label === 'Top Genre')!.detail).toBe('1 item');
    });
  });
});
