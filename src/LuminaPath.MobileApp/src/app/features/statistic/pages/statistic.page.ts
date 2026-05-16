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
  bookmarkOutline,
  calendarOutline,
  checkmarkCircleOutline,
  filmOutline,
  flameOutline,
  gameControllerOutline,
  hourglassOutline,
  layersOutline,
  pieChartOutline,
  pricetagOutline,
  pulseOutline,
  ribbonOutline,
  speedometerOutline,
  starOutline,
  timeOutline,
  trendingUpOutline,
  trophyOutline,
  tvOutline,
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
  rating: number | null;
  genre: string;
  startDate: Date | null;
  endDate: Date | null;
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
  consumedHours: number;
  completionRate: number;
  color: string;
  icon: string;
};

type HealthFlag = {
  title: string;
  detail: string;
  icon: string;
  tone: HealthTone;
};

type DonutSegment = {
  kind: StatisticKind;
  value: number;
  percent: number;
  length: number;
  gap: number;
  offset: number;
  color: string;
};

type RatingBucket = {
  rating: number;
  count: number;
  height: number;
  x: number;
  y: number;
};

type TrendPoint = {
  label: string;
  shortLabel: string;
  count: number;
  x: number;
  y: number;
};

type GenreSlice = {
  name: string;
  count: number;
  percent: number;
};

type TimeBar = {
  kind: StatisticKind;
  color: string;
  consumed: number;
  remaining: number;
  total: number;
  consumedPercent: number;
  remainingPercent: number;
};

type StatusSlice = {
  label: string;
  count: number;
  percent: number;
};

const DONUT_RADIUS = 46;
const DONUT_CIRCUMFERENCE = 2 * Math.PI * DONUT_RADIUS;
const TREND_MONTHS = 12;
const TREND_WIDTH = 320;
const TREND_HEIGHT = 110;
const TREND_PADDING_X = 14;
const TREND_PADDING_TOP = 14;
const TREND_PADDING_BOTTOM = 22;

const KIND_COLORS: Record<StatisticKind, string> = {
  Games: '#60a5fa',
  Anime: '#c084fc',
  Movies: '#fbbf24',
  Series: '#22d3ee',
};

const KIND_ICONS: Record<StatisticKind, string> = {
  Games: 'game-controller-outline',
  Anime: 'tv-outline',
  Movies: 'film-outline',
  Series: 'layers-outline',
};

