import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  IonBadge,
  IonButton,
  IonContent,
  IonIcon,
  IonLabel,
  IonProgressBar,
  IonReorder,
  IonReorderGroup,
  IonSegment,
  IonSegmentButton,
  ItemReorderEventDetail,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  addOutline,
  alertCircleOutline,
  bookOutline,
  brushOutline,
  calendarClearOutline,
  calendarOutline,
  checkmarkCircle,
  checkmarkCircleOutline,
  closeOutline,
  codeSlashOutline,
  createOutline,
  filterOutline,
  flagOutline,
  flameOutline,
  flashOutline,
  gameControllerOutline,
  libraryOutline,
  linkOutline,
  lockClosedOutline,
  mapOutline,
  pricetagOutline,
  refreshOutline,
  restaurantOutline,
  schoolOutline,
  searchOutline,
  shieldCheckmarkOutline,
  sparkles,
  sparklesOutline,
  starOutline,
  todayOutline,
  trashOutline,
  trophyOutline,
} from 'ionicons/icons';
import {
  Quest,
  QuestBoardService,
  QuestBoardState,
  QuestPriority,
  QuestRecurrence,
  QuestSkill,
  QuestType,
} from '../services/quest-board.service';
import { MyGameService } from 'src/app/features/my-games/services/my-game.service';
import { MyGame } from 'src/app/features/games/models/games.model';

type PageMode = 'quests' | 'skills';
type ModalMode = 'skill' | 'node' | null;
type QuestFilter = 'today' | 'upcoming' | 'overdue' | 'all';

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

