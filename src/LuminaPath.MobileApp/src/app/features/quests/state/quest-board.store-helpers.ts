import { toISODate } from 'src/app/shared/utils/date-helpers';

import type { QuestEditDraft } from '../components/quest-detail-sheet/quest-detail-sheet.component';
import type { QuestQuickAddSubmit } from '../components/quest-quick-add/quest-quick-add.component';
import type { LibraryGame } from '../models/quest-board-view.model';
import type {
  Quest,
  QuestCreate,
  QuestFolder,
  QuestSkill,
  QuestSubtask,
  QuestUpdate,
} from '../services/quest-board.service';
import type { SkillNodeState } from './quest-board.state';

export interface QuestFolderGroup {
  section: string | null;
  folders: QuestFolder[];
}

export function gameNameFor(library: readonly LibraryGame[], myGameId: number): string {
  return library.find((game) => game.myGameId === myGameId)?.gameName ?? 'Unknown game';
}

export function skillNameFor(skills: readonly QuestSkill[], skillId: number): string {
  return skills.find((skill) => skill.id === skillId)?.name ?? 'Skill';
}

export function replaceQuest(quests: readonly Quest[], id: number, updated: Quest): Quest[] {
  return quests.map((quest) => (quest.id === id ? updated : quest));
}

export function replaceSkill(skills: readonly QuestSkill[], id: number, updated: QuestSkill): QuestSkill[] {
  return skills.map((skill) => (skill.id === id ? updated : skill));
}

export function buildQuestEditDraft(quest: Quest): QuestEditDraft {
  return {
    title: quest.title,
    notes: quest.notes ?? '',
    type: quest.type,
    priority: quest.priority,
    recurrence: quest.recurrence,
    dueDate: quest.dueDate ? toISODate(new Date(quest.dueDate)) : null,
    tags: (quest.tags ?? []).join(', '),
    myGameId: quest.myGameId ?? null,
    skillId: quest.skillId ?? null,
    folderId: quest.folderId ?? null,
  };
}

export function groupFoldersBySection(folders: readonly QuestFolder[]): QuestFolderGroup[] {
  const groups = new Map<string, QuestFolderGroup>();
  const orderedKeys: string[] = [];

  for (const folder of folders) {
    const key = (folder.sectionName ?? '').trim().toLowerCase();
    if (!groups.has(key)) {
      groups.set(key, { section: folder.sectionName ?? null, folders: [] });
      orderedKeys.push(key);
    }
    groups.get(key)!.folders.push(folder);
  }

  orderedKeys.sort((a, b) => (a === '' ? -1 : b === '' ? 1 : 0));
  return orderedKeys.map((key) => groups.get(key)!);
}

export function collectKnownSectionsFromFolders(folders: readonly QuestFolder[]): string[] {
  const seen = new Set<string>();
  const sections: string[] = [];

  for (const folder of folders) {
    const name = (folder.sectionName ?? '').trim();
    if (!name) continue;
    const key = name.toLowerCase();
    if (seen.has(key)) continue;
    seen.add(key);
    sections.push(name);
  }

  return sections;
}

export function buildQuestDropListIds(folders: readonly QuestFolder[]): string[] {
  return [...folders.map((folder) => `quest-drop-folder-${folder.id}`), 'quest-drop-unfiled'];
}

export function getSkillNodeState(skill: QuestSkill, nodeIndex: number): SkillNodeState {
  if (skill.unlockedNodes.includes(nodeIndex)) return 'completed';
  const next = skill.nodes.findIndex((_, index) => !skill.unlockedNodes.includes(index));
  return nodeIndex === next ? 'available' : 'locked';
}

export function parseQuestTags(input: string): string[] {
  return input
    .split(',')
    .map((tag) => tag.trim())
    .filter((tag) => tag.length > 0);
}

export function buildOptimisticQuest(
  payload: QuestQuickAddSubmit,
  tempId: number,
  nowIso: string,
  gameName: string | null,
  skillName: string | null,
): Quest {
  return {
    id: tempId,
    title: payload.title,
    notes: null,
    type: payload.type,
    priority: payload.priority,
    recurrence: payload.recurrence,
    dueDate: payload.dueDate ?? null,
    scheduledStartAt: null,
    scheduledEndAt: null,
    tags: [],
    completed: false,
    createdAt: nowIso,
    updatedAt: nowIso,
    rewardXp: 0,
    sortOrder: 0,
    myGameId: payload.myGameId ?? null,
    gameName,
    skillId: payload.skillId ?? null,
    skillName,
    subtasks: [],
  };
}

export function buildQuestCreateInput(payload: QuestQuickAddSubmit): QuestCreate {
  return {
    title: payload.title,
    type: payload.type,
    priority: payload.priority,
    recurrence: payload.recurrence,
    dueDate: payload.dueDate ?? null,
    myGameId: payload.myGameId ?? null,
    skillId: payload.skillId ?? null,
  };
}

export function buildEditedQuest(
  quest: Quest,
  draft: QuestEditDraft,
  tags: readonly string[],
  notes: string | null,
  gameName: string | null,
  skillName: string | null,
  folder: QuestFolder | null,
): Quest {
  return {
    ...quest,
    title: draft.title.trim(),
    notes,
    type: draft.type,
    priority: draft.priority,
    recurrence: draft.recurrence,
    dueDate: draft.dueDate,
    tags: [...tags],
    myGameId: draft.myGameId ?? null,
    gameName,
    skillId: draft.skillId ?? null,
    skillName,
    folderId: draft.folderId ?? null,
    folderName: folder?.name ?? null,
    folderEmoji: folder?.emoji ?? null,
  };
}

export function buildQuestUpdateFromDraft(
  draft: QuestEditDraft,
  tags: readonly string[],
  notes: string | null,
): QuestUpdate {
  return {
    title: draft.title.trim(),
    notes,
    type: draft.type,
    priority: draft.priority,
    recurrence: draft.recurrence,
    dueDate: draft.dueDate,
    clearDueDate: draft.dueDate == null,
    tags: [...tags],
    myGameId: draft.myGameId ?? undefined,
    clearMyGame: draft.myGameId == null,
    skillId: draft.skillId ?? undefined,
    clearSkill: draft.skillId == null,
    folderId: draft.folderId ?? undefined,
    clearFolder: draft.folderId == null,
  };
}

export function buildOptimisticSubtask(id: number, title: string, sortOrder: number): QuestSubtask {
  return {
    id,
    title,
    completed: false,
    sortOrder,
  };
}
