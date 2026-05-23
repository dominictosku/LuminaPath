import { Location } from '@angular/common';
import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import {
  ActionSheetController,
  AlertController,
  IonButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonIcon,
  IonLabel,
  IonSegment,
  IonSegmentButton,
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
import { MEDIA_MODE_OPTIONS, MediaModeOption } from 'src/app/shared/services/media-mode.service';
import { MediaLibraryViewService } from 'src/app/features/library/services/media-library-view.service';
import { extractErrorMessage } from 'src/app/shared/utils/extract-error';
import { formatShortDate } from 'src/app/shared/utils/format';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { LibraryCreateDialogComponent } from 'src/app/features/library/components/library-create-dialog/library-create-dialog.component';
import {
  CreateMediaForm,
  emptyCreateForm,
} from 'src/app/features/library/components/library-create-dialog/library-create-dialog.model';
import { GameNewsComponent } from '../components/game-news/game-news.component';
import { GameNotesComponent } from '../components/game-notes/game-notes.component';
import { GameQuestsComponent } from '../components/game-quests/game-quests.component';
import { GameHeroComponent } from '../components/game-hero/game-hero.component';
import { GameForecastComponent } from '../components/game-forecast/game-forecast.component';
import { GameTrophiesComponent } from '../components/game-trophies/game-trophies.component';
import { GameDlcListComponent } from '../components/game-dlc-list/game-dlc-list.component';

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
    IonLabel,
    IonSegment,
    IonSegmentButton,
    IonSpinner,
    IonTitle,
    IonToolbar,
    GameNewsComponent,
    GameNotesComponent,
    GameQuestsComponent,
    GameHeroComponent,
    GameForecastComponent,
    GameTrophiesComponent,
    GameDlcListComponent,
    LibraryCreateDialogComponent,
  ],
})
export class MyGameDetailsPage implements OnInit {
  @ViewChild(GameNotesComponent) notesComponent?: GameNotesComponent;

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
  readonly auth = inject(AuthService);

  /** Catalog dialog needs a MediaModeOption — this page is games-only. */
  readonly gamesMode: MediaModeOption =
    MEDIA_MODE_OPTIONS.find((option) => option.id === 'games') ?? MEDIA_MODE_OPTIONS[0];

  game: GameWithFlexibleLibrary | null = null;
  forecast: GameForecast | null = null;
  achievements: UserGameAchievement[] = [];
  isLoading = true;
  isAchievementsLoading = false;
  errorMessage = '';
  achievementsErrorMessage = '';
  selectedTab: 'overview' | 'progress' | 'news' = 'overview';
  isUpdatingLibrary = false;
  isSavingNotes = false;
  headerCondensed = false;

  // Admin edit/delete state ----------------------------------------------------
  isEditDialogOpen = false;
  isSavingCatalogEdit = false;
  editErrorMessage = '';
  editForm: CreateMediaForm = emptyCreateForm();
  isDeletingCatalogEntry = false;

  ngOnInit(): void {
    this.route.paramMap.subscribe(async (params) => {
      const gameId = Number(params.get('gameId'));
      if (!Number.isInteger(gameId) || gameId <= 0) {
        this.showError('Game not found.');
        return;
      }

      this.selectedTab = 'overview';
      await this.loadGameAndQuests(gameId);
    });
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
    this.selectedTab = value === 'news' ? 'news' : value === 'progress' ? 'progress' : 'overview';
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
    if (window.history.length > 1) {
      this.location.back();
      return;
    }

    void this.router.navigateByUrl('/library');
  }

