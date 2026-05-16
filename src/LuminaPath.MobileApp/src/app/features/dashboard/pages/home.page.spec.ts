import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideIonicAngular } from '@ionic/angular/standalone';
import { of } from 'rxjs';

import { HomePage } from './home.page';
import { GameService } from '../../games/services/game.service';
import { AnimeService } from '../../animes/services/anime.service';
import { MovieService } from '../../movies/services/movie.service';
import { SeriesService } from '../../series/services/series.service';
import { GamingSessionService } from '../../planing/services/gaming-session.service';
import { QuestBoardService } from '../../quests/services/quest-board.service';
import { PaginateResult } from 'src/app/core/entities/paginatedResult';

function pageOf<T>(items: T[]): PaginateResult<T> {
  const page = new PaginateResult<T>();
  page.data = items;
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
});
