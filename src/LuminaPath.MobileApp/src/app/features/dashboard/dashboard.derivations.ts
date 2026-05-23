import { GamingSession } from '../planning/services/gaming-session.service';
import { Quest, QuestBoardState } from '../quests/services/quest-board.service';
import { GameStatus, isGameBacklogStatus } from '../library/models/library-status.model';
import { addDays, startOfDay } from 'src/app/shared/utils/date-helpers';

import {
  DashboardActivityItem,
  DashboardFocusItem,
  DashboardMediaItem,
  DashboardMetric,
} from './models/dashboard.model';
import { daysUntil, progressOf, releaseDateOf, remainingLabel, statusLabel } from './dashboard-view.helpers';

/**
 * Pure derivations the home page uses to turn raw fetched data into the
 * cards / lists / metrics rendered by its sub-components. Each function
 * takes exactly what it needs — no `this`, no Angular — so the page can
 * stay thin and the math is unit-testable in isolation.
 */

export function getUpcomingReleases(mediaItems: DashboardMediaItem[], now: Date = new Date()): DashboardMediaItem[] {
  const today = startOfDay(now);
  return mediaItems
    .filter((item) => releaseDateOf(item) >= today)
    .sort((a, b) => releaseDateOf(a).getTime() - releaseDateOf(b).getTime())
    .slice(0, 5);
}

export function getBacklogItems(ownedItems: DashboardMediaItem[]): DashboardMediaItem[] {
  return ownedItems
    .filter((item) => isGameBacklogStatus(item.status))
    .sort((a, b) => b.remainingHours - a.remainingHours)
    .slice(0, 4);
}

export function nextSession(sessions: GamingSession[], now: Date = new Date()): GamingSession | null {
  return [...sessions]
    .filter((session) => !session.completed && new Date(session.scheduledAt) >= now)
    .sort((a, b) => new Date(a.scheduledAt).getTime() - new Date(b.scheduledAt).getTime())[0] ?? null;
}

export function openQuests(board: QuestBoardState | null): Quest[] {
  return board?.quests.filter((quest) => !quest.completed) ?? [];
}

export function completedQuests(board: QuestBoardState | null): Quest[] {
  return [...(board?.quests ?? [])]
    .filter((quest) => quest.completed)
    .sort(
      (a, b) =>
        new Date(b.completedAt ?? b.updatedAt ?? '').getTime() -
        new Date(a.completedAt ?? a.updatedAt ?? '').getTime(),
    );
}

export type MetricsInput = {
  libraryTotal: number;
  ownedItemCount: number;
  activeItemCount: number;
  completedItemCount: number;
  completionRate: number;
  remainingHours: number;
};

export function buildMetrics(input: MetricsInput): DashboardMetric[] {
  const ownedItems = input.libraryTotal || input.ownedItemCount;
  return [
    {
      label: 'Library',
      value: String(ownedItems),
      detail: `${ownedItems} in your collection`,
      icon: 'library-outline',
      tone: 'blue',
    },
    {
      label: 'Active',
      value: String(input.activeItemCount),
      detail: input.activeItemCount === 1 ? 'currently active item' : 'currently active items',
      icon: 'game-controller-outline',
      tone: 'green',
    },
    {
      label: 'Completed',
      value: String(input.completedItemCount),
      detail: `${input.completionRate}% completion rate`,
      icon: 'checkmark-done-outline',
      tone: 'amber',
    },
    {
      label: 'Ahead',
      value: `${input.remainingHours}h`,
      detail: 'estimated backlog',
      icon: 'hourglass-outline',
      tone: 'rose',
    },
  ];
}

export type FocusInput = {
  sessions: GamingSession[];
  board: QuestBoardState | null;
  upcomingReleases: DashboardMediaItem[];
  backlogItems: DashboardMediaItem[];
  now?: Date;
};

