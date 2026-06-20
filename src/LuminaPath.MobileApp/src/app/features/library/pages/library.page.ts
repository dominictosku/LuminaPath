import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { Router } from '@angular/router';
import {
  IonButton,
  IonContent,
  IonIcon,
  IonInfiniteScroll,
  IonInfiniteScrollContent,
  IonRefresher,
  IonRefresherContent,
  IonSkeletonText,
} from '@ionic/angular/standalone';
import { Haptics, ImpactStyle } from '@capacitor/haptics';
import { firstValueFrom } from 'rxjs';
import { Platforms } from '../../games/models/games.model';
import { ReleaseNotificationService } from 'src/app/shared/services/release-notification.service';
import { DataExportService } from 'src/app/shared/services/data-export.service';
import { MediaMode, MediaModeOption, MediaModeService } from 'src/app/shared/services/media-mode.service';
import { extractErrorMessage } from 'src/app/shared/utils/extract-error';
import { capitalize } from 'src/app/shared/utils/format';
import { triggerDownload } from 'src/app/shared/utils/download-file';
import { MediaLibraryFacade } from '../services/media-library.facade';
import { MediaStore } from '../state/media.store';
import { MediaItem } from '../models/media-item.model';
import { MediaLibraryForm } from '../models/media-library-form.model';
import { MediaLibraryViewService } from '../services/media-library-view.service';
import { GameStatus } from '../models/library-status.model';
import { LibraryCardComponent } from '../components/library-card/library-card.component';
import { LibraryListRowComponent } from '../components/library-list-row/library-list-row.component';
import { LibraryHeroComponent } from '../components/library-hero/library-hero.component';
import { LibraryToolbarComponent } from '../components/library-toolbar/library-toolbar.component';
import { LibraryAddDialogComponent } from '../components/library-add-dialog/library-add-dialog.component';
import { LibraryCreateDialogComponent } from '../components/library-create-dialog/library-create-dialog.component';
import { MediaFilter } from 'src/app/core/entities/mediaFilter';
import { LibraryIntelligenceService } from '../services/library-intelligence.service';
import {
  LibraryFilterPreset,
  OwnershipFilter,
  ReleaseDateFilter,
  SmartFilter,
  SortMode,
  ViewMode,
} from '../models/library-filter.model';
import { LibraryFilterPresetService } from '../services/library-filter-preset.service';
import { buildPageFilter, LibraryFilterState } from '../domain/library-filter.helpers';
import { LIBRARY_LIST_ROW_HEIGHT, LIBRARY_LIST_ROW_STRIDE } from '../domain/library-virtual-list';
import { LibraryVirtualScroll } from '../services/library-virtual-scroll';
import { LibraryBulkActions } from '../services/library-bulk-actions';
import { LibraryCatalogCreator } from '../services/library-catalog-creator';

@Component({
  selector: 'app-library',
  templateUrl: './library.page.html',
  styleUrls: ['./library.page.scss'],
  providers: [LibraryBulkActions, LibraryCatalogCreator],
  imports: [
    IonButton,
    IonContent,
    IonIcon,
    IonInfiniteScroll,
    IonInfiniteScrollContent,
    IonRefresher,
    IonRefresherContent,
    IonSkeletonText,
    LibraryCardComponent,
    LibraryListRowComponent,
    LibraryHeroComponent,
    LibraryToolbarComponent,
    LibraryAddDialogComponent,
    LibraryCreateDialogComponent,
  ],
})
export class LibraryPage implements OnInit, AfterViewInit, OnDestroy {
  private mediaLibrary = inject(MediaLibraryFacade);
  private mediaStore = inject(MediaStore);
  private releaseNotifications = inject(ReleaseNotificationService);
  private dataExport = inject(DataExportService);
  private router = inject(Router);
  private mediaModeService = inject(MediaModeService);
  readonly mediaView = inject(MediaLibraryViewService);
  private libraryIntelligence = inject(LibraryIntelligenceService);
  private libraryFilterPresets = inject(LibraryFilterPresetService);
  readonly bulk = inject(LibraryBulkActions);
  readonly creator = inject(LibraryCatalogCreator);

  games: MediaItem[] = [];
  /**
   * Signal-backed so the virtualisation window (which reads the list length
   * to compute total height + slice bounds) reacts whenever the data set
   * changes — refresh, infinite-scroll page, mediaStore upsert from a
   * detail page, etc. Kept narrow on purpose; templates read via `()`.
   */
  filteredGames = signal<MediaItem[]>([]);
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
  exportState: 'idle' | 'working' = 'idle';
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

  private readonly pageSize = 24;

