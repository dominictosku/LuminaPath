import { Location } from '@angular/common';
import { Component, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
  ActionSheetController,
  AlertController,
  IonButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonIcon,
  IonSpinner,
  IonTitle,
  IonToolbar,
} from '@ionic/angular/standalone';
import { firstValueFrom } from 'rxjs';

import { AuthService } from 'src/app/core/auth/services/auth.service';
import { GameService } from 'src/app/features/games/services/game.service';
import { Game, platformLabelFromValue } from 'src/app/features/games/models/games.model';
import { MyGameService } from 'src/app/features/my-games/services/my-game.service';
import {
  GameLibraryEntry,
  GameWithFlexibleLibrary,
  UserGameAchievement,
} from 'src/app/features/my-games/models/my-game.model';
import { GameForecast, GamingSessionService } from 'src/app/features/planning/services/gaming-session.service';
import { gameStatusLabel } from 'src/app/features/library/models/library-status.model';
import { MediaStore } from 'src/app/features/library/state/media.store';
import { CatalogEditController } from 'src/app/features/library/services/catalog-edit.controller';
import { MEDIA_MODE_OPTIONS, MediaModeOption } from 'src/app/shared/services/media-mode.service';
import { RequestCache } from 'src/app/shared/services/request-cache.service';
import { MediaLibraryViewService } from 'src/app/features/library/services/media-library-view.service';
import { extractErrorMessage } from 'src/app/shared/utils/extract-error';
import { formatShortDate } from 'src/app/shared/utils/format';
import { parseDateOrNull, toDateInputValue } from 'src/app/shared/utils/date-helpers';
import { goBackOrHome } from 'src/app/shared/utils/navigation';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { LiveSessionTrackerService } from 'src/app/shared/services/live-session-tracker.service';
import { DetailTabOption, DetailTabsComponent } from 'src/app/shared/components/detail-tabs/detail-tabs.component';
import { MediaAvailabilityComponent } from 'src/app/shared/components/media-availability/media-availability.component';
import { MediaVideosComponent } from 'src/app/shared/components/media-videos/media-videos.component';
import { CompletionCardService } from 'src/app/shared/services/completion-card.service';
import { LibraryCreateDialogComponent } from 'src/app/features/library/components/library-create-dialog/library-create-dialog.component';
import {
  CreateMediaForm,
  emptyCreateForm,
} from 'src/app/features/library/components/library-create-dialog/library-create-dialog.model';
import { GameNewsComponent } from '../components/game-news/game-news.component';
import { GameNotesComponent } from '../components/game-notes/game-notes.component';
import { GameQuestsComponent } from '../components/game-quests/game-quests.component';
import { GameSessionsComponent } from '../components/game-sessions/game-sessions.component';
import { GameHeroComponent } from '../components/game-hero/game-hero.component';
import { GameForecastComponent } from '../components/game-forecast/game-forecast.component';
import { GameTrophiesComponent } from '../components/game-trophies/game-trophies.component';
import { GameDlcListComponent } from '../components/game-dlc-list/game-dlc-list.component';
import { LiveSessionController } from '../services/live-session.controller';
import {
  buildLibraryUpdate,
  trackedHoursLabel as formatTrackedHours,
} from './my-game-details.helpers';

@Component({
  selector: 'app-my-game-details',
  templateUrl: './my-game-details.page.html',
  styleUrls: ['./my-game-details.page.scss'],
  imports: [
    IonButton,
    IonButtons,
    IonContent,
    IonHeader,
    IonIcon,
    IonSpinner,
    IonTitle,
    IonToolbar,
    GameNewsComponent,
    GameNotesComponent,
    GameQuestsComponent,
    GameSessionsComponent,
    GameHeroComponent,
    GameForecastComponent,
    GameTrophiesComponent,
    GameDlcListComponent,
    DetailTabsComponent,
    MediaAvailabilityComponent,
    MediaVideosComponent,
    LibraryCreateDialogComponent,
    FormsModule,
  ],
})
export class MyGameDetailsPage implements OnInit, OnDestroy {
  @ViewChild(GameNotesComponent) notesComponent?: GameNotesComponent;
  @ViewChild(GameSessionsComponent) sessionsComponent?: GameSessionsComponent;

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly gameService = inject(GameService);
  private readonly myGameService = inject(MyGameService);
  private readonly sessionService = inject(GamingSessionService);
  private readonly alertController = inject(AlertController);
  private readonly actionSheetController = inject(ActionSheetController);
  private readonly mediaStore = inject(MediaStore);
  private readonly mediaView = inject(MediaLibraryViewService);
  private readonly cache = inject(RequestCache);
  private readonly liveSessionTracker = inject(LiveSessionTrackerService);
  private readonly completionCard = inject(CompletionCardService);
  readonly auth = inject(AuthService);

