import { Location } from '@angular/common';
import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  ActionSheetController,
  AlertController,
  IonBadge,
  IonButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonIcon,
  IonLabel,
  IonProgressBar,
  IonSegment,
  IonSegmentButton,
  IonSpinner,
  IonTitle,
  IonToolbar,
} from '@ionic/angular/standalone';
import { firstValueFrom } from 'rxjs';

import { GameService } from 'src/app/features/games/services/game.service';
import { GameSummary, platformLabelFromValue } from 'src/app/features/games/models/games.model';
import { MyGameService } from 'src/app/features/my-games/services/my-game.service';
import {
  GameLibraryEntry,
  GameWithFlexibleLibrary,
  UserGameAchievement,
} from 'src/app/features/my-games/models/my-game.model';
import { GameForecast, GamingSessionService } from 'src/app/features/planning/services/gaming-session.service';
import { gameStatusLabel } from 'src/app/features/library/models/library-status.model';
import { MediaStore } from 'src/app/features/library/state/media.store';
import { formatShortDate } from 'src/app/shared/utils/format';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { GameNewsComponent } from '../components/game-news/game-news.component';
import { GameNotesComponent } from '../components/game-notes/game-notes.component';
import { GameQuestsComponent } from '../components/game-quests/game-quests.component';
import { EmptyStateComponent } from 'src/app/shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-my-game-details',
  templateUrl: './my-game-details.page.html',
  styleUrls: ['./my-game-details.page.scss'],
  imports: [
    RouterLink,
    IonBadge,
    IonButton,
    IonButtons,
    IonContent,
    IonHeader,
    IonIcon,
    IonLabel,
    IonProgressBar,
    IonSegment,
    IonSegmentButton,
    IonSpinner,
    IonTitle,
    IonToolbar,
    GameNewsComponent,
    GameNotesComponent,
    GameQuestsComponent,
    EmptyStateComponent,
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

  get dlcs(): GameSummary[] {
    return this.game?.dlcs ?? [];
  }

  get hasParent(): boolean {
    return !!this.game?.parentGameId;
  }

  dlcReleaseLabel(dlc: GameSummary): string {
    return formatShortDate(dlc.releaseDate);
  }

  dlcImageUrl(dlc: GameSummary): string {
    return mediaImageUrl(dlc.image ?? null);
  }

  trackByDlc(_: number, dlc: GameSummary): number {
    return dlc.id;
  }

  trackByAchievement(_: number, achievement: UserGameAchievement): number {
    return achievement.id;
  }

  get achievementSummaryLabel(): string {
    if (this.isAchievementsLoading) {
      return 'Syncing';
    }
    if (!this.achievements.length) {
      return 'None earned yet';
    }
    return `${this.achievements.length} earned`;
  }

  achievementDateLabel(achievement: UserGameAchievement): string {
    const dateValue = achievement.unlockedAt ?? achievement.syncedAt;
    return formatShortDate(dateValue, 'Synced recently');
  }

  trophyTypeLabel(value?: string | null): string {
    const trophyType = value?.trim();
    if (!trophyType) {
      return 'Achievement';
    }

    return trophyType.charAt(0).toUpperCase() + trophyType.slice(1);
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

  forecastSummary(): string {
    if (!this.forecast) return '';
    if (this.forecast.remainingHours == null) {
      return 'Add a playtime estimate to see a forecast.';
    }
    if (this.forecast.remainingHours <= 0) {
      return 'You are already past the estimated playtime.';
    }
    if (this.forecast.projectedCompletionDate) {
      const sessions = this.forecast.sessionsToCompletion ?? 0;
      const date = formatShortDate(this.forecast.projectedCompletionDate);
      return `${sessions} session${sessions === 1 ? '' : 's'} to finish · ETA ${date}`;
    }
    if (this.forecast.weeksAtCurrentPace != null) {
      return `Need ${this.forecastHours(this.forecast.additionalHoursNeeded)} more · ~${this.forecast.weeksAtCurrentPace} weeks at ${this.forecastHours(this.forecast.weeklyHours)}/week`;
    }
    return `Need ${this.forecastHours(this.forecast.additionalHoursNeeded)} more — schedule sessions to project an ETA.`;
  }

  forecastHours(value: number): string {
    return `${Math.round(value * 10) / 10}h`;
  }

  forecastProgress(): number {
    if (!this.forecast?.playtimeEstimateHours || this.forecast.playtimeEstimateHours <= 0) return 0;
    return Math.min(1, this.forecast.playedHours / this.forecast.playtimeEstimateHours);
  }

  get forecastBarParts(): { played: number; scheduled: number; remaining: number; total: number } {
    const f = this.forecast;
    if (!f) return { played: 0, scheduled: 0, remaining: 0, total: 0 };

    const played = Math.max(0, f.playedHours ?? 0);
    const remainingTotal = f.remainingHours != null
      ? Math.max(0, f.remainingHours)
      : Math.max(0, (f.playtimeEstimateHours ?? 0) - played);
    const scheduled = Math.min(Math.max(0, f.scheduledHours ?? 0), remainingTotal);
    const remaining = Math.max(0, remainingTotal - scheduled);

    return { played, scheduled, remaining, total: played + scheduled + remaining };
  }

  forecastWidth(segment: 'played' | 'scheduled' | 'remaining'): number {
    const parts = this.forecastBarParts;
    if (parts.total <= 0) return 0;
    const value =
      segment === 'played' ? parts.played : segment === 'scheduled' ? parts.scheduled : parts.remaining;
    return Math.round((value / parts.total) * 1000) / 10;
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
}