  // ----- Virtual list (single-column "list" view only) -----------------------
  // Grid view keeps the original DOM-for-everything render path because its
  // density (2-4 columns) means item-count is naturally lower for the same
  // scroll length. List view is where huge libraries (~500+ rows) cause real
  // jank, so it's where we virtualise. The windowing math/state lives in
  // LibraryVirtualScroll; this component owns the DOM wiring it needs.
  /** Exposed for the template's `[style.top.px]` math. */
  protected readonly listRowStride = LIBRARY_LIST_ROW_STRIDE;
  protected readonly listRowHeight = LIBRARY_LIST_ROW_HEIGHT;
  protected readonly scroll = new LibraryVirtualScroll(this.filteredGames);

  private readonly contentRef = viewChild<IonContent>(IonContent);
  private readonly listAnchorRef = viewChild<ElementRef<HTMLElement>>('listAnchor');

  private readonly handleResize = () => this.scroll.syncViewportHeight();

  readonly platforms = Platforms;
  readonly sortOptions: { label: string; value: SortMode }[] = [
    { label: 'Title A-Z', value: 'title' },
    { label: 'Newest release', value: 'release-desc' },
    { label: 'Oldest release', value: 'release-asc' },
    { label: 'Highest rating', value: 'rating-desc' },
    { label: 'Most tracked hours', value: 'tracked-desc' },
    { label: 'Least remaining', value: 'remaining-asc' },
    { label: 'Recently added', value: 'recently-added' },
    { label: 'Best to finish', value: 'best-finish' },
  ];

  constructor() {
    this.mediaMode = this.mediaModeService.mode();
    let previousModeId: MediaMode = this.mediaMode.id;
    effect(() => {
      const mode = this.mediaModeService.mode();
      this.mediaMode = mode;
      if (mode.id !== previousModeId) {
        previousModeId = mode.id;
        this.bulk.resetForMode();
        this.clearFilters(false);
        this.loadSavedPresets();
        this.loadGames();
      }
    });
    // Mirror store state into local fields. Fires on initial load AND when
    // any consumer (e.g., a detail page) upserts/patches a cached item, so
    // the library view stays consistent with mutations that happen elsewhere.
    effect(() => {
      this.games = this.mediaStore.items();
      this.currentPage = this.mediaStore.page();
      this.totalPages = this.mediaStore.totalPages();
      const storeError = this.mediaStore.error();
      if (storeError) {
        this.errorMessage = storeError;
      }
      this.applyLoadedGames();
    });

    // Re-measure the list anchor whenever it (re)appears or the data set
    // size changes (the data above the list — finish-pick, results-heading
    // — can shift its Y between empty/non-empty states).
    effect(() => {
      const anchor = this.listAnchorRef();
      const _ = this.filteredGames();
      void _;
      if (anchor) {
        // Defer to the next microtask so the DOM is committed before we
        // read offsetTop / getBoundingClientRect.
        queueMicrotask(() => void this.measureListAnchor());
      }
    });
  }

  ngOnInit() {
    this.bulk.configure({
      filteredGames: this.filteredGames,
      onSuccess: (message) => (this.successMessage = message),
      onError: (message) => (this.errorMessage = message),
    });
    this.loadSavedPresets();
    this.loadGames();
    if (typeof window !== 'undefined') {
      window.addEventListener('resize', this.handleResize, { passive: true });
    }
  }

  ngAfterViewInit(): void {
    // Initial offset measurement once the list anchor is laid out. The
    // effect set up in the constructor will catch later changes (view-mode
    // flip, data load that grows content above).
    void this.measureListAnchor();
  }

  ngOnDestroy(): void {
    if (typeof window !== 'undefined') {
      window.removeEventListener('resize', this.handleResize);
    }
  }

  /**
   * IonContent fires `(ionScroll)` at roughly frame-rate; signal writes are
   * cheap and the visible-window computed re-derives lazily, so we just
   * forward the scrollTop and let the virtual-scroll controller do the rest.
   */
  protected onContentScroll(event: CustomEvent): void {
    this.scroll.onScroll((event.detail as { scrollTop?: number } | undefined)?.scrollTop);
  }

  /**
   * Convert the list anchor's viewport position into scroll-container
   * coordinates so the visible-window math can subtract it from scrollTop
   * and get the offset *inside* the list.
   */
  private async measureListAnchor(): Promise<void> {
    const anchor = this.listAnchorRef()?.nativeElement;
    const content = this.contentRef();
    if (!anchor || !content) return;
    try {
      const scrollEl = await content.getScrollElement();
      if (!scrollEl) return;
      const anchorRect = anchor.getBoundingClientRect();
      const scrollRect = scrollEl.getBoundingClientRect();
      this.scroll.setOffsetTop(anchorRect.top - scrollRect.top + scrollEl.scrollTop);
    } catch {
      // getScrollElement can reject pre-hydration in tests — non-fatal,
      // the computed falls back to its initial-render slab.
    }
  }

