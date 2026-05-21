import { Component, OnInit, inject } from '@angular/core';

import { catchError, forkJoin, from, of } from 'rxjs';
import {
  IonContent,
  IonIcon,
  IonRefresher,
  IonRefresherContent,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import { flameOutline } from 'ionicons/icons';
import { Game, platformLabelFromValue } from '../../games/models/games.model';
import { GameService } from '../../games/services/game.service';
import { Anime } from '../../animes/models/animes.model';
import { AnimeService } from '../../animes/services/anime.service';
import { Movie } from '../../movies/models/movies.model';
import { MovieService } from '../../movies/services/movie.service';
import { Series } from '../../series/models/series.model';
import { SeriesService } from '../../series/services/series.service';
import { GameStatus, isGameBacklogStatus } from '../../library/models/library-status.model';
import { GamingSession, GamingSessionService } from '../../planning/services/gaming-session.service';
import { Quest, QuestBoardService, QuestBoardState } from '../../quests/services/quest-board.service';
import {
  estimatedHoursOfGame,
  gameStatusOf,
  playedHoursOfGame,
  remainingHoursOfGame,
} from '../../games/domain/game-library-metrics';
import { MediaFilter } from 'src/app/core/entities/mediaFilter';
import {
  DashboardActivityItem,
  DashboardFocusItem,
  DashboardMediaItem,
  DashboardMediaKind,
  DashboardMetric,
} from '../models/dashboard.model';
import { progressOf, releaseDateOf, remainingLabel, statusLabel } from '../dashboard-view.helpers';
import { addDays, startOfDay } from 'src/app/shared/utils/date-helpers';
import { DashboardHeroComponent } from '../components/dashboard-hero/dashboard-hero.component';
import { DashboardMetricsComponent } from '../components/dashboard-metrics/dashboard-metrics.component';
import { DashboardMomentumComponent } from '../components/dashboard-momentum/dashboard-momentum.component';
import { DashboardFocusComponent } from '../components/dashboard-focus/dashboard-focus.component';
import { DashboardActivityComponent } from '../components/dashboard-activity/dashboard-activity.component';
import { DashboardActiveRailComponent } from '../components/dashboard-active-rail/dashboard-active-rail.component';
import { DashboardReleasesComponent } from '../components/dashboard-releases/dashboard-releases.component';
import { DashboardBacklogComponent } from '../components/dashboard-backlog/dashboard-backlog.component';
import { DashboardRecentComponent } from '../components/dashboard-recent/dashboard-recent.component';

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
  ],
})
export class HomePage implements OnInit {
  private gameService = inject(GameService);
  private animeService = inject(AnimeService);
  private movieService = inject(MovieService);
  private seriesService = inject(SeriesService);
  private sessionService = inject(GamingSessionService);
  private questBoardService = inject(QuestBoardService);

  games: Game[] = [];
  animes: Anime[] = [];
  movies: Movie[] = [];
  series: Series[] = [];
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

  constructor() {
    addIcons({ flameOutline });
  }

  ngOnInit() {
    this.loadDashboard();
  }

  loadDashboard(event?: CustomEvent) {
    this.isLoading = !event;
    this.errorMessage = '';
    const today = startOfDay(new Date());
    const gamesFilter = this.dashboardLibraryFilter();
    const animesFilter = this.dashboardLibraryFilter();
    const moviesFilter = this.dashboardLibraryFilter();
    const seriesFilter = this.dashboardLibraryFilter();

    forkJoin({
      games: this.gameService.getAll(gamesFilter),
      animes: this.animeService.getAll(animesFilter),
      movies: this.movieService.getAll(moviesFilter),
      series: this.seriesService.getAll(seriesFilter),
      sessions: this.sessionService.list({ from: today, to: addDays(today, 14) }).pipe(catchError(() => of([]))),
      board: from(this.questBoardService.getBoard()).pipe(catchError(() => of(null))),
    }).subscribe({
      next: (result) => {
        this.games = result.games.data ?? [];
        this.animes = result.animes.data ?? [];
        this.movies = result.movies.data ?? [];
        this.series = result.series.data ?? [];
        this.libraryTotal = this.totalOf(result.games) + this.totalOf(result.animes) + this.totalOf(result.movies) + this.totalOf(result.series);
        this.sessions = result.sessions ?? [];
        this.questBoard = result.board;
        this.buildDashboard();
        this.isLoading = false;
        this.completeRefresh(event);
      },
      error: () => {
        this.games = [];
        this.animes = [];
        this.movies = [];
        this.series = [];
        this.libraryTotal = 0;
        this.sessions = [];
        this.questBoard = null;
        this.buildDashboard();
        this.errorMessage = 'Dashboard data could not be loaded.';
        this.isLoading = false;
        this.completeRefresh(event);
      },
    });
  }

