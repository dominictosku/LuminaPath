import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';

import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { QuestBoardService } from './quest-board.service';

describe('QuestBoardService', () => {
  let service: QuestBoardService;
  let httpMock: HttpTestingController;
  let endpoint: (path: string) => string;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        QuestBoardService,
        ApiEndpointService,
      ],
    });

    service = TestBed.inject(QuestBoardService);
    httpMock = TestBed.inject(HttpTestingController);
    const api = TestBed.inject(ApiEndpointService);
    endpoint = (path) => api.url(path);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getBoard splits API quests into life columns by type', async () => {
    const promise = service.getBoard();

    const req = httpMock.expectOne(endpoint('quests/board'));
    expect(req.request.method).toBe('GET');
    req.flush({
      xp: 100,
      quests: [
        { id: 1, title: 'Main A', type: 0, rewardXp: 150, completed: false, sortOrder: 0 },
        { id: 2, title: 'Sub A', type: 1, rewardXp: 75, completed: true, sortOrder: 0 },
        { id: 3, title: 'Faction A', type: 2, rewardXp: 100, completed: false, sortOrder: 0 },
      ],
      skills: [],
    });

    const board = await promise;
    expect(board.xp).toBe(100);
    expect(board.quests.main.map((q) => q.title)).toEqual(['Main A']);
    expect(board.quests.sub.map((q) => q.title)).toEqual(['Sub A']);
    expect(board.quests.faction.map((q) => q.title)).toEqual(['Faction A']);
  });

  it('getBoard surfaces myGameId and gameName onto each quest', async () => {
    const promise = service.getBoard();

    const req = httpMock.expectOne(endpoint('quests/board'));
    req.flush({
      xp: 0,
      quests: [
        { id: 1, title: 'Beat boss', type: 0, rewardXp: 150, completed: false, sortOrder: 0, myGameId: 42, gameName: 'Hades' },
        { id: 2, title: 'Real life', type: 1, rewardXp: 75, completed: false, sortOrder: 0, myGameId: null, gameName: null },
      ],
      skills: [],
    });

    const board = await promise;
    expect(board.quests.main[0].myGameId).toBe(42);
    expect(board.quests.main[0].gameName).toBe('Hades');
    expect(board.quests.sub[0].myGameId).toBeNull();
    expect(board.quests.sub[0].gameName).toBeNull();
  });

  it('saveBoard sends myGameId in the API payload and back-fills nulls', async () => {
    const promise = service.saveBoard({
      xp: 50,
      quests: {
        main: [{ id: 7, title: 'Linked', completed: false, myGameId: 42 }],
        sub: [{ id: -1, title: 'Life', completed: false, myGameId: null }],
        faction: [],
      },
      skills: [],
    });

    const req = httpMock.expectOne(endpoint('quests/board'));
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.quests).toEqual([
      jasmine.objectContaining({
        id: 7,
        title: 'Linked',
        type: 0,
        completed: false,
        myGameId: 42,
        sortOrder: 0,
      }),
      jasmine.objectContaining({
        id: 0,
        title: 'Life',
        type: 1,
        myGameId: null,
        sortOrder: 0,
      }),
    ]);
    req.flush({ xp: 50, quests: [], skills: [] });
    await promise;
  });

  it('getQuestsForGame fetches the per-game endpoint and maps each entry', async () => {
    const promise = service.getQuestsForGame(42);

    const req = httpMock.expectOne(endpoint('quests/for-game/42'));
    expect(req.request.method).toBe('GET');
    expect(req.request.withCredentials).toBeTrue();
    req.flush([
      { id: 1, title: 'Beat boss', type: 0, rewardXp: 150, completed: false, sortOrder: 0, myGameId: 42, gameName: 'Hades' },
      { id: 2, title: 'Side', type: 1, rewardXp: 75, completed: true, sortOrder: 0, myGameId: 42, gameName: 'Hades' },
    ]);

    const quests = await promise;
    expect(quests.length).toBe(2);
    expect(quests[0]).toEqual(jasmine.objectContaining({ id: 1, title: 'Beat boss', myGameId: 42, gameName: 'Hades' }));
    expect(quests[1]).toEqual(jasmine.objectContaining({ id: 2, title: 'Side', completed: true }));
  });

  it('getQuestsForGame returns an empty array when the backend returns []', async () => {
    const promise = service.getQuestsForGame(99);
    httpMock.expectOne(endpoint('quests/for-game/99')).flush([]);
    expect(await promise).toEqual([]);
  });
});
