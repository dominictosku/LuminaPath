
import { Component, OnDestroy, OnInit, ViewEncapsulation, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  IonBadge,
  IonContent,
  IonIcon,
  IonLabel,
  IonReorder,
  IonReorderGroup,
  IonSegment,
  IonSegmentButton,
  ItemReorderEventDetail,
} from '@ionic/angular/standalone';
import {
  AchievementInfo,
  Quest,
  QuestBoardService,
  QuestBoardState,
  QuestFolder,
  QuestFolderCreate,
  QuestFolderUpdate,
  QuestMutationResult,
  QuestPriority,
  QuestRecurrence,
  QuestSkill,
  QuestSubtask,
  QuestType,
} from '../services/quest-board.service';
import { MyGameService } from 'src/app/features/my-games/services/my-game.service';
import { MyGame } from 'src/app/features/games/models/games.model';
import { SkillTreeComponent } from 'src/app/features/skill-tree/components/skill-tree.component';
import type { SkillTreeBranch } from 'src/app/features/skill-tree/models/skill-tree.model';
import {
  parseNodeId,
  questSkillsToBranches,
  unlockedNodeIdsFor,
} from 'src/app/features/skill-tree/util/skill-adapter';
import { addDays, startOfDay, toISODate } from 'src/app/shared/utils/date-helpers';
import { QuestDueState, dueDateLabel, dueState } from '../quest-due';
import { QuestBoardHeroComponent } from '../components/quest-board-hero/quest-board-hero.component';
import { QuestQuickAddComponent, QuestQuickAddPreset, QuestQuickAddSubmit } from '../components/quest-quick-add/quest-quick-add.component';
import { QuestBoardToolbarComponent, QuestViewMode } from '../components/quest-board-toolbar/quest-board-toolbar.component';
import { QuestAchievementsComponent } from '../components/quest-achievements/quest-achievements.component';
import { SkillsListComponent, SkillNodeAction, SkillNodeQuestAction } from '../components/skills-list/skills-list.component';
import { SkillForm, SkillModalComponent } from '../components/skill-modal/skill-modal.component';
import { QuestDetailSheetComponent, QuestEditDraft } from '../components/quest-detail-sheet/quest-detail-sheet.component';
import { QuestCardComponent } from '../components/quest-card/quest-card.component';
import { QuestFolderModalComponent } from '../components/quest-folder-modal/quest-folder-modal.component';
import { QuestFolderPickerComponent } from '../components/quest-folder-picker/quest-folder-picker.component';
import { EmptyStateComponent } from 'src/app/shared/components/empty-state/empty-state.component';
import {
  QuestFilter,
  QuestSection,
  buildQuestSections,
  collectAvailableTags,
  countQuestsByFilter,
  filterQuests,
  nextQueuedQuest,
} from '../quest-sections.builder';

type PageMode = 'quests' | 'folders' | 'skills' | 'tree';
type ModalMode = 'skill' | 'node' | null;

type LibraryGame = {
  myGameId: number;
  gameName: string;
};

type FilterOption = {
  value: QuestFilter;
  label: string;
  icon: string;
};

type PriorityOption = {
  value: QuestPriority;
  label: string;
  weight: number;
};

type TypeOption = {
  value: QuestType;
  label: string;
  icon: string;
};

type RecurrenceOption = {
  value: QuestRecurrence;
  label: string;
};

type PendingDelete = {
  quest: Quest;
  timeoutId: number;
};

type SkillTreeUnlockPayload = {
  branchId: string;
  nodeId: string;
  xp: number;
};

@Component({
  selector: 'app-quest-board',
  templateUrl: './quest-board.page.html',
  styleUrls: ['./quest-board.page.scss'],
  // Quest-board ships a single coherent visual system whose selectors are well
  // namespaced (`.quest-*`, `.skill-*`). Loading them globally on this route
  // lets all sub-components share the styling without duplicating SCSS.
  encapsulation: ViewEncapsulation.None,
  imports: [
    FormsModule,
    IonBadge,
    IonContent,
    IonIcon,
    IonLabel,
    IonReorder,
    IonReorderGroup,
    IonSegment,
    IonSegmentButton,
    SkillTreeComponent,
    QuestBoardHeroComponent,
    QuestQuickAddComponent,
    QuestBoardToolbarComponent,
    QuestAchievementsComponent,
    SkillsListComponent,
    SkillModalComponent,
    QuestDetailSheetComponent,
    QuestCardComponent,
    QuestFolderModalComponent,
    QuestFolderPickerComponent,
    EmptyStateComponent,
],
})
export class QuestBoardPage implements OnInit, OnDestroy {
  private questBoardService = inject(QuestBoardService);
  private myGameService = inject(MyGameService);

  readonly skillIconOptions = [
    { label: 'Code', icon: 'code-slash-outline' },
    { label: 'Art', icon: 'brush-outline' },
    { label: 'Cook', icon: 'restaurant-outline' },
    { label: 'Study', icon: 'book-outline' },
    { label: 'Craft', icon: 'school-outline' },
  ];

  readonly skillColorOptions = ['#2563eb', '#0891b2', '#0f766e', '#7c3aed', '#be123c'];

  readonly typeOptions: TypeOption[] = [
    { value: 'main', label: 'Main', icon: 'map-outline' },
    { value: 'sub', label: 'Sub', icon: 'flag-outline' },
    { value: 'faction', label: 'Faction', icon: 'shield-checkmark-outline' },
  ];

  readonly priorityOptions: PriorityOption[] = [
    { value: 'low', label: 'Low', weight: 0 },
    { value: 'medium', label: 'Medium', weight: 1 },
    { value: 'high', label: 'High', weight: 2 },
  ];

