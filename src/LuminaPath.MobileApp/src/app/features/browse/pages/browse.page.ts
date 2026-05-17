import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  IonButton,
  IonBadge,
  IonContent,
  IonIcon,
  IonModal,
  IonSegment,
  IonSegmentButton,
  IonSkeletonText,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  addOutline,
  calendarClearOutline,
  checkmarkCircleOutline,
  closeOutline,
  createOutline,
  filmOutline,
  gameControllerOutline,
  hourglassOutline,
  peopleOutline,
  refreshOutline,
  sparklesOutline,
  timeOutline,
} from 'ionicons/icons';
import { forkJoin } from 'rxjs';
import { Platforms } from '../../games/models/games.model';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { MediaModeOption, MediaModeService } from 'src/app/shared/services/media-mode.service';
import { MediaItem } from '../../library/models/media-item.model';
import { MediaLibraryForm } from '../../library/models/media-library-form.model';
import { MediaLibraryViewService } from '../../library/services/media-library-view.service';
import { GameStatus } from '../../library/models/library-status.model';
import { MediaLibraryFacade } from '../../library/services/media-library.facade';
import { BrowseGroup, BrowseItem, BrowseKind } from '../models/browse.model';
import { BrowseService } from '../services/browse.service';

const SEASONS = [
  { name: 'Winter', startMonth: 0 },
  { name: 'Spring', startMonth: 3 },
  { name: 'Summer', startMonth: 6 },
  { name: 'Fall', startMonth: 9 },
] as const;

@Component({
  selector: 'app-browse',
  templateUrl: './browse.page.html',
  styleUrls: ['./browse.page.scss'],
  imports: [
    CommonModule,
    FormsModule,
    IonBadge,
    IonButton,
    IonContent,
    IonIcon,
    IonModal,
    IonSegment,
    IonSegmentButton,
    IonSkeletonText,
  ],
})
export class BrowsePage implements OnInit {
  selectedKind: BrowseKind = 'games';
  games: BrowseItem[] = [];
  animes: BrowseItem[] = [];
  selectedGameMonth = this.startOfMonth(new Date());
  selectedAnimeSeason = this.startOfSeason(new Date());
  isLoading = true;
  errorMessage = '';
  successMessage = '';
  addingItemKeys = new Set<string>();
  selectedItem: BrowseItem | null = null;
  selectedMedia: MediaItem | null = null;
  isAddDialogOpen = false;
  addGameForm: MediaLibraryForm = {
    status: GameStatus.Planned,
    timeSpend: 0,
    rating: null,
    startDate: '',
    endDate: '',
    currentEpisode: null,
  };
  mediaMode: MediaModeOption;

  constructor(
    private readonly browseService: BrowseService,
    private readonly mediaModeService: MediaModeService,
    private readonly mediaLibrary: MediaLibraryFacade,
    public readonly mediaView: MediaLibraryViewService,
  ) {
    this.mediaMode = this.mediaModeService.current;
    addIcons({
      addOutline,
      calendarClearOutline,
      checkmarkCircleOutline,
      closeOutline,
      createOutline,
      filmOutline,
      gameControllerOutline,
      hourglassOutline,
      peopleOutline,
      refreshOutline,
      sparklesOutline,
      timeOutline,
    });
  }

  ngOnInit(): void {
    this.selectKind(this.selectedKind);
    this.load();
  }

  get title(): string {
    return this.selectedKind === 'games' ? 'Game releases' : 'Anime seasons';
  }

  get description(): string {
    return this.selectedKind === 'games'
      ? 'Review one release month at a time, with the most popular games highlighted first.'
      : 'Review one anime season at a time, with the most popular titles highlighted first.';
  }

  get items(): BrowseItem[] {
    return this.selectedKind === 'games' ? this.games : this.animes;
  }

  get selectedGroup(): BrowseGroup {
    return this.selectedKind === 'games'
      ? this.groupForGameMonth(this.selectedGameMonth)
      : this.groupForAnimeSeason(this.selectedAnimeSeason);
  }

