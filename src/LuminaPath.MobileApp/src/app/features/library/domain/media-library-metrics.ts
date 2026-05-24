import { MediaItem, UserMediaEntry } from '../models/media-item.model';

export type LibraryModeContext = {
  id: 'games' | 'animes' | 'movies' | 'series';
};

export function isGamesMode(mode: LibraryModeContext): boolean {
  return mode.id === 'games';
}

export function isEpisodeMode(mode: LibraryModeContext): boolean {
  return mode.id === 'animes' || mode.id === 'series';
}

export function libraryEntry(item: MediaItem): UserMediaEntry | null {
  return item.libraryEntry;
}

export function statusOf(item: MediaItem): number {
  return Number(item.libraryEntry?.status ?? -1);
}

export function playedOf(item: MediaItem, mode: LibraryModeContext): number {
  const entry = libraryEntry(item);
  if (!isGamesMode(mode)) {
    return (Number(entry?.currentWatchTimeMinutes) || 0) / 60;
  }

  const manual = Number(entry?.timeSpend) || 0;
  const tracked = Number(entry?.myGameInfo?.trackedHours) || 0;
  return manual + tracked;
}

export function expectedHoursOf(item: MediaItem, mode: LibraryModeContext): number {
  if (isGamesMode(mode)) {
    return Number(item.playtime) || 0;
  }

  if (item.expectedWatchTimeMinutes != null) {
    return Number(item.expectedWatchTimeMinutes) / 60;
  }

  return Number(item.playtime) || 0;
}

export function remainingOf(item: MediaItem, mode: LibraryModeContext): number {
  return Math.max(0, expectedHoursOf(item, mode) - playedOf(item, mode));
}

export function progressOf(item: MediaItem, mode: LibraryModeContext): number {
  const estimated = expectedHoursOf(item, mode);

  // No denominator (0 or null estimated hours) → treat as complete.
  // The honest answer is "indeterminate", but for the library's
  // glanceable progress bar that's noise; the user explicitly wants
  // 100% when there's no length to measure against. Covers brand-new
  // catalog entries with no playtime metadata and side titles like
  // unmeasured tools/utilities.
  if (estimated <= 0) {
    return 100;
  }

  // Overflow case (tracked > estimated, e.g. a 60h playthrough of a
  // game IGDB lists at 40h, or a series re-watch). Clamp to 100% so
  // the bar doesn't blow past its track.
  return Math.min(100, Math.round((playedOf(item, mode) / estimated) * 100));
}
