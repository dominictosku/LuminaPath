import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Location } from '@angular/common';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { provideIonicAngular } from '@ionic/angular/standalone';
import { of } from 'rxjs';

import { MyGameDetailsPage } from './my-game-details.page';
import { GameService } from 'src/app/features/games/services/game.service';
import { QuestBoardService } from 'src/app/features/quests/services/quest-board.service';
import { Game, MyGame } from 'src/app/features/games/models/games.model';
import { GamingSessionService } from 'src/app/features/planing/services/gaming-session.service';

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
  let router: jasmine.SpyObj<Router>;
  let location: jasmine.SpyObj<Location>;

  function configure(gameId: string | null, gameOverride?: Game) {
    gameService = jasmine.createSpyObj<GameService>('GameService', ['get', 'getNews']);
    questBoardService = jasmine.createSpyObj<QuestBoardService>('QuestBoardService', [
      'getBoard',
      'saveBoard',
      'getQuestsForGame',
    ]);
    sessionService = jasmine.createSpyObj<GamingSessionService>('GamingSessionService', ['forecast']);
    router = jasmine.createSpyObj<Router>('Router', ['navigate', 'navigateByUrl']);
    location = jasmine.createSpyObj<Location>('Location', ['back']);

    if (gameOverride) {
      gameService.get.and.returnValue(of(gameOverride));
    }
    gameService.getNews.and.returnValue(of([]));
    questBoardService.getQuestsForGame.and.resolveTo([]);
    sessionService.forecast.and.returnValue(of(null as any));

    TestBed.configureTestingModule({
      imports: [MyGameDetailsPage],
      providers: [
        provideIonicAngular(),
        { provide: GameService, useValue: gameService },
        { provide: QuestBoardService, useValue: questBoardService },
        { provide: GamingSessionService, useValue: sessionService },
        { provide: Router, useValue: router },
        { provide: Location, useValue: location },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap(gameId == null ? {} : { gameId }) } },
        },
      ],
    });

    fixture = TestBed.createComponent(MyGameDetailsPage);
    component = fixture.componentInstance;
  }

  it('shows an error when the route param is invalid', async () => {
    configure(null);
    await component.ngOnInit();
    expect(component.errorMessage).toBe('Game not found.');
    expect(gameService.get).not.toHaveBeenCalled();
  });

  it('loads the game and the linked quests when the user owns it', async () => {
    const myGame = makeMyGame(7, 42);
    const game = makeGame({ id: 42, name: 'Hades', myGames: myGame });
    configure('42', game);
    questBoardService.getQuestsForGame.and.resolveTo([
      { id: 1, title: 'Beat boss', completed: false, myGameId: 7 },
      { id: 2, title: 'Side quest', completed: true, myGameId: 7 },
    ]);

    await component.ngOnInit();

    expect(gameService.get).toHaveBeenCalledOnceWith(42);
    expect(questBoardService.getQuestsForGame).toHaveBeenCalledOnceWith(7);
    expect(component.game?.name).toBe('Hades');
    expect(component.quests.length).toBe(2);
    expect(component.isInLibrary).toBeTrue();
    expect(component.errorMessage).toBe('');
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

  it('loads game news on demand for the news tab', async () => {
    const game = makeGame({ id: 42, name: 'Hades', myGames: null });
    configure('42', game);
    gameService.getNews.and.returnValue(of([
      {
        title: 'Patch notes',
        summary: 'New update',
        url: 'https://example.com/news',
        source: 'Steam',
        provider: 'Steam',
        publishedAt: '2026-05-08T00:00:00.000Z',
      },
    ]));

    await component.ngOnInit();
    await component.loadNews();

    expect(gameService.getNews).toHaveBeenCalledOnceWith(42, false);
    expect(component.newsItems.length).toBe(1);
    expect(component.newsItems[0].title).toBe('Patch notes');
    expect(component.newsLoaded).toBeTrue();
    expect(component.newsErrorMessage).toBe('');
  });

  it('refreshes game news when requested', async () => {
    const game = makeGame({ id: 42, name: 'Hades', myGames: null });
    configure('42', game);

    await component.ngOnInit();
    await component.loadNews(true);

    expect(gameService.getNews).toHaveBeenCalledOnceWith(42, true);
  });

  it('does not query quests when the game is not yet in the library', async () => {
    const game = makeGame({ id: 42, name: 'Hades', myGames: null });
    configure('42', game);

    await component.ngOnInit();

    expect(questBoardService.getQuestsForGame).not.toHaveBeenCalled();
    expect(component.isInLibrary).toBeFalse();
    expect(component.quests).toEqual([]);
  });

  it('addQuest writes the new quest into the right type column and refetches', async () => {
    const myGame = makeMyGame(7, 42);
    const game = makeGame({ id: 42, name: 'Hades', myGames: myGame });
    configure('42', game);

    questBoardService.getBoard.and.resolveTo({
      xp: 0,
      quests: { main: [], sub: [], faction: [] },
      skills: [],
    });
    questBoardService.saveBoard.and.callFake((state) => Promise.resolve(state));
    questBoardService.getQuestsForGame.and.resolveTo([]);

    await component.ngOnInit();

    component.newQuestTitle = '  Kill Megaera  ';
    component.newQuestType = 'main';
    await component.addQuest();

    const savedState = questBoardService.saveBoard.calls.mostRecent().args[0];
    expect(savedState.quests.main.length).toBe(1);
    expect(savedState.quests.main[0]).toEqual(jasmine.objectContaining({
      title: 'Kill Megaera',
      myGameId: 7,
      completed: false,
    }));
    expect(component.newQuestTitle).toBe('');
    expect(questBoardService.getQuestsForGame).toHaveBeenCalledTimes(2);
  });

  it('addQuest is a no-op when the title is empty', async () => {
    const myGame = makeMyGame(7, 42);
    const game = makeGame({ id: 42, name: 'Hades', myGames: myGame });
    configure('42', game);
    await component.ngOnInit();

    component.newQuestTitle = '   ';
    await component.addQuest();

    expect(questBoardService.getBoard).not.toHaveBeenCalled();
    expect(questBoardService.saveBoard).not.toHaveBeenCalled();
  });

  it('deleteQuest removes the quest from the board state on save', async () => {
    const myGame = makeMyGame(7, 42);
    const game = makeGame({ id: 42, name: 'Hades', myGames: myGame });
    configure('42', game);

    questBoardService.getBoard.and.resolveTo({
      xp: 0,
      quests: {
        main: [{ id: 99, title: 'Doomed', completed: false, myGameId: 7 }],
        sub: [],
        faction: [],
      },
      skills: [],
    });
    questBoardService.saveBoard.and.callFake((state) => Promise.resolve(state));
    questBoardService.getQuestsForGame.and.resolveTo([]);

    await component.ngOnInit();
    await component.deleteQuest({ id: 99, title: 'Doomed', completed: false, myGameId: 7 });

    const savedState = questBoardService.saveBoard.calls.mostRecent().args[0];
    expect(savedState.quests.main.find((q) => q.id === 99)).toBeUndefined();
  });

  it('goBack navigates to the library', () => {
    configure('1', makeGame({ id: 1, name: 'X' }));
    component.goBack();
    expect(router.navigateByUrl).toHaveBeenCalledOnceWith('/media');
  });
});
