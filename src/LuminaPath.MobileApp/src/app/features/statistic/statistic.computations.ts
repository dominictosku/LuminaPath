import {
  BacklogItem,
  DONUT_CIRCUMFERENCE,
  DonutSegment,
  GenreSlice,
  HealthFlag,
  HealthTone,
  KIND_COLORS,
  KIND_ICONS,
  KindBreakdown,
  RatingBucket,
  STATUS_ORDER,
  StatMetric,
  StatisticKind,
  StatusSlice,
  TimeBar,
  TREND_HEIGHT,
  TREND_MONTHS,
  TREND_PADDING_BOTTOM,
  TREND_PADDING_TOP,
  TREND_PADDING_X,
  TREND_WIDTH,
  TrendData,
  TrendPoint,
} from './models/statistic.model';

/**
 * Pure derivations over the normalized backlog rows. Each export takes the
 * inputs it needs and returns a fresh data structure — no shared mutable state
 * with the page or with each other. Keeps the page TS short and lets these
 * pieces be unit-tested in isolation.
 */

export function formatHours(value: number): string {
  return `${Math.round(value)}h`;
}

export function statusClass(label: string): string {
  return label.toLowerCase().replace(/\s+/g, '-');
}

/**
 * 0–100 backlog health score plus the matching tone token. Penalises a crowded
 * active list, high remaining hours, low completion rates, and the special
 * case of having a backlog but no quick wins or no active items.
 */
export function computeHealth(
  ownedItems: BacklogItem[],
  activeItems: BacklogItem[],
  backlogItems: BacklogItem[],
  quickWins: BacklogItem[],
  remainingHours: number,
  completionRate: number,
): { score: number; tone: HealthTone } {
  let score = 100;
  score -= Math.min(28, Math.max(0, activeItems.length - 3) * 7);
  score -= Math.min(30, Math.max(0, remainingHours - 120) / 10);
  if (ownedItems.length >= 5 && completionRate < 30) {
    score -= 18;
  }
  if (!quickWins.length && backlogItems.length) {
    score -= 8;
  }
  if (!activeItems.length && backlogItems.length) {
    score -= 8;
  }
  const clamped = Math.max(0, Math.min(100, Math.round(score)));
  const tone: HealthTone = clamped >= 75 ? 'good' : clamped >= 50 ? 'steady' : 'risk';
  return { score: clamped, tone };
}

export function computeBreakdown(ownedItems: BacklogItem[]): KindBreakdown[] {
  const kinds: StatisticKind[] = ['Games', 'Anime', 'Movies', 'Series'];
  return kinds.map((kind) => {
    const owned = ownedItems.filter((item) => item.kind === kind);
    const completed = owned.filter((item) => item.completed);
    const remainingHours = owned
      .filter((item) => !item.completed && !item.dropped)
      .reduce((sum, item) => sum + item.remainingHours, 0);
    const consumedHours = owned.reduce((sum, item) => sum + item.consumedHours, 0);
    return {
      kind,
      owned: owned.length,
      active: owned.filter((item) => item.active).length,
      completed: completed.length,
      remainingHours,
      consumedHours,
      completionRate: owned.length ? Math.round((completed.length / owned.length) * 100) : 0,
      color: KIND_COLORS[kind],
      icon: KIND_ICONS[kind],
    };
  });
}

export function computeDonut(
  breakdown: KindBreakdown[],
  total: number,
): { segments: DonutSegment[]; total: number } {
  if (!total) {
    return { segments: [], total: 0 };
  }
  let cursor = 0;
  const segments = breakdown
    .filter((row) => row.owned > 0)
    .map((row) => {
      const percent = row.owned / total;
      const length = percent * DONUT_CIRCUMFERENCE;
      const segment: DonutSegment = {
        kind: row.kind,
        value: row.owned,
        percent: Math.round(percent * 100),
        length,
        gap: DONUT_CIRCUMFERENCE - length,
        offset: -cursor,
        color: row.color,
      };
      cursor += length;
      return segment;
    });
  return { segments, total };
}

export function computeRatingBuckets(
  ownedItems: BacklogItem[],
): { buckets: RatingBucket[]; count: number; average: number } {
  const rated = ownedItems.filter((item) => item.rating != null && item.rating > 0);
  const counts = new Array(10).fill(0);
  for (const item of rated) {
    const bucket = Math.min(10, Math.max(1, Math.round(item.rating ?? 0)));
    counts[bucket - 1] += 1;
  }
  const max = Math.max(1, ...counts);
  const chartHeight = 70;
  const barWidth = 18;
  const barGap = 6;
  const buckets = counts.map((count, index) => {
    const height = (count / max) * chartHeight;
    return {
      rating: index + 1,
      count,
      height,
      x: index * (barWidth + barGap) + 4,
      y: chartHeight + 4 - height,
    };
  });
  const average = rated.length
    ? rated.reduce((acc, item) => acc + (item.rating ?? 0), 0) / rated.length
    : 0;
  return { buckets, count: rated.length, average };
}

