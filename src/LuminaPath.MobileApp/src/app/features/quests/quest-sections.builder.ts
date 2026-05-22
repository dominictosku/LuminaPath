import { addDays, startOfDay } from 'src/app/shared/utils/date-helpers';

import { isDueByEndOfToday, dueState } from './quest-due';
import { Quest, QuestPriority } from './services/quest-board.service';

export type QuestFilter = 'today' | 'upcoming' | 'inbox' | 'all';

export type QuestSection = {
  id: string;
  title: string;
  subtitle: string;
  icon: string;
  tone: 'danger' | 'warning' | 'accent' | 'muted' | 'success';
  quests: Quest[];
};

/** Default priority weights used by the comparator when sorting by priority.
 *  Page can pass an override if the priorityOptions array ever diverges. */
export const DEFAULT_PRIORITY_WEIGHTS: Record<QuestPriority, number> = {
  low: 0,
  medium: 1,
  high: 2,
};

export type PriorityWeightFn = (priority: QuestPriority) => number;

export type QuestViewOptions = {
  filter: QuestFilter;
  searchQuery: string;
  tagFilter: string | null;
};

const NEAR_TERM_DAYS = 7;

/**
 * Pure section/filter logic for the quest-board. Extracted from the page so
 * each branch (today / upcoming / inbox / all + search + tag) can be
 * exercised without spinning up Angular. The page becomes a thin observer
 * that calls `buildQuestSections(...)` and `filterQuests(...)`.
 */

/** Sort key: priority desc → due date asc → sortOrder asc, with completed last. */
export function compareQuests(
  a: Quest,
  b: Quest,
  priorityWeight: PriorityWeightFn = (p) => DEFAULT_PRIORITY_WEIGHTS[p] ?? 1,
): number {
  if (a.completed !== b.completed) return a.completed ? 1 : -1;
  if (a.completed && b.completed) {
    return completedAtDesc(a, b);
  }
  const aPrio = priorityWeight(a.priority);
  const bPrio = priorityWeight(b.priority);
  if (aPrio !== bPrio) return bPrio - aPrio;
  const aDue = a.dueDate ? new Date(a.dueDate).getTime() : Number.MAX_SAFE_INTEGER;
  const bDue = b.dueDate ? new Date(b.dueDate).getTime() : Number.MAX_SAFE_INTEGER;
  if (aDue !== bDue) return aDue - bDue;
  return a.sortOrder - b.sortOrder;
}

/** Manual-order sort: respects sortOrder strictly within the active list,
 *  completed quests sink to the bottom, most-recent-completion first. */
export function compareForManualOrder(a: Quest, b: Quest): number {
  if (a.completed !== b.completed) return a.completed ? 1 : -1;
  if (a.completed && b.completed) {
    return completedAtDesc(a, b);
  }
  return a.sortOrder - b.sortOrder;
}

/** Apply filter + search + tag and return the sorted visible list. The
 *  inbox-with-no-scope case uses manual ordering so drag-to-reorder works. */
export function filterQuests(
  quests: Quest[],
  options: QuestViewOptions,
  priorityWeight: PriorityWeightFn = (p) => DEFAULT_PRIORITY_WEIGHTS[p] ?? 1,
  now: Date = new Date(),
): Quest[] {
  const today = startOfDay(now);
  const tomorrow = addDays(today, 1);
  const search = options.searchQuery.trim().toLowerCase();
  const tag = options.tagFilter?.toLowerCase() ?? null;

  const matchesFilter = (quest: Quest): boolean => {
    switch (options.filter) {
      case 'today':
        if (quest.completed) {
          return quest.completedAt
            ? startOfDay(new Date(quest.completedAt)).getTime() === today.getTime()
            : false;
        }
        if (!quest.dueDate) return false;
        return new Date(quest.dueDate) < tomorrow;
      case 'upcoming':
        if (quest.completed || !quest.dueDate) return false;
        return new Date(quest.dueDate) >= tomorrow;
      case 'inbox':
        return !quest.completed && !quest.dueDate;
      case 'all':
      default:
        return true;
    }
  };

  const matchesSearch = (quest: Quest): boolean =>
    !search ||
    quest.title.toLowerCase().includes(search) ||
    (quest.notes ?? '').toLowerCase().includes(search) ||
    (quest.tags ?? []).some((t) => t.toLowerCase().includes(search));

  const matchesTag = (quest: Quest): boolean =>
    !tag || (quest.tags ?? []).some((t) => t.toLowerCase() === tag);

  const filtered = quests.filter(
    (q) => matchesFilter(q) && matchesSearch(q) && matchesTag(q),
  );

  if (options.filter === 'all' && !search && !tag) {
    return [...filtered].sort(compareForManualOrder);
  }
  return [...filtered].sort((a, b) => compareQuests(a, b, priorityWeight));
}

/**
 * Group the (already-filtered) list into UI sections. Scoped by search or
 * tag → a single "Matching quests" section; otherwise the section set
 * depends on which top-level filter is active.
 */
