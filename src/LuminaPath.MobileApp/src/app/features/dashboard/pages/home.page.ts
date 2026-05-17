import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { catchError, forkJoin, from, of } from 'rxjs';
import {
  IonBadge,
  IonContent,
  IonIcon,
  IonRefresher,
  IonRefresherContent,
  IonSkeletonText,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  alertCircleOutline,
  calendarClearOutline,
  checkmarkDoneOutline,
  checkboxOutline,
  flameOutline,
  gameControllerOutline,
  hourglassOutline,
  libraryOutline,
  sparklesOutline,
  timeOutline,
  trendingUpOutline,
  filmOutline,
  bookOutline,
  tvOutline,
} from 'ionicons/icons';
import { Game, Platforms } from '../../games/models/games.model';
import { GameService } from '../../games/services/game.service';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { Anime } from '../../animes/models/animes.model';
import { AnimeService } from '../../animes/services/anime.service';
import { Movie } from '../../movies/models/movies.model';
import { MovieService } from '../../movies/services/movie.service';
import { Series } from '../../series/models/series.model';
import { SeriesService } from '../../series/services/series.service';
import { MediaFile } from '../../library/models/mediaFile.model';
import { GameStatus, gameStatusLabel, isGameBacklogStatus } from '../../library/models/library-status.model';
import { GamingSession, GamingSessionService } from '../../planing/services/gaming-session.service';
import { Quest, QuestBoardService, QuestBoardState } from '../../quests/services/quest-board.service';
import {
  estimatedHoursOfGame,
  gameStatusOf,
  playedHoursOfGame,
  releaseDateOfGame,
  remainingHoursOfGame,
} from '../../games/domain/game-library-metrics';
import { MediaFilter } from 'src/app/core/entities/mediaFilter';

type DashboardMetric = {
  label: string;
  value: string;
  detail: string;
  icon: string;
  tone: 'blue' | 'green' | 'amber' | 'rose';
};

type MediaKind = 'Game' | 'Anime' | 'Movie' | 'Series';

type DashboardMediaItem = {
  id: number;
  kind: MediaKind;
  name: string;
  description?: string | null;
  genre?: string | null;
  releaseDate: Date | string | null;
  image: MediaFile | null;
  status: number;
  estimatedHours: number;
  playedHours: number;
  remainingHours: number;
  context: string;
};

type DashboardFocusItem = {
  title: string;
  detail: string;
  icon: string;
  tone: 'blue' | 'green' | 'amber' | 'rose';
};

type DashboardActivityItem = {
  title: string;
  detail: string;
  icon: string;
};

@Component({
  selector: 'app-home',
  templateUrl: './home.page.html',
  styleUrls: ['./home.page.scss'],
  imports: [
    IonBadge,
    IonContent,
    IonIcon,
    IonRefresher,
    IonRefresherContent,
    IonSkeletonText,
    CommonModule,
  ],
})
export class HomePage implements OnInit {
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

  constructor(
    private gameService: GameService,
    private animeService: AnimeService,
    private movieService: MovieService,
    private seriesService: SeriesService,
    private sessionService: GamingSessionService,
    private questBoardService: QuestBoardService,
  ) {
    addIcons({
      alertCircleOutline,
      calendarClearOutline,
      checkmarkDoneOutline,
      checkboxOutline,
      flameOutline,
      gameControllerOutline,
      hourglassOutline,
      libraryOutline,
      sparklesOutline,
      timeOutline,
      trendingUpOutline,
      filmOutline,
      bookOutline,
      tvOutline,
    });
  }

  ngOnInit() {
    this.loadDashboard();
  }

  loadDashboard(event?: CustomEvent) {
    this.isLoading = !event;
    this.errorMessage = '';
    const today = this.startOfToday();
    const gamesFilter = this.dashboardLibraryFilter();
    const animesFilter = this.dashboardLibraryFilter();
    const moviesFilter = this.dashboardLibraryFilter();
    const seriesFilter = this.dashboardLibraryFilter();

    forkJoin({
      games: this.gameService.getAll(gamesFilter),
      animes: this.animeService.getAll(animesFilter),
      movies: this.movieService.getAll(moviesFilter),
      series: this.seriesService.getAll(seriesFilter),
      sessions: this.sessionService.list({ from: today, to: this.addDays(today, 14) }).pipe(catchError(() => of([]))),
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
      .sort((a, b) => this.progressOf(b) - this.progressOf(a))
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
    this.heroProgress = this.featuredItem ? this.progressOf(this.featuredItem) : 0;
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
    const today = this.startOfToday();

    return this.mediaItems
      .filter((item) => this.releaseDateOf(item) >= today)
      .sort((a, b) => this.releaseDateOf(a).getTime() - this.releaseDateOf(b).getTime())
      .slice(0, 5);
  }

  private getBacklogItems(): DashboardMediaItem[] {
    return this.ownedItems
      .filter((item) => isGameBacklogStatus(item.status))
      .sort((a, b) => b.remainingHours - a.remainingHours)
      .slice(0, 4);
  }

  private createFocusItems(): DashboardFocusItem[] {
    const nextSession = this.nextSession();
    const dueQuests = this.openQuests()
      .filter((quest) => quest.dueDate && new Date(quest.dueDate) <= this.addDays(this.startOfToday(), 1))
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
        detail: backlogPick ? `${this.remainingLabel(backlogPick)} · ${this.statusLabel(backlogPick)}` : 'No planned commitment found',
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

  imageFor(item: DashboardMediaItem | null): string {
    return mediaImageUrl(item?.image);
  }

  statusLabel(item: DashboardMediaItem): string {
    return gameStatusLabel(item.status, 'Not started');
  }

  releaseLabel(item: DashboardMediaItem): string {
    const date = this.releaseDateOf(item);

    if (Number.isNaN(date.getTime())) {
      return 'No release date';
    }

    return new Intl.DateTimeFormat('en', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    }).format(date);
  }

  daysUntil(item: DashboardMediaItem): string {
    const date = this.releaseDateOf(item);
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

  playedLabel(item: DashboardMediaItem): string {
    return `${Math.round(item.playedHours)}h logged`;
  }

  remainingLabel(item: DashboardMediaItem): string {
    return `${Math.round(item.remainingHours)}h left`;
  }

  progressOf(item: DashboardMediaItem): number {
    const estimated = item.estimatedHours;

    if (estimated <= 0) {
      return item.status === GameStatus.Completed ? 100 : 0;
    }

    return Math.min(100, Math.round((item.playedHours / estimated) * 100));
  }

  trackByItem(_: number, item: DashboardMediaItem): string {
    return `${item.kind}-${item.id}`;
  }

  trackByFocus(_: number, item: DashboardFocusItem): string {
    return `${item.icon}-${item.title}`;
  }

  trackByActivity(_: number, item: DashboardActivityItem): string {
    return `${item.icon}-${item.title}-${item.detail}`;
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
      context: Platforms.find((platform) => platform.value === game.platforms)?.label ?? 'Game',
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
    kind: MediaKind,
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

  private releaseDateOf(item: DashboardMediaItem): Date {
    return releaseDateOfGame(item);
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

  private startOfToday(): Date {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return today;
  }

  private addDays(value: Date, days: number): Date {
    const next = new Date(value);
    next.setDate(next.getDate() + days);
    return next;
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
