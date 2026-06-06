import { QuestPriority, QuestRecurrence, QuestType } from '../services/quest-board.service';
import { QuestFilter } from '../domain/quest-sections.builder';

export type PageMode = 'quests' | 'overview' | 'skills' | 'tree';
export type ModalMode = 'skill' | 'node' | null;

export type LibraryGame = {
  myGameId: number;
  gameName: string;
};

export type FilterOption = {
  value: QuestFilter;
  label: string;
  icon: string;
};

export type PriorityOption = {
  value: QuestPriority;
  label: string;
  weight: number;
};

export type TypeOption = {
  value: QuestType;
  label: string;
  icon: string;
};

export type RecurrenceOption = {
  value: QuestRecurrence;
  label: string;
};

export type SkillIconOption = {
  label: string;
  icon: string;
};

export const SKILL_ICON_OPTIONS: readonly SkillIconOption[] = [
  { label: 'Code', icon: 'code-slash-outline' },
  { label: 'Art', icon: 'brush-outline' },
  { label: 'Cook', icon: 'restaurant-outline' },
  { label: 'Study', icon: 'book-outline' },
  { label: 'Craft', icon: 'school-outline' },
];

export const SKILL_COLOR_OPTIONS: readonly string[] = [
  '#2563eb',
  '#0891b2',
  '#0f766e',
  '#7c3aed',
  '#be123c',
];

export const QUEST_TYPE_OPTIONS: readonly TypeOption[] = [
  { value: 'main', label: 'Main', icon: 'map-outline' },
  { value: 'sub', label: 'Sub', icon: 'flag-outline' },
  { value: 'faction', label: 'Faction', icon: 'shield-checkmark-outline' },
];

export const QUEST_PRIORITY_OPTIONS: readonly PriorityOption[] = [
  { value: 'low', label: 'Low', weight: 0 },
  { value: 'medium', label: 'Medium', weight: 1 },
  { value: 'high', label: 'High', weight: 2 },
];

export const QUEST_RECURRENCE_OPTIONS: readonly RecurrenceOption[] = [
  { value: 'none', label: 'No repeat' },
  { value: 'daily', label: 'Daily' },
  { value: 'weekly', label: 'Weekly' },
  { value: 'monthly', label: 'Monthly' },
];

export const QUEST_FILTER_OPTIONS: readonly FilterOption[] = [
  { value: 'today', label: 'Focus', icon: 'today-outline' },
  { value: 'upcoming', label: 'Upcoming', icon: 'calendar-outline' },
  { value: 'inbox', label: 'Inbox', icon: 'library-outline' },
  { value: 'all', label: 'All', icon: 'filter-outline' },
];

// ---- Label / icon lookups -------------------------------------------------
// Pure helpers shared by the page shell (detail sheet) and the quest-list
// view so the option arrays are the single source of truth for display text.

export function questTypeLabel(type: QuestType): string {
  return QUEST_TYPE_OPTIONS.find((option) => option.value === type)?.label ?? type;
}

export function questTypeIcon(type: QuestType): string {
  return QUEST_TYPE_OPTIONS.find((option) => option.value === type)?.icon ?? 'flag-outline';
}

export function questPriorityLabel(priority: QuestPriority): string {
  return QUEST_PRIORITY_OPTIONS.find((option) => option.value === priority)?.label ?? 'Medium';
}

export function questRecurrenceLabel(recurrence: QuestRecurrence): string {
  return QUEST_RECURRENCE_OPTIONS.find((option) => option.value === recurrence)?.label ?? 'No repeat';
}
