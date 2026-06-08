import type { GamingSession } from '../../planning/services/gaming-session.service';
import type { Quest, QuestFolder, QuestPriority } from '../../quests/services/quest-board.service';
import {
  dateKey,
  projectedRecurringOccurrenceForDay,
} from '../../planning/domain/planning-calendar.helpers';
import {
  layoutOverlappingBlocks,
  type WeekScheduleBlock,
  type WeekScheduleDay,
} from './weekly-schedule.helpers';

export type WeeklyScheduleCalendarGroupId = 'quests' | 'sessions';

export type WeeklyScheduleBuildInput = {
  days: WeekScheduleDay[];
  quests: Quest[];
  sessions: GamingSession[];
  folders: QuestFolder[];
  groupVisibility: Record<WeeklyScheduleCalendarGroupId, boolean>;
};

const DEFAULT_QUEST_COLOR = '#7c3aed';
const SESSION_COLOR = '#06b6d4';

export function buildWeeklyScheduleBlocksByDay(input: WeeklyScheduleBuildInput): Map<string, WeekScheduleBlock[]> {
  const map = new Map<string, WeekScheduleBlock[]>();

  for (const day of input.days) {
    map.set(day.key, layoutOverlappingBlocks(buildWeeklyScheduleBlocksForDay(input, day)));
  }

  return map;
}

export function buildWeeklyScheduleBlocksForDay(
  input: WeeklyScheduleBuildInput,
  day: WeekScheduleDay,
): WeekScheduleBlock[] {
  const blocks: WeekScheduleBlock[] = [];

  if (input.groupVisibility.quests) {
    for (const quest of input.quests) {
      const color = folderColor(input.folders, quest.folderId);
      if (isScheduledQuest(quest) && dateKey(new Date(quest.scheduledStartAt!)) === day.key) {
        blocks.push({
          id: `quest-${quest.id}`,
          sourceId: quest.id,
          kind: 'quest',
          title: quest.title,
          subtitle: quest.gameName ?? quest.folderName ?? priorityLabel(quest.priority),
          startAt: quest.scheduledStartAt!,
          endAt: quest.scheduledEndAt!,
          color,
          recurrence: quest.recurrence,
          completed: quest.completed,
          lane: 0,
          laneCount: 1,
        });
      }

      const projected = projectedRecurringOccurrenceForDay(quest, day.date);
      if (projected) {
        blocks.push({
          id: `quest-${quest.id}-projected-${day.key}`,
          sourceId: quest.id,
          kind: 'quest',
          title: quest.title,
          subtitle: 'Projected repeat',
          startAt: projected.start.toISOString(),
          endAt: projected.end.toISOString(),
          color,
          recurrence: quest.recurrence,
          projected: true,
          completed: false,
          lane: 0,
          laneCount: 1,
        });
      }
    }
  }

  if (input.groupVisibility.sessions) {
    for (const session of input.sessions) {
      const start = new Date(session.scheduledAt);
      if (dateKey(start) !== day.key) {
        continue;
      }

      const end = new Date(start);
      end.setMinutes(start.getMinutes() + session.durationMinutes);
      blocks.push({
        id: `session-${session.id}`,
        kind: 'session',
        title: session.gameName ?? 'Gaming session',
        subtitle: session.notes || `${session.durationMinutes} min`,
        startAt: start.toISOString(),
        endAt: end.toISOString(),
        color: SESSION_COLOR,
        completed: session.completed,
        lane: 0,
        laneCount: 1,
      });
    }
  }

  return blocks;
}

export function isScheduledQuest(quest: Pick<Quest, 'scheduledStartAt' | 'scheduledEndAt'>): boolean {
  return Boolean(quest.scheduledStartAt && quest.scheduledEndAt);
}

export function folderColor(folders: QuestFolder[], folderId: number | null | undefined): string {
  if (folderId == null) return DEFAULT_QUEST_COLOR;
  return folders.find((folder) => folder.id === folderId)?.color || DEFAULT_QUEST_COLOR;
}

function priorityLabel(priority: QuestPriority): string {
  return priority === 'high'
    ? 'High priority'
    : priority === 'low'
      ? 'Low priority'
      : 'Medium priority';
}
