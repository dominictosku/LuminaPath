import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import {
  IonBadge,
  IonContent,
  IonIcon,
  IonProgressBar,
  IonRefresher,
  IonRefresherContent,
  IonSkeletonText,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  alertCircleOutline,
  barChartOutline,
  checkmarkCircleOutline,
  flameOutline,
  gameControllerOutline,
  hourglassOutline,
  pulseOutline,
  speedometerOutline,
  timeOutline,
  trophyOutline,
} from 'ionicons/icons';
import { catchError, forkJoin, of } from 'rxjs';

import { MediaFilter } from 'src/app/core/entities/mediaFilter';
import { Anime } from '../../animes/models/animes.model';
import { AnimeService } from '../../animes/services/anime.service';
import { Game } from '../../games/models/games.model';
import { GameService } from '../../games/services/game.service';
import { Movie } from '../../movies/models/movies.model';
import { MovieService } from '../../movies/services/movie.service';
import { Series } from '../../series/models/series.model';
import { SeriesService } from '../../series/services/series.service';

type StatisticKind = 'Games' | 'Anime' | 'Movies' | 'Series';
type HealthTone = 'good' | 'steady' | 'amber' | 'risk';

type BacklogItem = {
  id: number;
  kind: StatisticKind;
  name: string;
  status: number;
  statusLabel: string;
  estimatedHours: number;
  consumedHours: number;
  remainingHours: number;
  completed: boolean;
  active: boolean;
  dropped: boolean;
};

type StatMetric = {
  label: string;
  value: string;
  detail: string;
  icon: string;
};

type KindBreakdown = {
  kind: StatisticKind;
  owned: number;
  active: number;
  completed: number;
  remainingHours: number;
  completionRate: number;
};

type HealthFlag = {
  title: string;
  detail: string;
  icon: string;
  tone: HealthTone;
};

@Component({
  selector: 'app-statistic',
  templateUrl: './statistic.page.html',
  styleUrls: ['./statistic.page.scss'],
  imports: [
    CommonModule,
    IonBadge,
    IonContent,
    IonIcon,
    IonProgressBar,
    IonRefresher,
    IonRefresherContent,
    IonSkeletonText,
  ],
})
export class StatisticPage implements OnInit {
  items: BacklogItem[] = [];
  ownedItems: BacklogItem[] = [];
  backlogItems: BacklogItem[] = [];
  activeItems: BacklogItem[] = [];
  completedItems: BacklogItem[] = [];
  quickWins: BacklogItem[] = [];
  longCommitments: BacklogItem[] = [];
  metrics: StatMetric[] = [];
  breakdown: KindBreakdown[] = [];
  flags: HealthFlag[] = [];
  healthScore = 100;
  healthTone: HealthTone = 'good';
  isLoading = true;
  errorMessage = '';

  constructor(
    private readonly gameService: GameService,
    private readonly animeService: AnimeService,
    private readonly movieService: MovieService,
    private readonly seriesService: SeriesService,
  ) {
    addIcons({
      alertCircleOutline,
      barChartOutline,
      checkmarkCircleOutline,
      flameOutline,
      gameControllerOutline,
      hourglassOutline,
      pulseOutline,
      speedometerOutline,
      timeOutline,
      trophyOutline,
    });
  }

  ngOnInit(): void {
    this.loadStatistic();
  }

  loadStatistic(event?: CustomEvent): void {
    this.isLoading = !event;
    this.errorMessage = '';
    const filter = this.createLargeFilter();

    forkJoin({
      games: this.gameService.getAll(filter).pipe(catchError(() => of({ data: [] }))),
      animes: this.animeService.getAll(filter).pipe(catchError(() => of({ data: [] }))),
      movies: this.movieService.getAll(filter).pipe(catchError(() => of({ data: [] }))),
      series: this.seriesService.getAll(filter).pipe(catchError(() => of({ data: [] }))),
    }).subscribe({
      next: ({ games, animes, movies, series }) => {
        this.items = [
          ...(games.data ?? []).map((game) => this.fromGame(game)),
          ...(animes.data ?? []).map((anime) => this.fromAnime(anime)),
          ...(movies.data ?? []).map((movie) => this.fromMovie(movie)),
          ...(series.data ?? []).map((show) => this.fromSeries(show)),
        ];
        this.buildStatistics();
        this.isLoading = false;
        this.completeRefresh(event);
      },
      error: () => {
        this.items = [];
        this.buildStatistics();
        this.errorMessage = 'Statistic data could not be loaded.';
        this.isLoading = false;
        this.completeRefresh(event);
      },
    });
  }

  trackByMetric(_: number, metric: StatMetric): string {
    return metric.label;
  }

