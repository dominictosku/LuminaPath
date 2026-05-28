import { Component, OnInit, ViewEncapsulation, inject } from '@angular/core';
import {
  IonContent,
  IonIcon,
  IonRefresher,
  IonRefresherContent,
} from '@ionic/angular/standalone';
// `addIcons` populates Ionic's global icon registry. Sub-components only
// declare which icons they render, so the page is responsible for ensuring
// every name used by any descendant template (including ones bound from
// data like `StatMetric.icon` and `HealthFlag.icon`) is registered here.
import { catchError, forkJoin, of } from 'rxjs';

import { MediaFilter } from 'src/app/core/entities/mediaFilter';
import { AnimeService } from '../../animes/services/anime.service';
import { GameService } from '../../games/services/game.service';
import { MovieService } from '../../movies/services/movie.service';
import { SeriesService } from '../../series/services/series.service';
import {
  BacklogItem,
  DonutSegment,
  GenreSlice,
  HealthFlag,
  HealthTone,
  KindBreakdown,
  RatingBucket,
  StatMetric,
  StatusSlice,
  TimeBar,
  TrendData,
} from '../models/statistic.model';
import { fromAnime, fromGame, fromMovie, fromSeries } from '../domain/statistic.mappers';
import {
  computeBreakdown,
  computeDonut,
  computeFlags,
  computeGenres,
  computeHealth,
  computeMetrics,
  computeRatingBuckets,
  computeStatusSlices,
  computeTimeBars,
  computeTrend,
} from '../domain/statistic.computations';
import { StatisticHeroComponent } from '../components/statistic-hero/statistic-hero.component';
import { StatisticMetricsComponent } from '../components/statistic-metrics/statistic-metrics.component';
import { StatisticMediaMixComponent } from '../components/statistic-media-mix/statistic-media-mix.component';
import { StatisticCompletionTrendComponent } from '../components/statistic-completion-trend/statistic-completion-trend.component';
import { StatisticRatingHistogramComponent } from '../components/statistic-rating-histogram/statistic-rating-histogram.component';
import { StatisticTimeByTypeComponent } from '../components/statistic-time-by-type/statistic-time-by-type.component';
import { StatisticTopGenresComponent } from '../components/statistic-top-genres/statistic-top-genres.component';
import { StatisticStatusMixComponent } from '../components/statistic-status-mix/statistic-status-mix.component';
import { StatisticHealthFlagsComponent } from '../components/statistic-health-flags/statistic-health-flags.component';
import { StatisticQuickWinsComponent } from '../components/statistic-quick-wins/statistic-quick-wins.component';
import { StatisticLongestCommitmentsComponent } from '../components/statistic-longest-commitments/statistic-longest-commitments.component';
import { StatisticTopRatedComponent } from '../components/statistic-top-rated/statistic-top-rated.component';
import { StatisticBreakdownComponent } from '../components/statistic-breakdown/statistic-breakdown.component';
import { StatisticPsnTrophiesComponent } from '../components/statistic-psn-trophies/statistic-psn-trophies.component';
import { RequestCache } from 'src/app/shared/services/request-cache.service';
import { PsnTrophyTotals, StatisticService } from '../services/statistic.service';

const STATISTIC_CACHE_KEY = 'statistic:items';
const PSN_TROPHY_CACHE_KEY = 'statistic:psn-trophies';

@Component({
  selector: 'app-statistic',
  templateUrl: './statistic.page.html',
  styleUrls: ['./statistic.page.scss'],
  // The statistic page ships a tightly themed visual system with selectors like
  // `.hero`, `.metric`, `.section-block`, `.legend`, `.stat-item`, etc. The 13
  // sub-components below use those class names, so loading the page SCSS
  // globally on this route avoids both duplication and per-component scoping
  // wrappers. Styles are added on activation and removed on destroy.
  encapsulation: ViewEncapsulation.None,
  imports: [
    IonContent,
    IonIcon,
    IonRefresher,
    IonRefresherContent,
    StatisticHeroComponent,
    StatisticMetricsComponent,
    StatisticMediaMixComponent,
    StatisticCompletionTrendComponent,
    StatisticRatingHistogramComponent,
    StatisticTimeByTypeComponent,
    StatisticTopGenresComponent,
    StatisticStatusMixComponent,
    StatisticHealthFlagsComponent,
    StatisticQuickWinsComponent,
    StatisticLongestCommitmentsComponent,
    StatisticTopRatedComponent,
    StatisticBreakdownComponent,
    StatisticPsnTrophiesComponent,
  ],
})
export class StatisticPage implements OnInit {
  private readonly gameService = inject(GameService);
  private readonly animeService = inject(AnimeService);
  private readonly movieService = inject(MovieService);
  private readonly seriesService = inject(SeriesService);
  private readonly statisticService = inject(StatisticService);
  private readonly cache = inject(RequestCache);

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
  trend: TrendData = { points: [], polyline: '', area: '', max: 0, total: 0, thisMonth: 0 };
  topGenres: GenreSlice[] = [];
  timeBars: TimeBar[] = [];
  statusSlices: StatusSlice[] = [];

