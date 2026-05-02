import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  IonBadge,
  IonButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonIcon,
  IonRefresher,
  IonRefresherContent,
  IonSkeletonText,
  IonTitle,
  IonToolbar,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  calendarClearOutline,
  checkmarkDoneOutline,
  flameOutline,
  gameControllerOutline,
  hourglassOutline,
  libraryOutline,
  refreshOutline,
  sparklesOutline,
  timeOutline,
  trendingUpOutline,
} from 'ionicons/icons';
import { Game, Platforms } from '../../games/models/games.model';
import { GameService } from '../../games/services/game.service';

enum GameStatus {
  OnHold = 0,
  Planned = 1,
  Playing = 2,
  StoryComplete = 3,
  Completed = 4,
  MainGame = 5,
}

type DashboardMetric = {
  label: string;
  value: string;
  detail: string;
  icon: string;
  tone: 'blue' | 'green' | 'amber' | 'rose';
};

@Component({
  selector: 'app-home',
  templateUrl: './home.page.html',
  styleUrls: ['./home.page.scss'],
  imports: [
    IonBadge,
    IonButton,
    IonButtons,
    IonContent,
    IonHeader,
    IonIcon,
    IonRefresher,
    IonRefresherContent,
    IonSkeletonText,
    IonTitle,
    IonToolbar,
    CommonModule,
  ],
})
export class HomePage implements OnInit {
  games: Game[] = [];
  metrics: DashboardMetric[] = [];
  ownedGames: Game[] = [];
  playingGames: Game[] = [];
  upcomingReleases: Game[] = [];
  backlogGames: Game[] = [];
  recentGames: Game[] = [];
  featuredGame: Game | null = null;
  remainingHours = 0;
  playedHours = 0;
  completionRate = 0;
  heroProgress = 0;
  isLoading = true;
  errorMessage = '';

  constructor(private gameService: GameService) {
    addIcons({
      calendarClearOutline,
      checkmarkDoneOutline,
      flameOutline,
      gameControllerOutline,
      hourglassOutline,
      libraryOutline,
      refreshOutline,
      sparklesOutline,
      timeOutline,
      trendingUpOutline,
    });
  }

  ngOnInit() {
    this.loadDashboard();
  }

  loadDashboard(event?: CustomEvent) {
    this.isLoading = !event;
    this.errorMessage = '';

    this.gameService.getAll().subscribe({
      next: (result) => {
        this.games = result.data ?? [];
        this.buildDashboard();
        this.isLoading = false;
        this.completeRefresh(event);
      },
      error: () => {
        this.games = [];
        this.buildDashboard();
        this.errorMessage = 'Dashboard data could not be loaded.';
        this.isLoading = false;
        this.completeRefresh(event);
      },
    });
  }

  private buildDashboard() {
    this.ownedGames = this.games.filter((game) => !!game.myGames);
    this.playingGames = this.ownedGames
      .filter((game) => this.statusOf(game) === GameStatus.Playing)
      .sort((a, b) => this.progressOf(b) - this.progressOf(a))
      .slice(0, 4);
    this.upcomingReleases = this.getUpcomingReleases();
    this.backlogGames = this.getBacklogGames();
    this.recentGames = [...this.games]
      .sort((a, b) => b.id - a.id)
      .slice(0, 8);
    this.featuredGame = this.playingGames[0] ?? this.upcomingReleases[0] ?? this.recentGames[0] ?? null;
    this.remainingHours = Math.round(
      this.ownedGames.reduce((sum, game) => sum + this.remainingOf(game), 0)
    );
    this.playedHours = Math.round(
      this.ownedGames.reduce((sum, game) => sum + this.playedOf(game), 0)
    );

    const completedGames = this.ownedGames.filter((game) => this.statusOf(game) === GameStatus.Completed).length;
    this.completionRate = this.ownedGames.length === 0
      ? 0
      : Math.round((completedGames / this.ownedGames.length) * 100);
    this.heroProgress = this.featuredGame ? this.progressOf(this.featuredGame) : 0;
    this.metrics = this.createMetrics(completedGames);
  }

