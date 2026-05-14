import { CommonModule, Location } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
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
  arrowBackOutline,
  calendarClearOutline,
  checkmarkCircle,
  checkmarkCircleOutline,
  cubeOutline,
  gameControllerOutline,
  hourglassOutline,
  libraryOutline,
  linkOutline,
  newspaperOutline,
  openOutline,
  refreshOutline,
  returnUpBackOutline,
  trashOutline,
} from 'ionicons/icons';
import { firstValueFrom } from 'rxjs';

import { GameService } from 'src/app/features/games/services/game.service';
import { Game, GameNewsItem, GameSummary, Platforms } from 'src/app/features/games/models/games.model';
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
  readonly newsSkeletonRows = [1, 2, 3];

  readonly questTypeOptions: { type: QuestType; label: string }[] = [
    { type: 'main', label: 'Main' },
    { type: 'sub', label: 'Sub' },
    { type: 'faction', label: 'Faction' },
  ];

  newQuestTitle = '';
  newQuestType: QuestType = 'sub';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly location: Location,
    private readonly gameService: GameService,
    private readonly questBoardService: QuestBoardService,
    private readonly sessionService: GamingSessionService,
  ) {
    addIcons({
      addOutline,
      arrowBackOutline,
      calendarClearOutline,
      checkmarkCircle,
      checkmarkCircleOutline,
      cubeOutline,
      gameControllerOutline,
      hourglassOutline,
      libraryOutline,
      linkOutline,
      newspaperOutline,
      openOutline,
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

  goToPlanning(): void {
    void this.router.navigateByUrl('/planing');
  }

  private showError(message: string): void {
    this.errorMessage = message;
    this.isLoading = false;
  }
}
