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

/**
 * Coerce a Date/ISO-string/nullish into a `YYYY-MM-DD` value for an
 * `<input type="date">`. Returns '' for empty or invalid input.
 */
export function toDateInputValue(value: Date | string | null | undefined): string {
  if (!value) return '';
  const date = value instanceof Date ? value : new Date(value);
  return Number.isNaN(date.getTime()) ? '' : toISODate(date);
}

/**
 * Coerce a Date/ISO-string/nullish into a full ISO string (or null).
 * Strings pass through unchanged; invalid Dates collapse to null.
 */
export function serializeDate(value: Date | string | null | undefined): string | null {
  if (!value) {
    return null;
  }
  if (value instanceof Date) {
    return Number.isNaN(value.getTime()) ? null : value.toISOString();
  }
  return value;
}

/** Parse a string into a Date, returning null for empty or invalid input. */
export function parseDateOrNull(value: string): Date | null {
  if (!value) return null;
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? null : parsed;
}

/**
 * Coerce a Date/ISO-string/nullish into a `YYYY-MM-DD` date string (or null).
 * Strings pass through unchanged; invalid Dates collapse to null.
 */
export function normalizeIsoDate(value: string | Date | null | undefined): string | null {
  if (!value) return null;
  if (value instanceof Date) {
    return Number.isNaN(value.getTime()) ? null : value.toISOString().slice(0, 10);
  }
  return value || null;
}