  readonly recurrenceOptions: RecurrenceOption[] = [
    { value: 'none', label: 'No repeat' },
    { value: 'daily', label: 'Daily' },
    { value: 'weekly', label: 'Weekly' },
    { value: 'monthly', label: 'Monthly' },
  ];

  readonly filterOptions: FilterOption[] = [
    { value: 'today', label: 'Focus', icon: 'today-outline' },
    { value: 'upcoming', label: 'Upcoming', icon: 'calendar-outline' },
    { value: 'inbox', label: 'Inbox', icon: 'library-outline' },
    { value: 'all', label: 'All', icon: 'filter-outline' },
  ];

  mode: PageMode = 'quests';
  filter: QuestFilter = 'today';
  /**
   * `cards`   — roomy card stack, all metadata visible, drag-to-reorder.
   * `compact` — single-line dense rows (checkbox + title + due chip).
   *             Reorder is suppressed: the row is too small for the handle
   *             and the use case (skim a long list) doesn't need it.
   */
  questViewMode: QuestViewMode = 'cards';
  modalMode: ModalMode = null;

  xp = 0;
  level = 1;
  title = 'Initiate';
  xpIntoLevel = 0;
  xpProgress = 0;
  completedQuestCount = 0;
  activeQuestCount = 0;
  unlockedNodeCount = 0;
  todayCount = 0;
  overdueCount = 0;
  currentStreakDays = 0;
  longestStreakDays = 0;
  toastMessage = '';
  achievementToast: AchievementInfo | null = null;
  isLoading = true;
  errorMessage = '';

  quests: Quest[] = [];
  skills: QuestSkill[] = [];
  achievements: AchievementInfo[] = [];

  // ===== Folders =============================================================
  folders: QuestFolder[] = [];
  /** Per-folder collapsed state — defaults to expanded. */
  collapsedFolderIds = new Set<number>();
  /** Folder ID that should also expand the "Unfiled" pseudo-group. */
  unfiledCollapsed = false;
  /** When non-null, the folder modal is open in create-or-edit mode. */
  folderModalState: { mode: 'create' | 'edit'; folder: QuestFolder | null } | null = null;
  folderSaving = false;
  /** When true, folders show drag handles and are reorderable. Collapse
   *  toggles are suppressed in this mode so dragging doesn't double-fire
   *  as a tap on the toggle button. */
  folderReorderActive = false;

  /** ID of the quest whose folder-picker popover is open in compact view.
   *  Only one row may have it open at a time — opening a new one closes
   *  the previous. Card view's picker state is owned by the card itself. */
  compactFolderPickerQuestId: number | null = null;

  newSubtaskTitle: Record<number, string> = {};

  // Quick-add prefs persisted across sessions (read on init, written on change).
  quickAddType: QuestType = 'sub';
  quickAddPriority: QuestPriority = 'medium';
  quickAddRecurrence: QuestRecurrence = 'none';
  /** Drop-in pre-fill for the quick-add row (skill-tree "Quest" button, etc.).
   *  Assign a new object reference to trigger the child's apply-effect. */
  quickAddPreset: QuestQuickAddPreset | null = null;

  // Search & tag filter
  searchQuery = '';
  tagFilter: string | null = null;

  // Inline edit
  expandedQuestId: number | null = null;
  editDraft: QuestEditDraft | null = null;

  // Skills
  newSkill: SkillForm = this.emptySkillForm();
  newNodeName = '';
  selectedSkillId: number | null = null;
  editingSkillId: number | null = null;

  library: LibraryGame[] = [];

  private readonly xpPerLevel = 200;
  private readonly prefsStorageKey = 'questboard.prefs.v1';
  private pendingDeletes = new Map<number, PendingDelete>();
  private undoToastTimer: number | undefined;

  /**
   * IDs of quests whose celebration animation is still in flight. The
   * card reads this via `[recentlyCompleted]` to add a CSS class that
   * runs the scale+glow keyframe and the floating "+XP" badge. Cleared
   * per-id by `celebrationTimers` after the animation duration.
   */
  recentlyCompletedIds = new Set<number>();
  private celebrationTimers = new Map<number, number>();
  private static readonly CELEBRATION_MS = 1200;

  // Bound callables passed to presentational sub-components so their templates
  // can reach helper logic that depends on parent state (e.g. label lookups,
  // counts, the subtask draft map). Bound up-front to keep stable references.
  readonly filterCountFn = (filter: QuestFilter) => this.filterCount(filter);
  readonly typeIconFn = (type: QuestType) => this.typeIcon(type);
  readonly typeLabelFn = (type: QuestType) => this.typeLabel(type);
  readonly priorityLabelFn = (priority: QuestPriority) => this.priorityLabel(priority);
  readonly recurrenceLabelFn = (recurrence: QuestRecurrence) => this.recurrenceLabel(recurrence);
  readonly subtaskDraftFn = (questId: number) => this.subtaskDraft(questId);
  readonly activeLinkedQuestCountFn = (skill: QuestSkill) => this.activeLinkedQuestCount(skill);
  readonly linkedQuestCountFn = (skill: QuestSkill) => this.linkedQuestCount(skill);

  async ngOnInit() {
    this.loadPrefs();
    await Promise.all([this.loadBoard(), this.loadLibrary()]);
  }

  ngOnDestroy(): void {
    for (const pending of this.pendingDeletes.values()) {
      window.clearTimeout(pending.timeoutId);
    }
    this.pendingDeletes.clear();
    for (const timerId of this.celebrationTimers.values()) {
      window.clearTimeout(timerId);
    }
    this.celebrationTimers.clear();
    if (this.undoToastTimer) {
      window.clearTimeout(this.undoToastTimer);
    }
  }

