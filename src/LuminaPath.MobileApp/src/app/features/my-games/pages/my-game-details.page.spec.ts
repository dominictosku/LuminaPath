import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { provideIonicAngular } from '@ionic/angular/standalone';
import { of } from 'rxjs';

import { MyGameDetailsPage } from './my-game-details.page';
import { GameService } from 'src/app/features/games/services/game.service';
import { QuestBoardService } from 'src/app/features/quests/services/quest-board.service';
import { Game, MyGame } from 'src/app/features/games/models/games.model';

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
  let router: jasmine.SpyObj<Router>;

  function configure(gameId: string | null, gameOverride?: Game) {
    gameService = jasmine.createSpyObj<GameService>('GameService', ['get']);
    questBoardService = jasmine.createSpyObj<QuestBoardService>('QuestBoardService', [
      'getBoard',
      'saveBoard',
      'getQuestsForGame',
    ]);
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);

    if (gameOverride) {
      gameService.get.and.returnValue(of(gameOverride));
    }
    questBoardService.getQuestsForGame.and.resolveTo([]);

    TestBed.configureTestingModule({
      imports: [MyGameDetailsPage],
      providers: [
        provideIonicAngular(),
        { provide: GameService, useValue: gameService },
        { provide: QuestBoardService, useValue: questBoardService },
        { provide: Router, useValue: router },
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
    expect(router.navigate).toHaveBeenCalledOnceWith(['/media']);
  });
});
