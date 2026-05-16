import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { QuestBoardPage } from './quest-board.page';
import {
  Quest,
  QuestBoardService,
  QuestBoardState,
  QuestMutationResult,
  QuestSkill,
  QuestSubtask,
} from '../services/quest-board.service';
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

describe('QuestBoardPage', () => {
  let fixture: ComponentFixture<QuestBoardPage>;
  let component: QuestBoardPage;
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
    questBoardService.updateQuest.and.callFake(async (id, input) => makeMutation(makeQuest({
      id,
      title: input.title ?? 'Updated',
      completed: input.completed ?? false,
      dueDate: input.clearDueDate ? null : input.dueDate ?? null,
    })));
    questBoardService.deleteQuest.and.resolveTo();
    questBoardService.reorderQuests.and.resolveTo();
    questBoardService.saveSkills.and.callFake(async (state) => state);
    questBoardService.getQuestsForGame.and.resolveTo([]);
    questBoardService.addSubtask.and.callFake(async (questId, title) => makeMutation(makeQuest({
      id: questId,
      subtasks: [{ id: 4, title, completed: false, sortOrder: 0 }],
    })));
    questBoardService.updateSubtask.and.callFake(async (questId, subtaskId, input) => makeMutation(makeQuest({
      id: questId,
      subtasks: [{ id: subtaskId, title: 'Prep', completed: input.completed ?? false, sortOrder: input.sortOrder ?? 0 }],
    })));
    questBoardService.deleteSubtask.and.resolveTo();
    myGameService.getAll.and.returnValue(of({
      data: [{ id: 42, game: { name: 'Hades' } }],
      pageIndex: 1,
      totalPages: 1,
    } as any));

    TestBed.configureTestingModule({
      imports: [QuestBoardPage],
      providers: [
        { provide: QuestBoardService, useValue: questBoardService },
        { provide: MyGameService, useValue: myGameService },
      ],
    });

    fixture = TestBed.createComponent(QuestBoardPage);
    component = fixture.componentInstance;
  });

  it('loads the board and library on init', async () => {
    questBoardService.getBoard.and.resolveTo(makeBoard({
      xp: 240,
      currentStreakDays: 3,
      quests: [makeQuest({ id: 1, title: 'Ship tests' })],
      skills: [makeSkill({ nodes: ['Basics'], unlockedNodes: [0] })],
    }));

    await component.ngOnInit();

    expect(component.isLoading).toBeFalse();
    expect(component.xp).toBe(240);
    expect(component.level).toBe(2);
    expect(component.quests.map((quest) => quest.title)).toEqual(['Ship tests']);
    expect(component.library).toEqual([{ myGameId: 42, gameName: 'Hades' }]);
    expect(component.unlockedNodeCount).toBe(1);
  });

  it('filters visible quests by focus lane, search, and tags', () => {
    const today = new Date();
    component.quests = [
      makeQuest({ id: 1, title: 'Today high', priority: 'high', dueDate: isoDate(today), tags: ['release'] }),
      makeQuest({ id: 2, title: 'Inbox low', priority: 'low', dueDate: null, tags: ['later'] }),
      makeQuest({ id: 3, title: 'Future', dueDate: isoDate(addDays(today, 5)), tags: ['release'] }),
      makeQuest({ id: 4, title: 'Done', completed: true, completedAt: today.toISOString(), dueDate: isoDate(today) }),
    ];

    component.setFilter('today');
    expect(component.visibleQuests.map((quest) => quest.id)).toEqual([1, 4]);

    component.setFilter('inbox');
    expect(component.visibleQuests.map((quest) => quest.id)).toEqual([2]);

    component.setFilter('all');
    component.searchQuery = 'future';
    expect(component.visibleQuests.map((quest) => quest.id)).toEqual([3]);

    component.searchQuery = '';
    component.selectTag('release');
    expect(component.visibleQuests.map((quest) => quest.id)).toEqual([1, 3]);
  });

  it('submitQuickAdd creates a quest with current quick-add options and replaces the optimistic row', async () => {
    component.library = [{ myGameId: 42, gameName: 'Hades' }];
    component.skills = [makeSkill({ id: 9, name: 'Programming' })];
    component.quickAddTitle = '  Beat boss  ';
    component.quickAddType = 'main';
    component.quickAddPriority = 'high';
    component.quickAddGameId = 42;
    component.quickAddSkillId = 9;
    component.setQuickAddToday();
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

    await component.submitQuickAdd();

    expect(questBoardService.createQuest).toHaveBeenCalledOnceWith(jasmine.objectContaining({
      title: 'Beat boss',
      type: 'main',
      priority: 'high',
      myGameId: 42,
      skillId: 9,
    }));
    expect(component.quickAddTitle).toBe('');
    expect(component.quests).toEqual([savedQuest]);
    expect(component.xp).toBe(25);
  });

  it('scheduleQuest updates due date and sends clearDueDate when moved to inbox', async () => {
    const quest = makeQuest({ id: 5, title: 'Plan', dueDate: isoDate(new Date()) });
    component.quests = [quest];
    questBoardService.updateQuest.and.resolveTo(makeMutation(makeQuest({ ...quest, dueDate: null })));

    await component.clearQuestDueDate(quest);

    expect(questBoardService.updateQuest).toHaveBeenCalledOnceWith(5, {
      dueDate: undefined,
      clearDueDate: true,
    });
    expect(component.quests[0].dueDate).toBeNull();
    expect(component.toastMessage).toBe('Moved to inbox');
  });

  it('addSubtask, toggleSubtask, and deleteSubtask use the current subtask API', async () => {
    const quest = makeQuest({ id: 6, title: 'Quest', subtasks: [] });
    component.quests = [quest];
    component.setSubtaskDraft(6, '  Prep  ');

    await component.addSubtask(quest);
    expect(questBoardService.addSubtask).toHaveBeenCalledOnceWith(6, 'Prep');
    expect(component.quests[0].subtasks[0]).toEqual(jasmine.objectContaining({ id: 4, title: 'Prep' }));

    const subtask = component.quests[0].subtasks[0];
    questBoardService.updateSubtask.and.resolveTo(makeMutation(makeQuest({
      id: 6,
      subtasks: [{ ...subtask, completed: true }],
    })));
    await component.toggleSubtask(component.quests[0], subtask);
    expect(questBoardService.updateSubtask).toHaveBeenCalledOnceWith(6, 4, { completed: true });

    await component.deleteSubtask(component.quests[0], component.quests[0].subtasks[0]);
    expect(questBoardService.deleteSubtask).toHaveBeenCalledOnceWith(6, 4);
    expect(component.quests[0].subtasks).toEqual([]);
  });

  it('saveSkill creates a minimal skill without predefined nodes', async () => {
    component.newSkill = {
      name: 'Programming',
      icon: 'code-slash-outline',
      color: '#2563eb',
    };
    questBoardService.saveSkills.and.callFake(async (state) => ({
      ...state,
      skills: state.skills.map((skill, index) => ({ ...skill, id: index + 1 })),
    }));

    await component.saveSkill();

    expect(questBoardService.saveSkills).toHaveBeenCalled();
    const sent = questBoardService.saveSkills.calls.mostRecent().args[0];
    expect(sent.skills[0]).toEqual(jasmine.objectContaining({
      name: 'Programming',
      nodes: [],
      unlockedNodes: [],
    }));
    expect(component.skills[0].id).toBe(1);
  });

  it('handleReorder persists reordered quest ids and updates local order', async () => {
    const first = makeQuest({ id: 1, title: 'First', sortOrder: 0 });
    const second = makeQuest({ id: 2, title: 'Second', sortOrder: 1 });
    component.filter = 'inbox';
    component.quests = [first, second];
    const complete = jasmine.createSpy('complete');

    await component.handleReorder({
      detail: { from: 1, to: 0, complete },
    } as unknown as CustomEvent);

    expect(complete).toHaveBeenCalled();
    expect(component.quests.map((quest) => quest.id)).toEqual([2, 1]);
    expect(questBoardService.reorderQuests).toHaveBeenCalledOnceWith([
      { id: 2, sortOrder: 0, type: 'sub' },
      { id: 1, sortOrder: 1, type: 'sub' },
    ]);
  });
});
