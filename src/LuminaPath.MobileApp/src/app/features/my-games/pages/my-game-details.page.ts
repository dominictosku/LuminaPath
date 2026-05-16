import { CommonModule, Location } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
  IonSelect,
  IonSelectOption,
  IonSegment,
  IonSegmentButton,
  IonSkeletonText,
  IonSpinner,
  IonTitle,
  IonToolbar,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  addOutline,
  alertCircleOutline,
  arrowBackOutline,
  calendarClearOutline,
  ellipsisVertical,
  checkmarkCircle,
  checkmarkCircleOutline,
  checkmarkDoneOutline,
  chevronDownOutline,
  chevronUpOutline,
  cubeOutline,
  flagOutline,
  gameControllerOutline,
  hourglassOutline,
  libraryOutline,
  linkOutline,
  newspaperOutline,
  openOutline,
  playOutline,
  refreshOutline,
  returnUpBackOutline,
  trashOutline,
} from 'ionicons/icons';
import { firstValueFrom } from 'rxjs';

import { GameService } from 'src/app/features/games/services/game.service';
import { Game, GameNewsItem, GameSummary, Platforms } from 'src/app/features/games/models/games.model';
import { MyGameService } from 'src/app/features/my-games/services/my-game.service';
import { Quest, QuestBoardService, QuestType } from 'src/app/features/quests/services/quest-board.service';
import { GameForecast, GamingSessionService } from 'src/app/features/planing/services/gaming-session.service';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';

const STATUS_LABELS: Record<number, string> = {
  0: 'On hold',
  1: 'Planned',
  2: 'Playing',
  3: 'Story complete',
  4: 'Completed',
  5: 'Main game',
};

type GameWithFlexibleLibrary = Game & {
  myGames?: { id?: number; status?: number } | { id?: number; status?: number }[] | null;
};

@Component({
  selector: 'app-my-game-details',
  templateUrl: './my-game-details.page.html',
  styleUrls: ['./my-game-details.page.scss'],
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    IonBadge,
    IonButton,
    IonButtons,
    IonContent,
    IonHeader,
    IonIcon,
    IonLabel,
    IonProgressBar,
    IonSelect,
    IonSelectOption,
    IonSegment,
    IonSegmentButton,
    IonSkeletonText,
    IonSpinner,
    IonTitle,
    IonToolbar,
  ],
})
export class MyGameDetailsPage implements OnInit {
  game: GameWithFlexibleLibrary | null = null;
  quests: Quest[] = [];
  forecast: GameForecast | null = null;
  newsItems: GameNewsItem[] = [];
  isLoading = true;
  isNewsLoading = false;
  newsLoaded = false;
  errorMessage = '';
  newsErrorMessage = '';
  selectedTab: 'overview' | 'news' = 'overview';
  selectedNewsProvider: string | null = null;
  readonly newsSkeletonRows = [1, 2, 3];

  readonly questTypeOptions: { type: QuestType; label: string }[] = [
    { type: 'main', label: 'Main' },
    { type: 'sub', label: 'Sub' },
    { type: 'faction', label: 'Faction' },
  ];

  newQuestTitle = '';
  newQuestType: QuestType = 'sub';

  isUpdatingLibrary = false;
  headerCondensed = false;
  completedExpanded = false;
  isAddingStarter = false;

