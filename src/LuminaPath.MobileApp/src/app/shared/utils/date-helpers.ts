/** Midnight of the same day, in the local timezone. */
export function startOfDay(date: Date): Date {
  const copy = new Date(date);
  copy.setHours(0, 0, 0, 0);
  return copy;
}

/** First day of the calendar month, in the local timezone. */
export function startOfMonth(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

/**
 * `date` shifted by `months` calendar months. Negative values go backwards.
 * Always returns the first of the resulting month (mirrors `startOfMonth`
 * semantics; useful for month-anchored navigation).
 */
export function addMonths(date: Date, months: number): Date {
  return new Date(date.getFullYear(), date.getMonth() + months, 1);
}

/** `date` shifted by `days`. Negative values go backwards. Preserves time-of-day. */
export function addDays(date: Date, days: number): Date {
  const copy = new Date(date);
  copy.setDate(copy.getDate() + days);
  return copy;
}

/** Format a date as `YYYY-MM-DD` in the local timezone (for `<input type="date">`). */
export function toISODate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
