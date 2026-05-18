import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideIonicAngular } from '@ionic/angular/standalone';
import { of } from 'rxjs';

import { HomePage } from './home.page';
import { GameService } from '../../games/services/game.service';
import { AnimeService } from '../../animes/services/anime.service';
import { MovieService } from '../../movies/services/movie.service';
import { SeriesService } from '../../series/services/series.service';
import { GamingSessionService } from '../../planning/services/gaming-session.service';
import { QuestBoardService } from '../../quests/services/quest-board.service';
import { PaginateResult } from 'src/app/core/entities/paginatedResult';

function pageOf<T>(items: T[]): PaginateResult<T> {
  const page = new PaginateResult<T>();
  page.data = items;
  page.totalCount = items.length;
  return page;
}

function pageWithTotal<T>(items: T[], totalCount: number): PaginateResult<T> {
  const page = pageOf(items);
  page.totalCount = totalCount;
  return page;
}

describe('HomePage', () => {
  let component: HomePage;
  let fixture: ComponentFixture<HomePage>;
  let gameService: jasmine.SpyObj<GameService>;
  let animeService: jasmine.SpyObj<AnimeService>;
  let movieService: jasmine.SpyObj<MovieService>;
  let seriesService: jasmine.SpyObj<SeriesService>;
  let sessionService: jasmine.SpyObj<GamingSessionService>;
  let questBoardService: jasmine.SpyObj<QuestBoardService>;

  beforeEach(() => {
    gameService = jasmine.createSpyObj<GameService>('GameService', ['getAll']);
    animeService = jasmine.createSpyObj<AnimeService>('AnimeService', ['getAll']);
    movieService = jasmine.createSpyObj<MovieService>('MovieService', ['getAll']);
    seriesService = jasmine.createSpyObj<SeriesService>('SeriesService', ['getAll']);
    sessionService = jasmine.createSpyObj<GamingSessionService>('GamingSessionService', ['list']);
    questBoardService = jasmine.createSpyObj<QuestBoardService>('QuestBoardService', ['getBoard']);

    gameService.getAll.and.returnValue(of(pageOf([])));
    animeService.getAll.and.returnValue(of(pageOf([])));
    movieService.getAll.and.returnValue(of(pageOf([])));
    seriesService.getAll.and.returnValue(of(pageOf([])));
    sessionService.list.and.returnValue(of([]));
    questBoardService.getBoard.and.resolveTo({
      xp: 0,
      currentStreakDays: 0,
      longestStreakDays: 0,
      quests: [],
      skills: [],
      achievements: [],
    });

    TestBed.configureTestingModule({
      imports: [HomePage],
      providers: [
        provideIonicAngular(),
        { provide: GameService, useValue: gameService },
        { provide: AnimeService, useValue: animeService },
        { provide: MovieService, useValue: movieService },
        { provide: SeriesService, useValue: seriesService },
        { provide: GamingSessionService, useValue: sessionService },
        { provide: QuestBoardService, useValue: questBoardService },
      ],
    });

    fixture = TestBed.createComponent(HomePage);
    component = fixture.componentInstance;
  });

  it('should create', fakeAsync(() => {
    fixture.detectChanges();
    tick();
    expect(component).toBeTruthy();
  }));

  it('builds focus and activity items from sessions, quests, releases, and library', fakeAsync(() => {
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    const release = new Date();
    release.setDate(release.getDate() + 8);

    gameService.getAll.and.returnValue(of(pageOf([
      {
        id: 7,
        name: 'Hades',
        description: '',
        releaseDate: release,
        genre: 'Roguelike',
        platforms: 2,
        playtime: 50,
        parentGameId: null,
        parentGameName: null,
        dlcs: null,
        image: null,
        myGames: null,
      } as never,
    ])));
    sessionService.list.and.returnValue(of([
      {
        id: 1,
        myGameId: 7,
        gameName: 'Hades',
        scheduledAt: tomorrow.toISOString(),
        durationMinutes: 90,
        completed: false,
        completedAt: null,
        notes: null,
        createdAt: new Date().toISOString(),
      },
    ]));
    questBoardService.getBoard.and.resolveTo({
      xp: 0,
      currentStreakDays: 0,
      longestStreakDays: 0,
      quests: [
        {
          id: 2,
          title: 'Beat boss',
          type: 'main',
          priority: 'medium',
          recurrence: 'none',
          tags: [],
          completed: true,
          rewardXp: 10,
          sortOrder: 0,
          subtasks: [],
          gameName: 'Hades',
        },
      ],
      skills: [],
      achievements: [],
    });

    fixture.detectChanges();
    tick();

    expect(component.focusItems.some((item) => item.title === 'Hades')).toBeTrue();
    expect(component.activityItems.some((item) => item.title === 'Beat boss')).toBeTrue();
  }));

  it('uses server-side library totals instead of the first page length', fakeAsync(() => {
    gameService.getAll.and.returnValue(of(pageWithTotal([
      {
        id: 1,
        name: 'Loaded Game',
        description: '',
        releaseDate: new Date(),
        genre: 'Action',
        platforms: 2,
        playtime: 10,
        parentGameId: null,
        parentGameName: null,
        dlcs: null,
        image: null,
        myGames: { status: 2, timeSpend: 1, myGameInfo: { trackedHours: 2 } },
      } as never,
    ], 123)));
    animeService.getAll.and.returnValue(of(pageWithTotal([], 4)));
    movieService.getAll.and.returnValue(of(pageWithTotal([], 3)));
    seriesService.getAll.and.returnValue(of(pageWithTotal([], 2)));

    fixture.detectChanges();
    tick();

    const gameFilter = gameService.getAll.calls.mostRecent().args[0];
    expect(gameFilter?.MyMedia).toBeTrue();
    expect(gameFilter?.Paging.Count).toBe(1000);
    expect(component.libraryTotal).toBe(132);
    expect(component.metrics.find((metric) => metric.label === 'Library')?.value).toBe('132');
  }));
});
