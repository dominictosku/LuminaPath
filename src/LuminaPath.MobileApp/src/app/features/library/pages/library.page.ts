import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  IonButton,
  IonContent,
  IonIcon,
  IonInfiniteScroll,
  IonInfiniteScrollContent,
  IonModal,
  IonRefresher,
  IonRefresherContent,
  IonSearchbar,
  IonSegment,
  IonSegmentButton,
  IonSelect,
  IonSelectOption,
  IonSkeletonText,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  albumsOutline,
  addOutline,
  bookmarkOutline,
  checkmarkCircleOutline,
  closeOutline,
  flashOutline,
  gameControllerOutline,
  gridOutline,
  hourglassOutline,
  listOutline,
  searchOutline,
} from 'ionicons/icons';
import { Haptics, ImpactStyle } from '@capacitor/haptics';
import { Platforms } from '../../games/models/games.model';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { ReleaseNotificationService } from 'src/app/shared/services/release-notification.service';
import { MediaModeOption, MediaModeService } from 'src/app/shared/services/media-mode.service';
import { Subscription } from 'rxjs';
import { MediaLibraryFacade } from '../services/media-library.facade';
import { MediaItem } from '../models/media-item.model';
import { MediaLibraryForm } from '../models/media-library-form.model';
import { GameStatus, MediaLibraryViewService } from '../services/media-library-view.service';
import { LibraryCardComponent } from '../components/library-card/library-card.component';
import { LibraryListRowComponent } from '../components/library-list-row/library-list-row.component';
import { MediaFilter } from 'src/app/core/entities/mediaFilter';

type ViewMode = 'grid' | 'list';
type OwnershipFilter = 'all' | 'mine' | 'catalog';
type ReleaseDateFilter = 'all' | 'released' | 'upcoming' | 'this-year' | 'last-year' | 'custom';
type SortMode = 'title' | 'release-desc' | 'release-asc' | 'rating-desc' | 'remaining-asc' | 'recently-added' | 'best-finish';
type SmartFilter = 'none' | 'short' | 'abandoned' | 'best';

type LibraryFilterPreset = {
  id: string;
  name: string;
  mediaModeId: string;
  searchTerm: string;
  ownershipFilter: OwnershipFilter;
  statusFilter: string;
  platformFilter: string;
  releaseDateFilter: ReleaseDateFilter;
  releaseDateFrom: string;
  releaseDateTo: string;
  sortMode: SortMode;
  smartFilter: SmartFilter;
};

@Component({
  selector: 'app-library',
  templateUrl: './library.page.html',
  styleUrls: ['./library.page.scss'],
  imports: [
    CommonModule,
    FormsModule,
    IonButton,
    IonContent,
    IonIcon,
    IonInfiniteScroll,
    IonInfiniteScrollContent,
    IonModal,
    IonRefresher,
    IonRefresherContent,
    IonSearchbar,
    IonSegment,
    IonSegmentButton,
    IonSelect,
    IonSelectOption,
    IonSkeletonText,
    LibraryCardComponent,
    LibraryListRowComponent,
  ],
})
export class LibraryPage implements OnInit, OnDestroy {
  games: MediaItem[] = [];
  filteredGames: MediaItem[] = [];
  searchTerm = '';
  ownershipFilter: OwnershipFilter = 'all';
  statusFilter = 'all';
  platformFilter = 'all';
  releaseDateFilter: ReleaseDateFilter = 'all';
  releaseDateFrom = '';
  releaseDateTo = '';
  sortMode: SortMode = 'title';
  smartFilter: SmartFilter = 'none';
  presetName = '';
  savedPresets: LibraryFilterPreset[] = [];
  totalGames = 0;
  ownedGames = 0;
  playingGames = 0;
  remainingHours = 0;
  shortGameCount = 0;
  abandonedGameCount = 0;
  nextBestGame: MediaItem | null = null;
  viewMode: ViewMode = 'grid';
  isLoading = true;
  isLoadingMore = false;
  errorMessage = '';
  successMessage = '';
  addingGameIds = new Set<number>();
  selectedGame: MediaItem | null = null;
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
  currentPage = 1;
  totalPages = 1;

  private mediaModeSub?: Subscription;
  private readonly pageSize = 24;
  private readonly presetStoragePrefix = 'luminapath.library.presets.v1';

