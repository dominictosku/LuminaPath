import { MediaItem, UserMediaEntry } from '../models/media-item.model';
import { GameStatus } from '../models/library-status.model';

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

  if (estimated <= 0) {
    return statusOf(item) === GameStatus.Completed ? 100 : 0;
  }

  return Math.min(100, Math.round((playedOf(item, mode) / estimated) * 100));
}
