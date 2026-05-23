import { GameForecast } from 'src/app/features/planning/services/gaming-session.service';
import { formatShortDate } from 'src/app/shared/utils/format';

export type ForecastBarParts = {
  played: number;
  scheduled: number;
  remaining: number;
  total: number;
};

export type ForecastSegment = 'played' | 'scheduled' | 'remaining';

/**
 * Pure helpers behind the my-game-details forecast panel. The page reads
 * `GameForecast` from the planning service and feeds it through these to
 * derive the three-segment bar, the segment widths in percent, and the
 * summary line.
 */

export function forecastHours(value: number): string {
  return `${Math.round(value * 10) / 10}h`;
}

/** Three non-negative segments that always sum to the total. Scheduled is
 *  capped so played + scheduled never exceeds the remaining-hours budget. */
export function forecastBarParts(forecast: GameForecast | null): ForecastBarParts {
  if (!forecast) return { played: 0, scheduled: 0, remaining: 0, total: 0 };

  const played = Math.max(0, forecast.playedHours ?? 0);
  const remainingTotal = forecast.remainingHours != null
    ? Math.max(0, forecast.remainingHours)
    : Math.max(0, (forecast.playtimeEstimateHours ?? 0) - played);
  const scheduled = Math.min(Math.max(0, forecast.scheduledHours ?? 0), remainingTotal);
  const remaining = Math.max(0, remainingTotal - scheduled);

  return { played, scheduled, remaining, total: played + scheduled + remaining };
}

/** Percent width of one segment within the bar (one decimal place). */
export function forecastWidth(forecast: GameForecast | null, segment: ForecastSegment): number {
  const parts = forecastBarParts(forecast);
  if (parts.total <= 0) return 0;
  const value = segment === 'played' ? parts.played
    : segment === 'scheduled' ? parts.scheduled
    : parts.remaining;
  return Math.round((value / parts.total) * 1000) / 10;
}

/** 0–1 progress fraction used by the fallback ion-progress-bar when the
 *  three-segment view can't be drawn (no playtime estimate yet). */
export function forecastProgress(forecast: GameForecast | null): number {
  if (!forecast?.playtimeEstimateHours || forecast.playtimeEstimateHours <= 0) return 0;
  return Math.min(1, forecast.playedHours / forecast.playtimeEstimateHours);
}

/** Human summary line under the bar — phrased differently depending on
 *  whether the user is past the estimate, has an ETA, or is still being
 *  asked to schedule sessions. */
export function forecastSummary(forecast: GameForecast | null): string {
  if (!forecast) return '';
  if (forecast.remainingHours == null) {
    return 'Add a playtime estimate to see a forecast.';
  }
  if (forecast.remainingHours <= 0) {
    return 'You are already past the estimated playtime.';
  }
  if (forecast.projectedCompletionDate) {
    const sessions = forecast.sessionsToCompletion ?? 0;
    const date = formatShortDate(forecast.projectedCompletionDate);
    return `${sessions} session${sessions === 1 ? '' : 's'} to finish · ETA ${date}`;
  }
  if (forecast.weeksAtCurrentPace != null) {
    return `Need ${forecastHours(forecast.additionalHoursNeeded)} more · ~${forecast.weeksAtCurrentPace} weeks at ${forecastHours(forecast.weeklyHours)}/week`;
  }
  return `Need ${forecastHours(forecast.additionalHoursNeeded)} more — schedule sessions to project an ETA.`;
}
