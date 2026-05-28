import { addDays, startOfDay } from 'src/app/shared/utils/date-helpers';

import { Quest } from '../services/quest-board.service';

export type QuestDueState = 'overdue' | 'today' | 'soon' | 'later' | 'none';

/**
 * Shared pure helpers for reasoning about a quest's due date. Used by both
 * the quest-card (per-row pill rendering) and the quest-sections builder
 * (which lane a quest belongs to). Kept here so the two implementations
 * never drift.
 *
 * Pass `now` only in tests where you want a deterministic clock — production
 * callers default to the wall clock.
 */

export function dueState(quest: Quest, now: Date = new Date()): QuestDueState {
  if (!quest.dueDate) return 'none';
  const due = new Date(quest.dueDate);
  if (Number.isNaN(due.getTime())) return 'none';
  const today = startOfDay(now);
  const dueDay = startOfDay(due);
  if (dueDay < today) return 'overdue';
  if (dueDay.getTime() === today.getTime()) return 'today';
  const diff = (dueDay.getTime() - today.getTime()) / 86_400_000;
  return diff <= 3 ? 'soon' : 'later';
}

export function dueDateLabel(quest: Quest, now: Date = new Date()): string {
  if (!quest.dueDate) return '';
  const due = new Date(quest.dueDate);
  if (Number.isNaN(due.getTime())) return '';
  const today = startOfDay(now);
  const dueDay = startOfDay(due);
  const diffDays = Math.round((dueDay.getTime() - today.getTime()) / 86_400_000);

  if (diffDays === 0) return 'Today';
  if (diffDays === 1) return 'Tomorrow';
  if (diffDays === -1) return 'Yesterday';
  if (diffDays < -1 && diffDays >= -7) return `${Math.abs(diffDays)}d overdue`;
  if (diffDays > 1 && diffDays <= 7) return `In ${diffDays}d`;

  return new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric' }).format(due);
}

/** True if the quest is open and its due date falls before tomorrow's start. */
export function isDueByEndOfToday(quest: Quest, now: Date = new Date()): boolean {
  if (quest.completed || !quest.dueDate) return false;
  const tomorrow = addDays(startOfDay(now), 1);
  return new Date(quest.dueDate) < tomorrow;
}