export function computeTrend(completedItems: BacklogItem[]): TrendData {
  const now = new Date();
  const months: TrendPoint[] = [];
  const buckets = new Map<string, number>();
  for (const item of completedItems) {
    const date = item.endDate ?? item.startDate;
    if (!date) continue;
    const key = `${date.getFullYear()}-${date.getMonth()}`;
    buckets.set(key, (buckets.get(key) ?? 0) + 1);
  }
  for (let i = TREND_MONTHS - 1; i >= 0; i--) {
    const date = new Date(now.getFullYear(), now.getMonth() - i, 1);
    const key = `${date.getFullYear()}-${date.getMonth()}`;
    months.push({
      label: date.toLocaleString(undefined, { month: 'short', year: 'numeric' }),
      shortLabel: date.toLocaleString(undefined, { month: 'short' }),
      count: buckets.get(key) ?? 0,
      x: 0,
      y: 0,
    });
  }
  const max = Math.max(1, ...months.map((m) => m.count));
  const usableW = TREND_WIDTH - TREND_PADDING_X * 2;
  const usableH = TREND_HEIGHT - TREND_PADDING_TOP - TREND_PADDING_BOTTOM;
  const step = months.length > 1 ? usableW / (months.length - 1) : 0;
  months.forEach((point, index) => {
    point.x = TREND_PADDING_X + step * index;
    point.y = TREND_PADDING_TOP + usableH - (point.count / max) * usableH;
  });
  const polyline = months.map((p) => `${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ');
  let area = '';
  if (months.length) {
    const baseline = TREND_HEIGHT - TREND_PADDING_BOTTOM;
    const first = months[0];
    const last = months[months.length - 1];
    const parts: string[] = [`M ${first.x.toFixed(1)} ${baseline.toFixed(1)}`];
    months.forEach((p) => parts.push(`L ${p.x.toFixed(1)} ${p.y.toFixed(1)}`));
    parts.push(`L ${last.x.toFixed(1)} ${baseline.toFixed(1)}`);
    parts.push('Z');
    area = parts.join(' ');
  }
  return {
    points: months,
    polyline,
    area,
    max,
    total: months.reduce((sum, p) => sum + p.count, 0),
    thisMonth: months[months.length - 1]?.count ?? 0,
  };
}

export function computeGenres(ownedItems: BacklogItem[]): GenreSlice[] {
  const counts = new Map<string, number>();
  for (const item of ownedItems) {
    const tokens = (item.genre ?? '')
      .split(/[,;|/]/)
      .map((token) => token.trim())
      .filter(Boolean);
    for (const token of tokens) {
      const key = token.toLowerCase();
      counts.set(key, (counts.get(key) ?? 0) + 1);
    }
  }
  const entries = Array.from(counts.entries())
    .map(([key, count]) => ({ name: toTitleCase(key), count }))
    .sort((a, b) => b.count - a.count)
    .slice(0, 5);
  const max = entries[0]?.count ?? 1;
  return entries.map((entry) => ({ ...entry, percent: Math.round((entry.count / max) * 100) }));
}

export function computeTimeBars(breakdown: KindBreakdown[]): TimeBar[] {
  const bars = breakdown
    .filter((row) => row.owned > 0)
    .map((row) => {
      const total = row.consumedHours + row.remainingHours;
      return {
        kind: row.kind,
        color: row.color,
        consumed: row.consumedHours,
        remaining: row.remainingHours,
        total,
        consumedPercent: 0,
        remainingPercent: 0,
      };
    });
  const max = Math.max(1, ...bars.map((bar) => bar.total));
  return bars.map((bar) => ({
    ...bar,
    consumedPercent: (bar.consumed / max) * 100,
    remainingPercent: (bar.remaining / max) * 100,
  }));
}

export function computeStatusSlices(ownedItems: BacklogItem[]): StatusSlice[] {
  const counts = new Map<string, number>();
  for (const item of ownedItems) {
    const key = normalizeStatus(item.statusLabel);
    counts.set(key, (counts.get(key) ?? 0) + 1);
  }
  const total = ownedItems.length;
  return STATUS_ORDER
    .filter((label) => (counts.get(label) ?? 0) > 0)
    .map((label) => ({
      label,
      count: counts.get(label) ?? 0,
      percent: total ? Math.round(((counts.get(label) ?? 0) / total) * 100) : 0,
    }));
}

export function computeFlags(
  activeItems: BacklogItem[],
  backlogItems: BacklogItem[],
  quickWins: BacklogItem[],
  remainingHours: number,
  completionRate: number,
): HealthFlag[] {
  return [
    {
      title: activeItems.length > 3 ? 'Active list is crowded' : 'Active list is focused',
      detail: `${activeItems.length} active item${activeItems.length === 1 ? '' : 's'}`,
      icon: activeItems.length > 3 ? 'alert-circle-outline' : 'checkmark-circle-outline',
      tone: activeItems.length > 3 ? 'amber' : 'good',
    },
    {
      title: remainingHours > 180 ? 'Backlog pressure is high' : 'Backlog pressure is readable',
      detail: `${formatHours(remainingHours)} remaining`,
      icon: remainingHours > 180 ? 'flame-outline' : 'checkmark-circle-outline',
      tone: remainingHours > 180 ? 'risk' : 'good',
    },
    {
      title: quickWins.length ? 'Quick wins available' : 'No short wins found',
      detail: quickWins.length
        ? `${quickWins.length} item${quickWins.length === 1 ? '' : 's'} under 12h`
        : 'Add shorter items or finish a larger one',
      icon: quickWins.length ? 'time-outline' : 'alert-circle-outline',
      tone: quickWins.length ? 'good' : 'steady',
    },
    {
      title: completionRate >= 40 ? 'Completion pace looks healthy' : 'Completion pace is still warming up',
      detail: `${completionRate}% completion rate`,
      icon: completionRate >= 40 ? 'trophy-outline' : 'pulse-outline',
      tone: completionRate >= 40 ? 'good' : 'steady',
    },
  ];
}

export function computeMetrics(input: {
  healthScore: number;
  remainingHours: number;
  consumedHours: number;
  estimatedHours: number;
  completionRate: number;
  ownedCount: number;
  completedCount: number;
  activeCount: number;
  backlogCount: number;
  topGenre: GenreSlice | undefined;
  ratedCount: number;
  averageRating: number;
  trendThisMonth: number;
}): StatMetric[] {
  const topGenreName = input.topGenre?.name ?? '—';
  return [
    {
      label: 'Health',
      value: String(input.healthScore),
      detail: input.healthScore >= 75 ? 'balanced backlog' : input.healthScore >= 50 ? 'needs some pruning' : 'too much pressure',
      icon: 'speedometer-outline',
    },
    {
      label: 'Remaining',
      value: formatHours(input.remainingHours),
      detail: `${input.backlogCount} open commitment${input.backlogCount === 1 ? '' : 's'}`,
      icon: 'hourglass-outline',
    },
    {
      label: 'Active',
      value: String(input.activeCount),
      detail: 'currently in progress',
      icon: 'pulse-outline',
    },
    {
      label: 'Complete',
      value: `${input.completionRate}%`,
      detail: `${input.completedCount}/${input.ownedCount} finished`,
      icon: 'trophy-outline',
    },
    {
      label: 'Logged',
      value: formatHours(input.consumedHours),
      detail: `${formatHours(input.estimatedHours)} tracked estimate`,
      icon: 'time-outline',
    },
    {
      label: 'Avg Rating',
      value: input.ratedCount ? input.averageRating.toFixed(1) : '—',
      detail: input.ratedCount ? `${input.ratedCount} rated item${input.ratedCount === 1 ? '' : 's'}` : 'no ratings yet',
      icon: 'star-outline',
    },
    {
      label: 'Top Genre',
      value: topGenreName,
      detail: input.topGenre ? `${input.topGenre.count} item${input.topGenre.count === 1 ? '' : 's'}` : 'tag your library',
      icon: 'pricetag-outline',
    },
    {
      label: 'This Month',
      value: String(input.trendThisMonth),
      detail: input.trendThisMonth ? 'completed recently' : 'nothing wrapped yet',
      icon: 'trending-up-outline',
    },
  ];
}

function normalizeStatus(raw: string): string {
  if (raw === 'Playing' || raw === 'Watching') return 'Active';
  if (raw === 'Story complete' || raw === 'Main game' || raw === 'Completed') return 'Completed';
  if (raw === 'On hold') return 'On hold';
  if (raw === 'Dropped') return 'Dropped';
  if (raw === 'Planned') return 'Planned';
  return raw;
}

function toTitleCase(value: string): string {
  return value
    .split(' ')
    .map((part) => (part.length ? part[0].toUpperCase() + part.slice(1) : part))
    .join(' ');
}
