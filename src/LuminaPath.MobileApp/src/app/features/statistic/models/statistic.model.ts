/**
 * Discriminator for normalized statistic items so we can group / colour-code
 * across all four media services.
 */
export type StatisticKind = 'Games' | 'Anime' | 'Movies' | 'Series';

/** Tone tokens for the health orbit and flag cards. */
export type HealthTone = 'good' | 'steady' | 'amber' | 'risk';

/**
 * Normalized view-model row produced from any of the four media types.
 * Every chart / list on the statistic page derives from a list of these.
 */
export type BacklogItem = {
  id: number;
  kind: StatisticKind;
  name: string;
  status: number;
  statusLabel: string;
  estimatedHours: number;
  consumedHours: number;
  remainingHours: number;
  completed: boolean;
  active: boolean;
  dropped: boolean;
  rating: number | null;
  genre: string;
  startDate: Date | null;
  endDate: Date | null;
};

export type StatMetric = {
  label: string;
  value: string;
  detail: string;
  icon: string;
};

export type KindBreakdown = {
  kind: StatisticKind;
  owned: number;
  active: number;
  completed: number;
  remainingHours: number;
  consumedHours: number;
  completionRate: number;
  color: string;
  icon: string;
};

export type HealthFlag = {
  title: string;
  detail: string;
  icon: string;
  tone: HealthTone;
};

export type DonutSegment = {
  kind: StatisticKind;
  value: number;
  percent: number;
  length: number;
  gap: number;
  offset: number;
  color: string;
};

export type RatingBucket = {
  rating: number;
  count: number;
  height: number;
  x: number;
  y: number;
};

export type TrendPoint = {
  label: string;
  shortLabel: string;
  count: number;
  x: number;
  y: number;
};

export type TrendData = {
  points: TrendPoint[];
  polyline: string;
  area: string;
  max: number;
  total: number;
  thisMonth: number;
};

export type GenreSlice = {
  name: string;
  count: number;
  percent: number;
};

export type TimeBar = {
  kind: StatisticKind;
  color: string;
  consumed: number;
  remaining: number;
  total: number;
  consumedPercent: number;
  remainingPercent: number;
};

export type StatusSlice = {
  label: string;
  count: number;
  percent: number;
};

export const KIND_COLORS: Record<StatisticKind, string> = {
  Games: '#60a5fa',
  Anime: '#c084fc',
  Movies: '#fbbf24',
  Series: '#22d3ee',
};

export const KIND_ICONS: Record<StatisticKind, string> = {
  Games: 'game-controller-outline',
  Anime: 'tv-outline',
  Movies: 'film-outline',
  Series: 'layers-outline',
};

export const STATUS_ORDER = ['Planned', 'Active', 'On hold', 'Completed', 'Dropped'];

export const DONUT_RADIUS = 46;
export const DONUT_CIRCUMFERENCE = 2 * Math.PI * DONUT_RADIUS;
export const TREND_MONTHS = 12;
export const TREND_WIDTH = 320;
export const TREND_HEIGHT = 110;
export const TREND_PADDING_X = 14;
export const TREND_PADDING_TOP = 14;
export const TREND_PADDING_BOTTOM = 22;
