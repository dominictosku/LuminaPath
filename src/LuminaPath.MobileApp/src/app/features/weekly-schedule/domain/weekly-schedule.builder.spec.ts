import type { GamingSession } from '../../planning/services/gaming-session.service';
import type { Quest, QuestFolder } from '../../quests/services/quest-board.service';
import { buildWeekDays } from './weekly-schedule.helpers';
import { buildWeeklyScheduleBlocksByDay } from './weekly-schedule.builder';

describe('weekly schedule builder', () => {
  it('builds scheduled quest, projected recurrence, and session blocks by day', () => {
    const days = buildWeekDays(new Date(2026, 5, 11), new Date(2026, 5, 1));
    const folders = [folder({ id: 5, color: '#123456' })];
    const blocks = buildWeeklyScheduleBlocksByDay({
      days,
      folders,
      quests: [
        quest({
          id: 1,
          title: 'Raid prep',
          folderId: 5,
          folderName: 'Focus',
          scheduledStartAt: new Date(2026, 5, 11, 21, 0).toISOString(),
          scheduledEndAt: new Date(2026, 5, 11, 22, 0).toISOString(),
        }),
        quest({
          id: 2,
          title: 'Weekly reset',
          recurrence: 'weekly',
          folderId: 5,
          scheduledStartAt: new Date(2026, 5, 4, 18, 30).toISOString(),
          scheduledEndAt: new Date(2026, 5, 4, 19, 30).toISOString(),
        }),
      ],
      sessions: [
        session({
          id: 7,
          gameName: 'Hades',
          scheduledAt: new Date(2026, 5, 11, 18, 45).toISOString(),
          durationMinutes: 90,
        }),
      ],
      groupVisibility: { quests: true, sessions: true },
    });

    const thursday = blocks.get('2026-06-11') ?? [];

    expect(thursday.map((block) => block.id)).toEqual([
      'quest-2-projected-2026-06-11',
      'session-7',
      'quest-1',
    ]);
    expect(thursday.find((block) => block.id === 'quest-2-projected-2026-06-11')).toEqual(jasmine.objectContaining({
      projected: true,
      sourceId: 2,
      color: '#123456',
      subtitle: 'Projected repeat',
    }));
    expect(thursday.find((block) => block.id === 'session-7')).toEqual(jasmine.objectContaining({
      kind: 'session',
      title: 'Hades',
      subtitle: '90 min',
      color: '#06b6d4',
    }));
  });

  it('honors calendar group visibility', () => {
    const days = buildWeekDays(new Date(2026, 5, 11), new Date(2026, 5, 1));
    const blocks = buildWeeklyScheduleBlocksByDay({
      days,
      folders: [],
      quests: [
        quest({
          id: 1,
          scheduledStartAt: new Date(2026, 5, 11, 20, 0).toISOString(),
          scheduledEndAt: new Date(2026, 5, 11, 21, 0).toISOString(),
        }),
      ],
      sessions: [
        session({
          id: 7,
          scheduledAt: new Date(2026, 5, 11, 18, 45).toISOString(),
        }),
      ],
      groupVisibility: { quests: false, sessions: true },
    });

    expect((blocks.get('2026-06-11') ?? []).map((block) => block.id)).toEqual(['session-7']);
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

function folder(overrides: Partial<QuestFolder> = {}): QuestFolder {
  return {
    id: 1,
    name: 'Focus',
    emoji: 'F',
    color: '#7c3aed',
    sortOrder: 0,
    ...overrides,
  };
}