  readonly questStarters: { title: string; type: QuestType }[] = [
    { title: 'Finish the main story', type: 'main' },
    { title: 'Reach max level', type: 'sub' },
    { title: '100% achievements', type: 'sub' },
  ];

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly location: Location,
    private readonly gameService: GameService,
    private readonly myGameService: MyGameService,
    private readonly questBoardService: QuestBoardService,
    private readonly sessionService: GamingSessionService,
    private readonly alertController: AlertController,
    private readonly actionSheetController: ActionSheetController,
  ) {
    addIcons({
      addOutline,
      alertCircleOutline,
      arrowBackOutline,
      calendarClearOutline,
      checkmarkCircle,
      checkmarkCircleOutline,
      checkmarkDoneOutline,
      chevronDownOutline,
      chevronUpOutline,
      cubeOutline,
      ellipsisVertical,
      flagOutline,
      gameControllerOutline,
      hourglassOutline,
      libraryOutline,
      linkOutline,
      newspaperOutline,
      openOutline,
      playOutline,
      refreshOutline,
      returnUpBackOutline,
      trashOutline,
    });
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(async (params) => {
      const gameId = Number(params.get('gameId'));
      if (!Number.isInteger(gameId) || gameId <= 0) {
        this.showError('Game not found.');
        return;
      }

      this.newsLoaded = false;
      this.newsItems = [];
      this.selectedNewsProvider = null;
      this.selectedTab = 'overview';
      await this.loadGameAndQuests(gameId);
    });
  }

  get libraryEntry(): { id?: number; status?: number } | null {
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
    return status == null ? 'Catalog' : STATUS_LABELS[Number(status)] ?? 'Catalog';
  }

  get platformLabel(): string {
    return Platforms.find((platform) => platform.value === Number(this.game?.platforms))?.label ?? 'Unknown platform';
  }

  get genreLabel(): string {
    const genre = (this.game?.genre ?? '').trim();
    return genre.length ? genre : 'Unspecified';
  }

  get releaseLabel(): string {
    if (!this.game?.releaseDate) {
      return 'No release date';
    }

    const date = new Date(this.game.releaseDate);
    if (Number.isNaN(date.getTime())) {
      return 'No release date';
    }

    return new Intl.DateTimeFormat('en', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    }).format(date);
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
    if (!dlc.releaseDate) {
      return 'No release date';
    }
    const date = new Date(dlc.releaseDate);
    if (Number.isNaN(date.getTime())) {
      return 'No release date';
    }
    return new Intl.DateTimeFormat('en', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    }).format(date);
  }

  dlcImageUrl(dlc: GameSummary): string {
    return mediaImageUrl(dlc.image ?? null);
  }

  trackByDlc(_: number, dlc: GameSummary): number {
    return dlc.id;
  }

  get completedQuestCount(): number {
    return this.quests.filter((quest) => quest.completed).length;
  }

  get activeQuests(): Quest[] {
    return this.quests.filter((quest) => !quest.completed);
  }

  get completedQuests(): Quest[] {
    return this.quests.filter((quest) => quest.completed);
  }

  toggleCompletedQuests(): void {
    this.completedExpanded = !this.completedExpanded;
  }

  async addStarterQuest(suggestion: { title: string; type: QuestType }): Promise<void> {
    const myGameId = this.myGameId;
    if (myGameId == null || this.isAddingStarter) {
      return;
    }

    this.isAddingStarter = true;
    try {
      await this.questBoardService.createQuest({
        title: suggestion.title,
        type: suggestion.type,
        myGameId,
      });
      await this.refreshQuests();
    } catch {
      // silent
    } finally {
      this.isAddingStarter = false;
    }
  }

  get questSummaryLabel(): string {
    if (!this.quests.length) {
      return 'No quests linked yet';
    }
    return `${this.completedQuestCount}/${this.quests.length} quests complete`;
  }

  imageUrl(): string {
    return mediaImageUrl(this.game?.image);
  }

  async addQuest(): Promise<void> {
    const title = this.newQuestTitle.trim();
    const myGameId = this.myGameId;

    if (!title || myGameId == null) {
      return;
    }

    try {
      await this.questBoardService.createQuest({
        title,
        type: this.newQuestType,
        myGameId,
      });
      this.newQuestTitle = '';
      await this.refreshQuests();
    } catch {
      // swallow; user-visible feedback can be added later
    }
  }

  async toggleQuest(quest: Quest): Promise<void> {
    const previous = quest.completed;
    quest.completed = !previous;
    try {
      await this.questBoardService.updateQuest(quest.id, { completed: !previous });
      await this.refreshQuests();
    } catch {
      quest.completed = previous;
    }
  }

  async deleteQuest(quest: Quest): Promise<void> {
    const id = quest.id;
    this.quests = this.quests.filter((item) => item.id !== id);
    try {
      await this.questBoardService.deleteQuest(id);
    } catch {
      await this.refreshQuests();
    }
  }

  questTypeFor(quest: Quest): QuestType {
    return quest.type;
  }

  questTypeLabel(type: QuestType): string {
    return this.questTypeOptions.find((option) => option.type === type)?.label ?? type;
  }

  trackByQuest(_: number, quest: Quest): number {
    return quest.id;
  }

  trackByNews(_: number, item: GameNewsItem): string {
    return item.url || item.title;
  }

  setDetailTab(value: unknown): void {
    this.selectedTab = value === 'news' ? 'news' : 'overview';
    if (this.selectedTab === 'news' && !this.newsLoaded && !this.isNewsLoading) {
      void this.loadNews();
    }
  }

  async loadNews(refresh = false): Promise<void> {
    if (!this.game?.id) {
      return;
    }

    this.isNewsLoading = true;
    this.newsErrorMessage = '';

    try {
      this.newsItems = await firstValueFrom(this.gameService.getNews(this.game.id, refresh));
      this.newsLoaded = true;
      this.selectedNewsProvider = null;
    } catch {
      this.newsErrorMessage = 'News could not be loaded right now.';
      this.newsItems = [];
      this.newsLoaded = true;
    } finally {
      this.isNewsLoading = false;
    }
  }

  newsDateLabel(item: GameNewsItem): string {
    if (!item.publishedAt) {
      return 'Recent';
    }

    const date = new Date(item.publishedAt);
    if (Number.isNaN(date.getTime())) {
      return 'Recent';
    }

    return new Intl.DateTimeFormat('en', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    }).format(date);
  }

  providerLabel(item: GameNewsItem): string {
    return item.provider === 'GoogleNews' ? 'Google News' : item.provider;
  }

  get newsProviderFilters(): { value: string; label: string; count: number }[] {
    const counts = new Map<string, { label: string; count: number }>();
    for (const item of this.newsItems) {
      const value = item.provider || 'Unknown';
      const label = this.providerLabel(item) || value;
      const current = counts.get(value);
      if (current) {
        current.count += 1;
      } else {
        counts.set(value, { label, count: 1 });
      }
    }
    return Array.from(counts, ([value, info]) => ({ value, label: info.label, count: info.count }))
      .sort((a, b) => b.count - a.count);
  }

  get filteredNewsItems(): GameNewsItem[] {
    if (!this.selectedNewsProvider) return this.newsItems;
    return this.newsItems.filter((item) => (item.provider || 'Unknown') === this.selectedNewsProvider);
  }

  setNewsProvider(value: string | null): void {
    this.selectedNewsProvider = value;
  }

  onScroll(event: CustomEvent<{ scrollTop: number }>): void {
    const scrollTop = event.detail?.scrollTop ?? 0;
    const condensed = scrollTop > 140;
    if (condensed !== this.headerCondensed) {
      this.headerCondensed = condensed;
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

    const entry = (this.libraryEntry ?? {}) as {
      timeSpend?: number | null;
      rating?: number | null;
      startDate?: string | null;
      endDate?: string | null;
    };

    this.isUpdatingLibrary = true;
    try {
      await firstValueFrom(
        this.myGameService.updateLibraryEntry(myGameId, gameId, {
          status: next,
          timeSpend: entry.timeSpend ?? null,
          rating: entry.rating ?? null,
          startDate: entry.startDate ?? null,
          endDate: entry.endDate ?? null,
        }),
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
      await this.refreshQuests();
    } catch {
      this.showError('Game could not be loaded.');
    } finally {
      this.isLoading = false;
    }
  }

  private async refreshQuests(): Promise<void> {
    const myGameId = this.myGameId;
    if (myGameId == null) {
      this.quests = [];
      this.forecast = null;
      return;
    }

    const [quests, forecast] = await Promise.all([
      this.questBoardService.getQuestsForGame(myGameId),
      firstValueFrom(this.sessionService.forecast(myGameId)).catch(() => null),
    ]);
    this.quests = quests;
    this.forecast = forecast;
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
      const date = new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date(this.forecast.projectedCompletionDate));
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
    void this.router.navigateByUrl('/planing');
  }

  private showError(message: string): void {
    this.errorMessage = message;
    this.isLoading = false;
  }
}
