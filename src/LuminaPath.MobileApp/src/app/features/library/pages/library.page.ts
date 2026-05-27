
import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  AlertController,
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
import { Game, Platforms } from '../../games/models/games.model';
import { Anime } from '../../animes/models/animes.model';
import { Series } from '../../series/models/series.model';
import { GameService } from '../../games/services/game.service';
import { AnimeService } from '../../animes/services/anime.service';
import { SeriesService } from '../../series/services/series.service';
import { ReleaseNotificationService } from 'src/app/shared/services/release-notification.service';
import { MediaMode, MediaModeOption, MediaModeService } from 'src/app/shared/services/media-mode.service';
import { extractErrorMessage } from 'src/app/shared/utils/extract-error';
import { AuthService } from 'src/app/core/auth/services/auth.service';
import { MediaLibraryFacade } from '../services/media-library.facade';
import { MediaStore } from '../state/media.store';
import { LibraryEntryDetails, MediaItem, UserMediaEntry } from '../models/media-item.model';
import { MediaLibraryForm } from '../models/media-library-form.model';
import { MediaLibraryViewService } from '../services/media-library-view.service';
import { GameStatus } from '../models/library-status.model';
import { LibraryCardComponent } from '../components/library-card/library-card.component';
import { LibraryListRowComponent } from '../components/library-list-row/library-list-row.component';
import { LibraryHeroComponent } from '../components/library-hero/library-hero.component';
import { LibraryToolbarComponent } from '../components/library-toolbar/library-toolbar.component';
import { LibraryAddDialogComponent } from '../components/library-add-dialog/library-add-dialog.component';
import { LibraryCreateDialogComponent } from '../components/library-create-dialog/library-create-dialog.component';
import {
  CreateMediaForm,
  emptyCreateForm,
} from '../components/library-create-dialog/library-create-dialog.model';
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
import { buildPageFilter, LibraryFilterState } from '../library-filter.helpers';
import {
  LIBRARY_LIST_ROW_HEIGHT,
  LIBRARY_LIST_ROW_STRIDE,
  computeLibraryListWindow,
} from '../domain/library-virtual-list';
import {
  bulkSelectionSummary,
  pruneSelectedMediaIds,
  selectVisibleLibraryItemIds,
  selectableLibraryItems,
  selectedLibraryItems,
  selectedMediaItems,
  toggleSelectedMediaId,
} from '../domain/library-selection';