  get topItems(): BrowseItem[] {
    return this.selectedGroup.items.slice(0, 4);
  }

  get hasItemsInPeriod(): boolean {
    return this.selectedGroup.items.length > 0;
  }

  get periodLabel(): string {
    return this.selectedGroup.title;
  }

  get periodSubtitle(): string {
    return this.selectedKind === 'games'
      ? 'Monthly release window'
      : 'Seasonal release window';
  }

  selectKind(kind: BrowseKind): void {
    this.selectedKind = kind;
    this.mediaModeService.select(kind);
    this.mediaMode = this.mediaModeService.current;
  }

  previousPeriod(): void {
    if (this.selectedKind === 'games') {
      this.selectedGameMonth = this.addMonths(this.selectedGameMonth, -1);
      return;
    }

    this.selectedAnimeSeason = this.addMonths(this.selectedAnimeSeason, -3);
  }

  nextPeriod(): void {
    if (this.selectedKind === 'games') {
      this.selectedGameMonth = this.addMonths(this.selectedGameMonth, 1);
      return;
    }

    this.selectedAnimeSeason = this.addMonths(this.selectedAnimeSeason, 3);
  }

  jumpToCurrentPeriod(): void {
    const now = new Date();
    if (this.selectedKind === 'games') {
      this.selectedGameMonth = this.startOfMonth(now);
      return;
    }

    this.selectedAnimeSeason = this.startOfSeason(now);
  }

  load(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    forkJoin({
      games: this.browseService.getGameReleases(),
      animes: this.browseService.getAnimeReleases(),
    }).subscribe({
      next: ({ games, animes }) => {
        this.games = games ?? [];
        this.animes = animes ?? [];
        this.selectedGameMonth = this.latestMonth(this.games) ?? this.startOfMonth(new Date());
        this.selectedAnimeSeason = this.latestSeason(this.animes) ?? this.startOfSeason(new Date());
        this.isLoading = false;
      },
      error: () => {
        this.games = [];
        this.animes = [];
        this.errorMessage = 'Browse could not be loaded.';
        this.isLoading = false;
      },
    });
  }

  openLibraryDialog(item: BrowseItem): void {
    if (this.isAdding(item)) {
      return;
    }

    this.selectKind(item.kind);
    this.errorMessage = '';
    this.successMessage = '';
    this.selectedItem = item;
    this.selectedMedia = this.toMediaItem(item);
    this.addGameForm = this.mediaView.createLibraryForm(this.selectedMedia, this.mediaMode);
    this.isAddDialogOpen = true;
  }

  closeAddDialog(): void {
    if (this.selectedItem && this.isAdding(this.selectedItem)) {
      return;
    }

    this.isAddDialogOpen = false;
    this.selectedItem = null;
    this.selectedMedia = null;
  }

  submitAddGame(): void {
    const item = this.selectedItem;
    const media = this.selectedMedia;

    if (!item || !media || this.isAdding(item)) {
      return;
    }

    this.selectKind(item.kind);
    this.errorMessage = '';
    this.successMessage = '';
    this.addingItemKeys.add(this.itemKey(item));

    const details = this.mediaView.toLibraryEntryDetails(this.addGameForm, this.mediaMode);
    const existingEntry = media.libraryEntry;
    const request = existingEntry
      ? this.mediaLibrary.updateLibraryEntry(existingEntry.id, media.id, details)
      : this.mediaLibrary.addToLibrary(media.id, details);

    request.subscribe({
      next: (libraryEntry) => {
        item.libraryEntry = libraryEntry;
        media.libraryEntry = libraryEntry;
        if (!existingEntry) {
          item.addedCount += 1;
        }

        this.successMessage = existingEntry
          ? `${item.name} was saved.`
          : `${item.name} was added to your ${this.mediaMode.singular} list.`;
        this.addingItemKeys.delete(this.itemKey(item));
        this.isAddDialogOpen = false;
        this.selectedItem = null;
        this.selectedMedia = null;
      },
      error: (error) => {
        this.errorMessage = this.addGameErrorMessage(error);
        this.addingItemKeys.delete(this.itemKey(item));
      },
    });
  }

