import { buildQuestBoardStats, titleForQuestLevel } from './quest-board-stats';
import { Quest, QuestSkill } from '../services/quest-board.service';

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
    rewardXp: 0,
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
    id: 1,
    name: 'Skill',
    icon: 'code-slash-outline',
    color: '#2563eb',
    xp: 0,
    nodes: [],
    unlockedNodes: [],
    ...overrides,
  };
}

describe('quest board stats', () => {
  it('derives level progress and title from XP', () => {
    const stats = buildQuestBoardStats(450, [], []);

    expect(stats.level).toBe(3);
    expect(stats.title).toBe('Apprentice');
    expect(stats.xpIntoLevel).toBe(50);
    expect(stats.xpProgress).toBe(0.25);
  });

  it('counts active, completed, due-today, overdue, and unlocked nodes', () => {
    const now = new Date('2026-05-27T12:00:00Z');
    const stats = buildQuestBoardStats(
      0,
      [
        makeQuest({ id: 1, dueDate: '2026-05-27', completed: false }),
        makeQuest({ id: 2, dueDate: '2026-05-26', completed: false }),
        makeQuest({ id: 3, dueDate: '2026-05-27', completed: true }),
      ],
      [makeSkill({ unlockedNodes: [0, 1] })],
      now,
    );

    expect(stats.activeQuestCount).toBe(2);
    expect(stats.completedQuestCount).toBe(1);
    expect(stats.todayCount).toBe(1);
    expect(stats.overdueCount).toBe(1);
    expect(stats.unlockedNodeCount).toBe(2);
  });

  it('maps level thresholds to titles', () => {
    expect(titleForQuestLevel(1)).toBe('Initiate');
    expect(titleForQuestLevel(3)).toBe('Apprentice');
    expect(titleForQuestLevel(6)).toBe('Adept');
    expect(titleForQuestLevel(10)).toBe('Master');
    expect(titleForQuestLevel(15)).toBe('Legend');
  });
});
