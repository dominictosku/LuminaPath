import { Component, OnInit, inject } from '@angular/core';

import { catchError, forkJoin, from, of } from 'rxjs';
import {
  IonContent,
  IonIcon,
  IonRefresher,
  IonRefresherContent,
} from '@ionic/angular/standalone';
import { Game } from '../../games/models/games.model';
import { GameService } from '../../games/services/game.service';
import { Anime } from '../../animes/models/animes.model';
import { AnimeService } from '../../animes/services/anime.service';
import { Movie } from '../../movies/models/movies.model';
import { MovieService } from '../../movies/services/movie.service';
import { Series } from '../../series/models/series.model';
import { SeriesService } from '../../series/services/series.service';
import { GamingSession, GamingSessionService } from '../../planning/services/gaming-session.service';
import { QuestBoardService, QuestBoardState } from '../../quests/services/quest-board.service';
import { MediaFilter } from 'src/app/core/entities/mediaFilter';
import { PaginateResult } from 'src/app/core/entities/paginatedResult';
import {
  DashboardActivityItem,
  DashboardFocusItem,
  DashboardMediaItem,
  DashboardMetric,
} from '../models/dashboard.model';
import { addDays, startOfDay } from 'src/app/shared/utils/date-helpers';
import { progressOf } from '../domain/dashboard-view.helpers';
import { fromAnime, fromGame, fromMovie, fromSeries } from '../domain/dashboard.mappers';
import {
  buildActivityItems,
  buildFocusItems,
  buildMetrics,
  completionRateFor,
  getBacklogItems,
  getUpcomingReleases,
  mostRecentItems,
  pickFeaturedItem,
  sumPlayedHours,
  sumRemainingHours,
  topPlayingItems,
} from '../domain/dashboard.derivations';
import { RequestCache } from 'src/app/shared/services/request-cache.service';
import { DashboardHeroComponent } from '../components/dashboard-hero/dashboard-hero.component';
import { DashboardMetricsComponent } from '../components/dashboard-metrics/dashboard-metrics.component';
import { DashboardMomentumComponent } from '../components/dashboard-momentum/dashboard-momentum.component';
import { DashboardFocusComponent } from '../components/dashboard-focus/dashboard-focus.component';
import { DashboardActivityComponent } from '../components/dashboard-activity/dashboard-activity.component';
import { DashboardActiveRailComponent } from '../components/dashboard-active-rail/dashboard-active-rail.component';
import { DashboardReleasesComponent } from '../components/dashboard-releases/dashboard-releases.component';
import { DashboardBacklogComponent } from '../components/dashboard-backlog/dashboard-backlog.component';
import { DashboardRecentComponent } from '../components/dashboard-recent/dashboard-recent.component';
import { ReleaseCalendarComponent } from '../../release-calendar/components/release-calendar.component';

/** Raw fetched data for the dashboard. Stored in RequestCache so repeat
 *  visits paint immediately, then refetch in the background. */
type DashboardSnapshot = {
  games: Game[];
  animes: Anime[];
  movies: Movie[];
  series: Series[];
  releasePlanGames: Game[];
  libraryTotal: number;
  sessions: GamingSession[];
  questBoard: QuestBoardState | null;
};

const DASHBOARD_CACHE_KEY = 'home:dashboard';

function emptyDashboardSnapshot(): DashboardSnapshot {
  return {
    games: [],
    animes: [],
    movies: [],
    series: [],
    releasePlanGames: [],
    libraryTotal: 0,
    sessions: [],
    questBoard: null,
  };
}

function emptyPage<T>(): PaginateResult<T> {
  return new PaginateResult<T>();
}

@Component({
  selector: 'app-home',
  templateUrl: './home.page.html',
  styleUrls: ['./home.page.scss'],
  imports: [
    IonContent,
    IonIcon,
    IonRefresher,
    IonRefresherContent,
    DashboardHeroComponent,
    DashboardMetricsComponent,
    DashboardMomentumComponent,
    DashboardFocusComponent,
    DashboardActivityComponent,
    DashboardActiveRailComponent,
    DashboardReleasesComponent,
    DashboardBacklogComponent,
    DashboardRecentComponent,
    ReleaseCalendarComponent,
  ],
})
export class HomePage implements OnInit {
  private gameService = inject(GameService);
  private animeService = inject(AnimeService);
  private movieService = inject(MovieService);
  private seriesService = inject(SeriesService);
  private sessionService = inject(GamingSessionService);
  private questBoardService = inject(QuestBoardService);
  private cache = inject(RequestCache);

  games: Game[] = [];
  animes: Anime[] = [];
  movies: Movie[] = [];
  series: Series[] = [];
  releasePlanGames: Game[] = [];
  sessions: GamingSession[] = [];
  questBoard: QuestBoardState | null = null;
  mediaItems: DashboardMediaItem[] = [];
  metrics: DashboardMetric[] = [];
  focusItems: DashboardFocusItem[] = [];
  activityItems: DashboardActivityItem[] = [];
  ownedItems: DashboardMediaItem[] = [];
  playingItems: DashboardMediaItem[] = [];
  upcomingReleases: DashboardMediaItem[] = [];
  backlogItems: DashboardMediaItem[] = [];
  recentItems: DashboardMediaItem[] = [];
  featuredItem: DashboardMediaItem | null = null;
  remainingHours = 0;
  playedHours = 0;
  completionRate = 0;
  heroProgress = 0;
  libraryTotal = 0;
  isLoading = true;
  errorMessage = '';

  ngOnInit() {
    this.loadDashboard();
  }

