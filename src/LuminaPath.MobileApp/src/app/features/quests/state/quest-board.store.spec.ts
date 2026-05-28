import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { QuestBoardStore } from './quest-board.store';
import {
  Quest,
  QuestBoardService,
  QuestBoardState,
  QuestMutationResult,
  QuestSkill,
} from '../services/quest-board.service';
import { QuestBoardFeedbackService } from '../services/quest-board-feedback.service';
import { MyGameService } from 'src/app/features/my-games/services/my-game.service';

function isoDate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

function addDays(date: Date, days: number): Date {
  const copy = new Date(date);
  copy.setDate(copy.getDate() + days);
  return copy;
}

function makeQuest(overrides: Partial<Quest> = {}): Quest {
  return {
    id: 1,
    title: 'Quest',
    notes: null,
    type: 'sub',
    priority: 'medium',
    recurrence: 'none',
    dueDate: null,
    tags: [],
    completed: false,
    rewardXp: 75,
    sortOrder: 0,
    myGameId: null,
    gameName: null,
    skillId: null,
    skillName: null,
    subtasks: [],
    ...overrides,
  };
}

function makeSkill(overrides: Partial<QuestSkill> = {}): QuestSkill {
  return {
    id: 9,
    name: 'Programming',
    icon: 'code-slash-outline',
    color: '#2563eb',
    xp: 0,
    nodes: [],
    unlockedNodes: [],
    ...overrides,
  };
}

function makeBoard(overrides: Partial<QuestBoardState> = {}): QuestBoardState {
  return {
    xp: 0,
    currentStreakDays: 0,
    longestStreakDays: 0,
    lastCompletionDate: null,
    quests: [],
    skills: [],
    achievements: [],
    ...overrides,
  };
}

function makeMutation(quest: Quest, overrides: Partial<QuestMutationResult> = {}): QuestMutationResult {
  return {
    quest,
    spawnedQuest: null,
    totalXp: 0,
    currentStreakDays: 0,
    longestStreakDays: 0,
    awardedSkillXp: null,
    awardedSkillId: null,
    unlockedAchievements: [],
    ...overrides,
  };
}

