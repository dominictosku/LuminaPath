import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { RequestCache } from 'src/app/shared/services/request-cache.service';

export type QuestType = 'main' | 'sub' | 'faction';

export type QuestPriority = 'low' | 'medium' | 'high';

export type QuestRecurrence = 'none' | 'daily' | 'weekly' | 'monthly';

export type Quest = {
  id: number;
  title: string;
  notes?: string | null;
  type: QuestType;
  priority: QuestPriority;
  recurrence: QuestRecurrence;
  dueDate?: string | null;
  tags: string[];
  completed: boolean;
  completedAt?: string;
  createdAt?: string;
  updatedAt?: string;
  rewardXp: number;
  sortOrder: number;
  myGameId?: number | null;
  gameName?: string | null;
  skillId?: number | null;
  skillName?: string | null;
  folderId?: number | null;
  folderName?: string | null;
  folderEmoji?: string | null;
  subtasks: QuestSubtask[];
};

export type QuestFolder = {
  id: number;
  name: string;
  emoji: string;
  color?: string | null;
  sectionName?: string | null;
  sortOrder: number;
};

export type QuestFolderCreate = {
  name: string;
  emoji: string;
  color?: string | null;
  sectionName?: string | null;
};

export type QuestFolderUpdate = {
  name?: string;
  emoji?: string;
  color?: string | null;
  clearColor?: boolean;
  sectionName?: string | null;
  clearSectionName?: boolean;
  sortOrder?: number;
};

export type QuestSubtask = {
  id: number;
  title: string;
  completed: boolean;
  completedAt?: string;
  sortOrder: number;
};

export type QuestCreate = {
  title: string;
  notes?: string | null;
  type?: QuestType;
  priority?: QuestPriority;
  recurrence?: QuestRecurrence;
  dueDate?: string | null;
  tags?: string[];
  myGameId?: number | null;
  skillId?: number | null;
  folderId?: number | null;
};

export type QuestUpdate = {
  title?: string;
  notes?: string | null;
  type?: QuestType;
  priority?: QuestPriority;
  recurrence?: QuestRecurrence;
  dueDate?: string | null;
  clearDueDate?: boolean;
  tags?: string[];
  completed?: boolean;
  myGameId?: number | null;
  clearMyGame?: boolean;
  sortOrder?: number;
  skillId?: number | null;
  clearSkill?: boolean;
  folderId?: number | null;
  clearFolder?: boolean;
};

export type QuestSubtaskUpdate = {
  title?: string;
  completed?: boolean;
  sortOrder?: number;
};

export type QuestReorderItem = {
  id: number;
  sortOrder: number;
  type: QuestType;
};

export type QuestMutationResult = {
  quest: Quest;
  spawnedQuest?: Quest | null;
  totalXp: number;
  currentStreakDays: number;
  longestStreakDays: number;
  awardedSkillXp?: number | null;
  awardedSkillId?: number | null;
  unlockedAchievements: AchievementInfo[];
};

export type AchievementInfo = {
  code: string;
  title: string;
  description: string;
  icon: string;
  unlockedAt: string;
};

export type QuestSkill = {
  id: number;
  name: string;
  icon: string;
  color: string;
  xp: number;
  nodes: string[];
  unlockedNodes: number[];
};

export type QuestBoardState = {
  xp: number;
  currentStreakDays: number;
  longestStreakDays: number;
  lastCompletionDate?: string | null;
  quests: Quest[];
  skills: QuestSkill[];
  /**
   * Optional so existing test fixtures + dashboard derivation snapshots
   * stay compile-clean. `toState()` always returns a real array.
   */
  folders?: QuestFolder[];
  achievements: AchievementInfo[];
};

type ApiQuestType = 0 | 1 | 2;
type ApiQuestPriority = 0 | 1 | 2;
type ApiQuestRecurrence = 0 | 1 | 2 | 3;

type ApiQuestBoard = {
  xp: number;
  currentStreakDays: number;
  longestStreakDays: number;
  lastCompletionDate?: string | null;
  quests: ApiQuest[];
  skills: ApiQuestSkill[];
  folders?: ApiQuestFolder[];
  achievements: ApiAchievement[];
};

type ApiQuest = {
  id: number;
  title: string;
  notes?: string | null;
  type: ApiQuestType;
  priority: ApiQuestPriority;
  recurrence: ApiQuestRecurrence;
  dueDate?: string | null;
  tags: string[];
  rewardXp: number;
  completed: boolean;
  completedAt?: string;
  createdAt?: string;
  updatedAt?: string;
  sortOrder: number;
  myGameId?: number | null;
  gameName?: string | null;
  skillId?: number | null;
  skillName?: string | null;
  questFolderId?: number | null;
  folderName?: string | null;
  folderEmoji?: string | null;
  subtasks: ApiQuestSubtask[];
};