  loadGames(event?: CustomEvent) {
    this.isLoading = !event;
    this.isLoadingMore = false;
    this.errorMessage = '';

    void this.mediaStore.loadCatalog(this.createPageFilter(1)).then(() => {
      this.isLoading = false;
      this.completeRefresh(event);
      if (this.mediaMode.id === 'games') {
        this.releaseNotifications.syncForGames(this.gamesForReleaseNotifications());
      }
    });
  }

  loadMoreGames(event?: CustomEvent) {
    if (this.isLoading || this.isLoadingMore || !this.hasMorePages) {
      this.completeInfiniteScroll(event);
      return;
    }

    this.isLoadingMore = true;
    this.errorMessage = '';

    void this.mediaStore.loadNextPage(this.createPageFilter(this.currentPage + 1)).then(() => {
      this.isLoadingMore = false;
      this.completeInfiniteScroll(event);
      if (this.mediaMode.id === 'games') {
        this.releaseNotifications.syncForGames(this.gamesForReleaseNotifications());
      }
    });
  }

  applyFilters() {
    this.loadGames();
  }

  private applyLoadedGames(): void {
    this.refreshLibraryIntelligence();
    this.filteredGames.set(this.games);
    this.bulk.prune();
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

  async submitAddGame(): Promise<void> {
    const game = this.selectedGame;

    if (!game || this.addingGameIds.has(game.id)) {
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';
    this.addingGameIds.add(game.id);

    const details = this.mediaView.toLibraryEntryDetails(this.addGameForm, this.mediaMode);
    const existingMyGame = this.libraryEntry(game);

    try {
      if (existingMyGame) {
        await this.mediaStore.updateLibraryEntry(existingMyGame.id, game.id, details);
        this.successMessage = `${game.name} was saved.`;
      } else {
        await this.mediaStore.addToLibrary(game.id, details);
        this.successMessage = `${game.name} was added to your ${this.mediaMode.singular} list.`;
      }
      this.isAddDialogOpen = false;
      this.selectedGame = null;
      this.triggerAddHaptic();
      if (this.mediaMode.id === 'games') {
        this.releaseNotifications.syncForGames(this.gamesForReleaseNotifications());
      }
    } catch (error) {
      this.errorMessage = this.addGameErrorMessage(error);
    } finally {
      this.addingGameIds.delete(game.id);
    }
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

  /** Create the catalog entry via the page-scoped creator, then surface
   *  feedback and refresh the list. */
  submitCreate(): void {
    void this.creator.submit((name) => {
      this.successMessage = `${name} was created.`;
      this.triggerAddHaptic();
      this.loadGames();
    });
  }

  async exportLibrary(): Promise<void> {
    if (this.exportState === 'working') {
      return;
    }

    this.exportState = 'working';
    this.errorMessage = '';
    this.successMessage = '';

    try {
      const download = await firstValueFrom(this.dataExport.downloadLibraryWorkbook());
      triggerDownload(download.blob, download.fileName);
      this.successMessage = 'Excel export downloaded.';
    } catch (error) {
      this.errorMessage = extractErrorMessage(error, 'Excel export could not be downloaded.');
    } finally {
      this.exportState = 'idle';
    }
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
    return this.mediaView.resultTitle(this.filteredGames().length, this.mediaMode);
  }

  get emptyTitle(): string {
    return this.mediaView.emptyTitle(this.mediaMode);
  }

  private refreshLibraryIntelligence(): void {
    const summary = this.libraryIntelligence.summarize(this.games, this.mediaMode);
    this.totalGames = summary.totalGames;
    this.ownedGames = summary.ownedGames;
    this.playingGames = summary.playingGames;
    this.remainingHours = summary.remainingHours;
    this.shortGameCount = summary.shortGameCount;
    this.abandonedGameCount = summary.abandonedGameCount;
    this.nextBestGame = summary.nextBestGame;
  }

  // Only the three labels used by the inline "finish-pick" section remain
  // on the page. Display helpers consumed by sub-components are reached
  // through MediaLibraryViewService injected inside those components.
  statusLabel(game: MediaItem): string {
    return this.mediaView.statusLabel(game, this.mediaMode);
  }

  progressOf(game: MediaItem): number {
    return this.mediaView.progressOf(game, this.mediaMode);
  }

  remainingLabel(game: MediaItem): string {
    return this.mediaView.remainingLabel(game, this.mediaMode);
  }

  trackByGameId(_: number, game: MediaItem): number {
    return game.id;
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
    const state: LibraryFilterState = {
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
    return buildPageFilter(state, {
      pageIndex,
      pageSize: this.pageSize,
      isGamesMode: this.isGamesMode,
    });
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
    this.savedPresets = this.libraryFilterPresets.load(this.mediaMode.id);
  }

  private persistSavedPresets(): void {
    this.libraryFilterPresets.save(this.mediaMode.id, this.savedPresets);
  }

  private addGameErrorMessage(error: unknown): string {
    return extractErrorMessage(error, `${capitalize(this.mediaMode.singular)} could not be added to your list.`);
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
