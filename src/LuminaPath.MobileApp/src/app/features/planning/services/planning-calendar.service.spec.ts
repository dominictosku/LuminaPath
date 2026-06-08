import { TestBed } from '@angular/core/testing';

import { Game } from '../../games/models/games.model';
import { Quest } from '../../quests/services/quest-board.service';
import { GamingSession } from './gaming-session.service';
import { PlanningCalendarService } from './planning-calendar.service';

function makeSession(overrides: Partial<GamingSession> = {}): GamingSession {
  return {
    id: 1,
    myGameId: null,
    gameName: null,
    scheduledAt: '2026-06-04T18:00:00.000Z',
    durationMinutes: 90,
    completed: false,
    completedAt: null,
    notes: null,
    createdAt: '2026-06-01T00:00:00.000Z',
    ...overrides,
  };
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
    rewardXp: 20,
    sortOrder: 0,
    myGameId: null,
    gameName: null,
    skillId: null,
    skillName: null,
    subtasks: [],
    ...overrides,
  };
}

describe('PlanningCalendarService', () => {
  let service: PlanningCalendarService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(PlanningCalendarService);
  });

  it('builds week calendars with sessions, quests, and releases in timeline order', () => {
    const game = Object.assign(new Game(), {
      id: 8,
      name: 'Silksong',
      releaseDate: new Date(2026, 5, 4),
    });
    const calendar = service.build({
      mode: 'week',
      anchor: new Date(2026, 5, 4),
      sessions: [makeSession({ id: 2, gameName: 'Hades', notes: 'Boss route' })],
      games: [game],
      quests: [makeQuest({ id: 3, title: 'Prepare route', dueDate: '2026-06-04T00:00:00.000Z', priority: 'high', recurrence: 'weekly' })],
    });

    expect(calendar.days.length).toBe(7);
    expect(calendar.title).toContain('Jun');

    const day = calendar.days.find((item) => item.key === '2026-06-04');
    expect(day?.events.map((event) => event.kind)).toEqual(['session', 'quest', 'release']);
    expect(day?.events.map((event) => event.title)).toEqual(['Hades', 'Prepare route', 'Silksong']);
    expect(day?.events.find((event) => event.kind === 'quest')).toEqual(jasmine.objectContaining({
      sourceId: 3,
      recurrence: 'weekly',
    }));
  });

  it('builds month calendars as six-week grids around the current month', () => {
    const calendar = service.build({
      mode: 'month',
      anchor: new Date(2026, 5, 15),
      sessions: [],
      games: [],
      quests: [],
    });

    expect(calendar.title).toBe('June 2026');
    expect(calendar.days.length).toBe(42);
    expect(calendar.days[0].key).toBe('2026-06-01');
  });

  it('groups sessions by local day and keeps buckets sorted', () => {
    const buckets = service.groupSessionsByDay([
      makeSession({ id: 2, scheduledAt: '2026-06-05T20:00:00.000Z' }),
      makeSession({ id: 1, scheduledAt: '2026-06-04T18:00:00.000Z' }),
      makeSession({ id: 3, scheduledAt: '2026-06-04T19:00:00.000Z' }),
    ]);

    expect(buckets.map((bucket) => bucket.key)).toEqual(['2026-06-04', '2026-06-05']);
    expect(buckets[0].sessions.map((session) => session.id)).toEqual([1, 3]);
  });

  it('shifts anchors by weeks or months', () => {
    expect(service.shiftAnchor(new Date(2026, 5, 4), 'week', 1).toDateString())
      .toBe(new Date(2026, 5, 11).toDateString());
    expect(service.shiftAnchor(new Date(2026, 5, 4), 'month', -1).toDateString())
      .toBe(new Date(2026, 4, 4).toDateString());
  });
});