  isAdding(item: BrowseItem | MediaItem): boolean {
    return this.addingItemKeys.has(`${item.kind}-${item.id}`);
  }

  hasLibraryEntry(item: BrowseItem): boolean {
    return !!item.libraryEntry;
  }

  isEditingSelectedGame(): boolean {
    return !!this.selectedMedia?.libraryEntry;
  }

  get isGamesMode(): boolean {
    return this.mediaView.isGamesMode(this.mediaMode);
  }

  get isEpisodeMode(): boolean {
    return this.mediaView.isEpisodeMode(this.mediaMode);
  }

  get statusOptions() {
    return this.mediaView.statusOptions(this.mediaMode);
  }

  platformLabel(value: number | null | undefined): string {
    return this.mediaView.platformLabel(value);
  }

  statusLabel(item: MediaItem): string {
    return this.mediaView.statusLabel(item, this.mediaMode);
  }

  progressOf(item: MediaItem): number {
    return this.mediaView.progressOf(item, this.mediaMode);
  }

  remainingLabel(item: MediaItem): string {
    return this.mediaView.remainingLabel(item, this.mediaMode);
  }

  durationLabel(item: MediaItem): string {
    return this.mediaView.durationLabel(item, this.mediaMode);
  }

  mediaTypeLabel(item: MediaItem): string {
    return this.mediaView.mediaTypeLabel(item, this.mediaMode);
  }

  imageFor(item: BrowseItem | MediaItem): string {
    return mediaImageUrl(item.image);
  }

