import type { GamingSession } from '../../planning/services/gaming-session.service';
import type { CalendarMode } from '../../planning/services/planning-calendar.service';
import type { Quest, QuestFolder } from '../../quests/services/quest-board.service';
import {
  startOfCalendarMonthGrid,
  startOfWeek,
} from '../../planning/domain/planning-calendar.helpers';
import type { LibraryGameOption } from './weekly-schedule.drafts';

export type SessionWindow = { from: Date; to: Date };

export type SidebarQuestGroup = {
  key: string;
  folder: QuestFolder | null;
  quests: Quest[];
};

export function buildSidebarQuestGroups(
  quests: Quest[],
  folders: QuestFolder[],
  search: string,
): SidebarQuestGroup[] {
  const query = search.trim().toLowerCase();
  const openQuests = quests
    .filter((quest) => !quest.completed)
    .filter((quest) => matchesQuestSearch(quest, query))
    .sort((a, b) => questSortKey(a).localeCompare(questSortKey(b)));

  const folderGroups = folders
    .map((folder) => ({
      key: `folder-${folder.id}`,
      folder,
      quests: openQuests.filter((quest) => quest.folderId === folder.id),
    }))
    .filter((group) => group.quests.length > 0);

  const unfiled = openQuests.filter((quest) => (quest.folderId ?? null) === null);
  return [
    ...folderGroups,
    ...(unfiled.length ? [{ key: 'unfiled', folder: null, quests: unfiled }] : []),
  ];
}

export function visibleGames(
  games: LibraryGameOption[],
  search: string,
  limit = 40,
): LibraryGameOption[] {
  const query = search.trim().toLowerCase();
  return games
    .filter((game) => !query || game.gameName.toLowerCase().includes(query))
    .slice(0, limit);
}

export function overviewSessionWindow(mode: CalendarMode, anchor: Date): SessionWindow {
  const start = mode === 'week'
    ? startOfWeek(anchor)
    : startOfCalendarMonthGrid(anchor);
  const to = new Date(start);
  to.setDate(start.getDate() + (mode === 'week' ? 7 : 42));
  return { from: start, to };
}

export function expandedSessionWindow(window: SessionWindow, paddingDays: number): SessionWindow {
  const from = new Date(window.from);
  from.setDate(from.getDate() - paddingDays);
  const to = new Date(window.to);
  to.setDate(to.getDate() + paddingDays);
  return { from, to };
}

export function sessionWindowContains(range: SessionWindow | null, window: SessionWindow): boolean {
  return range !== null
    && range.from.getTime() <= window.from.getTime()
    && range.to.getTime() >= window.to.getTime();
}

export function cloneSessionWindow(window: SessionWindow): SessionWindow {
  return {
    from: new Date(window.from),
    to: new Date(window.to),
  };
}

export function weekSessionWindow(anchor: Date): SessionWindow {
  const from = startOfWeek(anchor);
  const to = new Date(from);
  to.setDate(from.getDate() + 7);
  return { from, to };
}

export function upsertSessionInWindow(
  sessions: GamingSession[],
  session: GamingSession,
  window: SessionWindow,
): GamingSession[] {
  const withoutSession = sessions.filter((item) => item.id !== session.id);
  return sessionInWindow(session, window)
    ? [...withoutSession, session]
    : withoutSession;
}

export function sessionInWindow(session: GamingSession, window: SessionWindow): boolean {
  const time = new Date(session.scheduledAt).getTime();
  return time >= window.from.getTime() && time < window.to.getTime();
}

export function questSortKey(quest: Quest): string {
  const schedule = quest.scheduledStartAt ?? quest.dueDate ?? '9999';
  return `${schedule}|${quest.sortOrder.toString().padStart(5, '0')}|${quest.title.toLowerCase()}`;
}

function matchesQuestSearch(quest: Quest, query: string): boolean {
  if (!query) return true;
  return [
    quest.title,
    quest.gameName ?? '',
    quest.folderName ?? '',
    ...(quest.tags ?? []),
  ].some((value) => value.toLowerCase().includes(query));
}
