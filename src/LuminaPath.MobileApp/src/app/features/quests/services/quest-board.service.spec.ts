import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';

import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { QuestBoardService } from './quest-board.service';

function apiQuest(overrides: Record<string, unknown> = {}) {
  return {
    id: 1,
    title: 'Quest',
    type: 1,
    priority: 1,
    recurrence: 0,
    rewardXp: 75,
    completed: false,
    sortOrder: 0,
    tags: [],
    subtasks: [],
    ...overrides,
  };
}

function mutation(overrides: Record<string, unknown> = {}) {
  return {
    quest: apiQuest(),
    spawnedQuest: null,
    totalXp: 0,
    currentStreakDays: 0,
    longestStreakDays: 0,
    unlockedAchievements: [],
    ...overrides,
  };
}

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

  it('getBoard maps flat API quests, skills, subtasks, and achievements', async () => {
    const promise = service.getBoard();

    const req = httpMock.expectOne(endpoint('quests/board'));
    expect(req.request.method).toBe('GET');
    expect(req.request.withCredentials).toBeTrue();
    req.flush({
      xp: 100,
      currentStreakDays: 2,
      longestStreakDays: 4,
      lastCompletionDate: '2026-05-16T00:00:00Z',
      quests: [
        apiQuest({
          id: 7,
          title: 'Beat boss',
          type: 0,
          priority: 2,
          recurrence: 2,
          myGameId: 42,
          gameName: 'Hades',
          skillId: 9,
          skillName: 'Programming',
          tags: ['boss'],
          subtasks: [{ id: 2, title: 'Prep', completed: true, sortOrder: 0 }],
        }),
      ],
      skills: [
        {
          id: 9,
          name: 'Programming',
          icon: 'code-slash-outline',
          color: '#2563eb',
          xp: 120,
          sortOrder: 0,
          nodes: [
            { id: 1, name: 'Basics', unlocked: true, sortOrder: 0 },
            { id: 2, name: 'FP', unlocked: false, sortOrder: 1 },
          ],
        },
      ],
      achievements: [{ code: 'first', title: 'First', description: 'Done', icon: 'trophy', unlockedAt: '2026-05-16T00:00:00Z' }],
    });

    const board = await promise;
    expect(board.xp).toBe(100);
    expect(board.currentStreakDays).toBe(2);
    expect(board.quests[0]).toEqual(jasmine.objectContaining({
      id: 7,
      title: 'Beat boss',
      type: 'main',
      priority: 'high',
      recurrence: 'weekly',
      myGameId: 42,
      gameName: 'Hades',
      skillId: 9,
      skillName: 'Programming',
    }));
    expect(board.quests[0].subtasks[0]).toEqual(jasmine.objectContaining({ id: 2, title: 'Prep', completed: true }));
    expect(board.skills[0].nodes).toEqual(['Basics', 'FP']);
    expect(board.skills[0].unlockedNodes).toEqual([0]);
    expect(board.achievements[0].code).toBe('first');
  });

  it('createQuest posts enum payloads and maps the mutation response', async () => {
    const promise = service.createQuest({
      title: 'Ship',
      notes: 'note',
      type: 'faction',
      priority: 'high',
      recurrence: 'monthly',
      dueDate: '2026-05-16',
      tags: ['release'],
      myGameId: 42,
      skillId: 9,
    });

    const req = httpMock.expectOne(endpoint('quests'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      title: 'Ship',
      notes: 'note',
      type: 2,
      priority: 2,
      recurrence: 3,
      dueDate: '2026-05-16',
      tags: ['release'],
      myGameId: 42,
      skillId: 9,
      questFolderId: null,
    });
    req.flush(mutation({ quest: apiQuest({ id: 10, title: 'Ship', type: 2, priority: 2, recurrence: 3 }) }));

    const result = await promise;
    expect(result.quest).toEqual(jasmine.objectContaining({ id: 10, title: 'Ship', type: 'faction', priority: 'high', recurrence: 'monthly' }));
  });

  it('updateQuest sends only supplied fields', async () => {
    const promise = service.updateQuest(10, {
      title: 'Renamed',
      completed: true,
      clearDueDate: true,
      clearMyGame: true,
      clearSkill: true,
    });

    const req = httpMock.expectOne(endpoint('quests/10'));
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({
      title: 'Renamed',
      completed: true,
      clearDueDate: true,
      clearMyGame: true,
      clearSkill: true,
    });
    req.flush(mutation({ quest: apiQuest({ id: 10, title: 'Renamed', completed: true }) }));

    expect((await promise).quest.completed).toBeTrue();
  });

  it('handles subtasks and quest reordering endpoints', async () => {
    const addPromise = service.addSubtask(10, 'Prep');
    let req = httpMock.expectOne(endpoint('quests/10/subtasks'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ title: 'Prep' });
    req.flush(mutation({ quest: apiQuest({ id: 10, subtasks: [{ id: 3, title: 'Prep', completed: false, sortOrder: 0 }] }) }));
    expect((await addPromise).quest.subtasks[0].title).toBe('Prep');

    const updatePromise = service.updateSubtask(10, 3, { completed: true, sortOrder: 2 });
    req = httpMock.expectOne(endpoint('quests/10/subtasks/3'));
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ completed: true, sortOrder: 2 });
    req.flush(mutation({ quest: apiQuest({ id: 10 }) }));
    await updatePromise;

    const reorderPromise = service.reorderQuests([{ id: 10, sortOrder: 0, type: 'main' }]);
    req = httpMock.expectOne(endpoint('quests/reorder'));
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual([{ id: 10, sortOrder: 0, type: 0 }]);
    req.flush(null);
    await reorderPromise;
  });

  it('saveSkills serializes skill nodes and unlocked indexes without resending quests', async () => {
    const promise = service.saveSkills({
      xp: 40,
      currentStreakDays: 1,
      longestStreakDays: 2,
      lastCompletionDate: null,
      quests: [apiQuest({ id: 99 }) as any],
      skills: [{
        id: 8,
        name: 'Programming',
        icon: 'code-slash-outline',
        color: '#2563eb',
        xp: 120,
        nodes: ['Basics', 'FP'],
        unlockedNodes: [1],
      }],
      achievements: [],
    });

    const req = httpMock.expectOne(endpoint('quests/skills'));
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.quests).toEqual([]);
    expect(req.request.body.skills[0].nodes).toEqual([
      jasmine.objectContaining({ id: 0, name: 'Basics', unlocked: false, sortOrder: 0 }),
      jasmine.objectContaining({ id: 0, name: 'FP', unlocked: true, sortOrder: 1 }),
    ]);
    req.flush({ xp: 40, quests: [], skills: [], achievements: [] });
    await promise;
  });

  it('getQuestsForGame fetches the per-game endpoint and maps entries', async () => {
    const promise = service.getQuestsForGame(42);

    const req = httpMock.expectOne(endpoint('quests/for-game/42'));
    expect(req.request.method).toBe('GET');
    req.flush([
      apiQuest({ id: 1, title: 'Beat boss', type: 0, myGameId: 42, gameName: 'Hades' }),
      apiQuest({ id: 2, title: 'Side', type: 1, completed: true, myGameId: 42, gameName: 'Hades' }),
    ]);

    const quests = await promise;
    expect(quests.length).toBe(2);
    expect(quests[0]).toEqual(jasmine.objectContaining({ id: 1, title: 'Beat boss', type: 'main', myGameId: 42 }));
    expect(quests[1]).toEqual(jasmine.objectContaining({ id: 2, completed: true }));
  });
});
