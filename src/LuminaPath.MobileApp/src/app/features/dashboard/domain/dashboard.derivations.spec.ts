import {
  buildActivityItems,
  buildFocusItems,
  buildMetrics,
  completedQuests,
  completionRateFor,
  getBacklogItems,
  getUpcomingReleases,
  mostRecentItems,
  nextSession,
  openQuests,
  pickFeaturedItem,
  sumPlayedHours,
  sumRemainingHours,
  topPlayingItems,
} from './dashboard.derivations';
import { DashboardMediaItem } from '../models/dashboard.model';
import { GameStatus } from '../../library/models/library-status.model';
import { GamingSession } from '../../planning/services/gaming-session.service';
import { Quest, QuestBoardState } from '../../quests/services/quest-board.service';

const NOW = new Date(2026, 4, 22);

function item(overrides: Partial<DashboardMediaItem> = {}): DashboardMediaItem {
  return {
    id: overrides.id ?? 1,
    kind: overrides.kind ?? 'Game',
    name: overrides.name ?? 'Item',
    description: overrides.description ?? '',
    genre: overrides.genre ?? '',
    releaseDate: overrides.releaseDate ?? null,
    image: overrides.image ?? null,
    status: overrides.status ?? GameStatus.Planned,
    estimatedHours: overrides.estimatedHours ?? 0,
    playedHours: overrides.playedHours ?? 0,
    remainingHours: overrides.remainingHours ?? 0,
    context: overrides.context ?? '',
  } as DashboardMediaItem;
}

function session(overrides: Partial<GamingSession> = {}): GamingSession {
  return {
    id: 1,
    myGameId: null,
    gameName: null,
    scheduledAt: new Date(NOW.getTime() + 86_400_000).toISOString(),
    durationMinutes: 60,
    completed: false,
    completedAt: null,
    notes: null,
    createdAt: NOW.toISOString(),
    ...overrides,
  } as GamingSession;
}

function quest(overrides: Partial<Quest> = {}): Quest {
  return {
    id: 1,
    title: 'Q',
    type: 'main',
    priority: 'medium',
    recurrence: 'none',
    tags: [],
    completed: false,
    rewardXp: 0,
    sortOrder: 0,
    subtasks: [],
    ...overrides,
  } as Quest;
}

function board(quests: Quest[]): QuestBoardState {
  return {
    xp: 0,
    currentStreakDays: 0,
    longestStreakDays: 0,
    quests,
    skills: [],
    achievements: [],
  };
}

