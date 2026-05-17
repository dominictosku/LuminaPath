import { Injectable } from '@angular/core';

import { MediaModeOption } from 'src/app/shared/services/media-mode.service';
import { MediaItem } from '../models/media-item.model';
import { GameStatus, MediaLibraryViewService } from './media-library-view.service';

export type LibraryIntelligenceSummary = {
  totalGames: number;
  ownedGames: number;
  playingGames: number;
  remainingHours: number;
  shortGameCount: number;
  abandonedGameCount: number;
  nextBestGame: MediaItem | null;
};

@Injectable({ providedIn: 'root' })
export class LibraryIntelligenceService {
  constructor(private readonly mediaView: MediaLibraryViewService) {
  }

  summarize(items: MediaItem[], mode: MediaModeOption): LibraryIntelligenceSummary {
    const ownedItems = items.filter((item) => !!this.libraryEntry(item));
    const candidates = items
      .filter((item) => this.isFinishCandidate(item, mode))
      .sort((a, b) => this.bestFinishScore(a, mode) - this.bestFinishScore(b, mode) || this.compareTitle(a, b));

    return {
      totalGames: items.length,
      ownedGames: ownedItems.length,
      playingGames: items.filter((item) => this.isActive(item, mode)).length,
      remainingHours: Math.round(ownedItems.reduce((sum, item) => sum + this.remainingOf(item, mode), 0)),
      shortGameCount: items.filter((item) => this.isShortBacklog(item, mode)).length,
      abandonedGameCount: items.filter((item) => this.isStartedButAbandoned(item, mode)).length,
      nextBestGame: candidates[0] ?? null,
    };
  }

  private isShortBacklog(item: MediaItem, mode: MediaModeOption): boolean {
    const remaining = this.remainingOf(item, mode);
    return this.isFinishCandidate(item, mode) && remaining > 0 && remaining <= 10;
  }

  private isStartedButAbandoned(item: MediaItem, mode: MediaModeOption): boolean {
    return this.isFinishCandidate(item, mode)
      && this.mediaView.playedOf(item, mode) > 0
      && !this.isActive(item, mode);
  }

  private isFinishCandidate(item: MediaItem, mode: MediaModeOption): boolean {
    return !!this.libraryEntry(item)
      && !this.isCompleted(item, mode)
      && !this.isDropped(item, mode)
      && this.remainingOf(item, mode) > 0;
  }

  private bestFinishScore(item: MediaItem, mode: MediaModeOption): number {
    if (!this.isFinishCandidate(item, mode)) {
      return Number.MAX_SAFE_INTEGER;
    }

    const remaining = this.remainingOf(item, mode);
    const progressBonus = this.mediaView.progressOf(item, mode) / 20;
    const activeBonus = this.isActive(item, mode) ? 5 : 0;
    const startedBonus = this.mediaView.playedOf(item, mode) > 0 ? 3 : 0;
    const ratingBonus = (this.ratingOf(item) ?? 0) / 3;
    return remaining - progressBonus - activeBonus - startedBonus - ratingBonus;
  }

  private isActive(item: MediaItem, mode: MediaModeOption): boolean {
    return this.mediaView.isGamesMode(mode)
      ? this.statusOf(item) === GameStatus.Playing
      : this.statusOf(item) === 2;
  }

  private isCompleted(item: MediaItem, mode: MediaModeOption): boolean {
    return this.mediaView.isGamesMode(mode)
      ? this.statusOf(item) === GameStatus.Completed
      : this.statusOf(item) === 3;
  }

  private isDropped(item: MediaItem, mode: MediaModeOption): boolean {
    return !this.mediaView.isGamesMode(mode) && this.statusOf(item) === 4;
  }

  private remainingOf(item: MediaItem, mode: MediaModeOption): number {
    return this.mediaView.remainingOf(item, mode);
  }

  private ratingOf(item: MediaItem): number {
    return Number(this.libraryEntry(item)?.rating ?? -1);
  }

  private statusOf(item: MediaItem): number {
    return this.mediaView.statusOf(item);
  }

  private libraryEntry(item: MediaItem) {
    return this.mediaView.libraryEntry(item);
  }

  private compareTitle(a: MediaItem, b: MediaItem): number {
    return a.name.localeCompare(b.name);
  }
}