  readonly platforms = Platforms;
  readonly sortOptions: { label: string; value: SortMode }[] = [
    { label: 'Title A-Z', value: 'title' },
    { label: 'Newest release', value: 'release-desc' },
    { label: 'Oldest release', value: 'release-asc' },
    { label: 'Highest rating', value: 'rating-desc' },
    { label: 'Least remaining', value: 'remaining-asc' },
    { label: 'Recently added', value: 'recently-added' },
    { label: 'Best to finish', value: 'best-finish' },
  ];

  constructor(
    private mediaLibrary: MediaLibraryFacade,
    private releaseNotifications: ReleaseNotificationService,
    private router: Router,
    private mediaModeService: MediaModeService,
    public readonly mediaView: MediaLibraryViewService,
  ) {
    this.mediaMode = this.mediaModeService.current;
    addIcons({
      addOutline,
      albumsOutline,
      bookmarkOutline,
      checkmarkCircleOutline,
      closeOutline,
      flashOutline,
      gameControllerOutline,
      gridOutline,
      hourglassOutline,
      listOutline,
      searchOutline,
    });
  }

  ngOnInit() {
    this.loadSavedPresets();
    this.mediaModeSub = this.mediaModeService.mode$.subscribe((mode) => {
      const changed = mode.id !== this.mediaMode.id;
      this.mediaMode = mode;
      if (changed) {
        this.clearFilters(false);
        this.loadSavedPresets();
        this.loadGames();
      }
    });
    this.loadGames();
  }

  ngOnDestroy(): void {
    this.mediaModeSub?.unsubscribe();
  }

  loadGames(event?: CustomEvent) {
    this.isLoading = !event;
    this.isLoadingMore = false;
    this.errorMessage = '';

    this.mediaLibrary.getAll(this.createPageFilter(1)).subscribe({
      next: (result) => {
        this.games = result.data ?? [];
        this.currentPage = result.pageIndex ?? 1;
        this.totalPages = result.totalPages ?? 1;
        this.applyLoadedGames();
        this.isLoading = false;
        this.completeRefresh(event);
        if (this.mediaMode.id === 'games') {
          this.releaseNotifications.syncForGames(this.gamesForReleaseNotifications());
        }
      },
      error: () => {
        this.games = [];
        this.filteredGames = [];
        this.refreshLibraryIntelligence();
        this.errorMessage = `${this.mediaMode.label} could not be loaded.`;
        this.isLoading = false;
        this.currentPage = 1;
        this.totalPages = 1;
        this.completeRefresh(event);
      },
    });
  }

  loadMoreGames(event?: CustomEvent) {
    if (this.isLoading || this.isLoadingMore || !this.hasMorePages) {
      this.completeInfiniteScroll(event);
      return;
    }

    this.isLoadingMore = true;
    this.errorMessage = '';

    this.mediaLibrary.getAll(this.createPageFilter(this.currentPage + 1)).subscribe({
      next: (result) => {
        this.games = this.mergeGames(this.games, result.data ?? []);
        this.currentPage = result.pageIndex ?? this.currentPage + 1;
        this.totalPages = result.totalPages ?? this.totalPages;
        this.applyLoadedGames();
        this.isLoadingMore = false;
        this.completeInfiniteScroll(event);
        if (this.mediaMode.id === 'games') {
          this.releaseNotifications.syncForGames(this.gamesForReleaseNotifications());
        }
      },
      error: () => {
        this.errorMessage = `More ${this.mediaMode.label.toLowerCase()} could not be loaded.`;
        this.isLoadingMore = false;
        this.completeInfiniteScroll(event);
      },
    });
  }

  applyFilters() {
    this.loadGames();
  }

  private applyLoadedGames(): void {
    this.refreshLibraryIntelligence();
    this.filteredGames = this.games;
  }

  clearFilters(apply = true) {
    this.searchTerm = '';
    this.ownershipFilter = 'all';
    this.statusFilter = 'all';
    this.platformFilter = 'all';
    this.releaseDateFilter = 'all';
    this.releaseDateFrom = '';
    this.releaseDateTo = '';
    this.sortMode = 'title';
    this.smartFilter = 'none';
    this.presetName = '';
    if (apply) {
      this.loadGames();
    }
  }

  onReleaseDateFilterChange(): void {
    if (this.releaseDateFilter !== 'custom') {
      this.releaseDateFrom = '';
      this.releaseDateTo = '';
    }
    this.loadGames();
  }

  setSmartFilter(filter: SmartFilter): void {
    this.smartFilter = filter;
    if (filter === 'best') {
      this.ownershipFilter = 'mine';
      this.statusFilter = 'all';
      this.platformFilter = 'all';
      this.searchTerm = '';
      this.sortMode = 'best-finish';
    }
    this.loadGames();
  }

