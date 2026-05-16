import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Location } from '@angular/common';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { provideIonicAngular } from '@ionic/angular/standalone';
import { of } from 'rxjs';

import { MyGameDetailsPage } from './my-game-details.page';
import { GameService } from 'src/app/features/games/services/game.service';
import { Quest, QuestBoardService } from 'src/app/features/quests/services/quest-board.service';
import { Game, MyGame } from 'src/app/features/games/models/games.model';
import { GamingSessionService } from 'src/app/features/planing/services/gaming-session.service';
import { MyGameService } from 'src/app/features/my-games/services/my-game.service';

function makeGame(overrides: Partial<Game> = {}): Game {
  return Object.assign(new Game(), overrides);
}

function makeMyGame(id: number, gameId: number): MyGame {
  return Object.assign(new MyGame(gameId), { id });
}

function makeQuest(overrides: Partial<Quest>): Quest {
  return {
    id: 1,
    title: 'Quest',
    type: 'sub',
    priority: 'medium',
    recurrence: 'none',
    tags: [],
    completed: false,
    rewardXp: 10,
    sortOrder: 0,
    subtasks: [],
    ...overrides,
  };
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
      'delete',
    ]);
    router = jasmine.createSpyObj<Router>('Router', ['navigate', 'navigateByUrl']);
    location = jasmine.createSpyObj<Location>('Location', ['back']);

    if (gameOverride) {
      gameService.get.and.returnValue(of(gameOverride));
    }
    gameService.getNews.and.returnValue(of([]));
    questBoardService.getQuestsForGame.and.resolveTo([]);
    questBoardService.createQuest.and.resolveTo({} as any);
    questBoardService.updateQuest.and.resolveTo({} as any);
    questBoardService.deleteQuest.and.resolveTo(undefined as any);
    sessionService.forecast.and.returnValue(of(null as any));
    myGameService.updateLibraryEntry.and.returnValue(of(new MyGame(1)));

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
      makeQuest({ id: 1, title: 'Beat boss', completed: false, myGameId: 7 }),
      makeQuest({ id: 2, title: 'Side quest', completed: true, myGameId: 7 }),
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

    questBoardService.getQuestsForGame.and.resolveTo([]);

    await component.ngOnInit();

    component.newQuestTitle = '  Kill Megaera  ';
    component.newQuestType = 'main';
    await component.addQuest();

    expect(questBoardService.createQuest).toHaveBeenCalledOnceWith(jasmine.objectContaining({
      title: 'Kill Megaera',
      myGameId: 7,
      type: 'main',
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

    expect(questBoardService.createQuest).not.toHaveBeenCalled();
  });

  it('deleteQuest removes the quest through the quest service', async () => {
    const myGame = makeMyGame(7, 42);
    const game = makeGame({ id: 42, name: 'Hades', myGames: myGame });
    configure('42', game);

    questBoardService.getQuestsForGame.and.resolveTo([]);

    await component.ngOnInit();
    await component.deleteQuest(makeQuest({ id: 99, title: 'Doomed', completed: false, myGameId: 7 }));

    expect(questBoardService.deleteQuest).toHaveBeenCalledOnceWith(99);
  });

  it('saves personal notes on the library entry', async () => {
    const myGame = Object.assign(makeMyGame(7, 42), {
      status: 2,
      personalNotes: 'old note',
    });
    const game = makeGame({ id: 42, name: 'Hades', myGames: myGame });
    configure('42', game);

    await component.ngOnInit();
    component.startEditingNotes();
    component.notesDraft = '  # Build\n- Shield run\n\n**Heat 8**  ';
    await component.savePersonalNotes();

    expect(myGameService.updateLibraryEntry).toHaveBeenCalledOnceWith(7, 42, jasmine.objectContaining({
      status: 2,
      personalNotes: '# Build\n- Shield run\n\n**Heat 8**',
    }));
    expect(component.isEditingNotes).toBeFalse();
  });

  it('renders personal notes as escaped markdown', () => {
    configure('42', makeGame({ id: 42, name: 'Hades' }));

    const html = component.renderMarkdown('# Notes\n- **Win** `<script>`');

    expect(html).toContain('<h3>Notes</h3>');
    expect(html).toContain('<strong>Win</strong>');
    expect(html).toContain('&lt;script&gt;');
  });

  it('goBack navigates to the library', () => {
    configure('1', makeGame({ id: 1, name: 'X' }));
    component.goBack();
    expect(router.navigateByUrl).toHaveBeenCalledOnceWith('/library');
  });
});