  private buildDashboard() {
    this.mediaItems = [
      ...this.games.map((game) => this.fromGame(game)),
      ...this.animes.map((anime) => this.fromAnime(anime)),
      ...this.movies.map((movie) => this.fromMovie(movie)),
      ...this.series.map((series) => this.fromSeries(series)),
    ];
    this.ownedItems = this.mediaItems.filter((item) => item.status >= 0);
    this.playingItems = this.ownedItems
      .filter((item) => item.status === GameStatus.Playing)
      .sort((a, b) => progressOf(b) - progressOf(a))
      .slice(0, 4);
    this.upcomingReleases = this.getUpcomingReleases();
    this.backlogItems = this.getBacklogItems();
    this.recentItems = [...this.mediaItems]
      .sort((a, b) => b.id - a.id)
      .slice(0, 8);
    this.featuredItem = this.playingItems[0] ?? this.upcomingReleases[0] ?? this.recentItems[0] ?? null;
    this.remainingHours = Math.round(
      this.ownedItems.reduce((sum, item) => sum + item.remainingHours, 0)
    );
    this.playedHours = Math.round(
      this.ownedItems.reduce((sum, item) => sum + item.playedHours, 0)
    );

    const completedItems = this.ownedItems.filter((item) => item.status === GameStatus.Completed).length;
    this.completionRate = this.ownedItems.length === 0
      ? 0
      : Math.round((completedItems / this.ownedItems.length) * 100);
    this.heroProgress = this.featuredItem ? progressOf(this.featuredItem) : 0;
    this.metrics = this.createMetrics(completedItems);
    this.focusItems = this.createFocusItems();
    this.activityItems = this.createActivityItems();
  }