  private triggerCelebration(questId: number): void {
    // If the user re-completes the same quest before the previous
    // celebration finishes, reset the timer so the animation re-runs
    // cleanly rather than truncating.
    const existing = this.celebrationTimers.get(questId);
    if (existing !== undefined) {
      window.clearTimeout(existing);
    }
    // Re-create the Set so OnPush descendants see a new reference if
    // we later swap to a signal-backed equivalent.
    this.recentlyCompletedIds = new Set(this.recentlyCompletedIds).add(questId);
    const timerId = window.setTimeout(() => {
      const next = new Set(this.recentlyCompletedIds);
      next.delete(questId);
      this.recentlyCompletedIds = next;
      this.celebrationTimers.delete(questId);
    }, QuestBoardPage.CELEBRATION_MS);
    this.celebrationTimers.set(questId, timerId);
  }

  isRecentlyCompleted(questId: number): boolean {
    return this.recentlyCompletedIds.has(questId);
  }

  // -------- Filters & sorting (delegated to quest-sections.builder) --------

  private viewOptions(): { filter: QuestFilter; searchQuery: string; tagFilter: string | null } {
    return { filter: this.filter, searchQuery: this.searchQuery, tagFilter: this.tagFilter };
  }

  get visibleQuests(): Quest[] {
    return filterQuests(this.quests, this.viewOptions());
  }

  get questSections(): QuestSection[] {
    return buildQuestSections(this.quests, this.viewOptions());
  }

  get selectedQuest(): Quest | null {
    if (this.expandedQuestId === null) return null;
    return this.quests.find((quest) => quest.id === this.expandedQuestId) ?? null;
  }

  get nextQueuedQuest(): Quest | null {
    return nextQueuedQuest(this.quests);
  }

  get availableTags(): string[] {
    return collectAvailableTags(this.quests);
  }

  get isManualOrderActive(): boolean {
    return this.filter === 'inbox' && !this.searchQuery.trim() && !this.tagFilter;
  }

  setFilter(value: QuestFilter): void {
    this.filter = value;
    this.savePrefs();
  }

  setViewMode(mode: QuestViewMode): void {
    if (this.questViewMode === mode) return;
    this.questViewMode = mode;
    this.savePrefs();
  }

  // Helpers consumed by the compact-row template. Mirror what quest-card
  // computes internally; we don't go through quest-card here because it's
  // a heavier wrapper with its own host element + ion-reorder.
  dueStateOf(quest: Quest): QuestDueState {
    return dueState(quest);
  }

  dueLabelOf(quest: Quest): string {
    return dueDateLabel(quest);
  }

  subtaskCompletedCount(quest: Quest): number {
    return quest.subtasks.filter((s) => s.completed).length;
  }

  selectTag(tag: string): void {
    this.tagFilter = this.tagFilter === tag ? null : tag;
  }

  clearTagFilter(): void {
    this.tagFilter = null;
  }

  clearSearch(): void {
    this.searchQuery = '';
  }

  // -------- Quick add --------

  async handleQuickAdd(payload: QuestQuickAddSubmit): Promise<void> {
    // Mirror the quick-add form's persisted prefs (type/priority/recurrence)
    // back to the page so they survive a reload.
    this.quickAddType = payload.type;
    this.quickAddPriority = payload.priority;
    this.quickAddRecurrence = payload.recurrence;

    const tempId = this.nextTemporaryId();
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
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      rewardXp: 0,
      sortOrder: 0,
      myGameId: payload.myGameId ?? null,
      gameName: payload.myGameId == null ? null : this.gameNameFor(payload.myGameId),
      skillId: payload.skillId ?? null,
      skillName: payload.skillId == null ? null : this.skillNameFor(payload.skillId),
      subtasks: [],
    };

    this.quests = [optimistic, ...this.quests];
    this.rebuildStats();

