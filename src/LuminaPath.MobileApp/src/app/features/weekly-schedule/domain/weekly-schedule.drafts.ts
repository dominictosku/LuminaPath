import type { MyGame } from '../../games/models/games.model';
import type { GamingSession } from '../../planning/services/gaming-session.service';
import type { Quest, QuestPriority, QuestRecurrence, QuestType } from '../../quests/services/quest-board.service';
import {
  SLOT_MINUTES,
  WEEK_END_HOUR,
  dateAtMinutes,
  dateKey,
  minutesSinceDayStart,
  timeLabelFromMinutes,
  type WeekScheduleDay,
} from './weekly-schedule.helpers';

export type LibraryGameOption = {
  myGameId: number;
  gameName: string;
  playtime: number | null;
};

export type QuestScheduleDraft = {
  mode: 'create' | 'edit';
  questId: number | null;
  title: string;
  notes: string;
  type: QuestType;
  priority: QuestPriority;
  recurrence: QuestRecurrence;
  scheduledDate: string;
  startTime: string;
  endTime: string;
  folderId: number | null;
  myGameId: number | null;
};

export type SessionScheduleDraft = {
  sessionId: number;
  myGameId: number | null;
  scheduledDate: string;
  startTime: string;
  durationMinutes: number;
  notes: string;
  completed: boolean;
};

export function createQuestDraftForSlot(
  day: WeekScheduleDay,
  minutes: number,
  selectedGame: LibraryGameOption | null,
): QuestScheduleDraft {
  const start = dateAtMinutes(day.date, minutes);
  const endMinutes = clampedEndMinutes(minutes, 60);

  return {
    mode: 'create',
    questId: null,
    title: selectedGame ? `Play ${selectedGame.gameName}` : '',
    notes: '',
    type: 'sub',
    priority: 'medium',
    recurrence: 'none',
    scheduledDate: dateKey(start),
    startTime: timeLabelFromMinutes(minutes),
    endTime: timeLabelFromMinutes(endMinutes),
    folderId: null,
    myGameId: selectedGame?.myGameId ?? null,
  };
}

export function editQuestDraftForQuest(
  quest: Quest,
  options: { defaultStartMinutes: number; fallbackDate?: Date },
): QuestScheduleDraft {
  const fallbackDate = quest.dueDate ? new Date(quest.dueDate) : options.fallbackDate ?? new Date();
  const start = quest.scheduledStartAt
    ? new Date(quest.scheduledStartAt)
    : dateAtMinutes(fallbackDate, options.defaultStartMinutes);
  const end = quest.scheduledEndAt ? new Date(quest.scheduledEndAt) : new Date(start);

  if (!quest.scheduledEndAt) {
    end.setMinutes(start.getMinutes() + 60);
  }

  return {
    mode: 'edit',
    questId: quest.id,
    title: quest.title,
    notes: quest.notes ?? '',
    type: quest.type,
    priority: quest.priority,
    recurrence: quest.recurrence,
    scheduledDate: dateKey(start),
    startTime: timeLabelFromMinutes(minutesSinceDayStart(start)),
    endTime: timeLabelFromMinutes(minutesSinceDayStart(end)),
    folderId: quest.folderId ?? null,
    myGameId: quest.myGameId ?? null,
  };
}

export function sessionDraftForSession(session: GamingSession): SessionScheduleDraft {
  const start = new Date(session.scheduledAt);
  return {
    sessionId: session.id,
    myGameId: session.myGameId ?? null,
    scheduledDate: dateKey(start),
    startTime: timeLabelFromMinutes(minutesSinceDayStart(start)),
    durationMinutes: session.durationMinutes,
    notes: session.notes ?? '',
    completed: session.completed,
  };
}

export function questDraftRange(draft: QuestScheduleDraft): { start: Date; end: Date } | null {
  const start = combineDateTime(draft.scheduledDate, draft.startTime);
  const end = combineDateTime(draft.scheduledDate, draft.endTime);
  if (!start || !end || end <= start) {
    return null;
  }
  return { start, end };
}

export function combineDateTime(dateValue: string, timeValue: string): Date | null {
  const [year, month, day] = dateValue.split('-').map(Number);
  const [hour, minute] = timeValue.split(':').map(Number);
  if ([year, month, day, hour, minute].some((value) => !Number.isFinite(value))) {
    return null;
  }
  return new Date(year, month - 1, day, hour, minute, 0, 0);
}

export function questDurationMinutes(quest: Pick<Quest, 'scheduledStartAt' | 'scheduledEndAt'>): number {
  if (!quest.scheduledStartAt || !quest.scheduledEndAt) {
    return 60;
  }

  const start = new Date(quest.scheduledStartAt).getTime();
  const end = new Date(quest.scheduledEndAt).getTime();
  const minutes = Math.round((end - start) / 60_000);
  return Number.isFinite(minutes) && minutes > 0 ? minutes : 60;
}

export function clampedDuration(startMinutes: number, requestedMinutes: number): number {
  const remainingMinutes = WEEK_END_HOUR * 60 - startMinutes;
  return Math.max(SLOT_MINUTES, Math.min(requestedMinutes, remainingMinutes));
}

export function clampedEndMinutes(startMinutes: number, requestedMinutes: number): number {
  return Math.min(startMinutes + clampedDuration(startMinutes, requestedMinutes), WEEK_END_HOUR * 60 - 1);
}

export function toLibraryGame(myGame: MyGame): LibraryGameOption | null {
  const gameName = myGame.game?.name;
  if (!gameName) {
    return null;
  }

  return {
    myGameId: myGame.id,
    gameName,
    playtime: myGame.game?.playtime ?? null,
  };
}