  private createMetrics(completedGames: number): DashboardMetric[] {
    const ownedGames = this.ownedGames.length;
    const activeGames = this.playingGames.length;

    return [
      {
        label: 'Library',
        value: String(this.games.length),
        detail: `${ownedGames} in your collection`,
        icon: 'library-outline',
        tone: 'blue',
      },
      {
        label: 'Playing',
        value: String(activeGames),
        detail: activeGames === 1 ? 'active game' : 'active games',
        icon: 'game-controller-outline',
        tone: 'green',
      },
      {
        label: 'Completed',
        value: String(completedGames),
        detail: `${this.completionRate}% completion rate`,
        icon: 'checkmark-done-outline',
        tone: 'amber',
      },
      {
        label: 'Ahead',
        value: `${this.remainingHours}h`,
        detail: 'estimated backlog',
        icon: 'hourglass-outline',
        tone: 'rose',
      },
    ];
  }

  private getUpcomingReleases(): Game[] {
    const today = this.startOfToday();

    return this.games
      .filter((game) => this.releaseDateOf(game) >= today)
      .sort((a, b) => this.releaseDateOf(a).getTime() - this.releaseDateOf(b).getTime())
      .slice(0, 5);
  }

  private getBacklogGames(): Game[] {
    return this.ownedGames
      .filter((game) => {
        const status = this.statusOf(game);
        return status === GameStatus.Planned || status === GameStatus.OnHold || status === GameStatus.MainGame;
      })
      .sort((a, b) => this.remainingOf(b) - this.remainingOf(a))
      .slice(0, 4);
  }

  imageFor(game: Game | null): string {
    return game?.image?.uri ?? game?.image?.url ?? 'assets/png/Placeholder.png';
  }

  platformLabel(value: number | null | undefined): string {
    return Platforms.find((platform) => platform.value === value)?.label ?? 'Unknown';
  }

  statusLabel(game: Game): string {
    const labels: Record<number, string> = {
      [GameStatus.OnHold]: 'On hold',
      [GameStatus.Planned]: 'Planned',
      [GameStatus.Playing]: 'Playing',
      [GameStatus.StoryComplete]: 'Story complete',
      [GameStatus.Completed]: 'Completed',
      [GameStatus.MainGame]: 'Main game',
    };

    return labels[this.statusOf(game)] ?? 'Not started';
  }

  releaseLabel(game: Game): string {
    const date = this.releaseDateOf(game);

    if (Number.isNaN(date.getTime())) {
      return 'No release date';
    }

    return new Intl.DateTimeFormat('en', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    }).format(date);
  }

  daysUntil(game: Game): string {
    const date = this.releaseDateOf(game);
    const today = this.startOfToday();
    const days = Math.ceil((date.getTime() - today.getTime()) / 86400000);

    if (days <= 0) {
      return 'Today';
    }

    if (days === 1) {
      return 'Tomorrow';
    }

    return `${days} days`;
  }

  playedLabel(game: Game): string {
    return `${Math.round(this.playedOf(game))}h played`;
  }

  remainingLabel(game: Game): string {
    return `${Math.round(this.remainingOf(game))}h left`;
  }

  progressOf(game: Game): number {
    const estimated = Number(game.playtime) || 0;

    if (estimated <= 0) {
      return this.statusOf(game) === GameStatus.Completed ? 100 : 0;
    }

    return Math.min(100, Math.round((this.playedOf(game) / estimated) * 100));
  }

  trackByGameId(_: number, game: Game): number {
    return game.id;
  }

  private playedOf(game: Game): number {
    const manual = Number(game.myGames?.timeSpend) || 0;
    const tracked = Number(game.myGames?.myGameInfo?.trackedHours) || 0;
    return manual + tracked;
  }

  private remainingOf(game: Game): number {
    return Math.max(0, (Number(game.playtime) || 0) - this.playedOf(game));
  }

  private statusOf(game: Game): number {
    return Number(game.myGames?.status ?? -1);
  }

  private releaseDateOf(game: Game): Date {
    return game.releaseDate ? new Date(game.releaseDate) : new Date(Number.NaN);
  }

  private startOfToday(): Date {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return today;
  }

  private completeRefresh(event?: CustomEvent) {
    const target = event?.target as HTMLIonRefresherElement | undefined;
    target?.complete();
  }
}