@Component({
  selector: 'app-library',
  templateUrl: './library.page.html',
  styleUrls: ['./library.page.scss'],
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
  private alertController = inject(AlertController);
  private router = inject(Router);
  private mediaModeService = inject(MediaModeService);
  readonly mediaView = inject(MediaLibraryViewService);
  private libraryIntelligence = inject(LibraryIntelligenceService);
  private libraryFilterPresets = inject(LibraryFilterPresetService);
  private gameService = inject(GameService);
  private animeService = inject(AnimeService);
  private seriesService = inject(SeriesService);
  readonly auth = inject(AuthService);

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
  errorMessage = '';
  successMessage = '';
  addingGameIds = new Set<number>();
  selectionMode = false;
  selectedItemIds = new Set<number>();
  bulkStatusValue = GameStatus.Playing;
  isBulkMutating = false;
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

  // Admin create-catalog state ------------------------------------------------
  /** Only games / animes / series are creatable from this dialog. */
  private static readonly CREATABLE_KINDS: ReadonlySet<MediaMode> = new Set([
    'games',
    'animes',
    'series',
  ]);
  isCreateDialogOpen = false;
  isCreatingCatalogEntry = false;
  createErrorMessage = '';
  createForm: CreateMediaForm = emptyCreateForm();

  private readonly pageSize = 24;

  // ----- Virtual list (single-column "list" view only) -----------------------
  // Grid view keeps the original DOM-for-everything render path because its
  // density (2-4 columns) means item-count is naturally lower for the same
  // scroll length, and absolute-positioning a responsive grid is more invasive
  // than it's worth for the modest win. List view is where huge libraries
  // (~500+ rows) cause real jank, so it's where we virtualise.
  //
  // Strategy: keep the page scrolling as one (IonContent owns the scroll —
  // pull-to-refresh + infinite-scroll keep working). The list container gets
  // an explicit height matching the *full* row count; only rows in the
  // visible window are rendered, absolutely positioned at their natural Y.
  /** Exposed for the template's `[style.top.px]` math. */
  protected readonly listRowStride = LIBRARY_LIST_ROW_STRIDE;
  protected readonly listRowHeight = LIBRARY_LIST_ROW_HEIGHT;

  private readonly contentRef = viewChild<IonContent>(IonContent);
  private readonly listAnchorRef = viewChild<ElementRef<HTMLElement>>('listAnchor');

  private readonly listScrollTop = signal(0);
  // Sensible non-zero starting value so the *first* paint shows a real
  // window of rows instead of waiting for a resize event.
  private readonly listViewportHeight = signal(
    typeof window !== 'undefined' ? window.innerHeight : 800,
  );
  private readonly listOffsetTop = signal(0);

  protected readonly visibleListWindow = computed(() => {
    return computeLibraryListWindow({
      totalItems: this.filteredGames().length,
      scrollTop: this.listScrollTop(),
      viewportHeight: this.listViewportHeight(),
      offsetTop: this.listOffsetTop(),
    });

  });

  protected readonly visibleGames = computed(() =>
    this.filteredGames().slice(
      this.visibleListWindow().start,
      this.visibleListWindow().end,
    ),
  );

  private readonly handleResize = () => {
    this.listViewportHeight.set(window.innerHeight);
  };

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
        this.selectionMode = false;
        this.clearSelection();
        this.bulkStatusValue = this.mediaView.statusOptions(mode)[0]?.value ?? GameStatus.Planned;
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
   * forward the scrollTop and let Angular do the rest.
   */
  protected onContentScroll(event: CustomEvent): void {
    const detail = event.detail as { scrollTop?: number } | undefined;
    if (typeof detail?.scrollTop === 'number') {
      this.listScrollTop.set(detail.scrollTop);
    }
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
      this.listOffsetTop.set(
        anchorRect.top - scrollRect.top + scrollEl.scrollTop,
      );
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
    this.pruneSelectedItems();
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

  get visibleLibraryItems(): MediaItem[] {
    return selectableLibraryItems(this.filteredGames());
  }

  get selectedItems(): MediaItem[] {
    return selectedMediaItems(this.games, this.selectedItemIds);
  }

  get selectedLibraryItems(): MediaItem[] {
    return selectedLibraryItems(this.games, this.selectedItemIds);
  }

  get selectedCount(): number {
    return this.selectedLibraryItems.length;
  }

  get bulkSelectionSummary(): string {
    return bulkSelectionSummary(
      this.visibleLibraryItems.length,
      this.selectedCount,
      this.mediaMode.singular,
      this.mediaMode.label.toLowerCase(),
    );
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

  toggleSelectionMode(): void {
    this.selectionMode = !this.selectionMode;
    if (!this.selectionMode) {
      this.clearSelection();
    }
  }

  toggleItemSelection(item: MediaItem): void {
    this.selectedItemIds = toggleSelectedMediaId(this.selectedItemIds, item);
  }

  selectVisibleLibraryItems(): void {
    this.selectedItemIds = selectVisibleLibraryItemIds(this.selectedItemIds, this.filteredGames());
  }

  clearSelection(): void {
    this.selectedItemIds = new Set<number>();
  }

  isSelected(item: MediaItem): boolean {
    return this.selectedItemIds.has(item.id);
  }

  isSelectable(item: MediaItem): boolean {
    return !!item.libraryEntry;
  }

  setBulkStatus(value: string | number): void {
    const next = Number(value);
    if (Number.isFinite(next)) {
      this.bulkStatusValue = next;
    }
  }

  async bulkUpdateStatus(): Promise<void> {
    const selected = this.selectedLibraryItems;
    if (!selected.length || this.isBulkMutating) {
      return;
    }

    this.isBulkMutating = true;
    this.errorMessage = '';
    this.successMessage = '';

    try {
      for (const item of selected) {
        const entry = item.libraryEntry;
        if (!entry) {
          continue;
        }

        await this.mediaStore.updateLibraryEntry(
          entry.id,
          item.id,
          this.detailsWithStatus(entry, this.bulkStatusValue),
        );
      }

      this.successMessage = this.bulkSuccessMessage(selected.length, 'updated');
      this.clearSelection();
    } catch (error) {
      this.errorMessage = extractErrorMessage(
        error,
        `Selected ${this.mediaMode.label.toLowerCase()} could not be updated.`,
      );
    } finally {
      this.isBulkMutating = false;
    }
  }

  async confirmBulkRemove(): Promise<void> {
    const selected = this.selectedLibraryItems;
    if (!selected.length || this.isBulkMutating) {
      return;
    }

    const count = selected.length;
    const alert = await this.alertController.create({
      header: `Remove ${count} ${count === 1 ? this.mediaMode.singular : this.mediaMode.label.toLowerCase()}?`,
      message: `This removes the selected ${this.mediaMode.label.toLowerCase()} from your personal library. Catalog entries stay available.`,
      cssClass: 'media-confirm-alert',
      buttons: [
        { text: 'Keep', role: 'cancel' },
        {
          text: 'Remove',
          role: 'destructive',
          cssClass: 'media-confirm-alert__destructive',
          handler: () => {
            void this.bulkRemoveFromLibrary();
          },
        },
      ],
    });
    await alert.present();
  }

  async bulkRemoveFromLibrary(): Promise<void> {
    const selected = this.selectedLibraryItems;
    if (!selected.length || this.isBulkMutating) {
      return;
    }

    this.isBulkMutating = true;
    this.errorMessage = '';
    this.successMessage = '';

    try {
      for (const item of selected) {
        const entry = item.libraryEntry;
        if (entry) {
          await this.mediaStore.removeFromLibrary(entry.id, item.id);
        }
      }

      this.successMessage = this.bulkSuccessMessage(selected.length, 'removed from your library');
      this.clearSelection();
    } catch (error) {
      this.errorMessage = extractErrorMessage(
        error,
        `Selected ${this.mediaMode.label.toLowerCase()} could not be removed.`,
      );
    } finally {
      this.isBulkMutating = false;
    }
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

  private detailsWithStatus(entry: UserMediaEntry, status: number): LibraryEntryDetails {
    return {
      status,
      timeSpend: entry.timeSpend ?? null,
      rating: entry.rating ?? null,
      startDate: this.serializeDate(entry.startDate),
      endDate: this.serializeDate(entry.endDate),
      personalNotes: entry.personalNotes ?? null,
      currentEpisode: this.isEpisodeMode ? entry.currentEpisode ?? null : null,
    };
  }

  private serializeDate(value: Date | string | null | undefined): string | null {
    if (!value) {
      return null;
    }

    if (value instanceof Date) {
      return Number.isNaN(value.getTime()) ? null : value.toISOString();
    }

    return value;
  }

  private pruneSelectedItems(): void {
    if (!this.selectedItemIds.size) {
      return;
    }

    this.selectedItemIds = pruneSelectedMediaIds(this.selectedItemIds, this.games);
  }

  private loadSavedPresets(): void {
    this.savedPresets = this.libraryFilterPresets.load(this.mediaMode.id);
  }

  private persistSavedPresets(): void {
    this.libraryFilterPresets.save(this.mediaMode.id, this.savedPresets);
  }

  private addGameErrorMessage(error: unknown): string {
    return extractErrorMessage(error, `${this.capitalize(this.mediaMode.singular)} could not be added to your list.`);
  }

  private bulkSuccessMessage(count: number, action: string): string {
    const subject = count === 1 ? this.mediaMode.singular : this.mediaMode.label.toLowerCase();
    return `${count} ${subject} ${action}.`;
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

  // ---------------------------------------------------------------------------
  // Admin create flow
  // ---------------------------------------------------------------------------

  /** Show the Create button only when the user is an admin AND the active
   *  media mode is one we know how to create (games / animes / series). */
  get canCreateCatalogEntry(): boolean {
    return this.auth.isAdmin() && LibraryPage.CREATABLE_KINDS.has(this.mediaMode.id);
  }

  openCreateDialog(): void {
    if (!this.canCreateCatalogEntry) return;
    this.createErrorMessage = '';
    this.createForm = emptyCreateForm();
    this.isCreateDialogOpen = true;
  }

  closeCreateDialog(): void {
    if (this.isCreatingCatalogEntry) return;
    this.isCreateDialogOpen = false;
  }

  async submitCreate(): Promise<void> {
    if (!this.canCreateCatalogEntry || this.isCreatingCatalogEntry) return;

    const name = this.createForm.name.trim();
    if (!name) {
      this.createErrorMessage = 'Title is required.';
      return;
    }

    this.isCreatingCatalogEntry = true;
    this.createErrorMessage = '';

    try {
      const created = await this.createCatalogEntry(name);
      // Optional sibling: also create the personal library entry.
      if (this.createForm.createLibraryEntry && created.id > 0) {
        await this.mediaStore.addToLibrary(created.id, {
          status: this.createForm.libraryEntry.status,
          timeSpend: this.createForm.libraryEntry.timeSpend ?? 0,
          rating: this.createForm.libraryEntry.rating,
          startDate: this.normalizeIsoDate(this.createForm.libraryEntry.startDate),
          endDate: this.normalizeIsoDate(this.createForm.libraryEntry.endDate),
          personalNotes: this.createForm.libraryEntry.personalNotes,
          currentEpisode: this.createForm.libraryEntry.currentEpisode,
        });
      }
      this.isCreateDialogOpen = false;
      this.successMessage = `${name} was created.`;
      this.triggerAddHaptic();
      this.loadGames();
    } catch (error) {
      this.createErrorMessage = extractErrorMessage(
        error,
        `${this.capitalize(this.mediaMode.singular)} could not be created.`,
      );
    } finally {
      this.isCreatingCatalogEntry = false;
    }
  }

  /** Dispatch to the right typed service per media mode, returning the
   *  freshly-created entity (so we can chain MyGame/MyAnime/MySeries). */
  private async createCatalogEntry(name: string): Promise<{ id: number }> {
    const releaseDate = this.parseDateOrNull(this.createForm.releaseDate);
    const cover = this.createForm.cover;
    switch (this.mediaMode.id) {
      case 'games': {
        const game = Object.assign(new Game(), {
          name,
          description: this.createForm.description,
          releaseDate: releaseDate ?? new Date(),
          genre: this.createForm.genre,
          platforms: this.createForm.platforms,
          playtime: this.createForm.playtime ?? 0,
          image: cover,
        });
        return firstValueFrom(this.gameService.post(game));
      }
      case 'animes': {
        const anime = Object.assign(new Anime(), {
          name,
          description: this.createForm.description,
          releaseDate: this.createForm.releaseDate || null,
          genre: this.createForm.genre,
          episodeCount: this.createForm.episodeCount,
          expectedWatchTimePerEpisodeMinutes: this.createForm.expectedWatchTimePerEpisodeMinutes,
          expectedWatchTimeMinutes: this.createForm.expectedWatchTimeMinutes,
          image: cover,
        });
        return firstValueFrom(this.animeService.post(anime));
      }
      case 'series': {
        const series = Object.assign(new Series(), {
          name,
          description: this.createForm.description,
          releaseDate: this.createForm.releaseDate || null,
          genre: this.createForm.genre,
          episodeCount: this.createForm.episodeCount,
          expectedWatchTimePerEpisodeMinutes: this.createForm.expectedWatchTimePerEpisodeMinutes,
          expectedWatchTimeMinutes: this.createForm.expectedWatchTimeMinutes,
          image: cover,
        });
        return firstValueFrom(this.seriesService.post(series));
      }
      default:
        throw new Error(`Create not supported for ${this.mediaMode.id}.`);
    }
  }

  private parseDateOrNull(value: string): Date | null {
    if (!value) return null;
    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? null : parsed;
  }

  private normalizeIsoDate(value: string | Date | null | undefined): string | null {
    if (!value) return null;
    if (value instanceof Date) {
      return Number.isNaN(value.getTime()) ? null : value.toISOString().slice(0, 10);
    }
    return value || null;
  }
}
