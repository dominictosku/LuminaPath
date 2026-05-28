import { GameStatus, gameStatusLabel } from '../../library/models/library-status.model';
import { releaseDateOfGame } from '../../games/domain/game-library-metrics';
import { startOfDay } from 'src/app/shared/utils/date-helpers';
import { DashboardMediaItem } from '../models/dashboard.model';

/** Calendar date of the media item's release, or `Invalid Date` if unset. */
export function releaseDateOf(item: DashboardMediaItem): Date {
  return releaseDateOfGame(item);
}

/** "Today" / "Tomorrow" / "N days" relative to today. Empty if release date is invalid. */
export function daysUntil(item: DashboardMediaItem): string {
  const date = releaseDateOf(item);
  if (Number.isNaN(date.getTime())) {
    return '';
  }
  const today = startOfDay(new Date());
  const days = Math.ceil((date.getTime() - today.getTime()) / 86400000);
  if (days <= 0) return 'Today';
  if (days === 1) return 'Tomorrow';
  return `${days} days`;
}

/** Localised "Mon D, YYYY" or "No release date". */
export function releaseLabel(item: DashboardMediaItem): string {
  const date = releaseDateOf(item);
  if (Number.isNaN(date.getTime())) {
    return 'No release date';
  }
  return new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric', year: 'numeric' }).format(date);
}

export function statusLabel(item: DashboardMediaItem): string {
  return gameStatusLabel(item.status, 'Not started');
}

export function playedLabel(item: DashboardMediaItem): string {
  return `${Math.round(item.playedHours)}h logged`;
}

export function remainingLabel(item: DashboardMediaItem): string {
  return `${Math.round(item.remainingHours)}h left`;
}

/** 0-100 percent. Falls back to 100 for completed items with no estimate, 0 otherwise. */
export function progressOf(item: DashboardMediaItem): number {
  const estimated = item.estimatedHours;
  if (estimated <= 0) {
    return item.status === GameStatus.Completed ? 100 : 0;
  }
  return Math.min(100, Math.round((item.playedHours / estimated) * 100));
}
