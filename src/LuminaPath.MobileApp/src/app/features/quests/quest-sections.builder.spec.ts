import {
  buildQuestSections,
  collectAvailableTags,
  compareForManualOrder,
  compareQuests,
  countQuestsByFilter,
  filterQuests,
  nextQueuedQuest,
} from './quest-sections.builder';
import { Quest } from './services/quest-board.service';

function isoDate(now: Date, offsetDays: number): string {
  const d = new Date(now);
  d.setDate(d.getDate() + offsetDays);
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

function makeQuest(overrides: Partial<Quest> = {}): Quest {
  return {
    id: overrides.id ?? 1,
    title: overrides.title ?? 'Q',
    type: overrides.type ?? 'main',
    priority: overrides.priority ?? 'medium',
    recurrence: overrides.recurrence ?? 'none',
    tags: overrides.tags ?? [],
    completed: overrides.completed ?? false,
    rewardXp: overrides.rewardXp ?? 0,
    sortOrder: overrides.sortOrder ?? 0,
    subtasks: overrides.subtasks ?? [],
    ...overrides,
  } as Quest;
}

const NOW = new Date(2026, 4, 22); // Fri 22 May 2026

describe('quest-sections.builder', () => {
  describe('filterQuests', () => {
    it('"today" includes overdue + due-by-end-of-day + completed-today', () => {
      const quests: Quest[] = [
        makeQuest({ id: 1, title: 'overdue', dueDate: isoDate(NOW, -2) }),
        makeQuest({ id: 2, title: 'today',   dueDate: isoDate(NOW, 0) }),
        makeQuest({ id: 3, title: 'tomorrow', dueDate: isoDate(NOW, 1) }),
        makeQuest({ id: 4, title: 'inbox' }),
        makeQuest({
          id: 5, title: 'done-today',
          completed: true,
          completedAt: new Date(NOW.getFullYear(), NOW.getMonth(), NOW.getDate(), 10, 0).toISOString(),
        }),
        makeQuest({
          id: 6, title: 'done-yesterday',
          completed: true,
          completedAt: new Date(NOW.getFullYear(), NOW.getMonth(), NOW.getDate() - 1, 10, 0).toISOString(),
        }),
      ];
      const out = filterQuests(quests, { filter: 'today', searchQuery: '', tagFilter: null }, undefined, NOW);
      expect(out.map((q) => q.title).sort()).toEqual(['done-today', 'overdue', 'today']);
    });

    it('"upcoming" excludes today and completed, includes tomorrow onwards', () => {
      const quests: Quest[] = [
        makeQuest({ id: 1, dueDate: isoDate(NOW, 0), title: 'today' }),
        makeQuest({ id: 2, dueDate: isoDate(NOW, 1), title: 'tomorrow' }),
        makeQuest({ id: 3, dueDate: isoDate(NOW, 10), title: 'next-week' }),
        makeQuest({ id: 4, completed: true, dueDate: isoDate(NOW, 2), title: 'done' }),
      ];
      const out = filterQuests(quests, { filter: 'upcoming', searchQuery: '', tagFilter: null }, undefined, NOW);
      expect(out.map((q) => q.title)).toEqual(['tomorrow', 'next-week']);
    });

    it('"inbox" returns only open quests with no due date', () => {
      const quests: Quest[] = [
        makeQuest({ id: 1, title: 'a' }),
        makeQuest({ id: 2, title: 'b', dueDate: isoDate(NOW, 1) }),
        makeQuest({ id: 3, title: 'c', completed: true }),
      ];
      const out = filterQuests(quests, { filter: 'inbox', searchQuery: '', tagFilter: null }, undefined, NOW);
      expect(out.map((q) => q.title)).toEqual(['a']);
    });

    it('search matches title, notes, or tags case-insensitively', () => {
      const quests: Quest[] = [
        makeQuest({ id: 1, title: 'Walk the dog' }),
        makeQuest({ id: 2, title: 'Other', notes: 'remember dog food' }),
        makeQuest({ id: 3, title: 'Other2', tags: ['pet', 'DOG'] }),
        makeQuest({ id: 4, title: 'Unrelated' }),
      ];
      const out = filterQuests(quests, { filter: 'all', searchQuery: 'dog', tagFilter: null }, undefined, NOW);
      expect(out.map((q) => q.id).sort()).toEqual([1, 2, 3]);
    });

    it('tag filter matches case-insensitively', () => {
      const quests: Quest[] = [
        makeQuest({ id: 1, tags: ['Work'] }),
        makeQuest({ id: 2, tags: ['work', 'urgent'] }),
        makeQuest({ id: 3, tags: ['home'] }),
      ];
      const out = filterQuests(quests, { filter: 'all', searchQuery: '', tagFilter: 'work' }, undefined, NOW);
      expect(out.map((q) => q.id).sort()).toEqual([1, 2]);
    });

    it('sorts by priority desc → dueDate asc → sortOrder for non-manual cases', () => {
      const quests: Quest[] = [
        makeQuest({ id: 1, priority: 'low', dueDate: isoDate(NOW, 1), sortOrder: 0 }),
        makeQuest({ id: 2, priority: 'high', dueDate: isoDate(NOW, 5), sortOrder: 0 }),
        makeQuest({ id: 3, priority: 'high', dueDate: isoDate(NOW, 2), sortOrder: 0 }),
      ];
      // 'upcoming' filter forces the non-manual sort branch.
      const out = filterQuests(quests, { filter: 'upcoming', searchQuery: '', tagFilter: null }, undefined, NOW);
      expect(out.map((q) => q.id)).toEqual([3, 2, 1]); // high+earliest, high+later, low
    });

    it('uses manual order (sortOrder asc, completed last) for unscoped "all"', () => {
      const quests: Quest[] = [
        makeQuest({ id: 1, completed: true, completedAt: new Date(NOW.getTime() - 86_400_000).toISOString(), sortOrder: 5 }),
        makeQuest({ id: 2, priority: 'low', sortOrder: 2 }),
        makeQuest({ id: 3, priority: 'high', sortOrder: 1 }),
      ];
      const out = filterQuests(quests, { filter: 'all', searchQuery: '', tagFilter: null }, undefined, NOW);
      expect(out.map((q) => q.id)).toEqual([3, 2, 1]);
    });
  });

  describe('buildQuestSections', () => {
    it('returns a single "Matching quests" section when scoped by search', () => {
      const quests = [makeQuest({ id: 1, title: 'Cook dinner' })];
      const sections = buildQuestSections(quests, { filter: 'all', searchQuery: 'cook', tagFilter: null }, undefined, NOW);
      expect(sections.length).toBe(1);
      expect(sections[0].id).toBe('results');
      expect(sections[0].subtitle).toBe('1 found');
    });

    it('returns no sections when scoped search has no matches', () => {
      const quests = [makeQuest({ id: 1, title: 'Cook' })];
      const sections = buildQuestSections(quests, { filter: 'all', searchQuery: 'zzz', tagFilter: null }, undefined, NOW);
      expect(sections).toEqual([]);
    });

    it('"today" filter emits up to 3 sections (overdue / today / completed-today)', () => {
      const quests: Quest[] = [
        makeQuest({ id: 1, dueDate: isoDate(NOW, -1), title: 'overdue' }),
        makeQuest({ id: 2, dueDate: isoDate(NOW, 0), title: 'today' }),
        makeQuest({
          id: 3, title: 'done',
          completed: true,
          completedAt: new Date(NOW.getFullYear(), NOW.getMonth(), NOW.getDate(), 9, 0).toISOString(),
        }),
      ];
      const sections = buildQuestSections(quests, { filter: 'today', searchQuery: '', tagFilter: null }, undefined, NOW);
      expect(sections.map((s) => s.id)).toEqual(['overdue', 'today', 'completed']);
    });

    it('drops sections that end up empty', () => {
      const quests: Quest[] = [makeQuest({ id: 1, dueDate: isoDate(NOW, 0), title: 'today' })];
      const sections = buildQuestSections(quests, { filter: 'today', searchQuery: '', tagFilter: null }, undefined, NOW);
      expect(sections.map((s) => s.id)).toEqual(['today']);
    });

    it('"upcoming" splits into near-term (≤ 7d) and later', () => {
      const quests: Quest[] = [
        makeQuest({ id: 1, dueDate: isoDate(NOW, 2), title: 'near' }),
        makeQuest({ id: 2, dueDate: isoDate(NOW, 30), title: 'far' }),
      ];
      const sections = buildQuestSections(quests, { filter: 'upcoming', searchQuery: '', tagFilter: null }, undefined, NOW);
      expect(sections.map((s) => s.id)).toEqual(['soon', 'later']);
    });

    it('"all" produces the full 5-lane layout', () => {
      const quests: Quest[] = [
        makeQuest({ id: 1, dueDate: isoDate(NOW, -1), title: 'overdue' }),
        makeQuest({ id: 2, dueDate: isoDate(NOW, 0), title: 'today' }),
        makeQuest({ id: 3, title: 'inbox' }),
        makeQuest({ id: 4, dueDate: isoDate(NOW, 5), title: 'upcoming' }),
        makeQuest({ id: 5, title: 'done', completed: true,
          completedAt: new Date(NOW.getTime() - 2 * 86_400_000).toISOString() }),
      ];
      const sections = buildQuestSections(quests, { filter: 'all', searchQuery: '', tagFilter: null }, undefined, NOW);
      expect(sections.map((s) => s.id)).toEqual(['overdue', 'today', 'inbox', 'upcoming', 'completed']);
    });
  });

  describe('countQuestsByFilter', () => {
    const quests: Quest[] = [
      makeQuest({ id: 1, dueDate: isoDate(NOW, -1) }),     // overdue → 'today' counts
      makeQuest({ id: 2, dueDate: isoDate(NOW, 0) }),      // today → 'today' counts
      makeQuest({ id: 3, dueDate: isoDate(NOW, 3) }),      // upcoming
      makeQuest({ id: 4 }),                                // inbox
      makeQuest({ id: 5, completed: true }),               // completed → only 'all'
    ];

    it('"today" counts open quests due by end of today', () => {
      expect(countQuestsByFilter(quests, 'today', NOW)).toBe(2);
    });

    it('"upcoming" counts open quests due tomorrow or later', () => {
      expect(countQuestsByFilter(quests, 'upcoming', NOW)).toBe(1);
    });

    it('"inbox" counts open quests with no due date', () => {
      expect(countQuestsByFilter(quests, 'inbox', NOW)).toBe(1);
    });

    it('"all" counts everything including completed', () => {
      expect(countQuestsByFilter(quests, 'all', NOW)).toBe(5);
    });
  });

  describe('collectAvailableTags', () => {
    it('returns a sorted unique trimmed list', () => {
      const tags = collectAvailableTags([
        makeQuest({ tags: ['Work', 'home'] }),
        makeQuest({ tags: ['  home  ', '', 'urgent'] }),
      ]);
      expect(tags).toEqual(['home', 'urgent', 'Work']);
    });
  });

  describe('nextQueuedQuest', () => {
    it('prefers an inbox quest', () => {
      const inbox = makeQuest({ id: 1, title: 'inbox' });
      const future = makeQuest({ id: 2, dueDate: isoDate(NOW, 5), title: 'future' });
      expect(nextQueuedQuest([future, inbox], NOW)?.id).toBe(1);
    });

    it('falls back to a future quest when inbox is empty', () => {
      const a = makeQuest({ id: 1, dueDate: isoDate(NOW, 5) });
      const b = makeQuest({ id: 2, dueDate: isoDate(NOW, 2) });
      // Either is fine — builder returns the first match from the filter,
      // not the soonest-dated one (sorting is the section builder's job).
      const result = nextQueuedQuest([a, b], NOW);
      expect(result).not.toBeNull();
      expect([1, 2]).toContain(result!.id);
    });

    it('returns null when nothing is queueable', () => {
      expect(nextQueuedQuest([], NOW)).toBeNull();
      expect(nextQueuedQuest([makeQuest({ completed: true })], NOW)).toBeNull();
    });
  });

  describe('comparators', () => {
    it('compareQuests sinks completed quests to the bottom', () => {
      const a = makeQuest({ id: 1, completed: true });
      const b = makeQuest({ id: 2 });
      expect(compareQuests(a, b)).toBeGreaterThan(0);
      expect(compareQuests(b, a)).toBeLessThan(0);
    });

    it('compareForManualOrder uses sortOrder for active quests', () => {
      const a = makeQuest({ id: 1, sortOrder: 2 });
      const b = makeQuest({ id: 2, sortOrder: 5 });
      expect(compareForManualOrder(a, b)).toBeLessThan(0);
      expect(compareForManualOrder(b, a)).toBeGreaterThan(0);
    });
  });
});