const STATUS_ORDER = ['Planned', 'Active', 'On hold', 'Completed', 'Dropped'];

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
  topRated: BacklogItem[] = [];
  metrics: StatMetric[] = [];
  breakdown: KindBreakdown[] = [];
  flags: HealthFlag[] = [];
  healthScore = 100;
  healthTone: HealthTone = 'good';
  isLoading = true;
  errorMessage = '';

  donutSegments: DonutSegment[] = [];
  donutTotal = 0;
  ratingBuckets: RatingBucket[] = [];
  ratedCount = 0;
  averageRating = 0;
  trendPoints: TrendPoint[] = [];
  trendPolyline = '';
  trendArea = '';
  trendMax = 0;
  trendTotal = 0;
  trendThisMonth = 0;
  topGenres: GenreSlice[] = [];
  timeBars: TimeBar[] = [];
  timeMax = 0;
  statusSlices: StatusSlice[] = [];

  readonly donutRadius = DONUT_RADIUS;
  readonly donutCircumference = DONUT_CIRCUMFERENCE;
  readonly trendWidth = TREND_WIDTH;
  readonly trendHeight = TREND_HEIGHT;

  constructor(
    private readonly gameService: GameService,
    private readonly animeService: AnimeService,
    private readonly movieService: MovieService,
    private readonly seriesService: SeriesService,
  ) {
    addIcons({
      alertCircleOutline,
      barChartOutline,
      bookmarkOutline,
      calendarOutline,
      checkmarkCircleOutline,
      filmOutline,
      flameOutline,
      gameControllerOutline,
      hourglassOutline,
      layersOutline,
      pieChartOutline,
      pricetagOutline,
      pulseOutline,
      ribbonOutline,
      speedometerOutline,
      starOutline,
      timeOutline,
      trendingUpOutline,
      trophyOutline,
      tvOutline,
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

  trackBySegment(_: number, item: DonutSegment): string {
    return item.kind;
  }

  trackByBucket(_: number, item: RatingBucket): number {
    return item.rating;
  }

  trackByTrend(_: number, item: TrendPoint): string {
    return item.label;
  }

  trackByGenre(_: number, item: GenreSlice): string {
    return item.name;
  }

  trackByTimeBar(_: number, item: TimeBar): string {
    return item.kind;
  }

  trackByStatus(_: number, item: StatusSlice): string {
    return item.label;
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
    this.topRated = [...this.ownedItems]
      .filter((item) => item.rating != null && item.rating > 0)
      .sort((a, b) => (b.rating ?? 0) - (a.rating ?? 0))
      .slice(0, 5);

    const remainingHours = this.backlogItems.reduce((sum, item) => sum + item.remainingHours, 0);
    const estimatedHours = this.ownedItems.reduce((sum, item) => sum + item.estimatedHours, 0);
    const consumedHours = this.ownedItems.reduce((sum, item) => sum + item.consumedHours, 0);
    const completionRate = this.ownedItems.length ? Math.round((this.completedItems.length / this.ownedItems.length) * 100) : 0;

    this.healthScore = this.calculateHealthScore(remainingHours, completionRate);
    this.healthTone = this.healthScore >= 75 ? 'good' : this.healthScore >= 50 ? 'steady' : 'risk';

    this.breakdown = this.createBreakdown();
    this.donutSegments = this.createDonut();
    this.ratingBuckets = this.createRatingBuckets();
    this.computeTrend();
    this.topGenres = this.createGenres();
    this.timeBars = this.createTimeBars();
    this.statusSlices = this.createStatusSlices();
    this.averageRating = this.computeAverageRating();
    this.metrics = this.createMetrics(remainingHours, consumedHours, estimatedHours, completionRate);
    this.flags = this.createFlags(remainingHours, completionRate);
  }

  private createMetrics(remainingHours: number, consumedHours: number, estimatedHours: number, completionRate: number): StatMetric[] {
    const topGenre = this.topGenres[0]?.name ?? '—';
    return [
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
      {
        label: 'Avg Rating',
        value: this.ratedCount ? this.averageRating.toFixed(1) : '—',
        detail: this.ratedCount ? `${this.ratedCount} rated item${this.ratedCount === 1 ? '' : 's'}` : 'no ratings yet',
        icon: 'star-outline',
      },
      {
        label: 'Top Genre',
        value: topGenre,
        detail: this.topGenres[0] ? `${this.topGenres[0].count} item${this.topGenres[0].count === 1 ? '' : 's'}` : 'tag your library',
        icon: 'pricetag-outline',
      },
      {
        label: 'This Month',
        value: String(this.trendThisMonth),
        detail: this.trendThisMonth ? 'completed recently' : 'nothing wrapped yet',
        icon: 'trending-up-outline',
      },
    ];
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
      const remainingHours = owned.filter((item) => !item.completed && !item.dropped).reduce((sum, item) => sum + item.remainingHours, 0);
      const consumedHours = owned.reduce((sum, item) => sum + item.consumedHours, 0);
      return {
        kind,
        owned: owned.length,
        active: owned.filter((item) => item.active).length,
        completed: completed.length,
        remainingHours,
        consumedHours,
        completionRate: owned.length ? Math.round((completed.length / owned.length) * 100) : 0,
        color: KIND_COLORS[kind],
        icon: KIND_ICONS[kind],
      };
    });
  }

  private createDonut(): DonutSegment[] {
    const total = this.ownedItems.length;
    this.donutTotal = total;
    if (!total) {
      return [];
    }
    let cursor = 0;
    return this.breakdown
      .filter((row) => row.owned > 0)
      .map((row) => {
        const percent = row.owned / total;
        const length = percent * DONUT_CIRCUMFERENCE;
        const segment: DonutSegment = {
          kind: row.kind,
          value: row.owned,
          percent: Math.round(percent * 100),
          length,
          gap: DONUT_CIRCUMFERENCE - length,
          offset: -cursor,
          color: row.color,
        };
        cursor += length;
        return segment;
      });
  }

  private createRatingBuckets(): RatingBucket[] {
    const rated = this.ownedItems.filter((item) => item.rating != null && item.rating > 0);
    this.ratedCount = rated.length;
    const counts = new Array(10).fill(0);
    for (const item of rated) {
      const bucket = Math.min(10, Math.max(1, Math.round(item.rating ?? 0)));
      counts[bucket - 1] += 1;
    }
    const max = Math.max(1, ...counts);
    const chartHeight = 70;
    const barWidth = 18;
    const barGap = 6;
    return counts.map((count, index) => {
      const height = (count / max) * chartHeight;
      return {
        rating: index + 1,
        count,
        height,
        x: index * (barWidth + barGap) + 4,
        y: chartHeight + 4 - height,
      };
    });
  }

  private computeTrend(): void {
    const now = new Date();
    const months: TrendPoint[] = [];
    const buckets = new Map<string, number>();
    for (const item of this.completedItems) {
      const date = item.endDate ?? item.startDate;
      if (!date) {
        continue;
      }
      const key = `${date.getFullYear()}-${date.getMonth()}`;
      buckets.set(key, (buckets.get(key) ?? 0) + 1);
    }
    for (let i = TREND_MONTHS - 1; i >= 0; i--) {
      const date = new Date(now.getFullYear(), now.getMonth() - i, 1);
      const key = `${date.getFullYear()}-${date.getMonth()}`;
      const count = buckets.get(key) ?? 0;
      months.push({
        label: date.toLocaleString(undefined, { month: 'short', year: 'numeric' }),
        shortLabel: date.toLocaleString(undefined, { month: 'short' }),
        count,
        x: 0,
        y: 0,
      });
    }
    const max = Math.max(1, ...months.map((m) => m.count));
    const usableW = TREND_WIDTH - TREND_PADDING_X * 2;
    const usableH = TREND_HEIGHT - TREND_PADDING_TOP - TREND_PADDING_BOTTOM;
    const step = months.length > 1 ? usableW / (months.length - 1) : 0;
    months.forEach((point, index) => {
      point.x = TREND_PADDING_X + step * index;
      point.y = TREND_PADDING_TOP + usableH - (point.count / max) * usableH;
    });
    this.trendPoints = months;
    this.trendMax = max;
    this.trendTotal = months.reduce((sum, p) => sum + p.count, 0);
    this.trendThisMonth = months[months.length - 1]?.count ?? 0;
    this.trendPolyline = months.map((p) => `${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ');
    if (months.length) {
      const baseline = TREND_HEIGHT - TREND_PADDING_BOTTOM;
      const first = months[0];
      const last = months[months.length - 1];
      const pathParts: string[] = [`M ${first.x.toFixed(1)} ${baseline.toFixed(1)}`];
      months.forEach((p) => pathParts.push(`L ${p.x.toFixed(1)} ${p.y.toFixed(1)}`));
      pathParts.push(`L ${last.x.toFixed(1)} ${baseline.toFixed(1)}`);
      pathParts.push('Z');
      this.trendArea = pathParts.join(' ');
    } else {
      this.trendArea = '';
    }
  }

  private createGenres(): GenreSlice[] {
    const counts = new Map<string, number>();
    for (const item of this.ownedItems) {
      const tokens = (item.genre ?? '')
        .split(/[,;|/]/)
        .map((token) => token.trim())
        .filter(Boolean);
      for (const token of tokens) {
        const key = token.toLowerCase();
        counts.set(key, (counts.get(key) ?? 0) + 1);
      }
    }
    const entries = Array.from(counts.entries())
      .map(([key, count]) => ({ name: this.toTitleCase(key), count }))
      .sort((a, b) => b.count - a.count)
      .slice(0, 5);
    const max = entries[0]?.count ?? 1;
    return entries.map((entry) => ({ ...entry, percent: Math.round((entry.count / max) * 100) }));
  }

  private createTimeBars(): TimeBar[] {
    const bars = this.breakdown
      .filter((row) => row.owned > 0)
      .map((row) => {
        const total = row.consumedHours + row.remainingHours;
        return {
          kind: row.kind,
          color: row.color,
          consumed: row.consumedHours,
          remaining: row.remainingHours,
          total,
          consumedPercent: 0,
          remainingPercent: 0,
        };
      });
    const max = Math.max(1, ...bars.map((bar) => bar.total));
    this.timeMax = max;
    return bars.map((bar) => ({
      ...bar,
      consumedPercent: (bar.consumed / max) * 100,
      remainingPercent: (bar.remaining / max) * 100,
    }));
  }

  private createStatusSlices(): StatusSlice[] {
    const counts = new Map<string, number>();
    for (const item of this.ownedItems) {
      const key = this.normalizeStatus(item.statusLabel);
      counts.set(key, (counts.get(key) ?? 0) + 1);
    }
    const total = this.ownedItems.length;
    return STATUS_ORDER
      .filter((label) => (counts.get(label) ?? 0) > 0)
      .map((label) => ({
        label,
        count: counts.get(label) ?? 0,
        percent: total ? Math.round(((counts.get(label) ?? 0) / total) * 100) : 0,
      }));
  }

  private normalizeStatus(raw: string): string {
    if (raw === 'Playing' || raw === 'Watching') return 'Active';
    if (raw === 'Story complete' || raw === 'Main game' || raw === 'Completed') return 'Completed';
    if (raw === 'On hold') return 'On hold';
    if (raw === 'Dropped') return 'Dropped';
    if (raw === 'Planned') return 'Planned';
    return raw;
  }

  private computeAverageRating(): number {
    const rated = this.ownedItems.filter((item) => item.rating != null && item.rating > 0);
    if (!rated.length) {
      return 0;
    }
    const sum = rated.reduce((acc, item) => acc + (item.rating ?? 0), 0);
    return sum / rated.length;
  }

  statusClass(label: string): string {
    return label.toLowerCase().replace(/\s+/g, '-');
  }

  private toTitleCase(value: string): string {
    return value
      .split(' ')
      .map((part) => (part.length ? part[0].toUpperCase() + part.slice(1) : part))
      .join(' ');
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
      rating: game.myGames?.rating ?? null,
      genre: game.genre ?? '',
      startDate: this.toDate(game.myGames?.startDate),
      endDate: this.toDate(game.myGames?.endDate),
    });
  }

  private fromAnime(anime: Anime): BacklogItem {
    return this.fromWatchItem(
      'Anime',
      anime.id,
      anime.name,
      anime.myAnimes?.status,
      anime.expectedWatchTimeMinutes,
      anime.myAnimes?.currentWatchTimeMinutes,
      anime.myAnimes?.rating ?? null,
      anime.genre ?? '',
      this.toDate(anime.myAnimes?.startDate),
      this.toDate(anime.myAnimes?.endDate),
    );
  }

  private fromMovie(movie: Movie): BacklogItem {
    return this.fromWatchItem(
      'Movies',
      movie.id,
      movie.name,
      movie.myMovies?.status,
      movie.expectedWatchTimeMinutes,
      movie.myMovies?.currentWatchTimeMinutes,
      movie.myMovies?.rating ?? null,
      movie.genre ?? '',
      this.toDate(movie.myMovies?.startDate),
      this.toDate(movie.myMovies?.endDate),
    );
  }

  private fromSeries(series: Series): BacklogItem {
    return this.fromWatchItem(
      'Series',
      series.id,
      series.name,
      series.mySeries?.status,
      series.expectedWatchTimeMinutes,
      series.mySeries?.currentWatchTimeMinutes,
      series.mySeries?.rating ?? null,
      series.genre ?? '',
      this.toDate(series.mySeries?.startDate),
      this.toDate(series.mySeries?.endDate),
    );
  }

  private fromWatchItem(
    kind: StatisticKind,
    id: number,
    name: string,
    statusValue: number | null | undefined,
    expectedMinutes: number | null | undefined,
    watchedMinutes: number | null | undefined,
    rating: number | null,
    genre: string,
    startDate: Date | null,
    endDate: Date | null,
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
      rating,
      genre,
      startDate,
      endDate,
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
    rating: number | null;
    genre: string;
    startDate: Date | null;
    endDate: Date | null;
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
      rating: input.rating != null ? Number(input.rating) : null,
      genre: input.genre ?? '',
      startDate: input.startDate,
      endDate: input.endDate,
    };
  }

  private toDate(value: Date | string | null | undefined): Date | null {
    if (!value) {
      return null;
    }
    const date = value instanceof Date ? value : new Date(value);
    return isNaN(date.getTime()) ? null : date;
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
