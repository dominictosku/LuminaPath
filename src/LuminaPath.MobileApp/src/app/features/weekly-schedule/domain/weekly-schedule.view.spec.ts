import type { GamingSession } from '../../planning/services/gaming-session.service';
import type { Quest, QuestFolder } from '../../quests/services/quest-board.service';
import {
  buildSidebarQuestGroups,
  cloneSessionWindow,
  expandedSessionWindow,
  overviewSessionWindow,
  sessionInWindow,
  sessionWindowContains,
  upsertSessionInWindow,
  visibleGames,
  weekSessionWindow,
  type SessionWindow,
} from './weekly-schedule.view';

describe('weekly schedule view helpers', () => {
  it('groups open quests by folder, search query, and schedule sort order', () => {
    const groups = buildSidebarQuestGroups([
      quest({ id: 3, title: 'Later', folderId: 2, scheduledStartAt: '2026-06-12T18:00:00.000Z' }),
      quest({ id: 1, title: 'Soon', folderId: 2, scheduledStartAt: '2026-06-11T18:00:00.000Z' }),
      quest({ id: 2, title: 'Done', folderId: 2, completed: true }),
      quest({ id: 4, title: 'Unfiled boss', tags: ['boss'] }),
    ], [
      folder({ id: 2, name: 'Focus' }),
      folder({ id: 5, name: 'Empty' }),
    ], '');

    expect(groups.map((group) => group.key)).toEqual(['folder-2', 'unfiled']);
    expect(groups[0].quests.map((item) => item.id)).toEqual([1, 3]);
    expect(groups[1].quests.map((item) => item.id)).toEqual([4]);

    const searched = buildSidebarQuestGroups([
      quest({ id: 1, title: 'Daily route', folderId: 2 }),
      quest({ id: 2, title: 'Boss prep', tags: ['raid'] }),
    ], [folder({ id: 2 })], 'raid');

    expect(searched.map((group) => group.key)).toEqual(['unfiled']);
    expect(searched[0].quests.map((item) => item.id)).toEqual([2]);
  });

  it('filters visible games by query and caps the result count', () => {
    const games = Array.from({ length: 45 }, (_, index) => ({
      myGameId: index + 1,
      gameName: index % 2 === 0 ? `Hades ${index}` : `Zelda ${index}`,
      playtime: null,
    }));

    expect(visibleGames(games, '').length).toBe(40);
    expect(visibleGames(games, 'zelda', 3).map((game) => game.gameName)).toEqual([
      'Zelda 1',
      'Zelda 3',
      'Zelda 5',
    ]);
  });

  it('builds week, overview, and expanded session windows', () => {
    const week = weekSessionWindow(new Date(2026, 5, 11));
    expect(localKey(week.from)).toBe('2026-06-08');
    expect(localKey(week.to)).toBe('2026-06-15');

    const overviewWeek = overviewSessionWindow('week', new Date(2026, 5, 11));
    expect(localKey(overviewWeek.from)).toBe('2026-06-08');
    expect(localKey(overviewWeek.to)).toBe('2026-06-15');

    const overviewMonth = overviewSessionWindow('month', new Date(2026, 7, 15));
    expect(localKey(overviewMonth.from)).toBe('2026-07-27');
    expect(localKey(overviewMonth.to)).toBe('2026-09-07');

    const expanded = expandedSessionWindow(overviewWeek, 2);
    expect(localKey(expanded.from)).toBe('2026-06-06');
    expect(localKey(expanded.to)).toBe('2026-06-17');
  });

  it('checks window containment and clones cached ranges defensively', () => {
    const cached = window(new Date(2026, 5, 1), new Date(2026, 5, 30));
    const visible = window(new Date(2026, 5, 8), new Date(2026, 5, 15));
    const outside = window(new Date(2026, 4, 25), new Date(2026, 5, 2));

    expect(sessionWindowContains(cached, visible)).toBeTrue();
    expect(sessionWindowContains(cached, outside)).toBeFalse();
    expect(sessionWindowContains(null, visible)).toBeFalse();

    const cloned = cloneSessionWindow(visible);
    expect(cloned).toEqual(visible);
    expect(cloned.from).not.toBe(visible.from);
    expect(cloned.to).not.toBe(visible.to);
  });

  it('upserts sessions only while they are inside the requested window', () => {
    const range = window(new Date(2026, 5, 8), new Date(2026, 5, 15));
    const existing = [
      session({ id: 1, scheduledAt: new Date(2026, 5, 9, 18).toISOString() }),
      session({ id: 2, scheduledAt: new Date(2026, 5, 10, 18).toISOString() }),
    ];
    const updated = session({ id: 2, scheduledAt: new Date(2026, 5, 11, 18).toISOString() });
    const movedOutside = session({ id: 1, scheduledAt: new Date(2026, 5, 16, 18).toISOString() });

    expect(sessionInWindow(updated, range)).toBeTrue();
    expect(upsertSessionInWindow(existing, updated, range).map((item) => item.id)).toEqual([1, 2]);
    expect(upsertSessionInWindow(existing, movedOutside, range).map((item) => item.id)).toEqual([2]);
  });
});

function localKey(value: Date): string {
  return [
    value.getFullYear(),
    String(value.getMonth() + 1).padStart(2, '0'),
    String(value.getDate()).padStart(2, '0'),
  ].join('-');
}

function window(from: Date, to: Date): SessionWindow {
  return { from, to };
}

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
