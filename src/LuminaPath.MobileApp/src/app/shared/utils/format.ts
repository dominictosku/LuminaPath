/** Upper-case the first character, leaving the rest untouched. Empty-safe. */
export function capitalize(value: string): string {
  return `${value[0]?.toUpperCase() ?? ''}${value.slice(1)}`;
}

/**
 * Format a duration in minutes as "Xh Ym", "Xh", or "Ym".
 * Returns the fallback string when the value is null/undefined/zero/negative.
 */
export function formatHoursMinutes(
  value: number | null | undefined,
  fallback = 'No estimate',
): string {
  const minutes = Number(value) || 0;
  if (minutes <= 0) {
    return fallback;
  }

  const hours = Math.floor(minutes / 60);
  const remaining = minutes % 60;
  if (hours > 0 && remaining > 0) {
    return `${hours}h ${remaining}m`;
  }
  return hours > 0 ? `${hours}h` : `${remaining}m`;
}

const SHORT_DATE_FORMATTER = new Intl.DateTimeFormat('en', {
  month: 'short',
  day: 'numeric',
  year: 'numeric',
});

/**
 * Format a date (or ISO string) as e.g. "May 18, 2026".
 * Returns the fallback string for null/invalid input.
 */
export function formatShortDate(
  value: Date | string | null | undefined,
  fallback = 'No release date',
): string {
  if (!value) {
    return fallback;
  }
  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) {
    return fallback;
  }
  return SHORT_DATE_FORMATTER.format(date);
}