export function buildQuestSections(
  quests: Quest[],
  options: QuestViewOptions,
  priorityWeight: PriorityWeightFn = (p) => DEFAULT_PRIORITY_WEIGHTS[p] ?? 1,
  now: Date = new Date(),
): QuestSection[] {
  const visible = filterQuests(quests, options, priorityWeight, now);
  const scoped = Boolean(options.searchQuery.trim() || options.tagFilter);

  if (scoped) {
    return visible.length
      ? [{
          id: 'results',
          title: 'Matching quests',
          subtitle: `${visible.length} found`,
          icon: 'search-outline',
          tone: 'accent',
          quests: visible,
        }]
      : [];
  }

  const sort = (list: Quest[]) =>
    [...list].sort((a, b) => compareQuests(a, b, priorityWeight));

  if (options.filter === 'today') {
    return [
      section('overdue', 'Overdue', 'Needs a decision', 'alert-circle-outline', 'danger',
        sort(activeQuestsByDue(quests, 'overdue', now))),
      section('today', 'Today', 'Your current focus', 'today-outline', 'warning',
        sort(activeQuestsByDue(quests, 'today', now))),
      section('completed', 'Completed today', 'Momentum banked', 'checkmark-circle-outline', 'success',
        sort(completedToday(quests, now))),
    ].filter((s) => s.quests.length);
  }

  if (options.filter === 'upcoming') {
    return [
      section('soon', 'Next few days', 'Close enough to plan', 'calendar-outline', 'accent',
        sort(upcomingQuests(quests, NEAR_TERM_DAYS, now))),
      section('later', 'Later', 'Scheduled, not urgent', 'calendar-clear-outline', 'muted',
        sort(laterQuests(quests, NEAR_TERM_DAYS, now))),
    ].filter((s) => s.quests.length);
  }

  if (options.filter === 'inbox') {
    return [
      section('inbox', 'Inbox', 'Captured without a date', 'library-outline', 'muted', visible),
    ].filter((s) => s.quests.length);
  }

  return [
    section('overdue', 'Overdue', 'Needs a decision', 'alert-circle-outline', 'danger',
      sort(activeQuestsByDue(quests, 'overdue', now))),
    section('today', 'Today', 'Your current focus', 'today-outline', 'warning',
      sort(activeQuestsByDue(quests, 'today', now))),
    section('inbox', 'Inbox', 'Captured without a date', 'library-outline', 'muted',
      activeInboxQuests(quests)),
    section('upcoming', 'Upcoming', 'Scheduled ahead', 'calendar-outline', 'accent',
      sort(activeFutureQuests(quests, now))),
    section('completed', 'Completed', 'Recently finished', 'checkmark-circle-outline', 'success',
      sort(completedQuests(quests))),
  ].filter((s) => s.quests.length);
}

/** Per-filter chip counts in the toolbar. */
export function countQuestsByFilter(
  quests: Quest[],
  filter: QuestFilter,
  now: Date = new Date(),
): number {
  const today = startOfDay(now);
  const tomorrow = addDays(today, 1);

  switch (filter) {
    case 'today':
      return quests.filter((q) => isDueByEndOfToday(q, now)).length;
    case 'upcoming':
      return quests.filter(
        (q) => !q.completed && Boolean(q.dueDate) && new Date(q.dueDate as string) >= tomorrow,
      ).length;
    case 'inbox':
      return quests.filter((q) => !q.completed && !q.dueDate).length;
    case 'all':
    default:
      return quests.length;
  }
}

/** Sorted unique tag list across all quests. */
export function collectAvailableTags(quests: Quest[]): string[] {
  const set = new Set<string>();
  for (const quest of quests) {
    for (const tag of quest.tags ?? []) {
      const trimmed = tag.trim();
      if (trimmed) set.add(trimmed);
    }
  }
  return Array.from(set).sort((a, b) => a.localeCompare(b));
}

/** First inbox or future quest the user could pull into today — used by
 *  the "nothing here" empty-state shortcut. */
export function nextQueuedQuest(quests: Quest[], now: Date = new Date()): Quest | null {
  return activeInboxQuests(quests)[0] ?? activeFutureQuests(quests, now)[0] ?? null;
}

// ─── Internals ─────────────────────────────────────────────────────────────

function section(
  id: string,
  title: string,
  subtitle: string,
  icon: string,
  tone: QuestSection['tone'],
  quests: Quest[],
): QuestSection {
  return { id, title, subtitle, icon, tone, quests };
}

function activeQuestsByDue(quests: Quest[], state: 'overdue' | 'today', now: Date): Quest[] {
  return quests.filter((q) => !q.completed && dueState(q, now) === state);
}

function activeInboxQuests(quests: Quest[]): Quest[] {
  return quests
    .filter((q) => !q.completed && !q.dueDate)
    .sort(compareForManualOrder);
}

function activeFutureQuests(quests: Quest[], now: Date): Quest[] {
  const today = startOfDay(now);
  return quests.filter((q) => {
    if (q.completed || !q.dueDate) return false;
    return startOfDay(new Date(q.dueDate)) > today;
  });
}

function upcomingQuests(quests: Quest[], days: number, now: Date): Quest[] {
  const today = startOfDay(now);
  const tomorrow = addDays(today, 1);
  const limit = addDays(today, days);
  return quests.filter((q) => {
    if (q.completed || !q.dueDate) return false;
    const due = startOfDay(new Date(q.dueDate));
    return due >= tomorrow && due <= limit;
  });
}

function laterQuests(quests: Quest[], days: number, now: Date): Quest[] {
  const limit = addDays(startOfDay(now), days);
  return quests.filter((q) => {
    if (q.completed || !q.dueDate) return false;
    return startOfDay(new Date(q.dueDate)) > limit;
  });
}

function completedToday(quests: Quest[], now: Date): Quest[] {
  const today = startOfDay(now).getTime();
  return quests.filter(
    (q) => q.completed && q.completedAt &&
      startOfDay(new Date(q.completedAt)).getTime() === today,
  );
}

function completedQuests(quests: Quest[]): Quest[] {
  return quests.filter((q) => q.completed);
}

function completedAtDesc(a: Quest, b: Quest): number {
  const aAt = a.completedAt ? new Date(a.completedAt).getTime() : 0;
  const bAt = b.completedAt ? new Date(b.completedAt).getTime() : 0;
  return bAt - aAt;
}
