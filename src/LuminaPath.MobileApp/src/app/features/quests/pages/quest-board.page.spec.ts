import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AlertController } from '@ionic/angular/standalone';
import { of } from 'rxjs';

import { QuestBoardPage } from './quest-board.page';
import { QuestBoardService } from '../services/quest-board.service';
import { MyGameService } from 'src/app/features/my-games/services/my-game.service';

describe('QuestBoardPage (linked-quest derivation)', () => {
  let fixture: ComponentFixture<QuestBoardPage>;
  let component: QuestBoardPage;

  let questBoardService: jasmine.SpyObj<QuestBoardService>;
  let myGameService: jasmine.SpyObj<MyGameService>;
  let alertController: jasmine.SpyObj<AlertController>;

  beforeEach(() => {
    questBoardService = jasmine.createSpyObj<QuestBoardService>('QuestBoardService', [
      'getBoard',
      'saveBoard',
      'getQuestsForGame',
    ]);
    myGameService = jasmine.createSpyObj<MyGameService>('MyGameService', ['getAll']);
    alertController = jasmine.createSpyObj<AlertController>('AlertController', ['create']);

    questBoardService.getBoard.and.resolveTo({
      xp: 0,
      quests: { main: [], sub: [], faction: [] },
      skills: [],
    });
    questBoardService.saveBoard.and.callFake((state) => Promise.resolve(state));
    myGameService.getAll.and.returnValue(of({ data: [], pageIndex: 1, totalPages: 1 } as any));

    TestBed.configureTestingModule({
      imports: [QuestBoardPage],
      providers: [
        { provide: QuestBoardService, useValue: questBoardService },
        { provide: MyGameService, useValue: myGameService },
        { provide: AlertController, useValue: alertController },
      ],
    });

    fixture = TestBed.createComponent(QuestBoardPage);
    component = fixture.componentInstance;
  });

  it('lifeQuestsFor excludes quests linked to a game', () => {
    component.quests = {
      main: [
        { id: 1, title: 'Life main', completed: false, myGameId: null },
        { id: 2, title: 'Game main', completed: false, myGameId: 42 },
      ],
      sub: [
        { id: 3, title: 'Life sub', completed: false, myGameId: null },
      ],
      faction: [],
    };

    expect(component.lifeQuestsFor('main').map((q) => q.title)).toEqual(['Life main']);
    expect(component.lifeQuestsFor('sub').map((q) => q.title)).toEqual(['Life sub']);
  });

  it('linkedGroups groups quests by myGameId across all type columns', () => {
    component.library = [
      { myGameId: 42, gameName: 'Hades' },
      { myGameId: 7, gameName: 'Celeste' },
    ];
    component.quests = {
      main: [
        { id: 1, title: 'Beat boss', completed: false, myGameId: 42, gameName: 'Hades' },
      ],
      sub: [
        { id: 2, title: 'Find collectible', completed: false, myGameId: 42, gameName: 'Hades' },
        { id: 3, title: 'Reach summit', completed: false, myGameId: 7, gameName: 'Celeste' },
        { id: 4, title: 'Life todo', completed: false, myGameId: null },
      ],
      faction: [],
    };

    const groups = component.linkedGroups;
    expect(groups.length).toBe(2);

    const celeste = groups.find((g) => g.myGameId === 7);
    const hades = groups.find((g) => g.myGameId === 42);
    expect(celeste?.gameName).toBe('Celeste');
    expect(celeste?.quests.length).toBe(1);
    expect(hades?.gameName).toBe('Hades');
    expect(hades?.quests.length).toBe(2);

    expect(groups[0].gameName).toBe('Celeste');
    expect(groups[1].gameName).toBe('Hades');
  });

  it('unlinkedLibrary excludes games that already have quests', () => {
    component.library = [
      { myGameId: 42, gameName: 'Hades' },
      { myGameId: 7, gameName: 'Celeste' },
    ];
    component.quests = {
      main: [{ id: 1, title: 'Beat boss', completed: false, myGameId: 42, gameName: 'Hades' }],
      sub: [],
      faction: [],
    };

    expect(component.unlinkedLibrary.map((g) => g.myGameId)).toEqual([7]);
  });

  it('addQuest creates a life quest with myGameId null', async () => {
    component.newQuest.main = '  Big arc  ';
    await component.addQuest('main');

    expect(component.quests.main[0].title).toBe('Big arc');
    expect(component.quests.main[0].myGameId).toBeNull();
    expect(component.newQuest.main).toBe('');
  });

  it('addLinkedQuest creates a quest with the supplied myGameId', async () => {
    component.library = [{ myGameId: 42, gameName: 'Hades' }];
    component.draftForGame(42).title = 'Beat boss';
    component.draftForGame(42).type = 'main';
    await component.addLinkedQuest(42);

    const created = component.quests.main.find((q) => q.title === 'Beat boss');
    expect(created).toBeTruthy();
    expect(created?.myGameId).toBe(42);
    expect(created?.gameName).toBe('Hades');
    expect(component.draftForGame(42).title).toBe('');
  });

  it('addQuestForNewGame is a no-op when no game is selected', async () => {
    component.newLinkedGameTitle = 'Quest';
    component.newLinkedGameId = null;
    await component.addQuestForNewGame();

    const total = component.quests.main.length + component.quests.sub.length + component.quests.faction.length;
    expect(total).toBe(0);
    expect(questBoardService.saveBoard).not.toHaveBeenCalled();
  });

  it('addQuestForNewGame creates a quest linked to the chosen game and resets the form', async () => {
    component.library = [{ myGameId: 7, gameName: 'Celeste' }];
    component.newLinkedGameId = 7;
    component.newLinkedGameType = 'sub';
    component.newLinkedGameTitle = 'Reach summit';
    await component.addQuestForNewGame();

    const created = component.quests.sub.find((q) => q.title === 'Reach summit');
    expect(created?.myGameId).toBe(7);
    expect(component.newLinkedGameTitle).toBe('');
    expect(component.newLinkedGameId).toBeNull();
    expect(component.newLinkedGameType).toBe('sub');
  });
});