  releaseLabel(item: BrowseItem): string {
    const date = this.releaseDate(item);
    if (!date) {
      return 'No date';
    }

    return new Intl.DateTimeFormat('en', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    }).format(date);
  }

  detailsLabel(item: BrowseItem): string {
    if (item.kind === 'games') {
      const platform = Platforms.find((candidate) => candidate.value === Number(item.platforms))?.label ?? 'Unknown';
      const playtime = Number(item.playtime) || 0;
      return playtime > 0 ? `${platform} · ${playtime}h` : platform;
    }

    const episodes = Number(item.episodeCount) || 0;
    const watchTime = this.formatMinutes(item.expectedWatchTimeMinutes);
    if (episodes > 0 && watchTime) {
      return `${episodes} episodes · ${watchTime}`;
    }

    return episodes > 0 ? `${episodes} episodes` : watchTime || 'Anime';
  }

  trackByGroup(_: number, group: BrowseGroup): string {
    return group.id;
  }

  trackByItem(_: number, item: BrowseItem): string {
    return `${item.kind}-${item.id}`;
  }

  private groupForGameMonth(month: Date): BrowseGroup {
    const start = this.startOfMonth(month);
    const end = this.addMonths(start, 1);
    const items = this.games
      .filter((item) => this.isInRange(item, start, end))
      .sort((a, b) => b.addedCount - a.addedCount || this.releaseTime(a) - this.releaseTime(b));

    return {
      id: `${start.getFullYear()}-${start.getMonth()}`,
      title: new Intl.DateTimeFormat('en', { month: 'long', year: 'numeric' }).format(start),
      subtitle: 'Most popular games first',
      sortValue: start.getTime(),
      items,
    };
  }

  private groupForAnimeSeason(seasonStart: Date): BrowseGroup {
    const start = this.startOfSeason(seasonStart);
    const end = this.addMonths(start, 3);
    const season = this.seasonFor(start);
    const items = this.animes
      .filter((item) => this.isInRange(item, start, end))
      .sort((a, b) => b.addedCount - a.addedCount || this.releaseTime(a) - this.releaseTime(b));

    return {
      id: `${start.getFullYear()}-${season.name}`,
      title: `${season.name} ${start.getFullYear()}`,
      subtitle: 'Most popular anime first',
      sortValue: start.getTime(),
      items,
    };
  }

  private seasonFor(date: Date): typeof SEASONS[number] {
    const month = date.getMonth();
    if (month >= 9) {
      return SEASONS[3];
    }

    if (month >= 6) {
      return SEASONS[2];
    }

    if (month >= 3) {
      return SEASONS[1];
    }

    return SEASONS[0];
  }

  private releaseTime(item: BrowseItem): number {
    return this.releaseDate(item)?.getTime() ?? 0;
  }

  private isInRange(item: BrowseItem, start: Date, end: Date): boolean {
    const date = this.releaseDate(item);
    return !!date && date >= start && date < end;
  }

  private latestMonth(items: BrowseItem[]): Date | null {
    const latest = this.latestReleaseDate(items);
    return latest ? this.startOfMonth(latest) : null;
  }

  private latestSeason(items: BrowseItem[]): Date | null {
    const latest = this.latestReleaseDate(items);
    return latest ? this.startOfSeason(latest) : null;
  }

  private latestReleaseDate(items: BrowseItem[]): Date | null {
    return items
      .map((item) => this.releaseDate(item))
      .filter((date): date is Date => !!date)
      .sort((a, b) => b.getTime() - a.getTime())[0] ?? null;
  }

  private startOfMonth(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), 1);
  }

  private startOfSeason(date: Date): Date {
    const season = this.seasonFor(date);
    return new Date(date.getFullYear(), season.startMonth, 1);
  }

  private addMonths(date: Date, months: number): Date {
    return new Date(date.getFullYear(), date.getMonth() + months, 1);
  }

  private toMediaItem(item: BrowseItem): MediaItem {
    return {
      id: item.id,
      name: item.name,
      description: item.description ?? '',
      releaseDate: item.releaseDate,
      genre: item.genre ?? '',
      image: item.image,
      kind: item.kind,
      libraryEntry: item.libraryEntry ?? null,
      platforms: item.platforms,
      playtime: item.playtime,
      expectedWatchTimePerEpisodeMinutes: item.expectedWatchTimePerEpisodeMinutes,
      expectedWatchTimeMinutes: item.expectedWatchTimeMinutes,
      episodeCount: item.episodeCount,
    };
  }

  private itemKey(item: BrowseItem | MediaItem): string {
    return `${item.kind}-${item.id}`;
  }

  private addGameErrorMessage(error: unknown): string {
    const payload = (error as { error?: unknown })?.error;

    if (typeof payload === 'string') {
      return payload;
    }

    if (payload && typeof payload === 'object' && 'message' in payload) {
      return String((payload as { message: unknown }).message);
    }

    if (payload && typeof payload === 'object' && 'errorMessage' in payload) {
      const messages = (payload as { errorMessage: unknown }).errorMessage;
      return Array.isArray(messages) ? messages.join(' ') : String(messages);
    }

    if (payload && typeof payload === 'object' && 'errors' in payload) {
      const errors = (payload as { errors: Record<string, string[]> }).errors;
      const messages = Object.values(errors).flat();
      return messages.length ? messages.join(' ') : `${this.capitalize(this.mediaMode.singular)} could not be added to your list.`;
    }

    return `${this.capitalize(this.mediaMode.singular)} could not be added to your list.`;
  }

  private capitalize(value: string): string {
    return `${value[0]?.toUpperCase() ?? ''}${value.slice(1)}`;
  }

  private releaseDate(item: BrowseItem): Date | null {
    if (!item.releaseDate) {
      return null;
    }

    const date = new Date(item.releaseDate);
    return Number.isNaN(date.getTime()) ? null : date;
  }

  private formatMinutes(value: number | null | undefined): string {
    const minutes = Number(value) || 0;
    if (minutes <= 0) {
      return '';
    }

    const hours = Math.floor(minutes / 60);
    const remaining = minutes % 60;
    if (hours > 0 && remaining > 0) {
      return `${hours}h ${remaining}m`;
    }

    return hours > 0 ? `${hours}h` : `${remaining}m`;
  }
}