    try {
      const mutation = await this.questBoardService.createQuest({
        title: payload.title,
        type: payload.type,
        priority: payload.priority,
        recurrence: payload.recurrence,
        dueDate: payload.dueDate ?? null,
        myGameId: payload.myGameId ?? null,
        skillId: payload.skillId ?? null,
      });
      this.quests = this.quests.map((q) => (q.id === tempId ? mutation.quest : q));
      this.applyMutationMeta(mutation);
      this.savePrefs();
    } catch {
      this.quests = this.quests.filter((q) => q.id !== tempId);
      this.rebuildStats();
      this.showToast('Could not save quest');
    }
  }

  onQuickAddPrefsChange(): void {
    this.savePrefs();
  }

  filterCount(filter: QuestFilter): number {
    return countQuestsByFilter(this.quests, filter);
  }

  // -------- Toggle / edit / delete --------

  async toggleQuest(quest: Quest): Promise<void> {
    const previous = quest.completed;
    quest.completed = !previous;
    quest.completedAt = quest.completed ? new Date().toISOString() : undefined;
    this.rebuildStats();

    try {
      const mutation = await this.questBoardService.updateQuest(quest.id, { completed: quest.completed });
      this.applyMutation(quest.id, mutation.quest);
      if (mutation.spawnedQuest) {
        this.quests = [mutation.spawnedQuest, ...this.quests];
      }
      this.applyMutationMeta(mutation);
      if (mutation.quest.completed) {
        this.triggerCelebration(mutation.quest.id);
        const xpParts = [`+${mutation.quest.rewardXp} XP`];
        if (mutation.awardedSkillXp && mutation.awardedSkillXp > 0) {
          xpParts.push(`+${mutation.awardedSkillXp} skill XP`);
        }
        if (mutation.spawnedQuest) {
          xpParts.push('next one queued');
        }
        this.showToast(xpParts.join(' · '));
      }
    } catch {
      quest.completed = previous;
      quest.completedAt = previous ? quest.completedAt : undefined;
      this.rebuildStats();
      this.showToast('Could not update quest');
    }
  }

  toggleExpand(quest: Quest): void {
    if (this.expandedQuestId === quest.id) {
      this.expandedQuestId = null;
      this.editDraft = null;
      return;
    }

    this.expandedQuestId = quest.id;
    this.editDraft = {
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

  async saveEdit(quest: Quest): Promise<void> {
    if (!this.editDraft) return;
    const draft = this.editDraft;
    const title = draft.title.trim();
    if (!title) return;

    const previous = { ...quest, tags: [...(quest.tags ?? [])] };
    const tags = draft.tags
      .split(',')
      .map((tag) => tag.trim())
      .filter((tag) => tag.length > 0);

    quest.title = title;
    quest.notes = draft.notes.trim() ? draft.notes.trim() : null;
    quest.type = draft.type;
    quest.priority = draft.priority;
    quest.recurrence = draft.recurrence;
    quest.dueDate = draft.dueDate;
    quest.tags = tags;
    quest.myGameId = draft.myGameId ?? null;
    quest.gameName = draft.myGameId == null ? null : this.gameNameFor(draft.myGameId);
    quest.skillId = draft.skillId ?? null;
    quest.skillName = draft.skillId == null ? null : this.skillNameFor(draft.skillId);
    quest.folderId = draft.folderId ?? null;
    const draftFolder = draft.folderId == null ? null : this.folders.find((f) => f.id === draft.folderId) ?? null;
    quest.folderName = draftFolder?.name ?? null;
    quest.folderEmoji = draftFolder?.emoji ?? null;

    this.expandedQuestId = null;
    this.editDraft = null;

    try {
      const mutation = await this.questBoardService.updateQuest(quest.id, {
        title,
        notes: quest.notes,
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
      this.applyMutation(quest.id, mutation.quest);
      this.applyMutationMeta(mutation);
    } catch {
      const index = this.quests.findIndex((q) => q.id === quest.id);
      if (index >= 0) this.quests[index] = previous;
      this.rebuildStats();
      this.showToast('Could not save changes');
    }
  }

  cancelEdit(): void {
    this.expandedQuestId = null;
    this.editDraft = null;
  }

  async scheduleQuest(quest: Quest, dueDate: string | null, label: string): Promise<void> {
    const previous = quest.dueDate ?? null;
    quest.dueDate = dueDate;
    if (this.expandedQuestId === quest.id && this.editDraft) {
      this.editDraft = { ...this.editDraft, dueDate };
    }
    this.rebuildStats();

    try {
      const mutation = await this.questBoardService.updateQuest(quest.id, {
        dueDate: dueDate ?? undefined,
        clearDueDate: dueDate === null,
      });
      this.applyMutation(quest.id, mutation.quest);
      this.applyMutationMeta(mutation);
      this.showToast(label);
    } catch {
      quest.dueDate = previous;
      if (this.expandedQuestId === quest.id && this.editDraft) {
        this.editDraft = { ...this.editDraft, dueDate: previous };
      }
      this.rebuildStats();
      this.showToast('Could not reschedule quest');
    }
  }

  scheduleToday(quest: Quest): Promise<void> {
    return this.scheduleQuest(quest, toISODate(startOfDay(new Date())), 'Moved to today');
  }

  scheduleTomorrow(quest: Quest): Promise<void> {
    return this.scheduleQuest(quest, toISODate(addDays(startOfDay(new Date()), 1)), 'Moved to tomorrow');
  }

  clearQuestDueDate(quest: Quest): Promise<void> {
    return this.scheduleQuest(quest, null, 'Moved to inbox');
  }

  async pullNextQuestToToday(): Promise<void> {
    const quest = this.nextQueuedQuest;
    if (!quest) return;
    await this.scheduleToday(quest);
    this.filter = 'today';
    this.savePrefs();
  }

  beginDelete(quest: Quest): void {
    const removed = quest;
    this.quests = this.quests.filter((q) => q.id !== removed.id);
    this.rebuildStats();

    const timeoutId = window.setTimeout(() => {
      void this.commitDelete(removed.id);
    }, 5000);

    this.pendingDeletes.set(removed.id, { quest: removed, timeoutId });
    this.showUndoToast(removed);
  }

  undoDelete(id: number): void {
    const pending = this.pendingDeletes.get(id);
    if (!pending) return;
    window.clearTimeout(pending.timeoutId);
    this.pendingDeletes.delete(id);
    this.quests = [pending.quest, ...this.quests];
    this.rebuildStats();
    this.toastMessage = '';
  }

  private async commitDelete(id: number): Promise<void> {
    const pending = this.pendingDeletes.get(id);
    if (!pending) return;
    this.pendingDeletes.delete(id);

    try {
      await this.questBoardService.deleteQuest(id);
    } catch {
      this.quests = [pending.quest, ...this.quests];
      this.rebuildStats();
      this.showToast('Could not delete quest');
    }
  }

  // -------- Labels & helpers for template --------

  typeLabel(type: QuestType): string {
    return this.typeOptions.find((option) => option.value === type)?.label ?? type;
  }

  typeIcon(type: QuestType): string {
    return this.typeOptions.find((option) => option.value === type)?.icon ?? 'flag-outline';
  }

  priorityLabel(priority: QuestPriority): string {
    return this.priorityOptions.find((option) => option.value === priority)?.label ?? 'Medium';
  }

  recurrenceLabel(recurrence: QuestRecurrence): string {
    return this.recurrenceOptions.find((option) => option.value === recurrence)?.label ?? 'No repeat';
  }

  trackByQuest(_: number, quest: Quest): number {
    return quest.id;
  }

  trackByQuestSection(_: number, section: QuestSection): string {
    return section.id;
  }

  // -------- Skills --------

  async trainSkill(skill: QuestSkill) {
    skill.xp += 40;
    this.xp += 15;
    this.showToast(`${skill.name} training complete`);
    await this.persistSkills();
  }

  async unlockNode({ skill, nodeIndex }: SkillNodeAction) {
    if (skill.unlockedNodes.includes(nodeIndex)) {
      return;
    }
    if (this.skillNodeStateRaw(skill, nodeIndex) === 'locked') {
      this.showToast('Unlock the previous node first');
      return;
    }
    skill.unlockedNodes = [...skill.unlockedNodes, nodeIndex];
    skill.xp += 25;
    this.xp += 25;
    this.showToast(`${skill.nodes[nodeIndex]} unlocked`);
    await this.persistSkills();
  }

  openSkillModal() {
    this.newSkill = this.emptySkillForm();
    this.editingSkillId = null;
    this.modalMode = 'skill';
  }

  openEditSkillModal(skill: QuestSkill) {
    this.newSkill = { name: skill.name, icon: skill.icon, color: skill.color };
    this.editingSkillId = skill.id;
    this.modalMode = 'skill';
  }

  openNodeModal(skill: QuestSkill) {
    this.selectedSkillId = skill.id;
    this.newNodeName = '';
    this.modalMode = 'node';
  }

  closeModal() {
    this.modalMode = null;
    this.selectedSkillId = null;
    this.editingSkillId = null;
    this.newNodeName = '';
  }

  async saveSkill() {
    const name = this.newSkill.name.trim();
    if (!name) {
      return;
    }

  if (this.editingSkillId !== null) {
      const skill = this.skills.find((item) => item.id === this.editingSkillId);
      if (!skill) {
        return;
      }
      skill.name = name;
      skill.icon = this.newSkill.icon;
      skill.color = this.newSkill.color;
      this.closeModal();
      this.showToast(`${name} updated`);
      await this.persistSkills();
      return;
    }

    this.skills = [
      ...this.skills,
      {
        id: this.nextTemporaryId(),
        name,
        icon: this.newSkill.icon,
        color: this.newSkill.color,
        xp: 0,
        nodes: [],
        unlockedNodes: [],
      },
    ];
    this.closeModal();
    this.showToast(`${name} added to your skill tree`);
    await this.persistSkills();
  }

  async deleteSkill(skillId: number) {
    const skill = this.skills.find((item) => item.id === skillId);
    if (!skill) return;
    const previousSkills = this.skills.map((s) => ({ ...s, nodes: [...s.nodes], unlockedNodes: [...s.unlockedNodes] }));
    this.skills = this.skills.filter((item) => item.id !== skillId);
    this.showToast(`${skill.name} deleted`);
    const saved = await this.persistSkills();
    if (!saved) {
      this.skills = previousSkills;
    }
  }

  async addNode() {
    const skill = this.skills.find((item) => item.id === this.selectedSkillId);
    const nodeName = this.newNodeName.trim();
    if (!skill || !nodeName) {
      return;
    }
    skill.nodes = [...skill.nodes, nodeName];
    this.closeModal();
    this.showToast(`${nodeName} added`);
    await this.persistSkills();
  }

  linkedQuestCount(skill: QuestSkill): number {
    return this.quests.filter((quest) => quest.skillId === skill.id).length;
  }

  activeLinkedQuestCount(skill: QuestSkill): number {
    return this.quests.filter((quest) => quest.skillId === skill.id && !quest.completed).length;
  }

  startNodeQuest({ skill, node }: SkillNodeQuestAction): void {
    this.mode = 'quests';
    this.filter = 'today';
    this.quickAddType = 'sub';
    this.quickAddPriority = 'medium';
    // New object reference each call → child effect fires → form pre-fills.
    this.quickAddPreset = {
      title: `Practice: ${node}`,
      dueDate: toISODate(startOfDay(new Date())),
      skillId: skill.id,
      advancedOpen: true,
    };
    this.savePrefs();
    this.showToast(`${skill.name} quest draft ready`);
  }

  get skillTreeBranches(): SkillTreeBranch[] {
    return questSkillsToBranches(this.skills);
  }

  get skillTreeUnlockedNodeIds(): string[] {
    return unlockedNodeIdsFor(this.skills);
  }

  async handleSkillTreeNodeUnlocked(event: SkillTreeUnlockPayload): Promise<void> {
    const parsed = parseNodeId(event.nodeId);
    if (!parsed) return;
    const skill = this.skills.find((item) => item.id === parsed.skillId);
    if (!skill) return;
    await this.unlockNode({ skill, nodeIndex: parsed.nodeIndex });
  }

  private skillNodeStateRaw(skill: QuestSkill, nodeIndex: number): 'completed' | 'available' | 'locked' {
    if (skill.unlockedNodes.includes(nodeIndex)) return 'completed';
    const next = skill.nodes.findIndex((_, index) => !skill.unlockedNodes.includes(index));
    return nodeIndex === next ? 'available' : 'locked';
  }

  // -------- Drag-to-reorder --------

  async handleReorder(event: CustomEvent<ItemReorderEventDetail>, sectionQuests = this.visibleQuests): Promise<void> {
    const visible = sectionQuests;
    const moved = visible[event.detail.from];
    if (!moved) {
      event.detail.complete();
      return;
    }

    const reordered = [...visible];
    reordered.splice(event.detail.from, 1);
    reordered.splice(event.detail.to, 0, moved);
    event.detail.complete();

    const visibleIds = new Set(visible.map((q) => q.id));
    const others = this.quests.filter((q) => !visibleIds.has(q.id));
    const merged = [...reordered, ...others];

    merged.forEach((quest, index) => {
      quest.sortOrder = index;
    });
    this.quests = [...merged];

    try {
      await this.questBoardService.reorderQuests(
        reordered.map((q, index) => ({ id: q.id, sortOrder: index, type: q.type }))
      );
    } catch {
      this.showToast('Could not save order');
    }
  }

  // -------- Internal --------

  private applyMutation(id: number, updated: Quest): void {
    this.quests = this.quests.map((q) => (q.id === id ? updated : q));
  }

  private applyMutationMeta(mutation: QuestMutationResult): void {
    this.xp = mutation.totalXp;
    this.currentStreakDays = mutation.currentStreakDays;
    this.longestStreakDays = mutation.longestStreakDays;
    this.rebuildStats();

  if (mutation.unlockedAchievements?.length) {
      const known = new Set(this.achievements.map((a) => a.code));
      const newOnes = mutation.unlockedAchievements.filter((a) => !known.has(a.code));
      if (newOnes.length) {
        this.achievements = [...newOnes, ...this.achievements];
        this.showAchievementToast(newOnes[newOnes.length - 1]);
      }
    }
  }

  private skillNameFor(skillId: number): string {
    return this.skills.find((s) => s.id === skillId)?.name ?? 'Skill';
  }

  // -------- Subtasks --------

  async addSubtask(quest: Quest): Promise<void> {
    const draft = this.subtaskDraft(quest.id).trim();
    if (!draft) return;

    const tempId = this.nextTemporaryId();
    const optimistic: QuestSubtask = {
      id: tempId,
      title: draft,
      completed: false,
      sortOrder: quest.subtasks.length,
    };
    quest.subtasks = [...quest.subtasks, optimistic];
    this.newSubtaskTitle[quest.id] = '';

    try {
      const mutation = await this.questBoardService.addSubtask(quest.id, draft);
      this.applyMutation(quest.id, mutation.quest);
      this.applyMutationMeta(mutation);
    } catch {
      quest.subtasks = quest.subtasks.filter((s) => s.id !== tempId);
      this.showToast('Could not add subtask');
    }
  }

  async toggleSubtask({ quest, subtask }: { quest: Quest; subtask: QuestSubtask }): Promise<void> {
    const previous = subtask.completed;
    subtask.completed = !previous;
    subtask.completedAt = subtask.completed ? new Date().toISOString() : undefined;

    try {
      const mutation = await this.questBoardService.updateSubtask(quest.id, subtask.id, {
        completed: subtask.completed,
      });
      this.applyMutation(quest.id, mutation.quest);
      this.applyMutationMeta(mutation);
    } catch {
      subtask.completed = previous;
      subtask.completedAt = previous ? subtask.completedAt : undefined;
      this.showToast('Could not update subtask');
    }
  }

  async deleteSubtask({ quest, subtask }: { quest: Quest; subtask: QuestSubtask }): Promise<void> {
    const removed = subtask;
    quest.subtasks = quest.subtasks.filter((s) => s.id !== removed.id);
    try {
      await this.questBoardService.deleteSubtask(quest.id, removed.id);
    } catch {
      quest.subtasks = [...quest.subtasks, removed].sort((a, b) => a.sortOrder - b.sortOrder);
      this.showToast('Could not delete subtask');
    }
  }

  subtaskDraft(questId: number): string {
    return this.newSubtaskTitle[questId] ?? '';
  }

  setSubtaskDraft({ questId, value }: { questId: number; value: string }): void {
    this.newSubtaskTitle[questId] = value;
  }

  closeAchievementToast(): void {
    this.achievementToast = null;
  }

  private showAchievementToast(achievement: AchievementInfo): void {
    this.achievementToast = achievement;
    window.setTimeout(() => {
      if (this.achievementToast?.code === achievement.code) {
        this.achievementToast = null;
      }
    }, 4500);
  }

  private async loadBoard() {
    this.isLoading = true;
    this.errorMessage = '';
    try {
      this.applyBoard(await this.questBoardService.getBoard());
    } catch {
      this.errorMessage = 'Quest board could not be loaded.';
      this.showToast(this.errorMessage);
      this.rebuildStats();
    } finally {
      this.isLoading = false;
    }
  }

  private async loadLibrary() {
    try {
      const result = await new Promise<{ data?: MyGame[] }>((resolve, reject) => {
        this.myGameService.getAll().subscribe({ next: resolve, error: reject });
      });
      this.library = (result.data ?? [])
        .map((myGame) => ({
          myGameId: myGame.id,
          gameName: myGame.game?.name ?? 'Unknown game',
        }))
        .sort((a, b) => a.gameName.localeCompare(b.gameName));
    } catch {
      this.library = [];
    }
  }

  private gameNameFor(myGameId: number): string {
    return this.library.find((game) => game.myGameId === myGameId)?.gameName ?? 'Unknown game';
  }

  private applyBoard(board: QuestBoardState) {
    this.xp = board.xp;
    this.currentStreakDays = board.currentStreakDays;
    this.longestStreakDays = board.longestStreakDays;
    this.quests = board.quests;
    this.skills = board.skills;
    this.folders = board.folders ?? [];
    this.achievements = board.achievements;
    this.rebuildStats();
  }

  // ===== Folders ==========================================================

  /**
   * Grouped view consumed by the "Folders" mode template. Folders sharing
   * the same SectionName (case-insensitive trim) render under one header;
   * folders with no section come first in an unlabelled group. Inside each
   * section, folders keep their server-side SortOrder.
   *
   * Returned shape is plain arrays so Angular's @for can iterate cheaply
   * without recomputing on each change-detection cycle (we hand-call this
   * from a getter to keep things explicit).
   */
  get folderGroups(): { section: string | null; folders: QuestFolder[] }[] {
    const map = new Map<string, { section: string | null; folders: QuestFolder[] }>();
    const orderedKeys: string[] = [];

    for (const folder of this.folders) {
      const key = (folder.sectionName ?? '').trim().toLowerCase();
      if (!map.has(key)) {
        map.set(key, { section: folder.sectionName ?? null, folders: [] });
        orderedKeys.push(key);
      }
      map.get(key)!.folders.push(folder);
    }

    // Unfiled first, then sections by first-occurrence order.
    orderedKeys.sort((a, b) => (a === '' ? -1 : b === '' ? 1 : 0));
    return orderedKeys.map((key) => map.get(key)!);
  }

  /** Section-name suggestions used by the folder modal's chip strip. */
  get knownSections(): string[] {
    const seen = new Set<string>();
    const out: string[] = [];
    for (const folder of this.folders) {
      const name = (folder.sectionName ?? '').trim();
      if (!name) continue;
      const key = name.toLowerCase();
      if (seen.has(key)) continue;
      seen.add(key);
      out.push(name);
    }
    return out;
  }

  questsInFolder(folderId: number | null): Quest[] {
    return this.quests.filter((quest) => (quest.folderId ?? null) === folderId);
  }

  get unfiledQuestCount(): number {
    return this.questsInFolder(null).length;
  }

  isFolderCollapsed(folderId: number): boolean {
    return this.collapsedFolderIds.has(folderId);
  }

  toggleFolderCollapsed(folderId: number): void {
    const next = new Set(this.collapsedFolderIds);
    if (next.has(folderId)) next.delete(folderId);
    else next.add(folderId);
    this.collapsedFolderIds = next;
  }

  toggleUnfiledCollapsed(): void {
    this.unfiledCollapsed = !this.unfiledCollapsed;
  }

  openCreateFolderModal(): void {
    this.folderModalState = { mode: 'create', folder: null };
  }

  openEditFolderModal(folder: QuestFolder): void {
    this.folderModalState = { mode: 'edit', folder };
  }

  closeFolderModal(): void {
    if (this.folderSaving) return;
    this.folderModalState = null;
  }

  async createFolder(input: QuestFolderCreate): Promise<void> {
    if (this.folderSaving) return;
    this.folderSaving = true;
    try {
      const folder = await this.questBoardService.createFolder(input);
      this.folders = [...this.folders, folder];
      this.folderModalState = null;
    } catch {
      this.showToast('Could not create folder');
    } finally {
      this.folderSaving = false;
    }
  }

  async updateFolder(id: number, patch: QuestFolderUpdate): Promise<void> {
    if (this.folderSaving) return;
    this.folderSaving = true;
    try {
      const updated = await this.questBoardService.updateFolder(id, patch);
      this.folders = this.folders.map((f) => (f.id === id ? updated : f));
      // Propagate folder name/emoji into the quest list so cards re-render
      // with the latest label without a full board reload.
      this.quests = this.quests.map((quest) =>
        quest.folderId === id
          ? { ...quest, folderName: updated.name, folderEmoji: updated.emoji }
          : quest,
      );
      this.folderModalState = null;
    } catch {
      this.showToast('Could not save folder');
    } finally {
      this.folderSaving = false;
    }
  }

  async deleteFolder(folder: QuestFolder): Promise<void> {
    if (this.folderSaving) return;
    this.folderSaving = true;
    try {
      await this.questBoardService.deleteFolder(folder.id);
      this.folders = this.folders.filter((f) => f.id !== folder.id);
      // Backend SetNulls the FK; mirror locally so the unfiled bucket
      // picks them up immediately.
      this.quests = this.quests.map((quest) =>
        quest.folderId === folder.id
          ? { ...quest, folderId: null, folderName: null, folderEmoji: null }
          : quest,
      );
      this.folderModalState = null;
    } catch {
      this.showToast('Could not delete folder');
    } finally {
      this.folderSaving = false;
    }
  }

  trackByFolder(_: number, folder: QuestFolder): number {
    return folder.id;
  }

  trackByFolderGroup(_: number, group: { section: string | null }): string {
    return group.section ?? '__unfiled__';
  }

  openCompactFolderPicker(quest: Quest, event: MouseEvent): void {
    event.stopPropagation();
    this.compactFolderPickerQuestId = quest.id;
  }

  closeCompactFolderPicker(): void {
    this.compactFolderPickerQuestId = null;
  }

  async pickCompactFolder(quest: Quest, folderId: number | null): Promise<void> {
    this.compactFolderPickerQuestId = null;
    await this.assignFolderToQuest(quest, folderId);
  }

  /**
   * Move a quest into a folder (or out, when `folderId === null`) from
   * any view — quest card, compact row, anywhere the picker is hosted.
   * Optimistic: patch the local quest first so the UI flips instantly,
   * roll back on failure.
   */
  async assignFolderToQuest(quest: Quest, folderId: number | null): Promise<void> {
    const previousFolderId = quest.folderId ?? null;
    if (previousFolderId === folderId) return;

    const folder = folderId == null ? null : this.folders.find((f) => f.id === folderId) ?? null;
    const previousName = quest.folderName ?? null;
    const previousEmoji = quest.folderEmoji ?? null;

    // Mutate in place (the page leans on plain arrays elsewhere too —
    // matches the existing toggle / schedule flows).
    quest.folderId = folderId;
    quest.folderName = folder?.name ?? null;
    quest.folderEmoji = folder?.emoji ?? null;

    try {
      const mutation = await this.questBoardService.updateQuest(quest.id, {
        folderId: folderId ?? undefined,
        clearFolder: folderId == null,
      });
      this.applyMutation(quest.id, mutation.quest);
      this.applyMutationMeta(mutation);
    } catch {
      quest.folderId = previousFolderId;
      quest.folderName = previousName;
      quest.folderEmoji = previousEmoji;
      this.showToast('Could not move quest');
    }
  }

  // ----- Folder reorder ---------------------------------------------------

  toggleFolderReorder(): void {
    this.folderReorderActive = !this.folderReorderActive;
  }

  /**
   * `ion-reorder-group`'s detail carries `from` and `to` indices for the
   * *currently rendered* folder list. We splice locally for the optimistic
   * reorder, then persist a normalised (id, sortOrder) batch to the server.
   *
   * Note this operates on the *flat* `folders` array — section grouping is
   * a pure render-time derivation, so reordering folders here may visually
   * shuffle sections too. That's intentional: there's no separate "section
   * order" entity, sections inherit the order of their first folder.
   */
  async handleFolderReorder(event: CustomEvent): Promise<void> {
    const detail = event.detail as ItemReorderEventDetail;
    const from = detail.from;
    const to = detail.to;
    // Always call complete() — Ionic needs it to release the dragged element.
    detail.complete();

    if (from === to || from < 0 || from >= this.folders.length) {
      return;
    }

    const previous = [...this.folders];
    const next = [...this.folders];
    const [moved] = next.splice(from, 1);
    next.splice(to, 0, moved);

    // Normalise SortOrder values to the new array index, then send the
    // full set so the server doesn't have to reason about diffs.
    const payload = next.map((folder, index) => ({ id: folder.id, sortOrder: index }));
    this.folders = next.map((folder, index) => ({ ...folder, sortOrder: index }));

    try {
      await this.questBoardService.reorderFolders(payload);
    } catch {
      this.folders = previous;
      this.showToast('Could not save folder order');
    }
  }

  private async persistSkills(): Promise<boolean> {
    try {
      this.applyBoard(await this.questBoardService.saveSkills({
        xp: this.xp,
        currentStreakDays: this.currentStreakDays,
        longestStreakDays: this.longestStreakDays,
        lastCompletionDate: null,
        quests: this.quests,
        skills: this.skills,
        achievements: this.achievements,
      }));
      return true;
    } catch {
      this.showToast('Progress could not be saved');
      return false;
    }
  }

  private rebuildStats() {
    const today = startOfDay(new Date());
    const tomorrow = addDays(today, 1);

    this.level = Math.floor(this.xp / this.xpPerLevel) + 1;
    this.xpIntoLevel = this.xp % this.xpPerLevel;
    this.xpProgress = this.xpIntoLevel / this.xpPerLevel;
    this.title = this.titleForLevel(this.level);
    this.completedQuestCount = this.quests.filter((quest) => quest.completed).length;
    this.activeQuestCount = this.quests.filter((quest) => !quest.completed).length;
    this.unlockedNodeCount = this.skills.reduce((sum, skill) => sum + skill.unlockedNodes.length, 0);

    this.todayCount = this.quests.filter((quest) => {
      if (quest.completed || !quest.dueDate) return false;
      const due = new Date(quest.dueDate);
      return due >= today && due < tomorrow;
    }).length;

    this.overdueCount = this.quests.filter((quest) => {
      if (quest.completed || !quest.dueDate) return false;
      return new Date(quest.dueDate) < today;
    }).length;
  }

  private titleForLevel(level: number): string {
    if (level >= 15) return 'Legend';
    if (level >= 10) return 'Master';
    if (level >= 6) return 'Adept';
    if (level >= 3) return 'Apprentice';
    return 'Initiate';
  }

  private emptySkillForm(): SkillForm {
    return { name: '', icon: 'code-slash-outline', color: '#2563eb' };
  }

  private nextTemporaryId(): number {
    return -Math.floor(Math.random() * 1_000_000_000);
  }

  private loadPrefs(): void {
    if (typeof window === 'undefined' || !window.localStorage) return;
    try {
      const raw = window.localStorage.getItem(this.prefsStorageKey);
      if (!raw) return;
      const parsed = JSON.parse(raw) as {
        filter?: QuestFilter | 'overdue';
        quickAddType?: QuestType;
        quickAddPriority?: QuestPriority;
        quickAddRecurrence?: QuestRecurrence;
        questViewMode?: QuestViewMode;
      };
      if (parsed.filter === 'overdue') {
        this.filter = 'today';
      } else if (parsed.filter && ['today', 'upcoming', 'inbox', 'all'].includes(parsed.filter)) {
        this.filter = parsed.filter;
      }
      if (parsed.questViewMode === 'cards' || parsed.questViewMode === 'compact') {
        this.questViewMode = parsed.questViewMode;
      }
      if (parsed.quickAddType && ['main', 'sub', 'faction'].includes(parsed.quickAddType)) {
        this.quickAddType = parsed.quickAddType;
      }
      if (parsed.quickAddPriority && ['low', 'medium', 'high'].includes(parsed.quickAddPriority)) {
        this.quickAddPriority = parsed.quickAddPriority;
      }
      if (parsed.quickAddRecurrence && ['none', 'daily', 'weekly', 'monthly'].includes(parsed.quickAddRecurrence)) {
        this.quickAddRecurrence = parsed.quickAddRecurrence;
      }
    } catch {
      // ignore corrupt prefs
    }
  }

  private savePrefs(): void {
    if (typeof window === 'undefined' || !window.localStorage) return;
    try {
      window.localStorage.setItem(
        this.prefsStorageKey,
        JSON.stringify({
          filter: this.filter,
          quickAddType: this.quickAddType,
          quickAddPriority: this.quickAddPriority,
          quickAddRecurrence: this.quickAddRecurrence,
          questViewMode: this.questViewMode,
        })
      );
    } catch {
      // ignore quota errors
    }
  }

  private showToast(message: string) {
    this.toastMessage = message;
    if (this.undoToastTimer) {
      window.clearTimeout(this.undoToastTimer);
    }
    this.undoToastTimer = window.setTimeout(() => {
      this.toastMessage = '';
    }, 2600);
  }

  // Undo toast uses pendingDelete state in the template instead of toastMessage,
  // so it can render an undo button. Keep this lightweight.
  private showUndoToast(_quest: Quest) {
    this.toastMessage = '';
    if (this.undoToastTimer) {
      window.clearTimeout(this.undoToastTimer);
    }
  }

  get pendingDeleteList(): Quest[] {
    return Array.from(this.pendingDeletes.values()).map((p) => p.quest);
  }
}