describe('dashboard.derivations', () => {
  describe('getUpcomingReleases', () => {
    it('keeps only future releases, sorts ascending, caps at 5', () => {
      const items = [
        item({ id: 1, releaseDate: new Date(NOW.getTime() - 86_400_000) }),
        item({ id: 2, releaseDate: new Date(NOW.getTime() + 86_400_000) }),
        item({ id: 3, releaseDate: new Date(NOW.getTime() + 3 * 86_400_000) }),
        item({ id: 4, releaseDate: new Date(NOW.getTime() + 7 * 86_400_000) }),
        item({ id: 5, releaseDate: new Date(NOW.getTime() + 14 * 86_400_000) }),
        item({ id: 6, releaseDate: new Date(NOW.getTime() + 28 * 86_400_000) }),
        item({ id: 7, releaseDate: new Date(NOW.getTime() + 60 * 86_400_000) }),
      ];
      const out = getUpcomingReleases(items, NOW);
      expect(out.length).toBe(5);
      expect(out.map((i) => i.id)).toEqual([2, 3, 4, 5, 6]);
    });
  });

  describe('getBacklogItems', () => {
    it('keeps only backlog-status items, sorts by remaining hours desc, caps at 4', () => {
      const items = [
        item({ id: 1, status: GameStatus.Planned, remainingHours: 30 }),
        item({ id: 2, status: GameStatus.OnHold, remainingHours: 80 }),
        item({ id: 3, status: GameStatus.Completed, remainingHours: 0 }),
        item({ id: 4, status: GameStatus.Planned, remainingHours: 5 }),
        item({ id: 5, status: GameStatus.Planned, remainingHours: 50 }),
        item({ id: 6, status: GameStatus.OnHold, remainingHours: 10 }),
      ];
      const out = getBacklogItems(items);
      expect(out.map((i) => i.id)).toEqual([2, 5, 1, 6]);
    });
  });

  describe('nextSession', () => {
    it('returns the soonest non-completed session at or after now', () => {
      const s1 = session({ id: 1, scheduledAt: new Date(NOW.getTime() - 60_000).toISOString() });
      const s2 = session({ id: 2, scheduledAt: new Date(NOW.getTime() + 3_600_000).toISOString() });
      const s3 = session({ id: 3, scheduledAt: new Date(NOW.getTime() + 60_000).toISOString() });
      const completed = session({ id: 4, scheduledAt: new Date(NOW.getTime() + 30_000).toISOString(), completed: true });
      expect(nextSession([s1, s2, s3, completed], NOW)?.id).toBe(3);
    });

    it('returns null when no upcoming sessions', () => {
      expect(nextSession([], NOW)).toBeNull();
    });
  });

  describe('openQuests / completedQuests', () => {
    it('partitions quests by completed flag', () => {
      const opens = openQuests(board([quest({ id: 1 }), quest({ id: 2, completed: true })]));
      expect(opens.map((q) => q.id)).toEqual([1]);
    });

    it('sorts completed quests by completedAt desc', () => {
      const dones = completedQuests(board([
        quest({ id: 1, completed: true, completedAt: '2026-05-20T10:00:00Z' }),
        quest({ id: 2, completed: true, completedAt: '2026-05-22T10:00:00Z' }),
        quest({ id: 3, completed: true, completedAt: '2026-05-21T10:00:00Z' }),
      ]));
      expect(dones.map((q) => q.id)).toEqual([2, 3, 1]);
    });

    it('handles a null board safely', () => {
      expect(openQuests(null)).toEqual([]);
      expect(completedQuests(null)).toEqual([]);
    });
  });

  describe('buildMetrics', () => {
    it('emits the 4 expected cards with pluralisation', () => {
      const cards = buildMetrics({
        libraryTotal: 50,
        ownedItemCount: 12,
        activeItemCount: 1,
        completedItemCount: 5,
        completionRate: 42,
        remainingHours: 80,
      });
      expect(cards.map((c) => c.label)).toEqual(['Library', 'Active', 'Completed', 'Ahead']);
      expect(cards[0].value).toBe('50');
      expect(cards[1].detail).toBe('currently active item');
      expect(cards[2].detail).toBe('42% completion rate');
      expect(cards[3].value).toBe('80h');
    });

    it('falls back to ownedItemCount when libraryTotal is 0', () => {
      const cards = buildMetrics({
        libraryTotal: 0,
        ownedItemCount: 7,
        activeItemCount: 3,
        completedItemCount: 0,
        completionRate: 0,
        remainingHours: 0,
      });
      expect(cards[0].value).toBe('7');
      expect(cards[1].detail).toBe('currently active items');
    });
  });

  describe('buildFocusItems', () => {
    it('renders 4 cards covering session/quests/release/backlog', () => {
      const cards = buildFocusItems({
        sessions: [session({ id: 9, gameName: 'Hades', scheduledAt: new Date(NOW.getTime() + 7200_000).toISOString() })],
        board: board([
          quest({ id: 1, dueDate: '2026-05-22' }),
          quest({ id: 2, dueDate: '2026-05-23' }),
          quest({ id: 3 }),
        ]),
        upcomingReleases: [item({ id: 4, name: 'Game X', releaseDate: new Date(NOW.getTime() + 3 * 86_400_000), kind: 'Game' })],
        backlogItems: [item({ id: 5, name: 'Backlog Y', status: GameStatus.Planned, remainingHours: 30 })],
        now: NOW,
      });

      expect(cards.length).toBe(4);
      expect(cards[0].title).toBe('Hades');
      expect(cards[1].tone).toBe('amber');     // due quests > 0
      expect(cards[2].title).toBe('Game X');
      expect(cards[3].title).toBe('Backlog Y');
    });

    it('uses calm fallbacks when nothing is queued', () => {
      const cards = buildFocusItems({
        sessions: [],
        board: null,
        upcomingReleases: [],
        backlogItems: [],
        now: NOW,
      });
      expect(cards[0].title).toBe('Plan a session');
      expect(cards[1].tone).toBe('green');
      expect(cards[2].title).toBe('No upcoming release');
      expect(cards[3].title).toBe('Backlog is clear');
    });
  });

  describe('buildActivityItems', () => {
    it('returns mixed timeline rows capped at 8', () => {
      const items = buildActivityItems({
        sessions: [session({ id: 1, gameName: 'Hades', scheduledAt: new Date(NOW.getTime() + 3600_000).toISOString() })],
        board: board([
          quest({ id: 11, title: 'A', completed: true, completedAt: NOW.toISOString() }),
          quest({ id: 12, title: 'B', completed: true, completedAt: NOW.toISOString() }),
        ]),
        upcomingReleases: [
          item({ id: 21, name: 'R1', releaseDate: new Date(NOW.getTime() + 86_400_000) }),
        ],
        recentItems: [
          item({ id: 31, name: 'New 1' }),
          item({ id: 32, name: 'New 2' }),
        ],
        now: NOW,
      });
      // 1 session + 2 completed quests + 1 release + 2 recent = 6 rows.
      expect(items.length).toBe(6);
      expect(items[0].title).toBe('Hades');
    });
  });

  describe('utilities', () => {
    it('topPlayingItems keeps only Playing, sorts by progress desc, caps at 4', () => {
      const items = [
        item({ id: 1, status: GameStatus.Playing, playedHours: 5, estimatedHours: 10 }),
        item({ id: 2, status: GameStatus.Playing, playedHours: 8, estimatedHours: 10 }),
        item({ id: 3, status: GameStatus.Completed, playedHours: 10, estimatedHours: 10 }),
      ];
      const out = topPlayingItems(items);
      expect(out.map((i) => i.id)).toEqual([2, 1]);
    });

    it('mostRecentItems sorts by id desc, caps at 8', () => {
      const items = Array.from({ length: 15 }, (_, i) => item({ id: i + 1 }));
      const out = mostRecentItems(items);
      expect(out.length).toBe(8);
      expect(out[0].id).toBe(15);
      expect(out[7].id).toBe(8);
    });

    it('sumRemainingHours / sumPlayedHours round to integer', () => {
      const items = [item({ remainingHours: 5.4 }), item({ remainingHours: 3.3 }), item({ remainingHours: 1.1 })];
      expect(sumRemainingHours(items)).toBe(10);
      const played = [item({ playedHours: 1.2 }), item({ playedHours: 0.7 })];
      expect(sumPlayedHours(played)).toBe(2);
    });

    it('completionRateFor returns 0% for empty input', () => {
      expect(completionRateFor([])).toEqual({ completed: 0, rate: 0 });
    });

    it('completionRateFor computes percentage', () => {
      const items = [
        item({ id: 1, status: GameStatus.Completed }),
        item({ id: 2, status: GameStatus.Playing }),
        item({ id: 3, status: GameStatus.Planned }),
        item({ id: 4, status: GameStatus.Completed }),
      ];
      const { completed, rate } = completionRateFor(items);
      expect(completed).toBe(2);
      expect(rate).toBe(50);
    });

    it('pickFeaturedItem prefers playing → upcoming → recent → null', () => {
      const p = item({ id: 1 });
      const u = item({ id: 2 });
      const r = item({ id: 3 });
      expect(pickFeaturedItem([p], [u], [r])?.id).toBe(1);
      expect(pickFeaturedItem([], [u], [r])?.id).toBe(2);
      expect(pickFeaturedItem([], [], [r])?.id).toBe(3);
      expect(pickFeaturedItem([], [], [])).toBeNull();
    });
  });
});
