import { MediaItem } from '../models/media-item.model';
import { GameStatus, WatchStatus } from '../models/library-status.model';
import {
  LibraryModeContext,
  isGamesMode,
  libraryEntry,
  playedOf,
  progressOf,
  remainingOf,
  statusOf,
} from './media-library-metrics';

export type LibraryIntelligenceSummary = {
  totalGames: number;
  ownedGames: number;
  playingGames: number;
  remainingHours: number;
  shortGameCount: number;
  abandonedGameCount: number;
  nextBestGame: MediaItem | null;
};

export function summarizeLibraryIntelligence(
  items: MediaItem[],
  mode: LibraryModeContext,
): LibraryIntelligenceSummary {
  const ownedItems = items.filter((item) => !!libraryEntry(item));
  const candidates = items
    .filter((item) => isFinishCandidate(item, mode))
    .sort((a, b) => bestFinishScore(a, mode) - bestFinishScore(b, mode) || compareTitle(a, b));

  return {
    totalGames: items.length,
    ownedGames: ownedItems.length,
    playingGames: items.filter((item) => isActive(item, mode)).length,
    remainingHours: Math.round(ownedItems.reduce((sum, item) => sum + remainingOf(item, mode), 0)),
    shortGameCount: items.filter((item) => isShortBacklog(item, mode)).length,
    abandonedGameCount: items.filter((item) => isStartedButAbandoned(item, mode)).length,
    nextBestGame: candidates[0] ?? null,
  };
}

function isShortBacklog(item: MediaItem, mode: LibraryModeContext): boolean {
  const remaining = remainingOf(item, mode);
  return isFinishCandidate(item, mode) && remaining > 0 && remaining <= 10;
}

function isStartedButAbandoned(item: MediaItem, mode: LibraryModeContext): boolean {
  return isFinishCandidate(item, mode)
    && playedOf(item, mode) > 0
    && !isActive(item, mode);
}

function isFinishCandidate(item: MediaItem, mode: LibraryModeContext): boolean {
  return !!libraryEntry(item)
    && !isCompleted(item, mode)
    && !isDropped(item, mode)
    && remainingOf(item, mode) > 0;
}

function bestFinishScore(item: MediaItem, mode: LibraryModeContext): number {
  if (!isFinishCandidate(item, mode)) {
    return Number.MAX_SAFE_INTEGER;
  }

  const remaining = remainingOf(item, mode);
  const progressBonus = progressOf(item, mode) / 20;
  const activeBonus = isActive(item, mode) ? 5 : 0;
  const startedBonus = playedOf(item, mode) > 0 ? 3 : 0;
  const ratingBonus = (ratingOf(item) ?? 0) / 3;
  return remaining - progressBonus - activeBonus - startedBonus - ratingBonus;
}

function isActive(item: MediaItem, mode: LibraryModeContext): boolean {
  return isGamesMode(mode)
    ? statusOf(item) === GameStatus.Playing
    : statusOf(item) === WatchStatus.Watching;
}

function isCompleted(item: MediaItem, mode: LibraryModeContext): boolean {
  return isGamesMode(mode)
    ? statusOf(item) === GameStatus.Completed
    : statusOf(item) === WatchStatus.Completed;
}

function isDropped(item: MediaItem, mode: LibraryModeContext): boolean {
  return !isGamesMode(mode) && statusOf(item) === WatchStatus.Dropped;
}

function ratingOf(item: MediaItem): number {
  return Number(libraryEntry(item)?.rating ?? -1);
}

function compareTitle(a: MediaItem, b: MediaItem): number {
  return a.name.localeCompare(b.name);
}
