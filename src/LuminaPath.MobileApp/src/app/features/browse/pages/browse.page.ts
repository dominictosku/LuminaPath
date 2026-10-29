
import { Component, OnInit, effect, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MediaPosterComponent } from 'src/app/shared/components/media-poster/media-poster.component';
import { MediaFilterTabsComponent } from 'src/app/shared/components/media-filter-tabs/media-filter-tabs.component';
import { CollectionSectionComponent } from 'src/app/shared/components/collection-section/collection-section.component';
import { LibraryAddDialogComponent } from '../../library/components/library-add-dialog/library-add-dialog.component';
import { FormsModule } from '@angular/forms';
import {
  IonButton,
  IonContent,
  IonIcon,
  IonSkeletonText,
} from '@ionic/angular/standalone';
import { forkJoin } from 'rxjs';
import { platformLabelFromValue } from '../../games/models/games.model';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { MediaModeOption, MediaModeService } from 'src/app/shared/services/media-mode.service';
import { MediaItem } from '../../library/models/media-item.model';
import { MediaLibraryForm } from '../../library/models/media-library-form.model';
import { MediaLibraryViewService } from '../../library/services/media-library-view.service';
import { GameStatus } from '../../library/models/library-status.model';
import { MediaLibraryFacade } from '../../library/services/media-library.facade';
import { BrowseGroup, BrowseItem, BrowseKind } from '../models/browse.model';
import { BrowseService } from '../services/browse.service';
import { addMonths, startOfMonth } from 'src/app/shared/utils/date-helpers';
import { extractErrorMessage } from 'src/app/shared/utils/extract-error';
import { capitalize, formatHoursMinutes, formatShortDate } from 'src/app/shared/utils/format';

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
    FormsModule,
    IonButton,
    IonContent,
    IonIcon,
    IonSkeletonText,
    MediaPosterComponent,
    MediaFilterTabsComponent,
    CollectionSectionComponent,
    LibraryAddDialogComponent,
  ],
})
export class BrowsePage implements OnInit {
  private readonly router = inject(Router);
  private readonly browseService = inject(BrowseService);
  private readonly mediaModeService = inject(MediaModeService);
  private readonly mediaLibrary = inject(MediaLibraryFacade);
  readonly mediaView = inject(MediaLibraryViewService);

  selectedKind: BrowseKind = 'games';
  readonly kindOptions = [{ id: 'games', label: 'Games' }, { id: 'animes', label: 'Anime' }];
  searchTerm = '';
  sortMode = 'popular';
  games: BrowseItem[] = [];
  animes: BrowseItem[] = [];
  selectedGameMonth = startOfMonth(new Date());
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

  constructor() {
    this.mediaMode = this.mediaModeService.mode();
    this.selectedKind = this.mediaMode.id === 'animes' ? 'animes' : 'games';
    // Resync from the global mode when it changes (e.g., the user switches
    // tabs in the nav bar). Browse only supports 'games' and 'animes', so
    // other modes are ignored.
    let lastSyncedKind: BrowseKind = this.selectedKind;
    effect(() => {
      const mode = this.mediaModeService.mode();
      this.mediaMode = mode;
      if ((mode.id === 'games' || mode.id === 'animes') && mode.id !== lastSyncedKind) {
        lastSyncedKind = mode.id;
        this.selectedKind = mode.id;
      }
    });
  }

  ngOnInit(): void {
    this.selectKind(this.selectedKind);
    this.load();
  }




  get selectedGroup(): BrowseGroup {
    return this.selectedKind === 'games'
      ? this.groupForGameMonth(this.selectedGameMonth)
      : this.groupForAnimeSeason(this.selectedAnimeSeason);
  }

  get sortCaption(): string {
    if (this.sortMode === 'title') return 'Title A–Z';
    if (this.sortMode === 'release') return 'Newest releases first';
    return this.selectedGroup.subtitle;
  }

  get visibleItems(): BrowseItem[] {
    const query = this.searchTerm.trim().toLocaleLowerCase();
    const items = this.selectedGroup.items.filter((item) => item.name.toLocaleLowerCase().includes(query));
    if (this.sortMode === 'title') items.sort((a, b) => a.name.localeCompare(b.name));
    if (this.sortMode === 'release') items.sort((a, b) => this.releaseTime(b) - this.releaseTime(a));
    return items;
  }

  selectKindTab(id: string): void {
    if (id === 'games' || id === 'animes') this.selectKind(id);
  }

  openSelectedDetails(selected?: MediaItem): void {
    const media = selected ?? this.selectedMedia;
    if (!media) return;
    this.closeAddDialog();
    void this.router.navigate(this.mediaLibrary.detailsRoute(media));
  }

  browseStatus(item: BrowseItem): string {
    return item.libraryEntry ? this.mediaView.statusLabel(this.toMediaItem(item), this.mediaMode) : this.releaseLabel(item);
  }

  browseProgress(item: BrowseItem): number | null {
    return item.libraryEntry ? this.mediaView.progressOf(this.toMediaItem(item), this.mediaMode) : null;
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
    this.mediaMode = this.mediaModeService.mode();
  }

  previousPeriod(): void {
    if (this.selectedKind === 'games') {
      this.selectedGameMonth = addMonths(this.selectedGameMonth, -1);
      return;
    }

    this.selectedAnimeSeason = addMonths(this.selectedAnimeSeason, -3);
  }

  nextPeriod(): void {
    if (this.selectedKind === 'games') {
      this.selectedGameMonth = addMonths(this.selectedGameMonth, 1);
      return;
    }

    this.selectedAnimeSeason = addMonths(this.selectedAnimeSeason, 3);
  }

  jumpToCurrentPeriod(): void {
    const now = new Date();
    if (this.selectedKind === 'games') {
      this.selectedGameMonth = startOfMonth(now);
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
        this.selectedGameMonth = this.latestMonth(this.games) ?? startOfMonth(new Date());
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







  imageFor(item: BrowseItem | MediaItem): string {
    return mediaImageUrl(item.image);
  }

  releaseLabel(item: BrowseItem): string {
    return formatShortDate(this.releaseDate(item), 'No date');
  }

  detailsLabel(item: BrowseItem): string {
    if (item.kind === 'games') {
      const platform = platformLabelFromValue(item.platforms);
      const playtime = Number(item.playtime) || 0;
      return playtime > 0 ? `${platform} · ${playtime}h` : platform;
    }

    const episodes = Number(item.episodeCount) || 0;
    const watchTime = formatHoursMinutes(item.expectedWatchTimeMinutes, '');
    if (episodes > 0 && watchTime) {
      return `${episodes} episodes · ${watchTime}`;
    }

    return episodes > 0 ? `${episodes} episodes` : watchTime || 'Anime';
  }


  trackByItem(_: number, item: BrowseItem): string {
    return `${item.kind}-${item.id}`;
  }

  private groupForGameMonth(month: Date): BrowseGroup {
    const start = startOfMonth(month);
    const end = addMonths(start, 1);
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
    const end = addMonths(start, 3);
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
    return latest ? startOfMonth(latest) : null;
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

  private startOfSeason(date: Date): Date {
    const season = this.seasonFor(date);
    return new Date(date.getFullYear(), season.startMonth, 1);
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
    return extractErrorMessage(error, `${capitalize(this.mediaMode.singular)} could not be added to your list.`);
  }

  private releaseDate(item: BrowseItem): Date | null {
    if (!item.releaseDate) {
      return null;
    }

    const date = new Date(item.releaseDate);
    return Number.isNaN(date.getTime()) ? null : date;
  }

}