  private async loadGameAndQuests(gameId: number): Promise<void> {
    this.isLoading = true;
    this.errorMessage = '';

    try {
      this.game = await firstValueFrom(this.gameService.get(gameId));
      this.syncMediaStore(gameId);
      await this.refreshSideData();
    } catch {
      this.showError('Game could not be loaded.');
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

  goToPlanning(): void {
    void this.router.navigateByUrl('/planning');
  }

  private libraryUpdateDetails(overrides: Partial<{
    status: number;
    timeSpend: number | null;
    rating: number | null;
    startDate: string | null;
    endDate: string | null;
    personalNotes: string | null;
  }> = {}) {
    const entry = this.libraryEntry ?? {};
    return {
      status: Number(entry.status ?? 1),
      timeSpend: entry.timeSpend ?? null,
      rating: entry.rating ?? null,
      startDate: this.serializeDate(entry.startDate),
      endDate: this.serializeDate(entry.endDate),
      personalNotes: entry.personalNotes ?? null,
      ...overrides,
    };
  }

  private serializeDate(value: Date | string | null | undefined): string | null {
    if (!value) {
      return null;
    }
    return value instanceof Date ? value.toISOString() : value;
  }

  private showError(message: string): void {
    this.errorMessage = message;
    this.isLoading = false;
  }

  // ---------------------------------------------------------------------------
  // Admin / editor catalog actions
  // ---------------------------------------------------------------------------

  /** Status options bound to the (unused-in-edit-mode) library toggle. */
  get gameStatusOptions() {
    return this.mediaView.statusOptions(this.gamesMode);
  }

  /** Admins and editors can mutate the shared catalog entry from here. */
  get canEditCatalog(): boolean {
    return this.auth.canEditCatalog();
  }

  openEditDialog(): void {
    if (!this.canEditCatalog || !this.game) return;
    this.editErrorMessage = '';
    this.editForm = this.gameToCreateForm(this.game);
    this.isEditDialogOpen = true;
  }

  closeEditDialog(): void {
    if (this.isSavingCatalogEdit) return;
    this.isEditDialogOpen = false;
  }

  /** Commit catalog edits via GameService.put, then refetch the page. */
  async submitCatalogEdit(): Promise<void> {
    if (!this.canEditCatalog || !this.game || this.isSavingCatalogEdit) return;
    const name = this.editForm.name.trim();
    if (!name) {
      this.editErrorMessage = 'Title is required.';
      return;
    }

    this.isSavingCatalogEdit = true;
    this.editErrorMessage = '';
    const gameId = this.game.id;

    try {
      const updated = Object.assign(new Game(), this.game, {
        name,
        description: this.editForm.description,
        releaseDate: this.parseDateOrNull(this.editForm.releaseDate) ?? this.game.releaseDate,
        genre: this.editForm.genre,
        platforms: this.editForm.platforms,
        playtime: this.editForm.playtime ?? 0,
        image: this.editForm.cover ?? this.game.image,
      });

      await firstValueFrom(this.gameService.put(gameId, updated));
      this.isEditDialogOpen = false;
      await this.loadGameAndQuests(gameId);
    } catch (error) {
      this.editErrorMessage = extractErrorMessage(
        error,
        'Game could not be updated.',
      );
    } finally {
      this.isSavingCatalogEdit = false;
    }
  }

  /** Confirm + delete the catalog game. On success, navigates back to /library. */
  async confirmDeleteCatalogEntry(): Promise<void> {
    if (!this.canEditCatalog || !this.game || this.isDeletingCatalogEntry) return;

    const libraryCount = this.isInLibrary ? 1 : 0;
    const libraryWarning = libraryCount > 0
      ? ' It is currently referenced by your personal library entry.'
      : '';

    const alert = await this.alertController.create({
      header: `Delete "${this.game.name}"?`,
      message:
        `This removes the shared catalog game, including its metadata, cover link, achievements, and any dependent records.${libraryWarning} This cannot be undone.`,
      cssClass: 'media-confirm-alert',
      buttons: [
        { text: 'Keep', role: 'cancel' },
        {
          text: 'Delete',
          role: 'destructive',
          cssClass: 'media-confirm-alert__destructive',
          handler: () => {
            void this.deleteCatalogEntry();
          },
        },
      ],
    });
    await alert.present();
  }

  private async deleteCatalogEntry(): Promise<void> {
    if (!this.canEditCatalog || !this.game || this.isDeletingCatalogEntry) return;
    const gameId = this.game.id;
    this.isDeletingCatalogEntry = true;

    try {
      await firstValueFrom(this.gameService.delete(gameId));
      this.mediaStore.removeItem(gameId);
      void this.router.navigateByUrl('/library');
    } catch (error) {
      this.errorMessage = extractErrorMessage(error, 'Game could not be deleted.');
    } finally {
      this.isDeletingCatalogEntry = false;
    }
  }

  /** Maps the loaded Game into the create-dialog form draft for editing. */
  private gameToCreateForm(game: GameWithFlexibleLibrary): CreateMediaForm {
    const blank = emptyCreateForm();
    return {
      ...blank,
      name: game.name ?? '',
      description: game.description ?? '',
      releaseDate: this.toDateInputValue(game.releaseDate),
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

  /** Coerce the Game's releaseDate (Date | string) into a YYYY-MM-DD value. */
  private toDateInputValue(value: Date | string | null | undefined): string {
    if (!value) return '';
    const date = value instanceof Date ? value : new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  private parseDateOrNull(value: string): Date | null {
    if (!value) return null;
    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? null : parsed;
  }
}