type ApiQuestFolder = {
  id: number;
  name: string;
  emoji: string;
  color?: string | null;
  sectionName?: string | null;
  sortOrder: number;
};

type ApiQuestSubtask = {
  id: number;
  title: string;
  completed: boolean;
  completedAt?: string;
  sortOrder: number;
};

type ApiAchievement = {
  code: string;
  title: string;
  description: string;
  icon: string;
  unlockedAt: string;
};

type ApiQuestMutationResult = {
  quest: ApiQuest;
  spawnedQuest?: ApiQuest | null;
  totalXp: number;
  currentStreakDays: number;
  longestStreakDays: number;
  awardedSkillXp?: number | null;
  awardedSkillId?: number | null;
  unlockedAchievements: ApiAchievement[];
};

type ApiQuestSkill = {
  id: number;
  name: string;
  icon: string;
  color: string;
  xp: number;
  sortOrder: number;
  nodes: ApiQuestSkillNode[];
};

type ApiQuestSkillNode = {
  id: number;
  name: string;
  unlocked: boolean;
  unlockedAt?: string;
  sortOrder: number;
};

@Injectable({
  providedIn: 'root',
})
export class QuestBoardService {
  private http = inject(HttpClient);
  private apiEndpoint = inject(ApiEndpointService);
  private cache = inject(RequestCache);

  private readonly httpConfig = { withCredentials: true };

  /** Dashboard caches questBoard data, so every successful quest mutation
   *  has to expire the snapshot to avoid up-to-60s of stale "Active" /
   *  "This Month" metrics. */
  private invalidateDashboardCache(): void {
    this.cache.invalidate('home:dashboard');
  }

  async getBoard(): Promise<QuestBoardState> {
    const board = await firstValueFrom(this.http.get<ApiQuestBoard>(this.apiEndpoint.url('quests/board'), this.httpConfig));
    return this.toState(board);
  }

  async createQuest(input: QuestCreate): Promise<QuestMutationResult> {
    const payload = {
      title: input.title,
      notes: input.notes ?? null,
      type: this.toApiQuestType(input.type ?? 'sub'),
      priority: this.toApiPriority(input.priority ?? 'medium'),
      recurrence: this.toApiRecurrence(input.recurrence ?? 'none'),
      dueDate: input.dueDate ?? null,
      tags: input.tags ?? [],
      myGameId: input.myGameId ?? null,
      skillId: input.skillId ?? null,
      questFolderId: input.folderId ?? null,
    };
    const response = await firstValueFrom(this.http.post<ApiQuestMutationResult>(this.apiEndpoint.url('quests'), payload, this.httpConfig));
    this.invalidateDashboardCache();
    return this.toMutation(response);
  }

  async updateQuest(id: number, input: QuestUpdate): Promise<QuestMutationResult> {
    const payload: Record<string, unknown> = {};
    if (input.title !== undefined) payload['title'] = input.title;
    if (input.notes !== undefined) payload['notes'] = input.notes;
    if (input.type !== undefined) payload['type'] = this.toApiQuestType(input.type);
    if (input.priority !== undefined) payload['priority'] = this.toApiPriority(input.priority);
    if (input.recurrence !== undefined) payload['recurrence'] = this.toApiRecurrence(input.recurrence);
    if (input.dueDate !== undefined) payload['dueDate'] = input.dueDate;
    if (input.clearDueDate !== undefined) payload['clearDueDate'] = input.clearDueDate;
    if (input.tags !== undefined) payload['tags'] = input.tags;
    if (input.completed !== undefined) payload['completed'] = input.completed;
    if (input.myGameId !== undefined) payload['myGameId'] = input.myGameId;
    if (input.clearMyGame !== undefined) payload['clearMyGame'] = input.clearMyGame;
    if (input.sortOrder !== undefined) payload['sortOrder'] = input.sortOrder;
    if (input.skillId !== undefined) payload['skillId'] = input.skillId;
    if (input.clearSkill !== undefined) payload['clearSkill'] = input.clearSkill;
    if (input.folderId !== undefined) payload['questFolderId'] = input.folderId;
    if (input.clearFolder !== undefined) payload['clearQuestFolder'] = input.clearFolder;

    const response = await firstValueFrom(this.http.patch<ApiQuestMutationResult>(this.apiEndpoint.url(`quests/${id}`), payload, this.httpConfig));
    this.invalidateDashboardCache();
    return this.toMutation(response);
  }

  // ----- Folders ------------------------------------------------------------

  async listFolders(): Promise<QuestFolder[]> {
    const folders = await firstValueFrom(
      this.http.get<ApiQuestFolder[]>(this.apiEndpoint.url('quests/folders'), this.httpConfig),
    );
    return folders.map((folder) => this.toFolder(folder));
  }

