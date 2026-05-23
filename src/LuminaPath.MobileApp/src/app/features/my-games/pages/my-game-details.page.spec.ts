import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Location } from '@angular/common';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { provideIonicAngular } from '@ionic/angular/standalone';
import { of } from 'rxjs';

import { MyGameDetailsPage } from './my-game-details.page';
import { GameService } from 'src/app/features/games/services/game.service';
import { QuestBoardService } from 'src/app/features/quests/services/quest-board.service';
import { Game, MyGame } from 'src/app/features/games/models/games.model';
import { GamingSessionService } from 'src/app/features/planning/services/gaming-session.service';
import { MyGameService } from 'src/app/features/my-games/services/my-game.service';

function makeGame(overrides: Partial<Game> = {}): Game {
  return Object.assign(new Game(), overrides);
}

function makeMyGame(id: number, gameId: number): MyGame {
  return Object.assign(new MyGame(gameId), { id });
}

describe('MyGameDetailsPage', () => {
  let fixture: ComponentFixture<MyGameDetailsPage>;
  let component: MyGameDetailsPage;

  let gameService: jasmine.SpyObj<GameService>;
  let questBoardService: jasmine.SpyObj<QuestBoardService>;
  let sessionService: jasmine.SpyObj<GamingSessionService>;
  let myGameService: jasmine.SpyObj<MyGameService>;
  let router: jasmine.SpyObj<Router>;
  let location: jasmine.SpyObj<Location>;

  function configure(gameId: string | null, gameOverride?: Game) {
    gameService = jasmine.createSpyObj<GameService>('GameService', ['get', 'getNews']);
    // Sub-components inject these services; the spies just need to exist
    // so the children don't blow up if they happen to render in a test.
    questBoardService = jasmine.createSpyObj<QuestBoardService>('QuestBoardService', [
      'createQuest',
      'updateQuest',
      'deleteQuest',
      'getQuestsForGame',
    ]);
    sessionService = jasmine.createSpyObj<GamingSessionService>('GamingSessionService', ['forecast']);
    myGameService = jasmine.createSpyObj<MyGameService>('MyGameService', [
      'addToLibrary',
      'updateLibraryEntry',
      'getAchievements',
      'delete',
    ]);
    router = jasmine.createSpyObj<Router>('Router', ['navigate', 'navigateByUrl']);
    location = jasmine.createSpyObj<Location>('Location', ['back']);

    if (gameOverride) {
      gameService.get.and.returnValue(of(gameOverride));
    }
    gameService.getNews.and.returnValue(of([]));
    questBoardService.getQuestsForGame.and.resolveTo([]);
    sessionService.forecast.and.returnValue(of(null as any));
    myGameService.updateLibraryEntry.and.returnValue(of(new MyGame(1)));
    myGameService.getAchievements.and.returnValue(of([]));

    TestBed.configureTestingModule({
      imports: [MyGameDetailsPage],
      providers: [
        provideIonicAngular(),
        { provide: GameService, useValue: gameService },
        { provide: QuestBoardService, useValue: questBoardService },
        { provide: GamingSessionService, useValue: sessionService },
        { provide: MyGameService, useValue: myGameService },
        { provide: Router, useValue: router },
        { provide: Location, useValue: location },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap(gameId == null ? {} : { gameId })),
            snapshot: { paramMap: convertToParamMap(gameId == null ? {} : { gameId }) },
          },
        },
      ],
    });

    fixture = TestBed.createComponent(MyGameDetailsPage);
    component = fixture.componentInstance;
  }

  async function initialize(): Promise<void> {
    component.ngOnInit();
    await new Promise<void>((resolve) => setTimeout(resolve));
  }

  it('shows an error when the route param is invalid', async () => {
    configure(null);
    await initialize();
    expect(component.errorMessage).toBe('Game not found.');
    expect(gameService.get).not.toHaveBeenCalled();
  });

  it('loads the game and side data when the user owns it', async () => {
    const myGame = makeMyGame(7, 42);
    const game = makeGame({ id: 42, name: 'Hades', myGames: myGame });
    configure('42', game);

    await initialize();

    expect(gameService.get).toHaveBeenCalledWith(42);
    expect(myGameService.getAchievements).toHaveBeenCalledWith(7);
    expect(component.game?.name).toBe('Hades');
    expect(component.isInLibrary).toBeTrue();
    expect(component.errorMessage).toBe('');
  });

  it('loads earned trophies when the user owns the game', async () => {
    const myGame = makeMyGame(7, 42);
    const game = makeGame({ id: 42, name: 'Astro Bot', myGames: myGame });
    configure('42', game);
    myGameService.getAchievements.and.returnValue(of([
      {
        id: 9,
        gameAchievementId: 3,
        provider: 2,
        providerName: 'PlayStation',
        sourceAchievementId: 'default:1',
        title: 'First jump',
        description: 'Jump once',
        iconUrl: 'https://example.test/trophy.png',
        isHidden: false,
        trophyType: 'bronze',
        unlockedAt: '2026-05-10T00:00:00.000Z',
        syncedAt: '2026-05-11T00:00:00.000Z',
      },
    ]));

    await initialize();

    expect(component.achievements.length).toBe(1);
    expect(component.achievements[0].title).toBe('First jump');
    // Display labels (summary, trophy type) live on GameTrophiesComponent —
    // covered by its own spec.
  });

  it('smoke-checks the details tab state without a backend', () => {
    const myGame = makeMyGame(7, 42);
    const game = makeGame({ id: 42, name: 'Hades', description: 'Escape the underworld', myGames: myGame });
    configure('42', game);

    component.game = game;
    component.isLoading = false;
    component.setDetailTab('overview');

    expect(component.game.name).toBe('Hades');
    expect(component.selectedTab).toBe('overview');
    expect(component.isLoading).toBeFalse();
  });

  it('switches to the progress tab without fetching news from the parent', () => {
    configure('42', makeGame({ id: 42, name: 'Hades', myGames: makeMyGame(7, 42) }));

    component.setDetailTab('progress');

    expect(component.selectedTab).toBe('progress');
    // The parent no longer touches the news service; that's the GameNewsComponent's job.
    expect(gameService.getNews).not.toHaveBeenCalled();
  });

  it('does not query achievements when the game is not yet in the library', async () => {
    const game = makeGame({ id: 42, name: 'Hades', myGames: null });
    configure('42', game);

    await initialize();

    expect(myGameService.getAchievements).not.toHaveBeenCalled();
    expect(component.isInLibrary).toBeFalse();
    expect(component.achievements).toEqual([]);
  });

  it('onNotesSave persists the new note via updateLibraryEntry', async () => {
    const myGame = Object.assign(makeMyGame(7, 42), {
      status: 2,
      personalNotes: 'old note',
    });
    const game = makeGame({ id: 42, name: 'Hades', myGames: myGame });
    configure('42', game);

    await initialize();
    await component.onNotesSave('# Build\n- Shield run');

    expect(myGameService.updateLibraryEntry).toHaveBeenCalledOnceWith(7, 42, jasmine.objectContaining({
      status: 2,
      personalNotes: '# Build\n- Shield run',
    }));
    expect(component.isSavingNotes).toBeFalse();
  });

  it('goBack navigates to the library', () => {
    configure('1', makeGame({ id: 1, name: 'X' }));
    component.goBack();
    expect(router.navigateByUrl).toHaveBeenCalledOnceWith('/library');
  });
});