@Component({
  selector: 'app-quest-board',
  templateUrl: './quest-board.page.html',
  styleUrls: ['./quest-board.page.scss'],
  imports: [
    CommonModule,
    FormsModule,
    IonBadge,
    IonButton,
    IonContent,
    IonIcon,
    IonLabel,
    IonProgressBar,
    IonReorder,
    IonReorderGroup,
    IonSegment,
    IonSegmentButton,
  ],
})
export class QuestBoardPage implements OnInit, OnDestroy {
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
    { value: 'today', label: 'Today', icon: 'today-outline' },
    { value: 'upcoming', label: 'Upcoming', icon: 'calendar-outline' },
    { value: 'overdue', label: 'Overdue', icon: 'alert-circle-outline' },
    { value: 'all', label: 'All', icon: 'filter-outline' },
  ];

  mode: PageMode = 'quests';
  filter: QuestFilter = 'today';
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
  toastMessage = '';
  isLoading = true;
  errorMessage = '';

  quests: Quest[] = [];
  skills: QuestSkill[] = [];

  // Quick-add
  quickAddTitle = '';
  quickAddType: QuestType = 'sub';
  quickAddPriority: QuestPriority = 'medium';
  quickAddRecurrence: QuestRecurrence = 'none';
  quickAddDue: string | null = null;
  quickAddGameId: number | null = null;

  // Search & tag filter
  searchQuery = '';
  tagFilter: string | null = null;

  // Inline edit
  expandedQuestId: number | null = null;
  editDraft: {
    title: string;
    notes: string;
    type: QuestType;
    priority: QuestPriority;
    recurrence: QuestRecurrence;
    dueDate: string | null;
    tags: string;
    myGameId: number | null;
  } | null = null;

  // Skills
  newSkill = this.emptySkillForm();
  newNodeName = '';
  selectedSkillId: number | null = null;
  editingSkillId: number | null = null;

  library: LibraryGame[] = [];

  private readonly xpPerLevel = 200;
  private readonly prefsStorageKey = 'questboard.prefs.v1';
  private pendingDeletes = new Map<number, PendingDelete>();
  private undoToastTimer: number | undefined;

  constructor(
    private questBoardService: QuestBoardService,
    private myGameService: MyGameService,
  ) {
    addIcons({
      addOutline,
      alertCircleOutline,
      bookOutline,
      brushOutline,
      calendarClearOutline,
      calendarOutline,
      checkmarkCircle,
      checkmarkCircleOutline,
      closeOutline,
      codeSlashOutline,
      createOutline,
      filterOutline,
      flagOutline,
      flameOutline,
      flashOutline,
      gameControllerOutline,
      libraryOutline,
      linkOutline,
      lockClosedOutline,
      mapOutline,
      pricetagOutline,
      refreshOutline,
      restaurantOutline,
      schoolOutline,
      searchOutline,
      shieldCheckmarkOutline,
      sparkles,
      sparklesOutline,
      starOutline,
      todayOutline,
      trashOutline,
      trophyOutline,
    });
  }

  async ngOnInit() {
    this.loadPrefs();
    await Promise.all([this.loadBoard(), this.loadLibrary()]);
  }

  ngOnDestroy(): void {
    for (const pending of this.pendingDeletes.values()) {
      window.clearTimeout(pending.timeoutId);
    }
    this.pendingDeletes.clear();
    if (this.undoToastTimer) {
      window.clearTimeout(this.undoToastTimer);
    }
  }

  // -------- Filters & sorting --------

  get visibleQuests(): Quest[] {
    const now = new Date();
    const today = startOfDay(now);
    const tomorrow = addDays(today, 1);

    const search = this.searchQuery.trim().toLowerCase();
    const tag = this.tagFilter?.toLowerCase() ?? null;

    const matchesFilter = (quest: Quest): boolean => {
      switch (this.filter) {
        case 'today': {
          if (quest.completed) {
            return quest.completedAt ? startOfDay(new Date(quest.completedAt)).getTime() === today.getTime() : false;
          }
          if (!quest.dueDate) return false;
          const due = new Date(quest.dueDate);
          return due >= today && due < tomorrow;
        }
        case 'upcoming': {
          if (quest.completed || !quest.dueDate) return false;
          return new Date(quest.dueDate) >= tomorrow;
        }
        case 'overdue': {
          if (quest.completed || !quest.dueDate) return false;
          return new Date(quest.dueDate) < today;
        }
        case 'all':
        default:
          return true;
      }
    };

    const matchesSearch = (quest: Quest): boolean => {
      if (!search) return true;
      return (
        quest.title.toLowerCase().includes(search) ||
        (quest.notes ?? '').toLowerCase().includes(search) ||
        (quest.tags ?? []).some((t) => t.toLowerCase().includes(search))
      );
    };

    const matchesTag = (quest: Quest): boolean => {
      if (!tag) return true;
      return (quest.tags ?? []).some((t) => t.toLowerCase() === tag);
    };

    const filtered = this.quests
      .filter((quest) => matchesFilter(quest) && matchesSearch(quest) && matchesTag(quest));

    if (this.filter === 'all' && !search && !tag) {
      return filtered.sort((a, b) => this.compareForManualOrder(a, b));
    }

    return filtered.sort((a, b) => this.compareQuests(a, b));
  }

  get availableTags(): string[] {
    const set = new Set<string>();
    for (const quest of this.quests) {
      for (const tag of quest.tags ?? []) {
        if (tag.trim()) set.add(tag.trim());
      }
    }
    return Array.from(set).sort((a, b) => a.localeCompare(b));
  }

  get isManualOrderActive(): boolean {
    return this.filter === 'all' && !this.searchQuery.trim() && !this.tagFilter;
  }

  private compareForManualOrder(a: Quest, b: Quest): number {
    if (a.completed !== b.completed) return a.completed ? 1 : -1;
    if (a.completed && b.completed) {
      const aAt = a.completedAt ? new Date(a.completedAt).getTime() : 0;
      const bAt = b.completedAt ? new Date(b.completedAt).getTime() : 0;
      return bAt - aAt;
    }
    return a.sortOrder - b.sortOrder;
  }

  private compareQuests(a: Quest, b: Quest): number {
    if (a.completed !== b.completed) {
      return a.completed ? 1 : -1;
    }
    if (a.completed && b.completed) {
      const aAt = a.completedAt ? new Date(a.completedAt).getTime() : 0;
      const bAt = b.completedAt ? new Date(b.completedAt).getTime() : 0;
      return bAt - aAt;
    }
    const aPrio = this.priorityWeight(a.priority);
    const bPrio = this.priorityWeight(b.priority);
    if (aPrio !== bPrio) {
      return bPrio - aPrio;
    }
    const aDue = a.dueDate ? new Date(a.dueDate).getTime() : Number.MAX_SAFE_INTEGER;
    const bDue = b.dueDate ? new Date(b.dueDate).getTime() : Number.MAX_SAFE_INTEGER;
    if (aDue !== bDue) {
      return aDue - bDue;
    }
    return a.sortOrder - b.sortOrder;
  }

  private priorityWeight(priority: QuestPriority): number {
    return this.priorityOptions.find((option) => option.value === priority)?.weight ?? 1;
  }

  setFilter(value: unknown): void {
    if (value === 'today' || value === 'upcoming' || value === 'overdue' || value === 'all') {
      this.filter = value;
      this.savePrefs();
    }
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

  async submitQuickAdd(): Promise<void> {
    const title = this.quickAddTitle.trim();
    if (!title) {
      return;
    }

    const tempId = this.nextTemporaryId();
    const optimistic: Quest = {
      id: tempId,
      title,
      notes: null,
      type: this.quickAddType,
      priority: this.quickAddPriority,
      recurrence: this.quickAddRecurrence,
      dueDate: this.quickAddDue ?? null,
      tags: [],
      completed: false,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      rewardXp: 0,
      sortOrder: 0,
      myGameId: this.quickAddGameId ?? null,
      gameName: this.quickAddGameId == null ? null : this.gameNameFor(this.quickAddGameId),
    };

    this.quests = [optimistic, ...this.quests];
    this.quickAddTitle = '';
    this.rebuildStats();

    try {
      const mutation = await this.questBoardService.createQuest({
        title,
        type: this.quickAddType,
        priority: this.quickAddPriority,
        recurrence: this.quickAddRecurrence,
        dueDate: this.quickAddDue ?? null,
        myGameId: this.quickAddGameId ?? null,
      });
      this.quests = this.quests.map((q) => (q.id === tempId ? mutation.quest : q));
      this.xp = mutation.totalXp;
      this.rebuildStats();
      this.savePrefs();
    } catch {
      this.quests = this.quests.filter((q) => q.id !== tempId);
      this.rebuildStats();
      this.showToast('Could not save quest');
    }
  }

  resetQuickAddDue(): void {
    this.quickAddDue = null;
  }

  setQuickAddToday(): void {
    this.quickAddDue = isoDate(startOfDay(new Date()));
  }

  setQuickAddTomorrow(): void {
    this.quickAddDue = isoDate(addDays(startOfDay(new Date()), 1));
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
      this.xp = mutation.totalXp;
      this.rebuildStats();
      if (mutation.quest.completed) {
        const xpMsg = `+${mutation.quest.rewardXp} XP`;
        this.showToast(mutation.spawnedQuest ? `${xpMsg} · next one queued` : xpMsg);
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
      dueDate: quest.dueDate ? isoDate(new Date(quest.dueDate)) : null,
      tags: (quest.tags ?? []).join(', '),
      myGameId: quest.myGameId ?? null,
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
      });
      this.applyMutation(quest.id, mutation.quest);
      this.xp = mutation.totalXp;
      this.rebuildStats();
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

  dueDateLabel(quest: Quest): string {
    if (!quest.dueDate) return '';
    const due = new Date(quest.dueDate);
    if (Number.isNaN(due.getTime())) return '';
    const today = startOfDay(new Date());
    const dueDay = startOfDay(due);
    const diffDays = Math.round((dueDay.getTime() - today.getTime()) / 86400000);

    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Tomorrow';
    if (diffDays === -1) return 'Yesterday';
    if (diffDays < -1 && diffDays >= -7) return `${Math.abs(diffDays)}d overdue`;
    if (diffDays > 1 && diffDays <= 7) return `In ${diffDays}d`;

    return new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric' }).format(due);
  }

  dueState(quest: Quest): 'overdue' | 'today' | 'soon' | 'later' | 'none' {
    if (!quest.dueDate) return 'none';
    const due = new Date(quest.dueDate);
    if (Number.isNaN(due.getTime())) return 'none';
    const today = startOfDay(new Date());
    const dueDay = startOfDay(due);
    if (dueDay < today) return 'overdue';
    if (dueDay.getTime() === today.getTime()) return 'today';
    const diff = (dueDay.getTime() - today.getTime()) / 86400000;
    return diff <= 3 ? 'soon' : 'later';
  }

  trackByQuest(_: number, quest: Quest): number {
    return quest.id;
  }

  trackBySkill(_: number, skill: QuestSkill): number {
    return skill.id;
  }

  trackByText(_: number, item: string): string {
    return item;
  }

  trackByLibrary(_: number, game: LibraryGame): number {
    return game.myGameId;
  }

  // -------- Skills (unchanged behavior, board save) --------

  async trainSkill(skill: QuestSkill) {
    skill.xp += 40;
    this.xp += 15;
    this.showToast(`${skill.name} training complete`);
    await this.persistSkills();
  }

  async unlockNode(skill: QuestSkill, nodeIndex: number) {
    if (skill.unlockedNodes.includes(nodeIndex)) {
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
    this.newSkill = {
      name: skill.name,
      icon: skill.icon,
      color: skill.color,
    };
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
        nodes: ['First practice', 'Weekly streak', 'Personal project'],
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

  skillLevel(skill: QuestSkill): number {
    return Math.floor(skill.xp / 100) + 1;
  }

  skillProgress(skill: QuestSkill): number {
    return (skill.xp % 100) / 100;
  }

  unlockedCount(skill: QuestSkill): number {
    return skill.unlockedNodes.length;
  }

  // -------- Drag-to-reorder --------

  async handleReorder(event: CustomEvent<ItemReorderEventDetail>): Promise<void> {
    const visible = this.visibleQuests;
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
    this.quests = board.quests;
    this.skills = board.skills;
    this.rebuildStats();
  }

  private async persistSkills(): Promise<boolean> {
    try {
      this.applyBoard(await this.questBoardService.saveSkills({
        xp: this.xp,
        quests: this.quests,
        skills: this.skills,
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

  private emptySkillForm() {
    return {
      name: '',
      icon: 'code-slash-outline',
      color: '#2563eb',
    };
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
        filter?: QuestFilter;
        quickAddType?: QuestType;
        quickAddPriority?: QuestPriority;
        quickAddRecurrence?: QuestRecurrence;
      };
      if (parsed.filter && ['today', 'upcoming', 'overdue', 'all'].includes(parsed.filter)) {
        this.filter = parsed.filter;
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
  private showUndoToast(quest: Quest) {
    this.toastMessage = '';
    if (this.undoToastTimer) {
      window.clearTimeout(this.undoToastTimer);
    }
  }

  get pendingDeleteList(): Quest[] {
    return Array.from(this.pendingDeletes.values()).map((p) => p.quest);
  }
}

function startOfDay(date: Date): Date {
  const d = new Date(date);
  d.setHours(0, 0, 0, 0);
  return d;
}

function addDays(date: Date, days: number): Date {
  const d = new Date(date);
  d.setDate(d.getDate() + days);
  return d;
}

function isoDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