  async createFolder(input: QuestFolderCreate): Promise<QuestFolder> {
    const payload = {
      name: input.name,
      emoji: input.emoji,
      color: input.color ?? null,
      sectionName: input.sectionName ?? null,
    };
    const response = await firstValueFrom(
      this.http.post<ApiQuestFolder>(this.apiEndpoint.url('quests/folders'), payload, this.httpConfig),
    );
    this.invalidateDashboardCache();
    return this.toFolder(response);
  }

  async updateFolder(id: number, input: QuestFolderUpdate): Promise<QuestFolder> {
    const payload: Record<string, unknown> = {};
    if (input.name !== undefined) payload['name'] = input.name;
    if (input.emoji !== undefined) payload['emoji'] = input.emoji;
    if (input.color !== undefined) payload['color'] = input.color;
    if (input.clearColor !== undefined) payload['clearColor'] = input.clearColor;
    if (input.sectionName !== undefined) payload['sectionName'] = input.sectionName;
    if (input.clearSectionName !== undefined) payload['clearSectionName'] = input.clearSectionName;
    if (input.sortOrder !== undefined) payload['sortOrder'] = input.sortOrder;

    const response = await firstValueFrom(
      this.http.patch<ApiQuestFolder>(this.apiEndpoint.url(`quests/folders/${id}`), payload, this.httpConfig),
    );
    this.invalidateDashboardCache();
    return this.toFolder(response);
  }

  async deleteFolder(id: number): Promise<void> {
    await firstValueFrom(this.http.delete<void>(this.apiEndpoint.url(`quests/folders/${id}`), this.httpConfig));
    this.invalidateDashboardCache();
  }

  async addSubtask(questId: number, title: string): Promise<QuestMutationResult> {
    const response = await firstValueFrom(
      this.http.post<ApiQuestMutationResult>(this.apiEndpoint.url(`quests/${questId}/subtasks`), { title }, this.httpConfig)
    );
    this.invalidateDashboardCache();
    return this.toMutation(response);
  }

  async updateSubtask(questId: number, subtaskId: number, input: QuestSubtaskUpdate): Promise<QuestMutationResult> {
    const payload: Record<string, unknown> = {};
    if (input.title !== undefined) payload['title'] = input.title;
    if (input.completed !== undefined) payload['completed'] = input.completed;
    if (input.sortOrder !== undefined) payload['sortOrder'] = input.sortOrder;
    const response = await firstValueFrom(
      this.http.patch<ApiQuestMutationResult>(this.apiEndpoint.url(`quests/${questId}/subtasks/${subtaskId}`), payload, this.httpConfig)
    );
    this.invalidateDashboardCache();
    return this.toMutation(response);
  }

  async deleteSubtask(questId: number, subtaskId: number): Promise<void> {
    await firstValueFrom(
      this.http.delete<void>(this.apiEndpoint.url(`quests/${questId}/subtasks/${subtaskId}`), this.httpConfig)
    );
    this.invalidateDashboardCache();
  }

  async deleteQuest(id: number): Promise<void> {
    await firstValueFrom(this.http.delete<void>(this.apiEndpoint.url(`quests/${id}`), this.httpConfig));
    this.invalidateDashboardCache();
  }

  async reorderQuests(items: QuestReorderItem[]): Promise<void> {
    const payload = items.map((item) => ({
      id: item.id,
      sortOrder: item.sortOrder,
      type: this.toApiQuestType(item.type),
    }));
    await firstValueFrom(this.http.put<void>(this.apiEndpoint.url('quests/reorder'), payload, this.httpConfig));
    this.invalidateDashboardCache();
  }

  async saveSkills(state: QuestBoardState): Promise<QuestBoardState> {
    const board = await firstValueFrom(
      this.http.put<ApiQuestBoard>(this.apiEndpoint.url('quests/skills'), this.toApi(state), this.httpConfig)
    );
    this.invalidateDashboardCache();
    return this.toState(board);
  }

  async getQuestsForGame(myGameId: number): Promise<Quest[]> {
    const apiQuests = await firstValueFrom(
      this.http.get<ApiQuest[]>(this.apiEndpoint.url(`quests/for-game/${myGameId}`), this.httpConfig)
    );
    return apiQuests.map((quest) => this.toQuest(quest));
  }

