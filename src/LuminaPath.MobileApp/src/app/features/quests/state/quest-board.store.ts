import { computed, inject } from '@angular/core';
import { signalStore, withComputed, withHooks, withMethods, withState, patchState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';

import { MyGameService } from 'src/app/features/my-games/services/my-game.service';
import type { MyGame } from 'src/app/features/games/models/games.model';
import type { SkillTreeBranch } from 'src/app/features/skill-tree/models/skill-tree.model';
import {
  parseNodeId,
  questSkillsToBranches,
  unlockedNodeIdsFor,
} from 'src/app/features/skill-tree/util/skill-adapter';
import { addDays, startOfDay, toISODate } from 'src/app/shared/utils/date-helpers';

import {
  Quest,
  QuestBoardService,
  QuestBoardState,
  QuestFolder,
  QuestFolderCreate,
  QuestFolderUpdate,
  QuestMutationResult,
  QuestSkill,
  QuestSubtask,
} from '../services/quest-board.service';
import { QuestBoardFeedbackService } from '../services/quest-board-feedback.service';
import { QuestBoardPreferencesService } from '../services/quest-board-preferences.service';
import { QuestViewMode } from '../components/quest-board-toolbar/quest-board-toolbar.component';
import { QuestEditDraft } from '../components/quest-detail-sheet/quest-detail-sheet.component';
import { SkillForm } from '../components/skill-modal/skill-modal.component';
import { QuestQuickAddSubmit } from '../components/quest-quick-add/quest-quick-add.component';
import { PageMode } from '../models/quest-board-view.model';
import {
  QuestFilter,
  buildQuestSections,
  collectAvailableTags,
  countQuestsByFilter,
  filterQuests,
  nextQueuedQuest as selectNextQueuedQuest,
} from '../domain/quest-sections.builder';
import { buildQuestBoardStats } from '../domain/quest-board-stats';
import {
  DELETE_GRACE_MS,
  initialState,
  nextTemporaryId,
  SkillNodeState,
} from './quest-board.state';

/**
 * Single source of truth for the quest board: persisted board data (quests,
 * skills, folders, achievements, XP), the user's view preferences, and every
 * mutation against the {@link QuestBoardService}. The page shell and the
 * per-mode view components are thin consumers — they read signals and call
 * methods here, so cross-mode coordination (route focus, "start node quest",
 * folder edits reflecting on cards) lives in one place.
 *
 * Provided at the page level (not root): mutations touch the page-scoped
 * {@link QuestBoardFeedbackService} for toasts/celebrations, and there is no
 * cross-page consistency requirement like {@link MediaStore} has.
 */

export const QuestBoardStore = signalStore(
  withState(initialState),
  withComputed((store) => ({
    stats: computed(() => buildQuestBoardStats(store.xp(), store.quests(), store.skills())),
    visibleQuests: computed(() =>
      filterQuests(store.quests(), {
        filter: store.filter(),
        searchQuery: store.searchQuery(),
        tagFilter: store.tagFilter(),
      }),
    ),
    questSections: computed(() =>
      buildQuestSections(store.quests(), {
        filter: store.filter(),
        searchQuery: store.searchQuery(),
        tagFilter: store.tagFilter(),
      }),
    ),
    availableTags: computed(() => collectAvailableTags(store.quests())),
    nextQueuedQuest: computed(() => selectNextQueuedQuest(store.quests())),
    isManualOrderActive: computed(
      () => store.filter() === 'inbox' && !store.searchQuery().trim() && !store.tagFilter(),
    ),
    selectedQuest: computed(() => {
      const id = store.expandedQuestId();
      return id === null ? null : store.quests().find((quest) => quest.id === id) ?? null;
    }),
    unfiledQuestCount: computed(
      () => store.quests().filter((quest) => (quest.folderId ?? null) === null).length,
    ),
    skillTreeBranches: computed<SkillTreeBranch[]>(() => questSkillsToBranches(store.skills())),
    skillTreeUnlockedNodeIds: computed<string[]>(() => unlockedNodeIdsFor(store.skills())),
    /**
     * Folders grouped by SectionName (case-insensitive). The unlabelled
     * group sorts first; within a section, folders keep server SortOrder.
     */
    folderGroups: computed<{ section: string | null; folders: QuestFolder[] }[]>(() => {
      const map = new Map<string, { section: string | null; folders: QuestFolder[] }>();
      const orderedKeys: string[] = [];
      for (const folder of store.folders()) {
        const key = (folder.sectionName ?? '').trim().toLowerCase();
        if (!map.has(key)) {
          map.set(key, { section: folder.sectionName ?? null, folders: [] });
          orderedKeys.push(key);
        }
        map.get(key)!.folders.push(folder);
      }
      orderedKeys.sort((a, b) => (a === '' ? -1 : b === '' ? 1 : 0));
      return orderedKeys.map((key) => map.get(key)!);
    }),
    /** Distinct, first-seen section names — chip suggestions for the folder modal. */
    knownSections: computed<string[]>(() => {
      const seen = new Set<string>();
      const out: string[] = [];
      for (const folder of store.folders()) {
        const name = (folder.sectionName ?? '').trim();
        if (!name) continue;
        const key = name.toLowerCase();
        if (seen.has(key)) continue;
        seen.add(key);
        out.push(name);
      }
      return out;
    }),
  })),
  withComputed((store) => ({
    /** CDK drop-list ids: one per folder plus the Unfiled bucket. */
    questDropListIds: computed<string[]>(() => [
      ...store.folders().map((folder) => `quest-drop-folder-${folder.id}`),
      'quest-drop-unfiled',
    ]),
  })),
  withMethods((store) => {
    const service = inject(QuestBoardService);
    const feedback = inject(QuestBoardFeedbackService);
    const preferences = inject(QuestBoardPreferencesService);
    const myGameService = inject(MyGameService);

    /** Timers backing the undo grace period. Not reactive — the rendered
     *  list of pending quests lives in `pendingDeletes` state instead. */
    const deleteTimers = new Map<number, number>();

    // ---- internal helpers -------------------------------------------------

    const gameNameFor = (myGameId: number): string =>
      store.library().find((game) => game.myGameId === myGameId)?.gameName ?? 'Unknown game';

    const skillNameFor = (skillId: number): string =>
      store.skills().find((skill) => skill.id === skillId)?.name ?? 'Skill';

    const replaceQuest = (id: number, updated: Quest): void => {
      patchState(store, { quests: store.quests().map((quest) => (quest.id === id ? updated : quest)) });
    };

    const replaceSkill = (id: number, updated: QuestSkill): void => {
      patchState(store, { skills: store.skills().map((skill) => (skill.id === id ? updated : skill)) });
    };

    const savePrefs = (): void => {
      preferences.save({
        filter: store.filter(),
        quickAddType: store.quickAddType(),
        quickAddPriority: store.quickAddPriority(),
        quickAddRecurrence: store.quickAddRecurrence(),
        questViewMode: store.questViewMode(),
      });
    };

    const applyBoard = (board: QuestBoardState): void => {
      patchState(store, {
        xp: board.xp,
        currentStreakDays: board.currentStreakDays,
        longestStreakDays: board.longestStreakDays,
        quests: board.quests,
        skills: board.skills,
        folders: board.folders ?? [],
        achievements: board.achievements,
      });
    };

    /** Fold the XP / streak / achievement deltas a mutation returns back into
     *  state, surfacing a toast for any newly unlocked achievement. */
    const applyMutationMeta = (mutation: QuestMutationResult): void => {
      const known = new Set(store.achievements().map((a) => a.code));
      const fresh = (mutation.unlockedAchievements ?? []).filter((a) => !known.has(a.code));
      patchState(store, {
        xp: mutation.totalXp,
        currentStreakDays: mutation.currentStreakDays,
        longestStreakDays: mutation.longestStreakDays,
        ...(fresh.length ? { achievements: [...fresh, ...store.achievements()] } : {}),
      });
      if (fresh.length) {
        feedback.showAchievementToast(fresh[fresh.length - 1]);
      }
    };

    const skillNodeState = (skill: QuestSkill, nodeIndex: number): SkillNodeState => {
      if (skill.unlockedNodes.includes(nodeIndex)) return 'completed';
      const next = skill.nodes.findIndex((_, index) => !skill.unlockedNodes.includes(index));
      return nodeIndex === next ? 'available' : 'locked';
    };

    const persistSkills = async (): Promise<boolean> => {
      try {
        applyBoard(
          await service.saveSkills({
            xp: store.xp(),
            currentStreakDays: store.currentStreakDays(),
            longestStreakDays: store.longestStreakDays(),
            lastCompletionDate: null,
            quests: store.quests(),
            skills: store.skills(),
            achievements: store.achievements(),
          }),
        );
        return true;
      } catch {
        feedback.showToast('Progress could not be saved');
        return false;
      }
    };

    const loadLibrary = async (): Promise<void> => {
      try {
        const result = await firstValueFrom(myGameService.getAll());
        patchState(store, {
          library: (result.data ?? [])
            .map((myGame: MyGame) => ({
              myGameId: myGame.id,
              gameName: myGame.game?.name ?? 'Unknown game',
            }))
            .sort((a, b) => a.gameName.localeCompare(b.gameName)),
        });
      } catch {
        patchState(store, { library: [] });
      }
    };

    const loadBoard = async (): Promise<void> => {
      patchState(store, { isLoading: true, errorMessage: '' });
      try {
        applyBoard(await service.getBoard());
      } catch {
        patchState(store, { errorMessage: 'Quest board could not be loaded.' });
        feedback.showToast('Quest board could not be loaded.');
      } finally {
        patchState(store, { isLoading: false });
        applyFocus();
      }
    };

    // ---- inline edit ------------------------------------------------------

    const toggleExpand = (quest: Quest): void => {
      if (store.expandedQuestId() === quest.id) {
        patchState(store, { expandedQuestId: null, editDraft: null });
        return;
      }
      patchState(store, {
        expandedQuestId: quest.id,
        editDraft: {
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
        },
      });
    };

    const applyFocus = (): void => {
      const id = store.focusQuestId();
      if (id === null || store.isLoading()) return;
      const quest = store.quests().find((item) => item.id === id);
      if (!quest) return;
      patchState(store, { mode: 'quests', filter: 'all', tagFilter: null, searchQuery: '', focusQuestId: null });
      if (store.expandedQuestId() !== quest.id) {
        toggleExpand(quest);
      }
    };

    // ---- quest scheduling -------------------------------------------------

    const scheduleQuest = async (quest: Quest, dueDate: string | null, label: string): Promise<void> => {
      const previousDue = quest.dueDate ?? null;
      replaceQuest(quest.id, { ...quest, dueDate });
      if (store.expandedQuestId() === quest.id && store.editDraft()) {
        patchState(store, { editDraft: { ...store.editDraft()!, dueDate } });
      }
      try {
        const mutation = await service.updateQuest(quest.id, {
          dueDate: dueDate ?? undefined,
          clearDueDate: dueDate === null,
        });
        replaceQuest(quest.id, mutation.quest);
        applyMutationMeta(mutation);
        feedback.showToast(label);
      } catch {
        replaceQuest(quest.id, quest);
        if (store.expandedQuestId() === quest.id && store.editDraft()) {
          patchState(store, { editDraft: { ...store.editDraft()!, dueDate: previousDue } });
        }
        feedback.showToast('Could not reschedule quest');
      }
    };

    const scheduleToday = (quest: Quest): Promise<void> =>
      scheduleQuest(quest, toISODate(startOfDay(new Date())), 'Moved to today');

    // ---- delete / undo ----------------------------------------------------

    const commitDelete = async (id: number): Promise<void> => {
      deleteTimers.delete(id);
      const quest = store.pendingDeletes().find((q) => q.id === id);
      patchState(store, { pendingDeletes: store.pendingDeletes().filter((q) => q.id !== id) });
      if (!quest) return;
      try {
        await service.deleteQuest(id);
      } catch {
        patchState(store, { quests: [quest, ...store.quests()] });
        feedback.showToast('Could not delete quest');
      }
    };

    // ---- skills -----------------------------------------------------------

    const unlockNode = async (skill: QuestSkill, nodeIndex: number): Promise<void> => {
      if (skill.unlockedNodes.includes(nodeIndex)) return;
      if (skillNodeState(skill, nodeIndex) === 'locked') {
        feedback.showToast('Unlock the previous node first');
        return;
      }
      replaceSkill(skill.id, { ...skill, unlockedNodes: [...skill.unlockedNodes, nodeIndex], xp: skill.xp + 25 });
      patchState(store, { xp: store.xp() + 25 });
      feedback.showToast(`${skill.nodes[nodeIndex]} unlocked`);
      await persistSkills();
    };

    return {
      // ===== loading =======================================================
      loadPrefs(): void {
        const prefs = preferences.load();
        patchState(store, {
          filter: prefs.filter,
          questViewMode: prefs.questViewMode,
          quickAddType: prefs.quickAddType,
          quickAddPriority: prefs.quickAddPriority,
          quickAddRecurrence: prefs.quickAddRecurrence,
        });
      },
      async load(): Promise<void> {
        await Promise.all([loadBoard(), loadLibrary()]);
      },

      // ===== shared view state =============================================
      setMode(mode: PageMode): void {
        patchState(store, { mode });
      },
      setFilter(filter: QuestFilter): void {
        patchState(store, { filter });
        savePrefs();
      },
      setViewMode(mode: QuestViewMode): void {
        if (store.questViewMode() === mode) return;
        patchState(store, { questViewMode: mode });
        savePrefs();
      },
      setSearchQuery(searchQuery: string): void {
        patchState(store, { searchQuery });
      },
      clearSearch(): void {
        patchState(store, { searchQuery: '' });
      },
      selectTag(tag: string): void {
        patchState(store, { tagFilter: store.tagFilter() === tag ? null : tag });
      },
      clearTagFilter(): void {
        patchState(store, { tagFilter: null });
      },
      setQuickAddType(type: Quest['type']): void {
        patchState(store, { quickAddType: type });
      },
      setQuickAddPriority(priority: Quest['priority']): void {
        patchState(store, { quickAddPriority: priority });
      },
      setQuickAddRecurrence(recurrence: Quest['recurrence']): void {
        patchState(store, { quickAddRecurrence: recurrence });
      },

      // ===== route focus ===================================================
      focusQuest(id: number | null): void {
        patchState(store, { focusQuestId: id });
        applyFocus();
      },

      // ===== read helpers (parameterised) ==================================
      filterCount(filter: QuestFilter): number {
        return countQuestsByFilter(store.quests(), filter);
      },
      questsInFolder(folderId: number | null): Quest[] {
        return store.quests().filter((quest) => (quest.folderId ?? null) === folderId);
      },
      isFolderCollapsed(folderId: number): boolean {
        return store.collapsedFolderIds().has(folderId);
      },
      subtaskDraft(questId: number): string {
        return store.newSubtaskTitle()[questId] ?? '';
      },
      setSubtaskDraft(questId: number, value: string): void {
        patchState(store, { newSubtaskTitle: { ...store.newSubtaskTitle(), [questId]: value } });
      },
      linkedQuestCount(skill: QuestSkill): number {
        return store.quests().filter((quest) => quest.skillId === skill.id).length;
      },
      activeLinkedQuestCount(skill: QuestSkill): number {
        return store.quests().filter((quest) => quest.skillId === skill.id && !quest.completed).length;
      },
      isRecentlyCompleted(questId: number): boolean {
        return feedback.isRecentlyCompleted(questId);
      },

      // ===== inline edit ===================================================
      toggleExpand,
      cancelEdit(): void {
        patchState(store, { expandedQuestId: null, editDraft: null });
      },
      setEditDraft(draft: QuestEditDraft | null): void {
        patchState(store, { editDraft: draft });
      },

      // ===== quest CRUD ====================================================
      async createQuest(payload: QuestQuickAddSubmit): Promise<void> {
        patchState(store, {
          quickAddType: payload.type,
          quickAddPriority: payload.priority,
          quickAddRecurrence: payload.recurrence,
        });

        const tempId = nextTemporaryId();
        const now = new Date().toISOString();
        const optimistic: Quest = {
          id: tempId,
          title: payload.title,
          notes: null,
          type: payload.type,
          priority: payload.priority,
          recurrence: payload.recurrence,
          dueDate: payload.dueDate ?? null,
          tags: [],
          completed: false,
          createdAt: now,
          updatedAt: now,
          rewardXp: 0,
          sortOrder: 0,
          myGameId: payload.myGameId ?? null,
          gameName: payload.myGameId == null ? null : gameNameFor(payload.myGameId),
          skillId: payload.skillId ?? null,
          skillName: payload.skillId == null ? null : skillNameFor(payload.skillId),
          subtasks: [],
        };
        patchState(store, { quests: [optimistic, ...store.quests()] });

        try {
          const mutation = await service.createQuest({
            title: payload.title,
            type: payload.type,
            priority: payload.priority,
            recurrence: payload.recurrence,
            dueDate: payload.dueDate ?? null,
            myGameId: payload.myGameId ?? null,
            skillId: payload.skillId ?? null,
          });
          replaceQuest(tempId, mutation.quest);
          applyMutationMeta(mutation);
          savePrefs();
        } catch {
          patchState(store, { quests: store.quests().filter((q) => q.id !== tempId) });
          feedback.showToast('Could not save quest');
        }
      },

      async toggleQuest(quest: Quest): Promise<void> {
        const completed = !quest.completed;
        replaceQuest(quest.id, {
          ...quest,
          completed,
          completedAt: completed ? new Date().toISOString() : undefined,
        });

        try {
          const mutation = await service.updateQuest(quest.id, { completed });
          replaceQuest(quest.id, mutation.quest);
          if (mutation.spawnedQuest) {
            patchState(store, { quests: [mutation.spawnedQuest, ...store.quests()] });
          }
          applyMutationMeta(mutation);
          if (mutation.quest.completed) {
            feedback.triggerCelebration(mutation.quest.id);
            const parts = [`+${mutation.quest.rewardXp} XP`];
            if (mutation.awardedSkillXp && mutation.awardedSkillXp > 0) {
              parts.push(`+${mutation.awardedSkillXp} skill XP`);
            }
            if (mutation.spawnedQuest) {
              parts.push('next one queued');
            }
            feedback.showToast(parts.join(' · '));
          }
        } catch {
          replaceQuest(quest.id, quest);
          feedback.showToast('Could not update quest');
        }
      },

      async saveEdit(quest: Quest): Promise<void> {
        const draft = store.editDraft();
        if (!draft) return;
        const title = draft.title.trim();
        if (!title) return;

        const tags = draft.tags
          .split(',')
          .map((tag) => tag.trim())
          .filter((tag) => tag.length > 0);
        const draftFolder = draft.folderId == null ? null : store.folders().find((f) => f.id === draft.folderId) ?? null;
        const notes = draft.notes.trim() ? draft.notes.trim() : null;

        replaceQuest(quest.id, {
          ...quest,
          title,
          notes,
          type: draft.type,
          priority: draft.priority,
          recurrence: draft.recurrence,
          dueDate: draft.dueDate,
          tags,
          myGameId: draft.myGameId ?? null,
          gameName: draft.myGameId == null ? null : gameNameFor(draft.myGameId),
          skillId: draft.skillId ?? null,
          skillName: draft.skillId == null ? null : skillNameFor(draft.skillId),
          folderId: draft.folderId ?? null,
          folderName: draftFolder?.name ?? null,
          folderEmoji: draftFolder?.emoji ?? null,
        });
        patchState(store, { expandedQuestId: null, editDraft: null });

        try {
          const mutation = await service.updateQuest(quest.id, {
            title,
            notes,
            type: draft.type,
            priority: draft.priority,
            recurrence: draft.recurrence,
            dueDate: draft.dueDate,
            clearDueDate: draft.dueDate == null,
            tags,
            myGameId: draft.myGameId ?? undefined,
            clearMyGame: draft.myGameId == null,
            skillId: draft.skillId ?? undefined,
            clearSkill: draft.skillId == null,
            folderId: draft.folderId ?? undefined,
            clearFolder: draft.folderId == null,
          });
          replaceQuest(quest.id, mutation.quest);
          applyMutationMeta(mutation);
        } catch {
          replaceQuest(quest.id, quest);
          feedback.showToast('Could not save changes');
        }
      },

      scheduleToday,
      scheduleTomorrow(quest: Quest): Promise<void> {
        return scheduleQuest(quest, toISODate(addDays(startOfDay(new Date()), 1)), 'Moved to tomorrow');
      },
      clearQuestDueDate(quest: Quest): Promise<void> {
        return scheduleQuest(quest, null, 'Moved to inbox');
      },
      async pullNextQuestToToday(): Promise<void> {
        const quest = store.nextQueuedQuest();
        if (!quest) return;
        await scheduleToday(quest);
        patchState(store, { filter: 'today' });
        savePrefs();
      },

      beginDelete(quest: Quest): void {
        patchState(store, {
          quests: store.quests().filter((q) => q.id !== quest.id),
          pendingDeletes: [...store.pendingDeletes(), quest],
        });
        const timeoutId = window.setTimeout(() => void commitDelete(quest.id), DELETE_GRACE_MS);
        deleteTimers.set(quest.id, timeoutId);
        // Hide the plain toast — the undo toast renders from pendingDeletes.
        feedback.clearToast();
      },
      undoDelete(id: number): void {
        const timer = deleteTimers.get(id);
        if (timer !== undefined) window.clearTimeout(timer);
        deleteTimers.delete(id);
        const quest = store.pendingDeletes().find((q) => q.id === id);
        if (!quest) return;
        patchState(store, {
          pendingDeletes: store.pendingDeletes().filter((q) => q.id !== id),
          quests: [quest, ...store.quests()],
        });
        feedback.clearToast();
      },

      /** Persist a manual reorder for the dragged section. `from`/`to` are
       *  indices within `sectionQuests` (the section's visible quests). */
      async reorderQuests(from: number, to: number, sectionQuests: Quest[]): Promise<void> {
        const moved = sectionQuests[from];
        if (!moved) return;
        const reordered = [...sectionQuests];
        reordered.splice(from, 1);
        reordered.splice(to, 0, moved);

        const visibleIds = new Set(sectionQuests.map((q) => q.id));
        const others = store.quests().filter((q) => !visibleIds.has(q.id));
        patchState(store, {
          quests: [...reordered, ...others].map((quest, index) => ({ ...quest, sortOrder: index })),
        });

        try {
          await service.reorderQuests(reordered.map((q, index) => ({ id: q.id, sortOrder: index, type: q.type })));
        } catch {
          feedback.showToast('Could not save order');
        }
      },

      // ===== subtasks ======================================================
      async addSubtask(quest: Quest): Promise<void> {
        const draft = (store.newSubtaskTitle()[quest.id] ?? '').trim();
        if (!draft) return;

        const optimistic: QuestSubtask = {
          id: nextTemporaryId(),
          title: draft,
          completed: false,
          sortOrder: quest.subtasks.length,
        };
        replaceQuest(quest.id, { ...quest, subtasks: [...quest.subtasks, optimistic] });
        patchState(store, { newSubtaskTitle: { ...store.newSubtaskTitle(), [quest.id]: '' } });

        try {
          const mutation = await service.addSubtask(quest.id, draft);
          replaceQuest(quest.id, mutation.quest);
          applyMutationMeta(mutation);
        } catch {
          replaceQuest(quest.id, quest);
          feedback.showToast('Could not add subtask');
        }
      },
      async toggleSubtask({ quest, subtask }: { quest: Quest; subtask: QuestSubtask }): Promise<void> {
        const completed = !subtask.completed;
        const updatedSubtask: QuestSubtask = {
          ...subtask,
          completed,
          completedAt: completed ? new Date().toISOString() : undefined,
        };
        replaceQuest(quest.id, {
          ...quest,
          subtasks: quest.subtasks.map((s) => (s.id === subtask.id ? updatedSubtask : s)),
        });

        try {
          const mutation = await service.updateSubtask(quest.id, subtask.id, { completed });
          replaceQuest(quest.id, mutation.quest);
          applyMutationMeta(mutation);
        } catch {
          replaceQuest(quest.id, quest);
          feedback.showToast('Could not update subtask');
        }
      },
      async deleteSubtask({ quest, subtask }: { quest: Quest; subtask: QuestSubtask }): Promise<void> {
        replaceQuest(quest.id, { ...quest, subtasks: quest.subtasks.filter((s) => s.id !== subtask.id) });
        try {
          await service.deleteSubtask(quest.id, subtask.id);
        } catch {
          replaceQuest(quest.id, quest);
          feedback.showToast('Could not delete subtask');
        }
      },

      // ===== skills ========================================================
      async trainSkill(skill: QuestSkill): Promise<void> {
        replaceSkill(skill.id, { ...skill, xp: skill.xp + 40 });
        patchState(store, { xp: store.xp() + 15 });
        feedback.showToast(`${skill.name} training complete`);
        await persistSkills();
      },
      unlockNode({ skill, nodeIndex }: { skill: QuestSkill; nodeIndex: number }): Promise<void> {
        return unlockNode(skill, nodeIndex);
      },
      async saveSkill(form: SkillForm, editingSkillId: number | null): Promise<void> {
        const name = form.name.trim();
        if (!name) return;

        if (editingSkillId !== null) {
          const skill = store.skills().find((item) => item.id === editingSkillId);
          if (!skill) return;
          replaceSkill(editingSkillId, { ...skill, name, icon: form.icon, color: form.color });
          feedback.showToast(`${name} updated`);
          await persistSkills();
          return;
        }

        patchState(store, {
          skills: [
            ...store.skills(),
            { id: nextTemporaryId(), name, icon: form.icon, color: form.color, xp: 0, nodes: [], unlockedNodes: [] },
          ],
        });
        feedback.showToast(`${name} added to your skill tree`);
        await persistSkills();
      },
      async deleteSkill(skillId: number): Promise<void> {
        const skill = store.skills().find((item) => item.id === skillId);
        if (!skill) return;
        const previousSkills = store.skills();
        patchState(store, { skills: previousSkills.filter((item) => item.id !== skillId) });
        feedback.showToast(`${skill.name} deleted`);
        if (!(await persistSkills())) {
          patchState(store, { skills: previousSkills });
        }
      },
      async addNode(skillId: number, nodeName: string): Promise<void> {
        const skill = store.skills().find((item) => item.id === skillId);
        const name = nodeName.trim();
        if (!skill || !name) return;
        replaceSkill(skillId, { ...skill, nodes: [...skill.nodes, name] });
        feedback.showToast(`${name} added`);
        await persistSkills();
      },
      startNodeQuest({ skill, node }: { skill: QuestSkill; node: string }): void {
        patchState(store, {
          mode: 'quests',
          filter: 'today',
          quickAddType: 'sub',
          quickAddPriority: 'medium',
          quickAddPreset: {
            title: `Practice: ${node}`,
            dueDate: toISODate(startOfDay(new Date())),
            skillId: skill.id,
            advancedOpen: true,
          },
        });
        savePrefs();
        feedback.showToast(`${skill.name} quest draft ready`);
      },
      async handleSkillTreeNodeUnlocked(event: { nodeId: string }): Promise<void> {
        const parsed = parseNodeId(event.nodeId);
        if (!parsed) return;
        const skill = store.skills().find((item) => item.id === parsed.skillId);
        if (!skill) return;
        await unlockNode(skill, parsed.nodeIndex);
      },

      // ===== folders =======================================================
      toggleFolderCollapsed(folderId: number): void {
        const next = new Set(store.collapsedFolderIds());
        if (next.has(folderId)) next.delete(folderId);
        else next.add(folderId);
        patchState(store, { collapsedFolderIds: next });
      },
      toggleUnfiledCollapsed(): void {
        patchState(store, { unfiledCollapsed: !store.unfiledCollapsed() });
      },
      toggleFolderReorder(): void {
        patchState(store, { folderReorderActive: !store.folderReorderActive() });
      },
      async createFolder(input: QuestFolderCreate): Promise<boolean> {
        try {
          const folder = await service.createFolder(input);
          patchState(store, { folders: [...store.folders(), folder] });
          return true;
        } catch {
          feedback.showToast('Could not create folder');
          return false;
        }
      },
      async updateFolder(id: number, patch: QuestFolderUpdate): Promise<boolean> {
        try {
          const updated = await service.updateFolder(id, patch);
          patchState(store, {
            folders: store.folders().map((f) => (f.id === id ? updated : f)),
            quests: store
              .quests()
              .map((quest) =>
                quest.folderId === id ? { ...quest, folderName: updated.name, folderEmoji: updated.emoji } : quest,
              ),
          });
          return true;
        } catch {
          feedback.showToast('Could not save folder');
          return false;
        }
      },
      async deleteFolder(folder: QuestFolder): Promise<boolean> {
        try {
          await service.deleteFolder(folder.id);
          patchState(store, {
            folders: store.folders().filter((f) => f.id !== folder.id),
            quests: store
              .quests()
              .map((quest) =>
                quest.folderId === folder.id
                  ? { ...quest, folderId: null, folderName: null, folderEmoji: null }
                  : quest,
              ),
          });
          return true;
        } catch {
          feedback.showToast('Could not delete folder');
          return false;
        }
      },
      async assignFolderToQuest(quest: Quest, folderId: number | null): Promise<void> {
        const previousFolderId = quest.folderId ?? null;
        if (previousFolderId === folderId) return;
        const folder = folderId == null ? null : store.folders().find((f) => f.id === folderId) ?? null;
        replaceQuest(quest.id, {
          ...quest,
          folderId,
          folderName: folder?.name ?? null,
          folderEmoji: folder?.emoji ?? null,
        });
        try {
          const mutation = await service.updateQuest(quest.id, {
            folderId: folderId ?? undefined,
            clearFolder: folderId == null,
          });
          replaceQuest(quest.id, mutation.quest);
          applyMutationMeta(mutation);
        } catch {
          replaceQuest(quest.id, quest);
          feedback.showToast('Could not move quest');
        }
      },
      /** `from`/`to` index into the flat `folders` array (reorder mode). */
      async reorderFolders(from: number, to: number): Promise<void> {
        const folders = store.folders();
        if (from === to || from < 0 || from >= folders.length) return;
        const next = [...folders];
        const [moved] = next.splice(from, 1);
        next.splice(to, 0, moved);
        const payload = next.map((folder, index) => ({ id: folder.id, sortOrder: index }));
        patchState(store, { folders: next.map((folder, index) => ({ ...folder, sortOrder: index })) });
        try {
          await service.reorderFolders(payload);
        } catch {
          patchState(store, { folders });
          feedback.showToast('Could not save folder order');
        }
      },

      /** Release the pending-delete timers (called from the store's onDestroy). */
      clearDeleteTimers(): void {
        for (const timer of deleteTimers.values()) {
          window.clearTimeout(timer);
        }
        deleteTimers.clear();
      },
    };
  }),
  withHooks({
    onInit(store) {
      store.loadPrefs();
      void store.load();
    },
    onDestroy(store) {
      store.clearDeleteTimers();
    },
  }),
);