  /** Catalog dialog needs a MediaModeOption — this page is games-only. */
  readonly gamesMode: MediaModeOption =
    MEDIA_MODE_OPTIONS.find((option) => option.id === 'games') ?? MEDIA_MODE_OPTIONS[0];
  readonly detailTabs: readonly DetailTabOption[] = [
    { value: 'overview', label: 'Overview' },
    { value: 'sessions', label: 'Sessions' },
    { value: 'gallery', label: 'Gallery' },
    { value: 'progress', label: 'Progress' },
    { value: 'news', label: 'News' },
  ];

  game: GameWithFlexibleLibrary | null = null;
  forecast: GameForecast | null = null;
  achievements: UserGameAchievement[] = [];
  isLoading = true;
  isAchievementsLoading = false;
  errorMessage = '';
  achievementsErrorMessage = '';
  selectedTab: 'overview' | 'sessions' | 'gallery' | 'progress' | 'news' = 'overview';
  isUpdatingLibrary = false;
  isSavingNotes = false;
  headerCondensed = false;
  completionCardMessage = '';

  /** Owns the admin edit-dialog + delete-confirm flow for the catalog game. */
  readonly catalogEdit = new CatalogEditController<GameWithFlexibleLibrary>(
    this.alertController,
    this.mediaStore,
    this.router,
    {
      canEdit: () => this.canEditCatalog,
      entity: () => this.game,
      id: (game) => game.id,
      name: (game) => game.name,
      isInLibrary: () => this.isInLibrary,
      toForm: (game) => this.gameToCreateForm(game),
      saveEdit: (game, name, form) => this.gameService.put(game.id, this.buildCatalogPut(game, name, form)),
      deleteEntity: (game) => this.gameService.delete(game.id),
      reload: (game) => this.loadGameAndQuests(game.id),
      reportError: (message) => { this.errorMessage = message; },
      labels: { singular: 'game', deleteCascade: ', cover link, achievements, and any dependent records' },
    },
  );

  /** Owns the live-session timer card (start/stop/discard/restore). */
  readonly liveSession = new LiveSessionController(
    this.liveSessionTracker,
    this.sessionService,
    this.myGameService,
    {
      gameId: () => this.game?.id ?? null,
      myGameId: () => this.myGameId,
      gameName: () => this.game?.name ?? null,
      libraryEntry: () => this.libraryEntry,
      reload: (gameId) => this.loadGameAndQuests(gameId),
    },
  );

  ngOnInit(): void {
    this.route.paramMap.subscribe(async (params) => {
      const gameId = Number(params.get('gameId'));
      if (!Number.isInteger(gameId) || gameId <= 0) {
        this.showError('Game not found.');
        return;
      }

      this.selectedTab = 'overview';
      await this.loadGameAndQuests(gameId);
      this.liveSession.restore(gameId);
    });
  }

  ngOnDestroy(): void {
    this.liveSession.destroy();
  }

  get libraryEntry(): GameLibraryEntry | null {
    const entry = this.game?.myGames;
    if (Array.isArray(entry)) {
      return entry[0] ?? null;
    }
    return entry ?? null;
  }

  get myGameId(): number | null {
    const id = Number(this.libraryEntry?.id);
    return Number.isInteger(id) && id > 0 ? id : null;
  }

  get isInLibrary(): boolean {
    return this.myGameId !== null;
  }