  saveCurrentPreset(): void {
    const name = this.presetName.trim() || `${this.mediaMode.label} view ${this.savedPresets.length + 1}`;
    const preset: LibraryFilterPreset = {
      id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
      name,
      mediaModeId: this.mediaMode.id,
      searchTerm: this.searchTerm,
      ownershipFilter: this.ownershipFilter,
      statusFilter: this.statusFilter,
      platformFilter: this.platformFilter,
      releaseDateFilter: this.releaseDateFilter,
      releaseDateFrom: this.releaseDateFrom,
      releaseDateTo: this.releaseDateTo,
      sortMode: this.sortMode,
      smartFilter: this.smartFilter,
    };

    this.savedPresets = [preset, ...this.savedPresets.filter((item) => item.name !== name)].slice(0, 8);
    this.presetName = '';
    this.persistSavedPresets();
  }

  applyPreset(preset: LibraryFilterPreset): void {
    this.searchTerm = preset.searchTerm ?? '';
    this.ownershipFilter = preset.ownershipFilter ?? 'all';
    this.statusFilter = preset.statusFilter ?? 'all';
    this.platformFilter = preset.platformFilter ?? 'all';
    this.releaseDateFilter = preset.releaseDateFilter ?? 'all';
    this.releaseDateFrom = preset.releaseDateFrom ?? '';
    this.releaseDateTo = preset.releaseDateTo ?? '';
    this.sortMode = preset.sortMode ?? 'title';
    this.smartFilter = preset.smartFilter ?? 'none';
    this.loadGames();
  }

  deletePreset(preset: LibraryFilterPreset, event?: Event): void {
    event?.stopPropagation();
    this.savedPresets = this.savedPresets.filter((item) => item.id !== preset.id);
    this.persistSavedPresets();
  }

  onCustomReleaseDateChange(): void {
    if (this.releaseDateFilter !== 'custom') {
      return;
    }
    this.loadGames();
  }

