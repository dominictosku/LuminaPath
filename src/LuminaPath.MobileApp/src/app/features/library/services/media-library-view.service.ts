import { Injectable } from '@angular/core';
import { platformLabelFromValue } from '../../games/models/games.model';
import { LibraryEntryDetails, MediaItem, UserMediaEntry } from '../models/media-item.model';
import { MediaLibraryForm, MediaStatusOption } from '../models/media-library-form.model';
import { MediaModeOption } from 'src/app/shared/services/media-mode.service';
import { GameStatus } from '../models/library-status.model';
import {
  expectedHoursOf,
  isEpisodeMode,
  isGamesMode,
  libraryEntry,
  playedOf,
  progressOf,
  remainingOf,
  statusOf,
} from '../domain/media-library-metrics';

@Injectable({
  providedIn: 'root',
})
export class MediaLibraryViewService {
  readonly gameStatusOptions: MediaStatusOption[] = [
    { label: 'On hold', value: GameStatus.OnHold },
    { label: 'Planned', value: GameStatus.Planned },
    { label: 'Playing', value: GameStatus.Playing },
    { label: 'Story complete', value: GameStatus.StoryComplete },
    { label: 'Completed', value: GameStatus.Completed },
    { label: 'Main game', value: GameStatus.MainGame },
  ];

  readonly watchStatusOptions: MediaStatusOption[] = [
    { label: 'On hold', value: 0 },
    { label: 'Planned', value: 1 },
    { label: 'Watching', value: 2 },
    { label: 'Completed', value: 3 },
    { label: 'Dropped', value: 4 },
  ];

  isGamesMode(mode: MediaModeOption): boolean {
    return isGamesMode(mode);
  }

  isEpisodeMode(mode: MediaModeOption): boolean {
    return isEpisodeMode(mode);
  }

  statusOptions(mode: MediaModeOption): MediaStatusOption[] {
    return this.isGamesMode(mode) ? this.gameStatusOptions : this.watchStatusOptions;
  }

  heroTitle(mode: MediaModeOption): string {
    switch (mode.id) {
      case 'animes':
        return 'Find what to watch next.';
      case 'movies':
        return 'Plan the next movie night.';
      case 'series':
        return 'Keep every episode on track.';
      case 'games':
      default:
        return 'Find what to play next.';
    }
  }

  heroDescription(mode: MediaModeOption): string {
    return `Browse the catalog, track your owned ${mode.label.toLowerCase()}, and keep your backlog readable.`;
  }

  resultTitle(count: number, mode: MediaModeOption): string {
    return `${count} ${count === 1 ? mode.singular : mode.label.toLowerCase()}`;
  }

  emptyTitle(mode: MediaModeOption): string {
    return `No ${mode.label.toLowerCase()} match`;
  }

  platformLabel(value: number | null | undefined): string {
    return platformLabelFromValue(value);
  }

  statusLabel(item: MediaItem, mode: MediaModeOption): string {
    const status = this.statusOptions(mode).find((option) => option.value === this.statusOf(item));
    return status?.label ?? 'Catalog';
  }

  releaseLabel(item: MediaItem): string {
    const date = item.releaseDate ? new Date(item.releaseDate) : null;

    if (!date || Number.isNaN(date.getTime())) {
      return 'No date';
    }

    return new Intl.DateTimeFormat('en', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    }).format(date);
  }

  progressOf(item: MediaItem, mode: MediaModeOption): number {
    return progressOf(item, mode);
  }

  playedLabel(item: MediaItem, mode: MediaModeOption): string {
    return this.isGamesMode(mode)
      ? `${Math.round(this.playedOf(item, mode))}h played`
      : `${Math.round(this.playedOf(item, mode) * 60)}m watched`;
  }

  remainingLabel(item: MediaItem, mode: MediaModeOption): string {
    return this.isGamesMode(mode)
      ? `${Math.round(this.remainingOf(item, mode))}h left`
      : `${Math.round(this.remainingOf(item, mode) * 60)}m left`;
  }

  durationLabel(item: MediaItem, mode: MediaModeOption): string {
    if (this.isGamesMode(mode)) {
      return `${item.playtime || 0}h`;
    }

    const minutes = Number(item.expectedWatchTimeMinutes) || Math.round((Number(item.playtime) || 0) * 60);
    if (minutes <= 0) {
      return 'No estimate';
    }

    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;
    return hours > 0 && remainingMinutes > 0
      ? `${hours}h ${remainingMinutes}m`
      : hours > 0
        ? `${hours}h`
        : `${remainingMinutes}m`;
  }

  episodeLabel(item: MediaItem, mode: MediaModeOption): string {
    if (!this.isEpisodeMode(mode)) {
      return '';
    }

    const currentEpisode = Number(item.libraryEntry?.currentEpisode) || 0;
    const episodeCount = Number(item.episodeCount) || 0;
    return episodeCount > 0 ? `Episode ${currentEpisode}/${episodeCount}` : `Episode ${currentEpisode}`;
  }

  mediaTypeLabel(item: MediaItem, mode: MediaModeOption): string {
    if (this.isGamesMode(mode)) {
      return this.platformLabel(item.platforms);
    }

    if (this.isEpisodeMode(mode)) {
      return this.episodeLabel(item, mode);
    }

    return this.capitalize(item.kind.slice(0, -1) || mode.singular);
  }

  hasLibraryEntry(item: MediaItem): boolean {
    return !!item.libraryEntry;
  }

  remainingOf(item: MediaItem, mode: MediaModeOption): number {
    return remainingOf(item, mode);
  }

  statusOf(item: MediaItem): number {
    return statusOf(item);
  }

  createLibraryForm(item: MediaItem | null, mode: MediaModeOption): MediaLibraryForm {
    const entry = item?.libraryEntry;

    return {
      status: Number(entry?.status ?? GameStatus.Planned),
      timeSpend: this.isGamesMode(mode) ? entry?.timeSpend ?? 0 : Math.round((Number(entry?.currentWatchTimeMinutes) || 0) / 60),
      rating: entry?.rating ?? null,
      startDate: this.dateInputValue(entry?.startDate),
      endDate: this.dateInputValue(entry?.endDate),
      currentEpisode: entry?.currentEpisode ?? null,
    };
  }

  toLibraryEntryDetails(form: MediaLibraryForm, mode: MediaModeOption): LibraryEntryDetails {
    return {
      status: Number(form.status),
      timeSpend: this.numberOrNull(form.timeSpend),
      rating: this.numberOrNull(form.rating),
      startDate: form.startDate || null,
      endDate: form.endDate || null,
      currentEpisode: this.isEpisodeMode(mode) ? this.numberOrNull(form.currentEpisode) : null,
    };
  }

  libraryEntry(item: MediaItem): UserMediaEntry | null {
    return libraryEntry(item);
  }

  playedOf(item: MediaItem, mode: MediaModeOption): number {
    return playedOf(item, mode);
  }

  private expectedHoursOf(item: MediaItem, mode: MediaModeOption): number {
    return expectedHoursOf(item, mode);
  }

  private dateInputValue(value: Date | string | null | undefined): string {
    if (!value) {
      return '';
    }

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
      return '';
    }

    return date.toISOString().slice(0, 10);
  }

  private numberOrNull(value: number | string | null): number | null {
    if (value === null || value === '') {
      return null;
    }

    const numericValue = Number(value);
    return Number.isFinite(numericValue) ? numericValue : null;
  }

  private capitalize(value: string): string {
    return `${value[0]?.toUpperCase() ?? ''}${value.slice(1)}`;
  }
}