export function buildFocusItems(input: FocusInput): DashboardFocusItem[] {
  const now = input.now ?? new Date();
  const today = startOfDay(now);
  const session = nextSession(input.sessions, now);
  const opens = openQuests(input.board);
  const dueQuests = opens.filter(
    (quest) => quest.dueDate && new Date(quest.dueDate) <= addDays(today, 1),
  ).length;
  const nextRelease = input.upcomingReleases[0] ?? null;
  const backlogPick = input.backlogItems[0] ?? null;

  return [
    {
      title: session?.gameName ?? 'Plan a session',
      detail: session
        ? `${formatShortDateTime(session.scheduledAt)} · ${Math.round(session.durationMinutes / 60 * 10) / 10}h`
        : 'No gaming session scheduled in the next two weeks',
      icon: 'calendar-clear-outline',
      tone: 'blue',
    },
    {
      title: dueQuests
        ? `${dueQuests} quest${dueQuests === 1 ? '' : 's'} need attention`
        : 'Quest board is calm',
      detail: dueQuests
        ? 'Due today or already waiting'
        : `${opens.length} open quest${opens.length === 1 ? '' : 's'}`,
      icon: dueQuests ? 'alert-circle-outline' : 'checkbox-outline',
      tone: dueQuests ? 'amber' : 'green',
    },
    {
      title: nextRelease?.name ?? 'No upcoming release',
      detail: nextRelease
        ? `${daysUntil(nextRelease)} · ${nextRelease.kind}`
        : 'Nothing dated in the loaded catalog',
      icon: 'sparkles-outline',
      tone: 'rose',
    },
    {
      title: backlogPick?.name ?? 'Backlog is clear',
      detail: backlogPick
        ? `${remainingLabel(backlogPick)} · ${statusLabel(backlogPick)}`
        : 'No planned commitment found',
      icon: 'hourglass-outline',
      tone: 'blue',
    },
  ];
}

export type ActivityInput = {
  sessions: GamingSession[];
  board: QuestBoardState | null;
  upcomingReleases: DashboardMediaItem[];
  recentItems: DashboardMediaItem[];
  now?: Date;
};

/** Mixed timeline of "what's next" rows — capped at 8 because the rail is
 *  bounded. Order is: upcoming sessions, recent quest completions, upcoming
 *  releases, recently-added library items. */
export function buildActivityItems(input: ActivityInput): DashboardActivityItem[] {
  const items: DashboardActivityItem[] = [];
  const now = input.now ?? new Date();
  const session = nextSession(input.sessions, now);

  if (session) {
    items.push({
      title: session.gameName ?? 'Generic gaming time',
      detail: `Session ${formatShortDateTime(session.scheduledAt)}`,
      icon: 'time-outline',
    });
  }

  for (const quest of completedQuests(input.board).slice(0, 3)) {
    items.push({
      title: quest.title,
      detail: `Quest completed${quest.gameName ? ' · ' + quest.gameName : ''}`,
      icon: 'checkmark-done-outline',
    });
  }

  for (const release of input.upcomingReleases.slice(0, 2)) {
    items.push({
      title: release.name,
      detail: `Releases ${daysUntil(release)} · ${release.kind}`,
      icon: 'calendar-clear-outline',
    });
  }

  for (const item of input.recentItems.slice(0, 3)) {
    items.push({
      title: item.name,
      detail: `Recently added · ${item.kind}`,
      icon: 'library-outline',
    });
  }

  return items.slice(0, 8);
}

/** "Featured" hero pick: first active, fallback to next release, fallback to most recent. */
export function pickFeaturedItem(
  playingItems: DashboardMediaItem[],
  upcomingReleases: DashboardMediaItem[],
  recentItems: DashboardMediaItem[],
): DashboardMediaItem | null {
  return playingItems[0] ?? upcomingReleases[0] ?? recentItems[0] ?? null;
}

export function topPlayingItems(ownedItems: DashboardMediaItem[]): DashboardMediaItem[] {
  return ownedItems
    .filter((item) => item.status === GameStatus.Playing)
    .sort((a, b) => progressOf(b) - progressOf(a))
    .slice(0, 4);
}

export function mostRecentItems(mediaItems: DashboardMediaItem[]): DashboardMediaItem[] {
  return [...mediaItems].sort((a, b) => b.id - a.id).slice(0, 8);
}

export function sumRemainingHours(items: DashboardMediaItem[]): number {
  return Math.round(items.reduce((sum, item) => sum + item.remainingHours, 0));
}

export function sumPlayedHours(items: DashboardMediaItem[]): number {
  return Math.round(items.reduce((sum, item) => sum + item.playedHours, 0));
}

export function completionRateFor(ownedItems: DashboardMediaItem[]): { completed: number; rate: number } {
  const completed = ownedItems.filter((item) => item.status === GameStatus.Completed).length;
  const rate = ownedItems.length === 0 ? 0 : Math.round((completed / ownedItems.length) * 100);
  return { completed, rate };
}

function formatShortDateTime(value: string): string {
  return new Intl.DateTimeFormat('en', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value));
}
