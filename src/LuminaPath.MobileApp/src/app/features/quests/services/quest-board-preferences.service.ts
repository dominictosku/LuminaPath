import { Injectable } from '@angular/core';

import { QuestPriority, QuestRecurrence, QuestType } from './quest-board.service';
import { QuestViewMode } from '../components/quest-board-toolbar/quest-board-toolbar.component';
import { QuestFilter } from '../domain/quest-sections.builder';

export type QuestBoardPreferences = {
  filter: QuestFilter;
  quickAddType: QuestType;
  quickAddPriority: QuestPriority;
  quickAddRecurrence: QuestRecurrence;
  questViewMode: QuestViewMode;
};

const STORAGE_KEY = 'questboard.prefs.v1';
const DEFAULT_PREFERENCES: QuestBoardPreferences = {
  filter: 'today',
  quickAddType: 'sub',
  quickAddPriority: 'medium',
  quickAddRecurrence: 'none',
  questViewMode: 'cards',
};

const filters: readonly QuestFilter[] = ['today', 'upcoming', 'inbox', 'all'];
const types: readonly QuestType[] = ['main', 'sub', 'faction'];
const priorities: readonly QuestPriority[] = ['low', 'medium', 'high'];
const recurrences: readonly QuestRecurrence[] = ['none', 'daily', 'weekly', 'monthly'];
const viewModes: readonly QuestViewMode[] = ['cards', 'compact'];

type StoredPreferences = Partial<Omit<QuestBoardPreferences, 'filter'>> & {
  filter?: QuestFilter | 'overdue';
};

@Injectable({ providedIn: 'root' })
export class QuestBoardPreferencesService {
  load(): QuestBoardPreferences {
    const preferences = { ...DEFAULT_PREFERENCES };
    const storage = globalThis.localStorage;
    if (!storage) return preferences;

    try {
      const raw = storage.getItem(STORAGE_KEY);
      if (!raw) return preferences;

      const parsed = JSON.parse(raw) as StoredPreferences;
      return {
        filter: this.validFilter(parsed.filter) ?? preferences.filter,
        quickAddType: this.validValue(parsed.quickAddType, types) ?? preferences.quickAddType,
        quickAddPriority: this.validValue(parsed.quickAddPriority, priorities) ?? preferences.quickAddPriority,
        quickAddRecurrence: this.validValue(parsed.quickAddRecurrence, recurrences) ?? preferences.quickAddRecurrence,
        questViewMode: this.validValue(parsed.questViewMode, viewModes) ?? preferences.questViewMode,
      };
    } catch {
      return preferences;
    }
  }

  save(preferences: QuestBoardPreferences): void {
    const storage = globalThis.localStorage;
    if (!storage) return;

    try {
      storage.setItem(STORAGE_KEY, JSON.stringify(preferences));
    } catch {
      // Ignore quota and storage-access failures; preferences are non-critical.
    }
  }

  private validFilter(value: StoredPreferences['filter']): QuestFilter | null {
    if (value === 'overdue') return 'today';
    return this.validValue(value, filters);
  }

  private validValue<T extends string>(value: unknown, allowed: readonly T[]): T | null {
    return typeof value === 'string' && allowed.includes(value as T) ? value as T : null;
  }
}