  trackByItem(_: number, item: BacklogItem): string {
    return `${item.kind}-${item.id}`;
  }

  trackByBreakdown(_: number, item: KindBreakdown): string {
    return item.kind;
  }

  trackByFlag(_: number, item: HealthFlag): string {
    return item.title;
  }

  formatHours(value: number): string {
    return `${Math.round(value)}h`;
  }

  private buildStatistics(): void {
    this.ownedItems = this.items.filter((item) => item.status >= 0);
    this.completedItems = this.ownedItems.filter((item) => item.completed);
    this.activeItems = this.ownedItems.filter((item) => item.active);
    this.backlogItems = this.ownedItems.filter((item) => !item.completed && !item.dropped && item.remainingHours > 0);
    this.quickWins = [...this.backlogItems]
      .filter((item) => item.remainingHours <= 12)
      .sort((a, b) => a.remainingHours - b.remainingHours)
      .slice(0, 5);
    this.longCommitments = [...this.backlogItems]
      .sort((a, b) => b.remainingHours - a.remainingHours)
      .slice(0, 5);

    const remainingHours = this.backlogItems.reduce((sum, item) => sum + item.remainingHours, 0);
    const estimatedHours = this.ownedItems.reduce((sum, item) => sum + item.estimatedHours, 0);
    const consumedHours = this.ownedItems.reduce((sum, item) => sum + item.consumedHours, 0);
    const completionRate = this.ownedItems.length ? Math.round((this.completedItems.length / this.ownedItems.length) * 100) : 0;

    this.healthScore = this.calculateHealthScore(remainingHours, completionRate);
    this.healthTone = this.healthScore >= 75 ? 'good' : this.healthScore >= 50 ? 'steady' : 'risk';
    this.metrics = [
      {
        label: 'Health',
        value: String(this.healthScore),
        detail: this.healthScore >= 75 ? 'balanced backlog' : this.healthScore >= 50 ? 'needs some pruning' : 'too much pressure',
        icon: 'speedometer-outline',
      },
      {
        label: 'Remaining',
        value: this.formatHours(remainingHours),
        detail: `${this.backlogItems.length} open commitment${this.backlogItems.length === 1 ? '' : 's'}`,
        icon: 'hourglass-outline',
      },
      {
        label: 'Active',
        value: String(this.activeItems.length),
        detail: 'currently in progress',
        icon: 'pulse-outline',
      },
      {
        label: 'Complete',
        value: `${completionRate}%`,
        detail: `${this.completedItems.length}/${this.ownedItems.length} finished`,
        icon: 'trophy-outline',
      },
      {
        label: 'Logged',
        value: this.formatHours(consumedHours),
        detail: `${this.formatHours(estimatedHours)} tracked estimate`,
        icon: 'time-outline',
      },
    ];
    this.breakdown = this.createBreakdown();
    this.flags = this.createFlags(remainingHours, completionRate);
  }

  private calculateHealthScore(remainingHours: number, completionRate: number): number {
    let score = 100;
    score -= Math.min(28, Math.max(0, this.activeItems.length - 3) * 7);
    score -= Math.min(30, Math.max(0, remainingHours - 120) / 10);
    if (this.ownedItems.length >= 5 && completionRate < 30) {
      score -= 18;
    }
    if (!this.quickWins.length && this.backlogItems.length) {
      score -= 8;
    }
    if (!this.activeItems.length && this.backlogItems.length) {
      score -= 8;
    }
    return Math.max(0, Math.min(100, Math.round(score)));
  }

  private createBreakdown(): KindBreakdown[] {
    const kinds: StatisticKind[] = ['Games', 'Anime', 'Movies', 'Series'];
    return kinds.map((kind) => {
      const owned = this.ownedItems.filter((item) => item.kind === kind);
      const completed = owned.filter((item) => item.completed);
      return {
        kind,
        owned: owned.length,
        active: owned.filter((item) => item.active).length,
        completed: completed.length,
        remainingHours: owned.filter((item) => !item.completed && !item.dropped).reduce((sum, item) => sum + item.remainingHours, 0),
        completionRate: owned.length ? Math.round((completed.length / owned.length) * 100) : 0,
      };
    });
  }