  private createMetrics(completedItems: number): DashboardMetric[] {
    const ownedItems = this.libraryTotal || this.ownedItems.length;
    const activeItems = this.playingItems.length;

    return [
      {
        label: 'Library',
        value: String(ownedItems),
        detail: `${ownedItems} in your collection`,
        icon: 'library-outline',
        tone: 'blue',
      },
      {
        label: 'Active',
        value: String(activeItems),
        detail: activeItems === 1 ? 'currently active item' : 'currently active items',
        icon: 'game-controller-outline',
        tone: 'green',
      },
      {
        label: 'Completed',
        value: String(completedItems),
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

  private getUpcomingReleases(): DashboardMediaItem[] {
    const today = startOfDay(new Date());

    return this.mediaItems
      .filter((item) => releaseDateOf(item) >= today)
      .sort((a, b) => releaseDateOf(a).getTime() - releaseDateOf(b).getTime())
      .slice(0, 5);
  }

  private getBacklogItems(): DashboardMediaItem[] {
    return this.ownedItems
      .filter((item) => isGameBacklogStatus(item.status))
      .sort((a, b) => b.remainingHours - a.remainingHours)
      .slice(0, 4);
  }

  private createFocusItems(): DashboardFocusItem[] {
    const today = startOfDay(new Date());
    const nextSession = this.nextSession();
    const dueQuests = this.openQuests()
      .filter((quest) => quest.dueDate && new Date(quest.dueDate) <= addDays(today, 1))
      .length;
    const nextRelease = this.upcomingReleases[0] ?? null;
    const backlogPick = this.backlogItems[0] ?? null;

    return [
      {
        title: nextSession?.gameName ?? 'Plan a session',
        detail: nextSession
          ? `${this.shortDateTime(nextSession.scheduledAt)} · ${Math.round(nextSession.durationMinutes / 60 * 10) / 10}h`
          : 'No gaming session scheduled in the next two weeks',
        icon: 'calendar-clear-outline',
        tone: 'blue',
      },
      {
        title: dueQuests ? `${dueQuests} quest${dueQuests === 1 ? '' : 's'} need attention` : 'Quest board is calm',
        detail: dueQuests ? 'Due today or already waiting' : `${this.openQuests().length} open quest${this.openQuests().length === 1 ? '' : 's'}`,
        icon: dueQuests ? 'alert-circle-outline' : 'checkbox-outline',
        tone: dueQuests ? 'amber' : 'green',
      },
      {
        title: nextRelease?.name ?? 'No upcoming release',
        detail: nextRelease ? `${this.daysUntil(nextRelease)} · ${nextRelease.kind}` : 'Nothing dated in the loaded catalog',
        icon: 'sparkles-outline',
        tone: 'rose',
      },
      {
        title: backlogPick?.name ?? 'Backlog is clear',
        detail: backlogPick ? `${remainingLabel(backlogPick)} · ${statusLabel(backlogPick)}` : 'No planned commitment found',
        icon: 'hourglass-outline',
        tone: 'blue',
      },
    ];
  }

  private createActivityItems(): DashboardActivityItem[] {
    const items: DashboardActivityItem[] = [];

    const nextSession = this.nextSession();
    if (nextSession) {
      items.push({
        title: nextSession.gameName ?? 'Generic gaming time',
        detail: `Session ${this.shortDateTime(nextSession.scheduledAt)}`,
        icon: 'time-outline',
      });
    }

    for (const quest of this.completedQuests().slice(0, 3)) {
      items.push({
        title: quest.title,
        detail: `Quest completed${quest.gameName ? ' · ' + quest.gameName : ''}`,
        icon: 'checkmark-done-outline',
      });
    }

    for (const release of this.upcomingReleases.slice(0, 2)) {
      items.push({
        title: release.name,
        detail: `Releases ${this.daysUntil(release)} · ${release.kind}`,
        icon: 'calendar-clear-outline',
      });
    }

    for (const item of this.recentItems.slice(0, 3)) {
      items.push({
        title: item.name,
        detail: `Recently added · ${item.kind}`,
        icon: 'library-outline',
      });
    }

    return items.slice(0, 8);
  }

  private fromGame(game: Game): DashboardMediaItem {
    const playedHours = playedHoursOfGame(game);
    const estimatedHours = estimatedHoursOfGame(game);

    return {
      id: game.id,
      kind: 'Game',
      name: game.name,
      description: game.description,
      genre: game.genre,
      releaseDate: game.releaseDate,
      image: game.image,
      status: gameStatusOf(game),
      estimatedHours,
      playedHours,
      remainingHours: remainingHoursOfGame(game),
      context: platformLabelFromValue(game.platforms),
    };
  }

  private fromAnime(anime: Anime): DashboardMediaItem {
    return this.fromWatchMedia('Anime', anime, anime.myAnimes?.status, anime.expectedWatchTimeMinutes, anime.myAnimes?.currentWatchTimeMinutes);
  }

  private fromMovie(movie: Movie): DashboardMediaItem {
    return this.fromWatchMedia('Movie', movie, movie.myMovies?.status, movie.expectedWatchTimeMinutes, movie.myMovies?.currentWatchTimeMinutes);
  }

  private fromSeries(series: Series): DashboardMediaItem {
    return this.fromWatchMedia('Series', series, series.mySeries?.status, series.expectedWatchTimeMinutes, series.mySeries?.currentWatchTimeMinutes);
  }

  private fromWatchMedia(
    kind: DashboardMediaKind,
    media: Anime | Movie | Series,
    status: number | null | undefined,
    expectedMinutes: number | null | undefined,
    watchedMinutes: number | null | undefined,
  ): DashboardMediaItem {
    const estimatedHours = Math.round(((Number(expectedMinutes) || 0) / 60) * 10) / 10;
    const playedHours = Math.round(((Number(watchedMinutes) || 0) / 60) * 10) / 10;

    return {
      id: media.id,
      kind,
      name: media.name,
      description: media.description,
      genre: media.genre,
      releaseDate: media.releaseDate,
      image: media.image,
      status: Number(status ?? -1),
      estimatedHours,
      playedHours,
      remainingHours: Math.max(0, estimatedHours - playedHours),
      context: kind,
    };
  }

  private daysUntil(item: DashboardMediaItem): string {
    const date = releaseDateOf(item);
    const today = startOfDay(new Date());
    const days = Math.ceil((date.getTime() - today.getTime()) / 86400000);

    if (days <= 0) return 'Today';
    if (days === 1) return 'Tomorrow';
    return `${days} days`;
  }

  private nextSession(): GamingSession | null {
    const now = new Date();
    return [...this.sessions]
      .filter((session) => !session.completed && new Date(session.scheduledAt) >= now)
      .sort((a, b) => new Date(a.scheduledAt).getTime() - new Date(b.scheduledAt).getTime())[0] ?? null;
  }

  private openQuests(): Quest[] {
    return this.questBoard?.quests.filter((quest) => !quest.completed) ?? [];
  }

  private completedQuests(): Quest[] {
    return [...(this.questBoard?.quests ?? [])]
      .filter((quest) => quest.completed)
      .sort((a, b) => new Date(b.completedAt ?? b.updatedAt ?? '').getTime() - new Date(a.completedAt ?? a.updatedAt ?? '').getTime());
  }

  private shortDateTime(value: string): string {
    return new Intl.DateTimeFormat('en', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(value));
  }

  private dashboardLibraryFilter(): MediaFilter {
    const filter = new MediaFilter();
    filter.MyMedia = true;
    filter.Ownership = 'mine';
    filter.SortBy = 'recently-added';
    filter.setCount(1000);
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