describe('QuestBoardStore', () => {
  let questBoardService: jasmine.SpyObj<QuestBoardService>;
  let myGameService: jasmine.SpyObj<MyGameService>;

  beforeEach(() => {
    localStorage.clear();

    questBoardService = jasmine.createSpyObj<QuestBoardService>('QuestBoardService', [
      'getBoard',
      'createQuest',
      'updateQuest',
      'deleteQuest',
      'reorderQuests',
      'saveSkills',
      'getQuestsForGame',
      'addSubtask',
      'updateSubtask',
      'deleteSubtask',
    ]);
    myGameService = jasmine.createSpyObj<MyGameService>('MyGameService', ['getAll']);

    questBoardService.getBoard.and.resolveTo(makeBoard());
    questBoardService.createQuest.and.callFake(async (input) => makeMutation(makeQuest({ id: 20, title: input.title })));
    questBoardService.updateQuest.and.callFake(async (id, input) =>
      makeMutation(
        makeQuest({
          id,
          title: input.title ?? 'Updated',
          completed: input.completed ?? false,
          dueDate: input.clearDueDate ? null : input.dueDate ?? null,
        }),
      ),
    );
    questBoardService.deleteQuest.and.resolveTo();
    questBoardService.reorderQuests.and.resolveTo();
    questBoardService.saveSkills.and.callFake(async (state) => state);
    questBoardService.getQuestsForGame.and.resolveTo([]);
    questBoardService.addSubtask.and.callFake(async (questId, title) =>
      makeMutation(makeQuest({ id: questId, subtasks: [{ id: 4, title, completed: false, sortOrder: 0 }] })),
    );
    questBoardService.updateSubtask.and.callFake(async (questId, subtaskId, input) =>
      makeMutation(
        makeQuest({
          id: questId,
          subtasks: [{ id: subtaskId, title: 'Prep', completed: input.completed ?? false, sortOrder: input.sortOrder ?? 0 }],
        }),
      ),
    );
    questBoardService.deleteSubtask.and.resolveTo();
    myGameService.getAll.and.returnValue(
      of({ data: [{ id: 42, game: { name: 'Hades' } }], pageIndex: 1, totalPages: 1 } as any),
    );

    TestBed.configureTestingModule({
      providers: [
        { provide: QuestBoardService, useValue: questBoardService },
        { provide: MyGameService, useValue: myGameService },
        QuestBoardFeedbackService,
        QuestBoardStore,
      ],
    });
  });

  /** Inject the store and await the initial board + library load. */
  async function load(): Promise<InstanceType<typeof QuestBoardStore>> {
    const store = TestBed.inject(QuestBoardStore);
    await store.load();
    return store;
  }

  it('loads the board and library on init', async () => {
    questBoardService.getBoard.and.resolveTo(
      makeBoard({
        xp: 240,
        currentStreakDays: 3,
        quests: [makeQuest({ id: 1, title: 'Ship tests' })],
        skills: [makeSkill({ nodes: ['Basics'], unlockedNodes: [0] })],
      }),
    );

    const store = await load();

    expect(store.isLoading()).toBeFalse();
    expect(store.xp()).toBe(240);
    expect(store.stats().level).toBe(2);
    expect(store.quests().map((quest) => quest.title)).toEqual(['Ship tests']);
    expect(store.library()).toEqual([{ myGameId: 42, gameName: 'Hades' }]);
    expect(store.stats().unlockedNodeCount).toBe(1);
  });

  it('filters visible quests by focus lane, search, and tags', async () => {
    const today = new Date();
    questBoardService.getBoard.and.resolveTo(
      makeBoard({
        quests: [
          makeQuest({ id: 1, title: 'Today high', priority: 'high', dueDate: isoDate(today), tags: ['release'] }),
          makeQuest({ id: 2, title: 'Inbox low', priority: 'low', dueDate: null, tags: ['later'] }),
          makeQuest({ id: 3, title: 'Future', dueDate: isoDate(addDays(today, 5)), tags: ['release'] }),
          makeQuest({ id: 4, title: 'Done', completed: true, completedAt: today.toISOString(), dueDate: isoDate(today) }),
        ],
      }),
    );
    const store = await load();

    store.setFilter('today');
    expect(store.visibleQuests().map((quest) => quest.id)).toEqual([1, 4]);

    store.setFilter('inbox');
    expect(store.visibleQuests().map((quest) => quest.id)).toEqual([2]);

    store.setFilter('all');
    store.setSearchQuery('future');
    expect(store.visibleQuests().map((quest) => quest.id)).toEqual([3]);

    store.setSearchQuery('');
    store.selectTag('release');
    expect(store.visibleQuests().map((quest) => quest.id)).toEqual([1, 3]);
  });

  it('createQuest sends the submitted options and replaces the optimistic row', async () => {
    questBoardService.getBoard.and.resolveTo(makeBoard({ skills: [makeSkill({ id: 9, name: 'Programming' })] }));
    const store = await load();

    const savedQuest = makeQuest({
      id: 77,
      title: 'Beat boss',
      type: 'main',
      priority: 'high',
      myGameId: 42,
      gameName: 'Hades',
      skillId: 9,
      skillName: 'Programming',
    });
    questBoardService.createQuest.and.resolveTo(makeMutation(savedQuest, { totalXp: 25 }));

    await store.createQuest({
      title: 'Beat boss',
      type: 'main',
      priority: 'high',
      recurrence: 'none',
      dueDate: isoDate(new Date()),
      myGameId: 42,
      skillId: 9,
    });

    expect(questBoardService.createQuest).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({ title: 'Beat boss', type: 'main', priority: 'high', myGameId: 42, skillId: 9 }),
    );
    expect(store.quests()).toEqual([savedQuest]);
    expect(store.xp()).toBe(25);
    expect(store.quickAddType()).toBe('main');
    expect(store.quickAddPriority()).toBe('high');
  });

  it('clearQuestDueDate updates due date and sends clearDueDate', async () => {
    const quest = makeQuest({ id: 5, title: 'Plan', dueDate: isoDate(new Date()) });
    questBoardService.getBoard.and.resolveTo(makeBoard({ quests: [quest] }));
    questBoardService.updateQuest.and.resolveTo(makeMutation(makeQuest({ ...quest, dueDate: null })));
    const store = await load();
    const feedback = TestBed.inject(QuestBoardFeedbackService);

    await store.clearQuestDueDate(store.quests()[0]);

    expect(questBoardService.updateQuest).toHaveBeenCalledOnceWith(5, { dueDate: undefined, clearDueDate: true });
    expect(store.quests()[0].dueDate).toBeNull();
    expect(feedback.toastMessage()).toBe('Moved to inbox');
  });

  it('addSubtask, toggleSubtask, and deleteSubtask use the current subtask API', async () => {
    questBoardService.getBoard.and.resolveTo(makeBoard({ quests: [makeQuest({ id: 6, title: 'Quest', subtasks: [] })] }));
    const store = await load();
    store.setSubtaskDraft(6, '  Prep  ');

    await store.addSubtask(store.quests()[0]);
    expect(questBoardService.addSubtask).toHaveBeenCalledOnceWith(6, 'Prep');
    expect(store.quests()[0].subtasks[0]).toEqual(jasmine.objectContaining({ id: 4, title: 'Prep' }));

    const subtask = store.quests()[0].subtasks[0];
    questBoardService.updateSubtask.and.resolveTo(makeMutation(makeQuest({ id: 6, subtasks: [{ ...subtask, completed: true }] })));
    await store.toggleSubtask({ quest: store.quests()[0], subtask });
    expect(questBoardService.updateSubtask).toHaveBeenCalledOnceWith(6, 4, { completed: true });

    await store.deleteSubtask({ quest: store.quests()[0], subtask: store.quests()[0].subtasks[0] });
    expect(questBoardService.deleteSubtask).toHaveBeenCalledOnceWith(6, 4);
    expect(store.quests()[0].subtasks).toEqual([]);
  });

  it('saveSkill creates a minimal skill without predefined nodes', async () => {
    const store = await load();
    questBoardService.saveSkills.and.callFake(async (state) => ({
      ...state,
      skills: state.skills.map((skill, index) => ({ ...skill, id: index + 1 })),
    }));

    await store.saveSkill({ name: 'Programming', icon: 'code-slash-outline', color: '#2563eb' }, null);

    expect(questBoardService.saveSkills).toHaveBeenCalled();
    const sent = questBoardService.saveSkills.calls.mostRecent().args[0];
    expect(sent.skills[0]).toEqual(jasmine.objectContaining({ name: 'Programming', nodes: [], unlockedNodes: [] }));
    expect(store.skills()[0].id).toBe(1);
  });

  it('reorderQuests persists reordered ids and updates local order', async () => {
    const first = makeQuest({ id: 1, title: 'First', sortOrder: 0 });
    const second = makeQuest({ id: 2, title: 'Second', sortOrder: 1 });
    questBoardService.getBoard.and.resolveTo(makeBoard({ quests: [first, second] }));
    const store = await load();
    store.setFilter('inbox');

    await store.reorderQuests(1, 0, store.visibleQuests());

    expect(store.quests().map((quest) => quest.id)).toEqual([2, 1]);
    expect(questBoardService.reorderQuests).toHaveBeenCalledOnceWith([
      { id: 2, sortOrder: 0, type: 'sub' },
      { id: 1, sortOrder: 1, type: 'sub' },
    ]);
  });
});