  get libraryStatusLabel(): string {
    return this.isInLibrary ? 'Already in your library' : 'Not in library';
  }

  get statusLabel(): string {
    const status = this.libraryEntry?.status;
    return status == null ? 'Catalog' : gameStatusLabel(Number(status));
  }

  get platformLabel(): string {
    return platformLabelFromValue(this.game?.platforms);
  }

  get genreLabel(): string {
    const genre = (this.game?.genre ?? '').trim();
    return genre.length ? genre : 'Unspecified';
  }

  get releaseLabel(): string {
    return formatShortDate(this.game?.releaseDate);
  }

  get playtimeLabel(): string {
    return this.game?.playtime ? `${this.game.playtime}h estimated` : 'No estimate';
  }

  get canShareCompletionCard(): boolean {
    const status = Number(this.libraryEntry?.status ?? -1);
    return this.isInLibrary && (status === 3 || status === 4);
  }

  get dlcs() {
    return this.game?.dlcs ?? [];
  }

  get hasParent(): boolean {
    return !!this.game?.parentGameId;
  }

  imageUrl(): string {
    return mediaImageUrl(this.game?.image);
  }

  setDetailTab(value: unknown): void {
    this.selectedTab = value === 'news'
      ? 'news'
      : value === 'progress'
        ? 'progress'
        : value === 'gallery'
          ? 'gallery'
          : value === 'sessions'
            ? 'sessions'
            : 'overview';
  }

  onScroll(event: CustomEvent<{ scrollTop: number }>): void {
    const scrollTop = event.detail?.scrollTop ?? 0;
    const condensed = scrollTop > 140;
    if (condensed !== this.headerCondensed) {
      this.headerCondensed = condensed;
    }
  }

  async onNotesSave(notes: string | null): Promise<void> {
    const gameId = this.game?.id;
    const myGameId = this.myGameId;
    if (!gameId || myGameId == null || this.isSavingNotes) {
      return;
    }

    this.isSavingNotes = true;
    try {
      await firstValueFrom(
        this.myGameService.updateLibraryEntry(myGameId, gameId, this.libraryUpdateDetails({
          personalNotes: notes,
        })),
      );
      this.notesComponent?.resetEditor();
      await this.loadGameAndQuests(gameId);
    } catch {
      // silent; the draft stays in place so the note is not lost
    } finally {
      this.isSavingNotes = false;
    }
  }

  get primaryActionLabel(): string {
    const status = Number(this.libraryEntry?.status ?? 1);
    if (status === 1) return 'Start playing';
    if (status === 2) return 'Mark story complete';
    if (status === 3) return 'Mark completed';
    if (status === 4) return 'Replay';
    return 'Set as playing';
  }

  get primaryActionIcon(): string {
    const status = Number(this.libraryEntry?.status ?? 1);
    if (status === 2) return 'flag-outline';
    if (status === 3) return 'checkmark-done-outline';
    if (status === 4) return 'refresh-outline';
    return 'play-outline';
  }

  async addToLibrary(): Promise<void> {
    if (!this.game?.id || this.isUpdatingLibrary) return;
    this.isUpdatingLibrary = true;
    try {
      await firstValueFrom(
        this.myGameService.addToLibrary(this.game.id, {
          status: 1,
          timeSpend: null,
          rating: null,
          startDate: null,
          endDate: null,
          personalNotes: null,
        }),
      );
      await this.loadGameAndQuests(this.game.id);
    } catch {
      // silent — user can retry
    } finally {
      this.isUpdatingLibrary = false;
    }
  }

  async exportCompletionCard(): Promise<void> {
    if (!this.game || !this.canShareCompletionCard) {
      return;
    }

    this.completionCardMessage = '';
    try {
      const fileName = await this.completionCard.export({
        title: this.game.name,
        kindLabel: 'Game',
        statusLabel: this.statusLabel,
        coverUrl: this.imageUrl(),
        rating: this.libraryEntry?.rating ?? null,
        completedAt: this.libraryEntry?.endDate ?? null,
        detailLines: [
          this.platformLabel,
          this.playtimeLabel,
          this.trackedHoursLabel(),
        ],
      });
      this.completionCardMessage = `Saved ${fileName}.`;
    } catch (error) {
      this.completionCardMessage = extractErrorMessage(error, 'Completion card could not be exported.');
    }
  }

