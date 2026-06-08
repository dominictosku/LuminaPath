import { addDays, startOfDay, toISODate } from 'src/app/shared/utils/date-helpers';

export const WEEK_START_HOUR = 0;
export const WEEK_END_HOUR = 24;
export const SLOT_MINUTES = 30;
export const HOUR_HEIGHT = 72;

export type WeekScheduleDay = {
  key: string;
  date: Date;
  label: string;
  shortLabel: string;
  dayNumber: number;
  isToday: boolean;
};

export type WeekScheduleBlock = {
  id: string;
  kind: 'quest' | 'session';
  title: string;
  subtitle: string;
  startAt: string;
  endAt: string;
  color: string;
  recurrence?: 'none' | 'daily' | 'weekly' | 'monthly';
  completed: boolean;
  lane: number;
  laneCount: number;
};

export function startOfWeek(date: Date): Date {
  const start = startOfDay(date);
  start.setDate(start.getDate() - ((start.getDay() + 6) % 7));
  return start;
}

export function shiftWeek(anchor: Date, direction: -1 | 1): Date {
  return addDays(startOfWeek(anchor), direction * 7);
}

export function buildWeekDays(anchor: Date, now: Date = new Date()): WeekScheduleDay[] {
  const todayKey = dateKey(now);
  const weekStart = startOfWeek(anchor);

  return Array.from({ length: 7 }, (_, index) => {
    const date = addDays(weekStart, index);
    const key = dateKey(date);
    return {
      key,
      date,
      label: new Intl.DateTimeFormat('en', { weekday: 'short' }).format(date),
      shortLabel: new Intl.DateTimeFormat('en', { weekday: 'narrow' }).format(date),
      dayNumber: date.getDate(),
      isToday: key === todayKey,
    };
  });
}

export function weekTitle(anchor: Date): string {
  const start = startOfWeek(anchor);
  const end = addDays(start, 6);
  const sameMonth = start.getMonth() === end.getMonth();
  const sameYear = start.getFullYear() === end.getFullYear();
  const monthDayFormat = new Intl.DateTimeFormat('en', {
    month: 'short',
    day: 'numeric',
  });
  const fullFormat = new Intl.DateTimeFormat('en', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });

  if (sameMonth && sameYear) {
    return `${monthDayFormat.format(start)} - ${end.getDate()}, ${end.getFullYear()}`;
  }

  if (sameYear) {
    return `${monthDayFormat.format(start)} - ${monthDayFormat.format(end)}, ${end.getFullYear()}`;
  }

  return `${fullFormat.format(start)} - ${fullFormat.format(end)}`;
}

export function dateKey(value: Date): string {
  return toISODate(value);
}

export function dateAtMinutes(day: Date, minutes: number): Date {
  const date = startOfDay(day);
  date.setMinutes(minutes);
  return date;
}

export function minutesSinceDayStart(value: Date): number {
  return value.getHours() * 60 + value.getMinutes();
}

export function timeLabelFromMinutes(minutes: number): string {
  const hours = Math.floor(minutes / 60);
  const remaining = minutes % 60;
  return `${String(hours).padStart(2, '0')}:${String(remaining).padStart(2, '0')}`;
}

export function formatTimeRange(startAt: string, endAt: string): string {
  const start = new Date(startAt);
  const end = new Date(endAt);
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) {
    return '';
  }
  return `${formatTime(start)} - ${formatTime(end)}`;
}

export function eventTop(startAt: string): number {
  const start = new Date(startAt);
  return (minutesSinceDayStart(start) / 60) * HOUR_HEIGHT;
}

export function eventHeight(startAt: string, endAt: string): number {
  const start = new Date(startAt);
  const end = new Date(endAt);
  const minutes = Math.max(SLOT_MINUTES, (end.getTime() - start.getTime()) / 60_000);
  return (minutes / 60) * HOUR_HEIGHT;
}

export function layoutOverlappingBlocks<T extends WeekScheduleBlock>(blocks: T[]): T[] {
  const sorted = [...blocks].sort((a, b) =>
    new Date(a.startAt).getTime() - new Date(b.startAt).getTime()
      || new Date(a.endAt).getTime() - new Date(b.endAt).getTime()
      || a.title.localeCompare(b.title),
  );
  const laneEnds: number[] = [];

  for (const block of sorted) {
    const start = new Date(block.startAt).getTime();
    const end = new Date(block.endAt).getTime();
    const lane = laneEnds.findIndex((laneEnd) => laneEnd <= start);
    block.lane = lane === -1 ? laneEnds.length : lane;
    laneEnds[block.lane] = end;
  }

  for (const block of sorted) {
    const start = new Date(block.startAt).getTime();
    const end = new Date(block.endAt).getTime();
    block.laneCount = Math.max(
      1,
      ...sorted
        .filter((candidate) => {
          const candidateStart = new Date(candidate.startAt).getTime();
          const candidateEnd = new Date(candidate.endAt).getTime();
          return candidateStart < end && candidateEnd > start;
        })
        .map((candidate) => candidate.lane + 1),
    );
  }

  return sorted;
}

function formatTime(date: Date): string {
  return new Intl.DateTimeFormat('en', {
    hour: '2-digit',
    minute: '2-digit',
  }).format(date);
}