  loadDashboard(event?: CustomEvent) {
    // Stale-while-revalidate: paint cached snapshot instantly so repeat
    // visits don't flash a skeleton, then refetch in the background unless
    // the cache is still fresh and the user didn't pull-to-refresh.
    const cached = this.cache.get<DashboardSnapshot>(DASHBOARD_CACHE_KEY);
    if (cached) {
      this.applyDashboard(cached);
      this.isLoading = false;
      if (!event && this.cache.isFresh(DASHBOARD_CACHE_KEY)) {
        return;
      }
    } else {
      this.isLoading = !event;
    }
    this.errorMessage = '';

    const today = startOfDay(new Date());
    const gamesFilter = this.dashboardLibraryFilter();
    const animesFilter = this.dashboardLibraryFilter();
    const moviesFilter = this.dashboardLibraryFilter();
    const seriesFilter = this.dashboardLibraryFilter();
    const releasePlanFilter = this.releasePlanFilter();

    forkJoin({
      games: this.gameService.getAll(gamesFilter),
      animes: this.animeService.getAll(animesFilter),
      movies: this.movieService.getAll(moviesFilter),
      series: this.seriesService.getAll(seriesFilter),
      releasePlanGames: this.gameService.getAll(releasePlanFilter).pipe(catchError(() => of(emptyPage<Game>()))),
      sessions: this.sessionService.list({ from: today, to: addDays(today, 14) }).pipe(catchError(() => of([]))),
      board: from(this.questBoardService.getBoard()).pipe(catchError(() => of(null))),
    }).subscribe({
      next: (result) => {
        const snapshot: DashboardSnapshot = {
          games: result.games.data ?? [],
          animes: result.animes.data ?? [],
          movies: result.movies.data ?? [],
          series: result.series.data ?? [],
          releasePlanGames: result.releasePlanGames.data ?? [],
          libraryTotal:
            this.totalOf(result.games) +
            this.totalOf(result.animes) +
            this.totalOf(result.movies) +
            this.totalOf(result.series),
          sessions: result.sessions ?? [],
          questBoard: result.board,
        };
        this.cache.set(DASHBOARD_CACHE_KEY, snapshot);
        this.applyDashboard(snapshot);
        this.isLoading = false;
        this.completeRefresh(event);
      },
      error: () => {
        // Background refetch failed: keep the cached paint, surface the
        // error message so the user can pull-to-refresh. Only wipe data
        // if we never had any to begin with. Suppress the error text when
        // we still have a cached painting — the OfflineBanner already
        // explains the staleness and we don't want to double up alarms.
        if (!cached) {
          this.applyDashboard(emptyDashboardSnapshot());
          this.errorMessage = 'Dashboard data could not be loaded.';
        }
        this.isLoading = false;
        this.completeRefresh(event);
      },
    });
  }

  private applyDashboard(snapshot: DashboardSnapshot): void {
    this.games = snapshot.games;
    this.animes = snapshot.animes;
    this.movies = snapshot.movies;
    this.series = snapshot.series;
    this.releasePlanGames = snapshot.releasePlanGames;
    this.libraryTotal = snapshot.libraryTotal;
    this.sessions = snapshot.sessions;
    this.questBoard = snapshot.questBoard;
    this.buildDashboard();
  }

  private buildDashboard() {
    this.mediaItems = [
      ...this.games.map(fromGame),
      ...this.animes.map(fromAnime),
      ...this.movies.map(fromMovie),
      ...this.series.map(fromSeries),
    ];
    this.ownedItems = this.mediaItems.filter((item) => item.status >= 0);
    this.playingItems = topPlayingItems(this.ownedItems);
    this.upcomingReleases = getUpcomingReleases(this.mediaItems);
    this.backlogItems = getBacklogItems(this.ownedItems);
    this.recentItems = mostRecentItems(this.mediaItems);
    this.featuredItem = pickFeaturedItem(this.playingItems, this.upcomingReleases, this.recentItems);
    this.remainingHours = sumRemainingHours(this.ownedItems);
    this.playedHours = sumPlayedHours(this.ownedItems);

    const { completed, rate } = completionRateFor(this.ownedItems);
    this.completionRate = rate;
    this.heroProgress = this.featuredItem ? progressOf(this.featuredItem) : 0;
    this.metrics = buildMetrics({
      libraryTotal: this.libraryTotal,
      ownedItemCount: this.ownedItems.length,
      activeItemCount: this.playingItems.length,
      completedItemCount: completed,
      completionRate: this.completionRate,
      remainingHours: this.remainingHours,
    });
    this.focusItems = buildFocusItems({
      sessions: this.sessions,
      board: this.questBoard,
      upcomingReleases: this.upcomingReleases,
      backlogItems: this.backlogItems,
    });
    this.activityItems = buildActivityItems({
      sessions: this.sessions,
      board: this.questBoard,
      upcomingReleases: this.upcomingReleases,
      recentItems: this.recentItems,
    });
  }

  private dashboardLibraryFilter(): MediaFilter {
    const filter = new MediaFilter();
    filter.MyMedia = true;
    filter.Ownership = 'mine';
    filter.SortBy = 'recently-added';
    filter.setCount(1000);
    return filter;
  }

  private releasePlanFilter(): MediaFilter {
    const filter = new MediaFilter();
    filter.Paging.Count = 500;
    return filter;
  }

  private totalOf<T>(page: { data?: T[] | null; totalCount?: number | null }): number {
    return Number(page.totalCount ?? page.data?.length ?? 0);
  }

  private completeRefresh(event?: CustomEvent) {
    const target = event?.target as HTMLIonRefresherElement | undefined;
    target?.complete();
  }
}
