import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  IonButton,
  IonContent,
  IonIcon,
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
  checkmarkCircleOutline,
  closeOutline,
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
import { MediaLibraryCardComponent } from '../components/media-library-card/media-library-card.component';
import { MediaLibraryListRowComponent } from '../components/media-library-list-row/media-library-list-row.component';

type ViewMode = 'grid' | 'list';
type OwnershipFilter = 'all' | 'mine' | 'catalog';

@Component({
  selector: 'app-media',
  templateUrl: './media.page.html',
  styleUrls: ['./media.page.scss'],
  imports: [
    CommonModule,
    FormsModule,
    IonButton,
    IonContent,
    IonIcon,
    IonModal,
    IonRefresher,
    IonRefresherContent,
    IonSearchbar,
    IonSegment,
    IonSegmentButton,
    IonSelect,
    IonSelectOption,
    IonSkeletonText,
    MediaLibraryCardComponent,
    MediaLibraryListRowComponent,
  ],
})
export class MediaPage implements OnInit, OnDestroy {
  games: MediaItem[] = [];
  filteredGames: MediaItem[] = [];
  searchTerm = '';
  ownershipFilter: OwnershipFilter = 'all';
  statusFilter = 'all';
  platformFilter = 'all';
  viewMode: ViewMode = 'grid';
  isLoading = true;
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

  private mediaModeSub?: Subscription;

  readonly platforms = Platforms;

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
      checkmarkCircleOutline,
      closeOutline,
      gameControllerOutline,
      gridOutline,
      hourglassOutline,
      listOutline,
      searchOutline,
    });
  }

  ngOnInit() {
    this.mediaModeSub = this.mediaModeService.mode$.subscribe((mode) => {
      const changed = mode.id !== this.mediaMode.id;
      this.mediaMode = mode;
      if (changed) {
        this.clearFilters(false);
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
    this.errorMessage = '';

    this.mediaLibrary.getAll().subscribe({
      next: (result) => {
        this.games = result.data ?? [];
        this.applyFilters();
        this.isLoading = false;
        this.completeRefresh(event);
        if (this.mediaMode.id === 'games') {
          this.releaseNotifications.syncForGames(this.gamesForReleaseNotifications());
        }
      },
      error: () => {
        this.games = [];
        this.filteredGames = [];
        this.errorMessage = `${this.mediaMode.label} could not be loaded.`;
        this.isLoading = false;
        this.completeRefresh(event);
      },
    });
  }

  applyFilters() {
    const normalizedSearch = this.searchTerm.trim().toLowerCase();

    this.filteredGames = this.games
      .filter((game) => this.matchesOwnership(game))
      .filter((game) => this.matchesStatus(game))
      .filter((game) => this.matchesPlatform(game))
      .filter((game) => {
        if (!normalizedSearch) {
          return true;
        }

        return [
          game.name,
          game.description,
          game.genre,
          this.platformLabel(game.platforms),
          this.statusLabel(game),
        ]
          .filter(Boolean)
          .some((value) => String(value).toLowerCase().includes(normalizedSearch));
      })
      .sort((a, b) => a.name.localeCompare(b.name));
  }

  clearFilters(apply = true) {
    this.searchTerm = '';
    this.ownershipFilter = 'all';
    this.statusFilter = 'all';
    this.platformFilter = 'all';
    if (apply) {
      this.applyFilters();
    }
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
        this.applyFilters();
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

  get totalGames() {
    return this.games.length;
  }

  get ownedGames() {
    return this.games.filter((game) => !!this.libraryEntry(game)).length;
  }

  get playingGames() {
    return this.games.filter((game) => this.statusOf(game) === GameStatus.Playing).length;
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

  get remainingHours() {
    return Math.round(
      this.games
        .filter((game) => !!this.libraryEntry(game))
        .reduce((sum, game) => sum + this.remainingOf(game), 0)
    );
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

  private matchesOwnership(game: MediaItem): boolean {
    if (this.ownershipFilter === 'mine') {
      return !!this.libraryEntry(game);
    }

    if (this.ownershipFilter === 'catalog') {
      return !this.libraryEntry(game);
    }

    return true;
  }

  private matchesStatus(game: MediaItem): boolean {
    return this.statusFilter === 'all' || this.statusOf(game) === Number(this.statusFilter);
  }

  private matchesPlatform(game: MediaItem): boolean {
    return !this.isGamesMode || this.platformFilter === 'all' || Number(game.platforms) === Number(this.platformFilter);
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