  private createFlags(remainingHours: number, completionRate: number): HealthFlag[] {
    const flags: HealthFlag[] = [];

    flags.push({
      title: this.activeItems.length > 3 ? 'Active list is crowded' : 'Active list is focused',
      detail: `${this.activeItems.length} active item${this.activeItems.length === 1 ? '' : 's'}`,
      icon: this.activeItems.length > 3 ? 'alert-circle-outline' : 'checkmark-circle-outline',
      tone: this.activeItems.length > 3 ? 'amber' : 'good',
    });

    flags.push({
      title: remainingHours > 180 ? 'Backlog pressure is high' : 'Backlog pressure is readable',
      detail: `${this.formatHours(remainingHours)} remaining`,
      icon: remainingHours > 180 ? 'flame-outline' : 'checkmark-circle-outline',
      tone: remainingHours > 180 ? 'risk' : 'good',
    });

    flags.push({
      title: this.quickWins.length ? 'Quick wins available' : 'No short wins found',
      detail: this.quickWins.length ? `${this.quickWins.length} item${this.quickWins.length === 1 ? '' : 's'} under 12h` : 'Add shorter items or finish a larger one',
      icon: this.quickWins.length ? 'time-outline' : 'alert-circle-outline',
      tone: this.quickWins.length ? 'good' : 'steady',
    });

    flags.push({
      title: completionRate >= 40 ? 'Completion pace looks healthy' : 'Completion pace is still warming up',
      detail: `${completionRate}% completion rate`,
      icon: completionRate >= 40 ? 'trophy-outline' : 'pulse-outline',
      tone: completionRate >= 40 ? 'good' : 'steady',
    });

    return flags;
  }

  private fromGame(game: Game): BacklogItem {
    const status = Number(game.myGames?.status ?? -1);
    const estimatedHours = Number(game.playtime) || 0;
    const consumedHours = (Number(game.myGames?.timeSpend) || 0) + (Number(game.myGames?.myGameInfo?.trackedHours) || 0);
    return this.createItem({
      id: game.id,
      kind: 'Games',
      name: game.name,
      status,
      estimatedHours,
      consumedHours,
      completedStatus: 4,
      activeStatus: 2,
      droppedStatus: null,
      statusLabel: this.gameStatusLabel(status),
    });
  }

  private fromAnime(anime: Anime): BacklogItem {
    return this.fromWatchItem('Anime', anime.id, anime.name, anime.myAnimes?.status, anime.expectedWatchTimeMinutes, anime.myAnimes?.currentWatchTimeMinutes);
  }

  private fromMovie(movie: Movie): BacklogItem {
    return this.fromWatchItem('Movies', movie.id, movie.name, movie.myMovies?.status, movie.expectedWatchTimeMinutes, movie.myMovies?.currentWatchTimeMinutes);
  }

  private fromSeries(series: Series): BacklogItem {
    return this.fromWatchItem('Series', series.id, series.name, series.mySeries?.status, series.expectedWatchTimeMinutes, series.mySeries?.currentWatchTimeMinutes);
  }

  private fromWatchItem(
    kind: StatisticKind,
    id: number,
    name: string,
    statusValue: number | null | undefined,
    expectedMinutes: number | null | undefined,
    watchedMinutes: number | null | undefined,
  ): BacklogItem {
    const status = Number(statusValue ?? -1);
    return this.createItem({
      id,
      kind,
      name,
      status,
      estimatedHours: (Number(expectedMinutes) || 0) / 60,
      consumedHours: (Number(watchedMinutes) || 0) / 60,
      completedStatus: 3,
      activeStatus: 2,
      droppedStatus: 4,
      statusLabel: this.watchStatusLabel(status),
    });
  }

  private createItem(input: {
    id: number;
    kind: StatisticKind;
    name: string;
    status: number;
    estimatedHours: number;
    consumedHours: number;
    completedStatus: number;
    activeStatus: number;
    droppedStatus: number | null;
    statusLabel: string;
  }): BacklogItem {
    return {
      id: input.id,
      kind: input.kind,
      name: input.name,
      status: input.status,
      statusLabel: input.statusLabel,
      estimatedHours: input.estimatedHours,
      consumedHours: input.consumedHours,
      remainingHours: Math.max(0, input.estimatedHours - input.consumedHours),
      completed: input.status === input.completedStatus,
      active: input.status === input.activeStatus,
      dropped: input.droppedStatus != null && input.status === input.droppedStatus,
    };
  }

  private gameStatusLabel(status: number): string {
    return {
      0: 'On hold',
      1: 'Planned',
      2: 'Playing',
      3: 'Story complete',
      4: 'Completed',
      5: 'Main game',
    }[status] ?? 'Catalog';
  }

  private watchStatusLabel(status: number): string {
    return {
      0: 'On hold',
      1: 'Planned',
      2: 'Watching',
      3: 'Completed',
      4: 'Dropped',
    }[status] ?? 'Catalog';
  }

  private createLargeFilter(): MediaFilter {
    const filter = new MediaFilter();
    filter.Paging.PageIndex = 1;
    filter.Paging.Count = 500;
    return filter;
  }

  private completeRefresh(event?: CustomEvent): void {
    const target = event?.target as HTMLIonRefresherElement | undefined;
    target?.complete();
  }
}