  openGameListDialog(game: MediaItem) {
    if (this.addingGameIds.has(game.id)) {
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';
    this.selectedGame = game;
    this.addGameForm = this.createAddGameForm(game);
    this.isAddDialogOpen = true;
  }

  closeAddDialog() {
    if (this.selectedGame && this.isAdding(this.selectedGame)) {
      return;
    }

    this.isAddDialogOpen = false;
    this.selectedGame = null;
  }

  submitAddGame() {
    const game = this.selectedGame;

    if (!game || this.addingGameIds.has(game.id)) {
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';
    this.addingGameIds.add(game.id);

    const details = this.mediaView.toLibraryEntryDetails(this.addGameForm, this.mediaMode);

    const existingMyGame = this.libraryEntry(game);
    const request = existingMyGame
      ? this.mediaLibrary.updateLibraryEntry(existingMyGame.id, game.id, details)
      : this.mediaLibrary.addToLibrary(game.id, details);

    request.subscribe({
      next: (myGame) => {
        game.libraryEntry = myGame;
        this.loadGames();
        this.successMessage = existingMyGame
          ? `${game.name} was saved.`
          : `${game.name} was added to your ${this.mediaMode.singular} list.`;
        this.addingGameIds.delete(game.id);
        this.isAddDialogOpen = false;
        this.selectedGame = null;
        this.triggerAddHaptic();
        if (this.mediaMode.id === 'games') {
          this.releaseNotifications.syncForGames(this.gamesForReleaseNotifications());
        }
      },
      error: (error) => {
        this.errorMessage = this.addGameErrorMessage(error);
        this.addingGameIds.delete(game.id);
      },
    });
  }

  isAdding(game: MediaItem): boolean {
    return this.addingGameIds.has(game.id);
  }

  openDetails(game: MediaItem) {
    this.router.navigate(this.mediaLibrary.detailsRoute(game));
  }

  isEditingSelectedGame(): boolean {
    return !!(this.selectedGame && this.libraryEntry(this.selectedGame));
  }

  get hasMorePages(): boolean {
    return this.currentPage < this.totalPages;
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

  get heroTitle(): string {
    return this.mediaView.heroTitle(this.mediaMode);
  }

  get heroDescription(): string {
    return this.mediaView.heroDescription(this.mediaMode);
  }

  get resultTitle(): string {
    return this.mediaView.resultTitle(this.filteredGames.length, this.mediaMode);
  }

  get emptyTitle(): string {
    return this.mediaView.emptyTitle(this.mediaMode);
  }

  private refreshLibraryIntelligence(): void {
    const ownedGames = this.games.filter((game) => !!this.libraryEntry(game));
    this.totalGames = this.games.length;
    this.ownedGames = ownedGames.length;
    this.playingGames = this.games.filter((game) => this.isActive(game)).length;
    this.remainingHours = Math.round(ownedGames.reduce((sum, game) => sum + this.remainingOf(game), 0));
    this.shortGameCount = this.games.filter((game) => this.isShortBacklog(game)).length;
    this.abandonedGameCount = this.games.filter((game) => this.isStartedButAbandoned(game)).length;
    const candidates = this.games
      .filter((game) => this.isFinishCandidate(game))
      .sort((a, b) => this.bestFinishScore(a) - this.bestFinishScore(b) || this.compareTitle(a, b));

    this.nextBestGame = candidates[0] ?? null;
  }

  imageFor(game: MediaItem): string {
    return mediaImageUrl(game.image);
  }

  platformLabel(value: number | null | undefined): string {
    return this.mediaView.platformLabel(value);
  }

  statusLabel(game: MediaItem): string {
    return this.mediaView.statusLabel(game, this.mediaMode);
  }

  releaseLabel(game: MediaItem): string {
    return this.mediaView.releaseLabel(game);
  }

  progressOf(game: MediaItem): number {
    return this.mediaView.progressOf(game, this.mediaMode);
  }

  playedLabel(game: MediaItem): string {
    return this.mediaView.playedLabel(game, this.mediaMode);
  }

  remainingLabel(game: MediaItem): string {
    return this.mediaView.remainingLabel(game, this.mediaMode);
  }

  durationLabel(game: MediaItem): string {
    return this.mediaView.durationLabel(game, this.mediaMode);
  }

  episodeLabel(game: MediaItem): string {
    return this.mediaView.episodeLabel(game, this.mediaMode);
  }

  mediaTypeLabel(game: MediaItem): string {
    return this.mediaView.mediaTypeLabel(game, this.mediaMode);
  }

  hasLibraryEntry(game: MediaItem): boolean {
    return this.mediaView.hasLibraryEntry(game);
  }

  trackByGameId(_: number, game: MediaItem): number {
    return game.id;
  }

  private isShortBacklog(game: MediaItem): boolean {
    return this.isFinishCandidate(game) && this.remainingOf(game) > 0 && this.remainingOf(game) <= 10;
  }

  private isStartedButAbandoned(game: MediaItem): boolean {
    return this.isFinishCandidate(game)
      && this.playedOf(game) > 0
      && !this.isActive(game);
  }

  private isFinishCandidate(game: MediaItem): boolean {
    return !!this.libraryEntry(game)
      && !this.isCompleted(game)
      && !this.isDropped(game)
      && this.remainingOf(game) > 0;
  }

  private bestFinishScore(game: MediaItem): number {
    if (!this.isFinishCandidate(game)) {
      return Number.MAX_SAFE_INTEGER;
    }

    const remaining = this.remainingOf(game);
    const progressBonus = this.progressOf(game) / 20;
    const activeBonus = this.isActive(game) ? 5 : 0;
    const startedBonus = this.playedOf(game) > 0 ? 3 : 0;
    const ratingBonus = (this.ratingOf(game) ?? 0) / 3;
    return remaining - progressBonus - activeBonus - startedBonus - ratingBonus;
  }

  private compareTitle(a: MediaItem, b: MediaItem): number {
    return a.name.localeCompare(b.name);
  }

  private ratingOf(game: MediaItem): number {
    return Number(this.libraryEntry(game)?.rating ?? -1);
  }

  private playedOf(game: MediaItem): number {
    const entry = this.libraryEntry(game);
    if (!this.isGamesMode) {
      return (Number(entry?.currentWatchTimeMinutes) || 0) / 60;
    }
    return (Number(entry?.timeSpend) || 0) + (Number(entry?.myGameInfo?.trackedHours) || 0);
  }

  private isActive(game: MediaItem): boolean {
    return this.isGamesMode ? this.statusOf(game) === GameStatus.Playing : this.statusOf(game) === 2;
  }

  private isCompleted(game: MediaItem): boolean {
    return this.isGamesMode ? this.statusOf(game) === GameStatus.Completed : this.statusOf(game) === 3;
  }

  private isDropped(game: MediaItem): boolean {
    return !this.isGamesMode && this.statusOf(game) === 4;
  }

  private remainingOf(game: MediaItem): number {
    return this.mediaView.remainingOf(game, this.mediaMode);
  }

  private statusOf(game: MediaItem): number {
    return this.mediaView.statusOf(game);
  }

  private completeRefresh(event?: CustomEvent) {
    const target = event?.target as HTMLIonRefresherElement | undefined;
    target?.complete();
  }

  private completeInfiniteScroll(event?: CustomEvent) {
    const target = event?.target as HTMLIonInfiniteScrollElement | undefined;
    target?.complete();
  }

  private createPageFilter(pageIndex: number): MediaFilter {
    const filter = new MediaFilter();
    filter.Paging.PageIndex = pageIndex;
    filter.Paging.Count = this.pageSize;
    filter.SearchString = this.searchTerm.trim();
    filter.Ownership = this.ownershipFilter;
    filter.SortBy = this.sortMode;
    filter.SmartFilter = this.smartFilter;
    const range = this.releaseDateRange();
    filter.From = range.from;
    filter.To = range.to;
    if (this.statusFilter !== 'all') {
      if (this.isGamesMode) {
        filter.Status = Number(this.statusFilter);
      } else {
        filter.MediaStatus = Number(this.statusFilter);
      }
    }
    if (this.isGamesMode && this.platformFilter !== 'all') {
      filter.Platform = Number(this.platformFilter);
    }
    return filter;
  }

  private releaseDateRange(): { from: string | null; to: string | null } {
    const today = new Date();
    const year = today.getFullYear();

    switch (this.releaseDateFilter) {
      case 'released':
        return { from: null, to: this.toDateParam(this.addDays(today, 1)) };
      case 'upcoming':
        return { from: this.toDateParam(this.startOfDay(today)), to: null };
      case 'this-year':
        return {
          from: this.toDateParam(new Date(year, 0, 1)),
          to: this.toDateParam(new Date(year + 1, 0, 1)),
        };
      case 'last-year':
        return {
          from: this.toDateParam(new Date(year - 1, 0, 1)),
          to: this.toDateParam(new Date(year, 0, 1)),
        };
      case 'custom':
        return {
          from: this.releaseDateFrom ? this.toDateParam(this.parseDateInput(this.releaseDateFrom)) : null,
          to: this.releaseDateTo ? this.toDateParam(this.addDays(this.parseDateInput(this.releaseDateTo), 1)) : null,
        };
      case 'all':
      default:
        return { from: null, to: null };
    }
  }

  private parseDateInput(value: string): Date {
    const [year, month, day] = value.split('-').map(Number);
    return new Date(year, (month || 1) - 1, day || 1);
  }

  private startOfDay(value: Date): Date {
    return new Date(value.getFullYear(), value.getMonth(), value.getDate());
  }

  private addDays(value: Date, days: number): Date {
    const next = this.startOfDay(value);
    next.setDate(next.getDate() + days);
    return next;
  }

  private toDateParam(value: Date): string {
    return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}`;
  }

  private mergeGames(current: MediaItem[], next: MediaItem[]): MediaItem[] {
    const byId = new Map(current.map((game) => [game.id, game]));
    for (const game of next) {
      byId.set(game.id, game);
    }
    return Array.from(byId.values());
  }

  private triggerAddHaptic(): void {
    Haptics.impact({ style: ImpactStyle.Medium }).catch(() => {
    });
  }

  private createAddGameForm(game?: MediaItem): MediaLibraryForm {
    return this.mediaView.createLibraryForm(game ?? null, this.mediaMode);
  }

  private libraryEntry(game: MediaItem) {
    return this.mediaView.libraryEntry(game);
  }

  private loadSavedPresets(): void {
    try {
      const raw = localStorage.getItem(this.presetStorageKey);
      const parsed = raw ? JSON.parse(raw) : [];
      this.savedPresets = Array.isArray(parsed) ? parsed.filter((item) => item && typeof item.name === 'string') : [];
    } catch {
      this.savedPresets = [];
    }
  }

  private persistSavedPresets(): void {
    try {
      localStorage.setItem(this.presetStorageKey, JSON.stringify(this.savedPresets));
    } catch {
    }
  }

  private get presetStorageKey(): string {
    return `${this.presetStoragePrefix}.${this.mediaMode.id}`;
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

  private gamesForReleaseNotifications() {
    return this.games.map((game) => ({
      id: game.id,
      name: game.name,
      releaseDate: game.releaseDate,
      myGames: game.libraryEntry,
    })) as never[];
  }
}
