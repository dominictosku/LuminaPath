import { Game, MyGame } from '../../games/models/games.model';
import type { GamingSession } from '../../planning/services/gaming-session.service';
import type { Quest } from '../../quests/services/quest-board.service';
import { buildWeekDays } from './weekly-schedule.helpers';
import {
  clampedDuration,
  clampedEndMinutes,
  combineDateTime,
  createQuestDraftForSlot,
  editQuestDraftForQuest,
  questDraftRange,
  questDurationMinutes,
  sessionDraftForSession,
  toLibraryGame,
} from './weekly-schedule.drafts';

describe('weekly schedule drafts', () => {
  it('creates a scheduled quest draft from a selected slot and game', () => {
    const day = buildWeekDays(new Date(2026, 5, 11), new Date(2026, 5, 1))[3];
    const draft = createQuestDraftForSlot(day, 20 * 60 + 30, {
      myGameId: 9,
      gameName: 'Hades',
      playtime: 20,
    });

    expect(draft).toEqual(jasmine.objectContaining({
      mode: 'create',
      title: 'Play Hades',
      scheduledDate: '2026-06-11',
      startTime: '20:30',
      endTime: '21:30',
      myGameId: 9,
    }));
  });

  it('creates edit drafts from scheduled quests or due-date fallbacks', () => {
    const scheduled = editQuestDraftForQuest(quest({
      id: 4,
      title: 'Raid',
      notes: 'Bring potions',
      priority: 'high',
      recurrence: 'weekly',
      folderId: 3,
      myGameId: 9,
      scheduledStartAt: new Date(2026, 5, 11, 18, 15).toISOString(),
      scheduledEndAt: new Date(2026, 5, 11, 19, 45).toISOString(),
    }), { defaultStartMinutes: 6 * 60 });

    expect(scheduled).toEqual(jasmine.objectContaining({
      mode: 'edit',
      questId: 4,
      title: 'Raid',
      notes: 'Bring potions',
      priority: 'high',
      recurrence: 'weekly',
      scheduledDate: '2026-06-11',
      startTime: '18:15',
      endTime: '19:45',
      folderId: 3,
      myGameId: 9,
    }));

    const fallback = editQuestDraftForQuest(quest({
      dueDate: '2026-06-12T00:00:00.000Z',
    }), { defaultStartMinutes: 6 * 60 });

    expect(fallback.scheduledDate).toBe('2026-06-12');
    expect(fallback.startTime).toBe('06:00');
    expect(fallback.endTime).toBe('07:00');
  });

  it('parses quest draft ranges and rejects invalid ranges', () => {
    const draft = createQuestDraftForSlot(
      buildWeekDays(new Date(2026, 5, 11), new Date(2026, 5, 1))[3],
      12 * 60,
      null,
    );

    expect(combineDateTime('2026-06-11', '12:30')?.getHours()).toBe(12);
    expect(combineDateTime('nope', '12:30')).toBeNull();
    expect(questDraftRange(draft)).toEqual(jasmine.objectContaining({
      start: jasmine.any(Date),
      end: jasmine.any(Date),
    }));
    expect(questDraftRange({ ...draft, endTime: '11:00' })).toBeNull();
  });

  it('clamps durations to the visible schedule day', () => {
    expect(questDurationMinutes(quest({
      scheduledStartAt: new Date(2026, 5, 11, 18, 0).toISOString(),
      scheduledEndAt: new Date(2026, 5, 11, 19, 30).toISOString(),
    }))).toBe(90);
    expect(questDurationMinutes(quest())).toBe(60);
    expect(clampedDuration(23 * 60 + 45, 90)).toBe(30);
    expect(clampedEndMinutes(23 * 60 + 45, 90)).toBe(1439);
  });

  it('creates session drafts and library game options', () => {
    const draft = sessionDraftForSession(session({
      id: 6,
      myGameId: 9,
      gameName: 'Hades',
      scheduledAt: new Date(2026, 5, 11, 20, 15).toISOString(),
      durationMinutes: 75,
      notes: 'Boss route',
      completed: true,
    }));

    expect(draft).toEqual(jasmine.objectContaining({
      sessionId: 6,
      myGameId: 9,
      scheduledDate: '2026-06-11',
      startTime: '20:15',
      durationMinutes: 75,
      notes: 'Boss route',
      completed: true,
    }));

    const myGame = new MyGame(12);
    myGame.id = 9;
    myGame.game = new Game('Hades', null, null, null, null, 20);

    expect(toLibraryGame(myGame)).toEqual({
      myGameId: 9,
      gameName: 'Hades',
      playtime: 20,
    });
    expect(toLibraryGame(new MyGame(13))).toBeNull();
  });
});

function quest(overrides: Partial<Quest> = {}): Quest {
  return {
    id: 1,
    title: 'Quest',
    notes: null,
    type: 'sub',
    priority: 'medium',
    recurrence: 'none',
    dueDate: null,
    scheduledStartAt: null,
    scheduledEndAt: null,
    tags: [],
    completed: false,
    rewardXp: 20,
    sortOrder: 0,
    myGameId: null,
    gameName: null,
    skillId: null,
    skillName: null,
    folderId: null,
    folderName: null,
    subtasks: [],
    ...overrides,
  };
}

function session(overrides: Partial<GamingSession> = {}): GamingSession {
  return {
    id: 1,
    myGameId: null,
    gameName: null,
    scheduledAt: new Date(2026, 5, 11, 18, 0).toISOString(),
    durationMinutes: 60,
    completed: false,
    completedAt: null,
    notes: null,
    createdAt: new Date(2026, 5, 1).toISOString(),
    ...overrides,
  };
}