  private toState(board: ApiQuestBoard): QuestBoardState {
    return {
      xp: board.xp,
      currentStreakDays: board.currentStreakDays ?? 0,
      longestStreakDays: board.longestStreakDays ?? 0,
      lastCompletionDate: board.lastCompletionDate ?? null,
      quests: board.quests.map((quest) => this.toQuest(quest)),
      skills: board.skills.map((skill) => ({
        id: skill.id,
        name: skill.name,
        icon: skill.icon,
        color: skill.color,
        xp: skill.xp,
        nodes: skill.nodes.map((node) => node.name),
        unlockedNodes: skill.nodes
          .map((node, index) => (node.unlocked ? index : -1))
          .filter((index) => index >= 0),
      })),
      folders: (board.folders ?? []).map((folder) => this.toFolder(folder)),
      achievements: (board.achievements ?? []).map((a) => ({ ...a })),
    };
  }

  private toQuest(apiQuest: ApiQuest): Quest {
    return {
      id: apiQuest.id,
      title: apiQuest.title,
      notes: apiQuest.notes ?? null,
      type: this.toQuestType(apiQuest.type),
      priority: this.toPriority(apiQuest.priority),
      recurrence: this.toRecurrence(apiQuest.recurrence ?? 0),
      dueDate: apiQuest.dueDate ?? null,
      tags: apiQuest.tags ?? [],
      completed: apiQuest.completed,
      completedAt: apiQuest.completedAt,
      createdAt: apiQuest.createdAt,
      updatedAt: apiQuest.updatedAt,
      rewardXp: apiQuest.rewardXp,
      sortOrder: apiQuest.sortOrder,
      myGameId: apiQuest.myGameId ?? null,
      gameName: apiQuest.gameName ?? null,
      skillId: apiQuest.skillId ?? null,
      skillName: apiQuest.skillName ?? null,
      folderId: apiQuest.questFolderId ?? null,
      folderName: apiQuest.folderName ?? null,
      folderEmoji: apiQuest.folderEmoji ?? null,
      subtasks: (apiQuest.subtasks ?? []).map((s) => ({
        id: s.id,
        title: s.title,
        completed: s.completed,
        completedAt: s.completedAt,
        sortOrder: s.sortOrder,
      })),
    };
  }

  private toFolder(folder: ApiQuestFolder): QuestFolder {
    return {
      id: folder.id,
      name: folder.name,
      emoji: folder.emoji ?? '',
      color: folder.color ?? null,
      sectionName: folder.sectionName ?? null,
      sortOrder: folder.sortOrder,
    };
  }

  private toMutation(api: ApiQuestMutationResult): QuestMutationResult {
    return {
      quest: this.toQuest(api.quest),
      spawnedQuest: api.spawnedQuest ? this.toQuest(api.spawnedQuest) : null,
      totalXp: api.totalXp,
      currentStreakDays: api.currentStreakDays ?? 0,
      longestStreakDays: api.longestStreakDays ?? 0,
      awardedSkillXp: api.awardedSkillXp ?? null,
      awardedSkillId: api.awardedSkillId ?? null,
      unlockedAchievements: (api.unlockedAchievements ?? []).map((a) => ({ ...a })),
    };
  }

  private toApi(state: QuestBoardState): ApiQuestBoard {
    return {
      xp: state.xp,
      currentStreakDays: state.currentStreakDays,
      longestStreakDays: state.longestStreakDays,
      lastCompletionDate: state.lastCompletionDate ?? null,
      quests: [],
      skills: state.skills.map((skill, skillIndex) => ({
        id: skill.id > 0 ? skill.id : 0,
        name: skill.name,
        icon: skill.icon,
        color: skill.color,
        xp: skill.xp,
        sortOrder: skillIndex,
        nodes: skill.nodes.map((node, nodeIndex) => ({
          id: 0,
          name: node,
          unlocked: skill.unlockedNodes.includes(nodeIndex),
          unlockedAt: skill.unlockedNodes.includes(nodeIndex) ? new Date().toISOString() : undefined,
          sortOrder: nodeIndex,
        })),
      })),
      achievements: [],
    };
  }

  private toQuestType(type: ApiQuestType): QuestType {
    return type === 0 ? 'main' : type === 2 ? 'faction' : 'sub';
  }

  private toApiQuestType(type: QuestType): ApiQuestType {
    return type === 'main' ? 0 : type === 'faction' ? 2 : 1;
  }

  private toPriority(value: ApiQuestPriority): QuestPriority {
    return value === 0 ? 'low' : value === 2 ? 'high' : 'medium';
  }

  private toApiPriority(value: QuestPriority): ApiQuestPriority {
    return value === 'low' ? 0 : value === 'high' ? 2 : 1;
  }

  private toRecurrence(value: ApiQuestRecurrence): QuestRecurrence {
    return value === 1 ? 'daily' : value === 2 ? 'weekly' : value === 3 ? 'monthly' : 'none';
  }

  private toApiRecurrence(value: QuestRecurrence): ApiQuestRecurrence {
    return value === 'daily' ? 1 : value === 'weekly' ? 2 : value === 'monthly' ? 3 : 0;
  }
}
