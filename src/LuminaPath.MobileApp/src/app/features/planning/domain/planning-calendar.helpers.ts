import { addDays, startOfDay, toISODate } from 'src/app/shared/utils/date-helpers';

export type CalendarRecurrence = 'none' | 'daily' | 'weekly' | 'monthly';

export type ScheduledRecurringItem = {
  recurrence?: CalendarRecurrence | null;
  completed?: boolean | null;
  scheduledStartAt?: string | null;
  scheduledEndAt?: string | null;
};

export function startOfWeek(date: Date): Date {
  const start = startOfDay(date);
  start.setDate(start.getDate() - ((start.getDay() + 6) % 7));
  return start;
}

export function shiftWeek(anchor: Date, direction: -1 | 1): Date {
  return addDays(startOfWeek(anchor), direction * 7);
}

export function startOfCalendarMonthGrid(date: Date): Date {
  const monthStart = new Date(date.getFullYear(), date.getMonth(), 1);
  return startOfWeek(monthStart);
}

export function dateKey(value: Date): string {
  return toISODate(value);
}

export function sameDay(a: Date, b: Date): boolean {
  return a.getFullYear() === b.getFullYear()
    && a.getMonth() === b.getMonth()
    && a.getDate() === b.getDate();
}

export function dateAtMinutes(day: Date, minutes: number): Date {
  const date = startOfDay(day);
  date.setMinutes(minutes);
  return date;
}

export function minutesSinceDayStart(value: Date): number {
  return value.getHours() * 60 + value.getMinutes();
}

export function hasRecurrence(recurrence: CalendarRecurrence | null | undefined): boolean {
  return Boolean(recurrence && recurrence !== 'none');
}

export function shiftForRecurrence(value: Date, recurrence: CalendarRecurrence): Date {
  const next = new Date(value);
  if (recurrence === 'daily') {
    next.setDate(next.getDate() + 1);
  } else if (recurrence === 'weekly') {
    next.setDate(next.getDate() + 7);
  } else if (recurrence === 'monthly') {
    return addMonthsClamped(value, 1);
  }
  return next;
}

export function addMonthsClamped(value: Date, months: number): Date {
  const year = value.getFullYear();
  const month = value.getMonth() + months;
  const day = value.getDate();
  const lastDayOfTargetMonth = new Date(year, month + 1, 0).getDate();
  return new Date(
    year,
    month,
    Math.min(day, lastDayOfTargetMonth),
    value.getHours(),
    value.getMinutes(),
    value.getSeconds(),
    value.getMilliseconds(),
  );
}

export function projectedRecurringOccurrenceForDay(
  item: ScheduledRecurringItem,
  day: Date,
): { start: Date; end: Date } | null {
  if (!hasRecurrence(item.recurrence) || item.completed || !item.scheduledStartAt || !item.scheduledEndAt) {
    return null;
  }

  const sourceStart = new Date(item.scheduledStartAt);
  const sourceEnd = new Date(item.scheduledEndAt);
  if (Number.isNaN(sourceStart.getTime()) || Number.isNaN(sourceEnd.getTime())) {
    return null;
  }

  const targetDay = startOfDay(day);
  if (sameDay(sourceStart, targetDay) || targetDay < startOfDay(sourceStart)) {
    return null;
  }

  const projectedStart = projectedOccurrenceStartForDay(sourceStart, item.recurrence!, targetDay);
  const durationMs = sourceEnd.getTime() - sourceStart.getTime();
  if (!projectedStart || durationMs <= 0) {
    return null;
  }

  return {
    start: projectedStart,
    end: new Date(projectedStart.getTime() + durationMs),
  };
}

export function projectedOccurrenceStartForDay(
  sourceStart: Date,
  recurrence: CalendarRecurrence,
  day: Date,
): Date | null {
  const sourceDay = startOfDay(sourceStart);
  const targetDay = startOfDay(day);
  if (targetDay <= sourceDay) {
    return null;
  }

  if (recurrence === 'daily') {
    return dateAtMinutes(targetDay, minutesSinceDayStart(sourceStart));
  }

  if (recurrence === 'weekly') {
    const dayDifference = Math.round((targetDay.getTime() - sourceDay.getTime()) / 86_400_000);
    return dayDifference % 7 === 0
      ? dateAtMinutes(targetDay, minutesSinceDayStart(sourceStart))
      : null;
  }

  if (recurrence !== 'monthly') {
    return null;
  }

  let candidate = new Date(sourceStart);
  for (let index = 0; index < 240 && startOfDay(candidate) <= targetDay; index += 1) {
    candidate = shiftForRecurrence(candidate, 'monthly');
    if (sameDay(candidate, targetDay)) {
      return candidate;
    }
  }

  return null;
}

export function validDate(value: Date | string | null | undefined): Date | null {
  if (!value) {
    return null;
  }
  const date = value instanceof Date ? value : new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
}