  async openMoreMenu(): Promise<void> {
    if (!this.isInLibrary) return;

    const sheet = await this.actionSheetController.create({
      header: this.game?.name ?? 'Game options',
      cssClass: 'media-action-sheet',
      buttons: [
        {
          text: 'Remove from library',
          role: 'destructive',
          icon: 'trash-outline',
          handler: () => {
            void this.confirmRemoveFromLibrary();
          },
        },
        {
          text: 'Cancel',
          role: 'cancel',
        },
      ],
    });
    await sheet.present();
  }

  async confirmRemoveFromLibrary(): Promise<void> {
    if (!this.isInLibrary || this.isUpdatingLibrary) return;

    const alert = await this.alertController.create({
      header: 'Remove from library?',
      subHeader: this.game?.name ?? undefined,
      message:
        'This will also delete the quests and gaming sessions you linked to this game. This cannot be undone.',
      cssClass: 'media-confirm-alert',
      buttons: [
        { text: 'Keep', role: 'cancel' },
        {
          text: 'Remove',
          role: 'destructive',
          cssClass: 'media-confirm-alert__destructive',
          handler: () => {
            void this.removeFromLibrary();
          },
        },
      ],
    });
    await alert.present();
  }

  private async removeFromLibrary(): Promise<void> {
    const gameId = this.game?.id;
    const myGameId = this.myGameId;
    if (!gameId || myGameId == null || this.isUpdatingLibrary) return;

    this.isUpdatingLibrary = true;
    try {
      await firstValueFrom(this.myGameService.delete(myGameId));
      await this.loadGameAndQuests(gameId);
    } catch {
      // silent
    } finally {
      this.isUpdatingLibrary = false;
    }
  }

  async advanceStatus(): Promise<void> {
    const gameId = this.game?.id;
    const myGameId = this.myGameId;
    if (!gameId || myGameId == null || this.isUpdatingLibrary) return;

    const current = Number(this.libraryEntry?.status ?? 1);
    const next = current === 4 ? 1 : current === 1 ? 2 : current === 2 ? 3 : current === 3 ? 4 : 2;

    this.isUpdatingLibrary = true;
    try {
      await firstValueFrom(
        this.myGameService.updateLibraryEntry(myGameId, gameId, this.libraryUpdateDetails({
          status: next,
        })),
      );
      await this.loadGameAndQuests(gameId);
    } catch {
      // silent
    } finally {
      this.isUpdatingLibrary = false;
    }
  }

  goBack(): void {
    goBackOrHome(this.location, this.router);
  }

  private async loadGameAndQuests(gameId: number): Promise<void> {
    // Cache-first: paint the last-known good game (if any) so detail-page
    // deep links work offline. Then refetch in the background and replace
    // on success. On failure, keep the cached painting silently.
    const cacheKey = `media:games:${gameId}`;
    const cached = this.cache.get<GameWithFlexibleLibrary>(cacheKey);
    if (cached) {
      this.game = cached;
      this.syncMediaStore(gameId);
      this.isLoading = false;
    } else {
      this.isLoading = true;
    }
    this.errorMessage = '';

    try {
      const fresh = await firstValueFrom(this.gameService.get(gameId));
      this.game = fresh;
      this.cache.set(cacheKey, fresh);
      this.syncMediaStore(gameId);
      await this.refreshSideData();
      await this.sessionsComponent?.refresh();
    } catch {
      if (!cached) {
        this.showError('Game could not be loaded.');
      }
      // else: keep cached painting; OfflineBanner already explains the
      // staleness. refreshSideData is skipped so we don't fire more
      // requests that will fail.
    } finally {
      this.isLoading = false;
    }
  }