  /**
   * PSN trophy totals — loaded in parallel with the catalogue data so
   * the bottom panel never gates the rest of the page. Null until the
   * first response (the panel is hidden in that window).
   */
  psnTrophyTotals: PsnTrophyTotals | null = null;

  constructor() {
    // Only the page's own "error" notice uses an icon here; sub-components
    // register their own icons in their constructors.
  }

  ngOnInit(): void {
    this.loadStatistic();
    this.loadPsnTrophies();
  }

  /**
   * Independent of the catalogue load — runs in parallel and updates the
   * trophy panel on its own. Cached so subsequent visits paint instantly.
   * Failures are silent because PSN totals are nice-to-have, not blocking.
   */
  private loadPsnTrophies(event?: CustomEvent): void {
    const cached = this.cache.get<PsnTrophyTotals>(PSN_TROPHY_CACHE_KEY);
    if (cached) {
      this.psnTrophyTotals = cached;
      if (!event && this.cache.isFresh(PSN_TROPHY_CACHE_KEY)) return;
    }

    this.statisticService.getPsnTrophyTotals().subscribe({
      next: (totals) => {
        this.psnTrophyTotals = totals;
        this.cache.set(PSN_TROPHY_CACHE_KEY, totals);
      },
      error: () => {
        // Leave whatever we had cached; if nothing was cached, the
        // panel just stays hidden. PSN sync may not be set up yet —
        // not an error worth surfacing on the page.
        if (!cached) this.psnTrophyTotals = null;
      },
    });
  }

  loadStatistic(event?: CustomEvent): void {
    // Pull-to-refresh should re-fetch the trophy totals too, otherwise
    // a user who just synced PSN won't see the new counts until they
    // navigate away and back.
    if (event) this.loadPsnTrophies(event);
    // Stale-while-revalidate: paint cached items immediately so repeat
    // visits don't flash skeletons across 13 sub-components.
    const cached = this.cache.get<BacklogItem[]>(STATISTIC_CACHE_KEY);
    if (cached) {
      this.items = cached;
      this.buildStatistics();
      this.isLoading = false;
      if (!event && this.cache.isFresh(STATISTIC_CACHE_KEY)) {
        return;
      }
    } else {
      this.isLoading = !event;
    }
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
          ...(games.data ?? []).map(fromGame),
          ...(animes.data ?? []).map(fromAnime),
          ...(movies.data ?? []).map(fromMovie),
          ...(series.data ?? []).map(fromSeries),
        ];
        this.cache.set(STATISTIC_CACHE_KEY, this.items);
        this.buildStatistics();
        this.isLoading = false;
        this.completeRefresh(event);
      },
      error: () => {
        // Keep the cached paint if we had one; only wipe on a true cold
        // error. Suppress the error text when cached data is still on
        // screen — the OfflineBanner covers it and a second alarm reads
        // as duplicate noise.
        if (!cached) {
          this.items = [];
          this.buildStatistics();
          this.errorMessage = 'Statistic data could not be loaded.';
        }
        this.isLoading = false;
        this.completeRefresh(event);
      },
    });
  }

  /**
   * Recompute every derived dataset. Order matters: breakdown feeds the donut
   * and time bars; trend feeds the metrics card; everything funnels into the
   * final metrics + flags so they pick up the latest counts.
   */
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
    const completionRate = this.ownedItems.length
      ? Math.round((this.completedItems.length / this.ownedItems.length) * 100)
      : 0;

    const health = computeHealth(this.ownedItems, this.activeItems, this.backlogItems, this.quickWins, remainingHours, completionRate);
    this.healthScore = health.score;
    this.healthTone = health.tone;

    this.breakdown = computeBreakdown(this.ownedItems);
    const donut = computeDonut(this.breakdown, this.ownedItems.length);
    this.donutSegments = donut.segments;
    this.donutTotal = donut.total;

    const ratings = computeRatingBuckets(this.ownedItems);
    this.ratingBuckets = ratings.buckets;
    this.ratedCount = ratings.count;
    this.averageRating = ratings.average;

    this.trend = computeTrend(this.completedItems);
    this.topGenres = computeGenres(this.ownedItems);
    this.timeBars = computeTimeBars(this.breakdown);
    this.statusSlices = computeStatusSlices(this.ownedItems);

    this.metrics = computeMetrics({
      healthScore: this.healthScore,
      remainingHours,
      consumedHours,
      estimatedHours,
      completionRate,
      ownedCount: this.ownedItems.length,
      completedCount: this.completedItems.length,
      activeCount: this.activeItems.length,
      backlogCount: this.backlogItems.length,
      topGenre: this.topGenres[0],
      ratedCount: this.ratedCount,
      averageRating: this.averageRating,
      trendThisMonth: this.trend.thisMonth,
    });

    this.flags = computeFlags(this.activeItems, this.backlogItems, this.quickWins, remainingHours, completionRate);
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
