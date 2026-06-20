import type { Quest } from '../../quests/services/quest-board.service';
import type { WeekScheduleBlock } from './weekly-schedule.helpers';
import {
  blockDragPayload,
  blockDragPreview,
  dragData,
  dragId,
  dragPreviewTransform,
  gameDragPayload,
  gameDragPreview,
  priorityLabel,
  questDragPayload,
  questDragPreview,
} from './weekly-schedule.drag';

describe('weekly schedule drag helpers', () => {
  it('serializes drag payload ids and data consistently', () => {
    expect(dragId({ type: 'quest', questId: 4 })).toBe('quest-4');
    expect(dragId({
      type: 'questOccurrence',
      questId: 4,
      occurrenceDate: '2026-06-11',
      scheduledStartAt: '2026-06-11T18:00:00.000Z',
      scheduledEndAt: '2026-06-11T19:00:00.000Z',
    })).toBe('quest-4-projected-2026-06-11');
    expect(dragId({ type: 'game', myGameId: 7 })).toBe('game-7');
    expect(dragId({ type: 'session', sessionId: 9 })).toBe('session-9');

    expect(dragData({ type: 'quest', questId: 4 })).toBe('quest:4');
    expect(dragData({
      type: 'questOccurrence',
      questId: 4,
      occurrenceDate: '2026-06-11',
      scheduledStartAt: '2026-06-11T18:00:00.000Z',
      scheduledEndAt: '2026-06-11T19:00:00.000Z',
    })).toBe('quest-occurrence:4:2026-06-11');
    expect(dragData({ type: 'game', myGameId: 7 })).toBe('game:7');
    expect(dragData({ type: 'session', sessionId: 9 })).toBe('session:9');
  });

  it('builds quest and game payloads with useful preview text', () => {
    const quest = makeQuest({
      id: 4,
      title: 'Raid prep',
      gameName: 'Hades',
      scheduledStartAt: '2026-06-11T18:00:00.000Z',
    });
    const game = { myGameId: 7, gameName: 'Zelda', playtime: 45 };

    expect(questDragPayload(quest)).toEqual({ type: 'quest', questId: 4 });
    expect(questDragPreview(quest, '#123456')).toEqual({
      kind: 'quest',
      title: 'Raid prep',
      subtitle: 'Hades',
      action: 'Move quest',
      color: '#123456',
    });
    expect(gameDragPayload(game)).toEqual({ type: 'game', myGameId: 7 });
    expect(gameDragPreview(game)).toEqual({
      kind: 'game',
      title: 'Zelda',
      subtitle: '45h estimate',
      action: 'Plan session',
      color: '#06b6d4',
    });
  });

  it('falls back to priority labels for quest previews', () => {
    expect(priorityLabel('high')).toBe('High priority');
    expect(questDragPreview(makeQuest({ priority: 'low', gameName: null, folderName: null }), '#7c3aed').subtitle)
      .toBe('Low priority');
  });

  it('builds block payloads including projected occurrences', () => {
    expect(blockDragPayload(block({ id: 'quest-4', sourceId: 12, kind: 'quest' })))
      .toEqual({ type: 'quest', questId: 12 });
    expect(blockDragPayload(block({ id: 'session-9', kind: 'session', sourceId: undefined })))
      .toEqual({ type: 'session', sessionId: 9 });
    expect(blockDragPayload(block({
      id: 'quest-4-projected-2026-06-11',
      sourceId: 4,
      kind: 'quest',
      projected: true,
      occurrenceDate: '2026-06-11',
      startAt: '2026-06-11T18:00:00.000Z',
      endAt: '2026-06-11T19:00:00.000Z',
    }))).toEqual({
      type: 'questOccurrence',
      questId: 4,
      occurrenceDate: '2026-06-11',
      scheduledStartAt: '2026-06-11T18:00:00.000Z',
      scheduledEndAt: '2026-06-11T19:00:00.000Z',
    });
  });

  it('builds block preview content and transforms preview position', () => {
    const preview = blockDragPreview(block({ title: 'Raid prep', subtitle: 'Boss route' }), 'Move quest');

    expect(preview).toEqual(jasmine.objectContaining({
      kind: 'quest',
      title: 'Raid prep',
      action: 'Move quest',
      color: '#7c3aed',
    }));
    expect(preview.subtitle).toContain('Boss route');
    expect(dragPreviewTransform(null)).toBe('translate3d(0, 0, 0)');
    expect(dragPreviewTransform({ ...preview, x: 10, y: 20 })).toBe('translate3d(28px, 38px, 0)');
  });
});

function makeQuest(overrides: Partial<Quest> = {}): Quest {
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

function block(overrides: Partial<WeekScheduleBlock> = {}): WeekScheduleBlock {
  return {
    id: 'quest-1',
    sourceId: 1,
    kind: 'quest',
    title: 'Quest',
    subtitle: '',
    startAt: new Date(2026, 5, 11, 18, 0).toISOString(),
    endAt: new Date(2026, 5, 11, 19, 30).toISOString(),
    color: '#7c3aed',
    recurrence: 'none',
    completed: false,
    lane: 0,
    laneCount: 1,
    ...overrides,
  };
}