  private syncMediaStore(gameId: number): void {
    const entry = this.libraryEntry;
    this.mediaStore.setLibraryEntry(gameId, entry ? {
      id: entry.id ?? 0,
      rating: entry.rating ?? null,
      startDate: entry.startDate ?? null,
      endDate: entry.endDate ?? null,
      status: Number(entry.status ?? 1),
      timeSpend: entry.timeSpend ?? null,
      personalNotes: entry.personalNotes ?? null,
    } : null);
  }

  /** Loads forecast + achievements. Quests are owned by GameQuestsComponent. */
  private async refreshSideData(): Promise<void> {
    const myGameId = this.myGameId;
    if (myGameId == null) {
      this.forecast = null;
      this.achievements = [];
      this.achievementsErrorMessage = '';
      this.isAchievementsLoading = false;
      return;
    }

    this.isAchievementsLoading = true;
    this.achievementsErrorMessage = '';

    try {
      const [forecast, achievements] = await Promise.all([
        firstValueFrom(this.sessionService.forecast(myGameId)).catch(() => null),
        firstValueFrom(this.myGameService.getAchievements(myGameId)).catch(() => {
          this.achievementsErrorMessage = 'Trophies could not be loaded right now.';
          return [];
        }),
      ]);
      this.forecast = forecast;
      this.achievements = achievements;
    } finally {
      this.isAchievementsLoading = false;
    }
  }

  openSessionsTab(): void {
    this.selectedTab = 'sessions';
  }

  protected async refreshSessionData(): Promise<void> {
    await this.refreshSideData();
  }

  private libraryUpdateDetails(overrides: Parameters<typeof buildLibraryUpdate>[1] = {}) {
    return buildLibraryUpdate(this.libraryEntry, overrides);
  }

  private trackedHoursLabel(): string {
    return formatTrackedHours(this.libraryEntry);
  }

  private showError(message: string): void {
    this.errorMessage = message;
    this.isLoading = false;
  }

  // ---------------------------------------------------------------------------
  // Admin / editor catalog actions
  // ---------------------------------------------------------------------------

  /** Status options passed through the shared catalog dialog input contract. */
  get gameStatusOptions() {
    return this.mediaView.statusOptions(this.gamesMode);
  }

  /** Admins and editors can mutate the shared catalog entry from here. */
  get canEditCatalog(): boolean {
    return this.auth.canEditCatalog();
  }

  /**
   * Build the PUT payload from scratch — DO NOT spread `this.game`.
   * The loaded Game includes `myGames` (personal library entries) and
   * `dlcs` / `parentGame` navigation collections. Echoing them back makes
   * EF Core try to upsert them, which violates
   * FK_MyGames_AspNetUsers_LuminaUserId because the frontend never sees the
   * owner's user id. The catalog PUT only cares about the shared metadata +
   * the cover, so null the personal-library + child-DLC collections.
   */
  private buildCatalogPut(game: GameWithFlexibleLibrary, name: string, form: CreateMediaForm): Game {
    return Object.assign(new Game(), {
      id: game.id,
      name,
      description: form.description,
      releaseDate: parseDateOrNull(form.releaseDate) ?? game.releaseDate,
      genre: form.genre,
      platforms: form.platforms,
      playtime: form.playtime ?? 0,
      parentGameId: game.parentGameId ?? null,
      parentGameName: game.parentGameName ?? null,
      image: form.cover ?? game.image,
      myGames: null,
      dlcs: null,
    });
  }

  /** Maps the loaded Game into the create-dialog form draft for editing. */
  private gameToCreateForm(game: GameWithFlexibleLibrary): CreateMediaForm {
    const blank = emptyCreateForm();
    return {
      ...blank,
      name: game.name ?? '',
      description: game.description ?? '',
      releaseDate: toDateInputValue(game.releaseDate),
      genre: game.genre ?? '',
      platforms: Number(game.platforms ?? 0),
      playtime: game.playtime ?? null,
      // Always render the edit form with the toggle off; it's hidden anyway
      // in edit mode but keep the data shape consistent.
      createLibraryEntry: false,
      cover: game.image ?? null,
      coverPreviewUrl: null,
    };
  }

}
