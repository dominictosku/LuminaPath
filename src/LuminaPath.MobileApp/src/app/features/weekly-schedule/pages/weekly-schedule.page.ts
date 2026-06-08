import { AfterViewInit, Component, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IonContent, IonIcon, IonSegment, IonSegmentButton, IonSpinner } from '@ionic/angular/standalone';
import { firstValueFrom } from 'rxjs';

import { MediaFilter } from 'src/app/core/entities/mediaFilter';
import { Game } from '../../games/models/games.model';
import { GameService } from '../../games/services/game.service';
import { MyGameService } from '../../my-games/services/my-game.service';
import { QuestBoardService } from '../../quests/services/quest-board.service';
import type {
  Quest,
  QuestFolder,
  QuestMutationResult,
  QuestPriority,
  QuestRecurrence,
} from '../../quests/services/quest-board.service';
import { GamingSession, GamingSessionService } from '../../planning/services/gaming-session.service';
import {
  hasRecurrence as hasCalendarRecurrence,
  shiftForRecurrence,
  startOfCalendarMonthGrid,
} from '../../planning/domain/planning-calendar.helpers';
import { PlanningCalendarService } from '../../planning/services/planning-calendar.service';
import type {
  CalendarMode,
  PlanningCalendarDay,
  TimelineEvent,
  TimelineEventKind,
} from '../../planning/services/planning-calendar.service';
import {
  HOUR_HEIGHT,
  SLOT_MINUTES,
  WEEK_END_HOUR,
  WEEK_START_HOUR,
  WeekScheduleBlock,
  WeekScheduleDay,
  buildWeekDays,
  dateAtMinutes,
  dateKey,
  eventHeight,
  eventTop,
  formatTimeRange,
  minutesSinceDayStart,
  shiftWeek,
  startOfWeek,
  timeLabelFromMinutes,
  weekTitle,
} from '../domain/weekly-schedule.helpers';
import {
  buildWeeklyScheduleBlocksByDay,
  folderColor as scheduleFolderColor,
  isScheduledQuest,
  type WeeklyScheduleCalendarGroupId,
} from '../domain/weekly-schedule.builder';
import {
  clampedDuration,
  combineDateTime,
  createQuestDraftForSlot,
  editQuestDraftForQuest,
  questDraftRange,
  questDurationMinutes,
  sessionDraftForSession,
  toLibraryGame,
  type LibraryGameOption,
  type QuestScheduleDraft,
  type SessionScheduleDraft,
} from '../domain/weekly-schedule.drafts';

type CalendarGroupId = WeeklyScheduleCalendarGroupId;
type ScheduleViewMode = 'planner' | 'overview';
type SessionWindow = { from: Date; to: Date };
type ScheduleLoadOptions = {
  autoScrollCalendar?: boolean;
  forceBoard?: boolean;
  forceLibrary?: boolean;
};
type SlotActionDraft = {
  day: WeekScheduleDay;
  minutes: number;
  game: LibraryGameOption | null;
  quest: Quest | null;
};

type SidebarQuestGroup = {
  key: string;
  folder: QuestFolder | null;
  quests: Quest[];
};

type DragPayload =
  | { type: 'quest'; questId: number }
  | { type: 'game'; myGameId: number }
  | { type: 'session'; sessionId: number };

type DragPreview = {
  kind: 'quest' | 'game' | 'session';
  title: string;
  subtitle: string;
  action: string;
  color: string;
  x: number;
  y: number;
};

@Component({
  selector: 'app-weekly-schedule',
  templateUrl: './weekly-schedule.page.html',
  styleUrls: ['./weekly-schedule.page.scss'],
  imports: [
    FormsModule,
    IonContent,
    IonIcon,
    IonSegment,
    IonSegmentButton,
    IonSpinner,
  ],
})
export class WeeklySchedulePage implements OnInit, AfterViewInit {
  private questService = inject(QuestBoardService);
  private gameService = inject(GameService);
  private myGameService = inject(MyGameService);
  private sessionService = inject(GamingSessionService);
  private planningCalendar = inject(PlanningCalendarService);

  protected readonly hours = Array.from(
    { length: WEEK_END_HOUR - WEEK_START_HOUR },
    (_, index) => WEEK_START_HOUR + index,
  );
  protected readonly slots = Array.from(
    { length: ((WEEK_END_HOUR - WEEK_START_HOUR) * 60) / SLOT_MINUTES },
    (_, index) => WEEK_START_HOUR * 60 + index * SLOT_MINUTES,
  );
  protected readonly dayHeight = (WEEK_END_HOUR - WEEK_START_HOUR) * HOUR_HEIGHT;
  protected readonly slotHeight = HOUR_HEIGHT / 2;
  protected readonly timezoneLabel = this.resolveTimezoneLabel();

  protected weekAnchor = startOfWeek(new Date());
  protected weekDays: WeekScheduleDay[] = buildWeekDays(this.weekAnchor);
  protected title = weekTitle(this.weekAnchor);
  protected isLoading = true;
  protected isOverviewLoading = false;
  protected isSaving = false;
  protected viewMode: ScheduleViewMode = 'planner';
  protected overviewMode: CalendarMode = 'week';
  protected overviewAnchor = this.planningCalendar.startOfToday();
  protected overviewTitle = '';
  protected overviewDays: PlanningCalendarDay[] = [];
  protected errorMessage = '';
  protected questSearch = '';
  protected gameSearch = '';
  protected draft: QuestScheduleDraft | null = null;
  protected sessionDraft: SessionScheduleDraft | null = null;
  protected slotActionDraft: SlotActionDraft | null = null;
  protected draftError = '';
  protected sessionDraftError = '';
  protected pendingQuestId: number | null = null;
  protected pendingGameId: number | null = null;
  protected dragPreview: DragPreview | null = null;
  protected activeDropKey: string | null = null;
  protected readonly groupVisibility: Record<CalendarGroupId, boolean> = {
    quests: true,
    sessions: true,
  };

  private quests: Quest[] = [];
  private folders: QuestFolder[] = [];
  private sessions: GamingSession[] = [];
  private overviewSessions: GamingSession[] = [];
  private overviewSessionRange: SessionWindow | null = null;
  private games: LibraryGameOption[] = [];
  private allGames: Game[] = [];
  private dragged: DragPayload | null = null;
  private draggingId: string | null = null;
  private transparentDragImage: HTMLCanvasElement | null = null;
  private boardLoaded = false;
  private libraryLoaded = false;
  private readonly overviewCachePaddingDays = 42;
  private readonly defaultCalendarStartMinutes = 6 * 60;
  private calendarAutoScrollQueued = false;
  protected blocksByDay = new Map<string, WeekScheduleBlock[]>();

  @ViewChild('calendarScroll') private calendarScroll?: ElementRef<HTMLElement>;

  async ngOnInit(): Promise<void> {
    await this.load({ autoScrollCalendar: true });
  }

  ngAfterViewInit(): void {
    this.queueCalendarAutoScroll();
  }

  protected async load(options: ScheduleLoadOptions = {}): Promise<void> {
    this.isLoading = true;
    this.errorMessage = '';
    try {
      const weekWindow = this.weekSessionWindow();
      const overviewWindow = this.overviewSessionWindow();
      const shouldLoadBoard = options.forceBoard || !this.boardLoaded;
      const shouldLoadLibrary = options.forceLibrary || !this.libraryLoaded;
      const shouldLoadOverview = !this.overviewSessionRangeContains(overviewWindow);
      const expandedOverviewWindow = shouldLoadOverview
        ? this.expandedOverviewSessionWindow(overviewWindow)
        : null;

      const [board, sessions, overviewSessions, library] = await Promise.all([
        shouldLoadBoard ? this.questService.getBoard() : Promise.resolve(null),
        firstValueFrom(this.sessionService.list(weekWindow)),
        expandedOverviewWindow
          ? firstValueFrom(this.sessionService.list(expandedOverviewWindow))
          : Promise.resolve(null),
        shouldLoadLibrary
          ? Promise.all([
            firstValueFrom(this.myGameService.getAll(this.libraryFilter())),
            firstValueFrom(this.gameService.getAll(this.libraryFilter())),
          ])
          : Promise.resolve(null),
      ]);

      if (board) {
        this.quests = board.quests ?? [];
        this.folders = [...(board.folders ?? [])].sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name));
        this.boardLoaded = true;
      }
      this.sessions = sessions ?? [];
      if (expandedOverviewWindow && overviewSessions) {
        this.setOverviewSessionCache(expandedOverviewWindow, overviewSessions);
      }
      if (library) {
        const [myGames, games] = library;
        this.games = (myGames.data ?? [])
          .map(toLibraryGame)
          .filter((item): item is LibraryGameOption => item !== null)
          .sort((a, b) => a.gameName.localeCompare(b.gameName));
        this.allGames = games.data ?? [];
        this.libraryLoaded = true;
      }
      this.rebuildCalendar();
      this.buildOverviewCalendar();
    } catch {
      this.errorMessage = 'Weekly schedule could not be loaded.';
    } finally {
      this.isLoading = false;
      if (options.autoScrollCalendar) {
        this.queueCalendarAutoScroll();
      }
    }
  }

  protected async shift(direction: -1 | 1): Promise<void> {
    this.weekAnchor = shiftWeek(this.weekAnchor, direction);
    await this.load({ autoScrollCalendar: true });
  }

  protected async today(): Promise<void> {
    this.weekAnchor = startOfWeek(new Date());
    await this.load({ autoScrollCalendar: true });
  }

  protected async setViewMode(mode: ScheduleViewMode): Promise<void> {
    if (this.viewMode === mode) {
      return;
    }

    this.viewMode = mode;
    if (mode === 'planner') {
      this.queueCalendarAutoScroll();
    }
    if (mode === 'overview' && this.overviewDays.length === 0) {
      await this.refreshOverviewCalendar();
    }
  }

  protected async shiftOverview(direction: -1 | 1): Promise<void> {
    this.overviewAnchor = this.planningCalendar.shiftAnchor(this.overviewAnchor, this.overviewMode, direction);
    await this.refreshOverviewCalendar();
  }

  protected async overviewToday(): Promise<void> {
    this.overviewAnchor = this.planningCalendar.startOfToday();
    await this.refreshOverviewCalendar();
  }

  protected async setOverviewMode(mode: CalendarMode): Promise<void> {
    if (this.overviewMode === mode) {
      return;
    }

    this.overviewMode = mode;
    await this.refreshOverviewCalendar();
  }

  protected async openPlannerDay(day: PlanningCalendarDay): Promise<void> {
    this.viewMode = 'planner';
    const nextAnchor = startOfWeek(day.date);
    const sameWeek = dateKey(nextAnchor) === dateKey(startOfWeek(this.weekAnchor));
    this.weekAnchor = nextAnchor;
    if (sameWeek) {
      this.rebuildCalendar();
      this.queueCalendarAutoScroll();
      return;
    }

    await this.load({ autoScrollCalendar: true });
  }

  protected async openOverviewEvent(event: TimelineEvent, day: PlanningCalendarDay, clickEvent: MouseEvent): Promise<void> {
    clickEvent.stopPropagation();
    if (event.kind === 'quest') {
      this.openQuestById(event.sourceId);
      return;
    }

    if (event.kind === 'session') {
      this.openSessionById(event.sourceId);
      return;
    }

    await this.openPlannerDay(day);
  }

  protected toggleGroup(group: CalendarGroupId): void {
    this.groupVisibility[group] = !this.groupVisibility[group];
    this.rebuildCalendar();
  }

  protected calendarGroups(): { id: CalendarGroupId; label: string; count: number; color: string; icon: string }[] {
    return [
      {
        id: 'quests',
        label: 'Scheduled quests',
        count: this.quests.filter(isScheduledQuest).length,
        color: '#7c3aed',
        icon: 'sparkles-outline',
      },
      {
        id: 'sessions',
        label: 'Gaming sessions',
        count: this.sessions.length,
        color: '#06b6d4',
        icon: 'game-controller-outline',
      },
    ];
  }

  protected sidebarQuestGroups(): SidebarQuestGroup[] {
    const query = this.questSearch.trim().toLowerCase();
    const openQuests = this.quests
      .filter((quest) => !quest.completed)
      .filter((quest) => {
        if (!query) return true;
        return [
          quest.title,
          quest.gameName ?? '',
          quest.folderName ?? '',
          ...(quest.tags ?? []),
        ].some((value) => value.toLowerCase().includes(query));
      })
      .sort((a, b) => this.questSortKey(a).localeCompare(this.questSortKey(b)));

    const folderGroups = this.folders
      .map((folder) => ({
        key: `folder-${folder.id}`,
        folder,
        quests: openQuests.filter((quest) => quest.folderId === folder.id),
      }))
      .filter((group) => group.quests.length > 0);

    const unfiled = openQuests.filter((quest) => (quest.folderId ?? null) === null);
    return [
      ...folderGroups,
      ...(unfiled.length ? [{ key: 'unfiled', folder: null, quests: unfiled }] : []),
    ];
  }

  protected visibleGames(): LibraryGameOption[] {
    const query = this.gameSearch.trim().toLowerCase();
    return this.games
      .filter((game) => !query || game.gameName.toLowerCase().includes(query))
      .slice(0, 40);
  }

  protected blocksForDay(day: WeekScheduleDay): WeekScheduleBlock[] {
    return this.blocksByDay.get(day.key) ?? [];
  }

  protected openCreateQuest(day: WeekScheduleDay, minutes: number): void {
    const selectedGame = this.pendingGameId == null
      ? null
      : this.games.find((game) => game.myGameId === this.pendingGameId) ?? null;

    this.draft = createQuestDraftForSlot(day, minutes, selectedGame);
    this.sessionDraft = null;
    this.slotActionDraft = null;
    this.draftError = '';
  }

  protected openSlotActions(day: WeekScheduleDay, minutes: number): void {
    const selectedQuest = this.pendingQuestId == null
      ? null
      : this.quests.find((quest) => quest.id === this.pendingQuestId) ?? null;
    const selectedGame = this.pendingGameId == null
      ? null
      : this.games.find((game) => game.myGameId === this.pendingGameId) ?? null;
    this.slotActionDraft = {
      day,
      minutes,
      game: selectedGame,
      quest: selectedQuest,
    };
    this.draft = null;
    this.sessionDraft = null;
  }

  protected closeSlotActions(): void {
    this.slotActionDraft = null;
  }

  protected async planQuestFromSlot(): Promise<void> {
    if (!this.slotActionDraft) return;
    const { day, minutes, quest } = this.slotActionDraft;
    this.slotActionDraft = null;
    if (quest) {
      this.pendingQuestId = null;
      await this.scheduleQuestAt(quest, day.date, minutes);
      return;
    }

    this.openCreateQuest(day, minutes);
  }

  protected async createSessionFromSlot(): Promise<void> {
    if (!this.slotActionDraft) return;
    const { day, minutes, game } = this.slotActionDraft;
    this.slotActionDraft = null;
    await this.createSessionAt(game?.myGameId ?? null, day.date, minutes);
  }

  protected openQuest(block: WeekScheduleBlock): void {
    if (block.kind !== 'quest') return;
    const questId = block.sourceId ?? Number(block.id.replace('quest-', ''));
    this.openQuestById(questId);
  }

  protected closeDraft(): void {
    this.draft = null;
    this.draftError = '';
  }

  protected closeSessionDraft(): void {
    this.sessionDraft = null;
    this.sessionDraftError = '';
  }

  protected async saveDraft(): Promise<void> {
    if (!this.draft || this.isSaving) return;
    const title = this.draft.title.trim();
    if (!title) {
      this.draftError = 'Title is required.';
      return;
    }

    const range = questDraftRange(this.draft);
    if (!range) {
      this.draftError = 'Pick a valid time range.';
      return;
    }

    this.isSaving = true;
    this.draftError = '';
    try {
      let result: QuestMutationResult | null = null;
      if (this.draft.mode === 'create') {
        result = await this.questService.createQuest({
          title,
          notes: this.draft.notes.trim() || null,
          type: this.draft.type,
          priority: this.draft.priority,
          recurrence: this.draft.recurrence,
          dueDate: this.draft.scheduledDate,
          scheduledStartAt: range.start.toISOString(),
          scheduledEndAt: range.end.toISOString(),
          folderId: this.draft.folderId,
          myGameId: this.draft.myGameId,
        });
      } else if (this.draft.questId != null) {
        result = await this.questService.updateQuest(this.draft.questId, {
          title,
          notes: this.draft.notes.trim(),
          type: this.draft.type,
          priority: this.draft.priority,
          recurrence: this.draft.recurrence,
          dueDate: this.draft.scheduledDate,
          scheduledStartAt: range.start.toISOString(),
          scheduledEndAt: range.end.toISOString(),
          folderId: this.draft.folderId ?? undefined,
          clearFolder: this.draft.folderId == null,
          myGameId: this.draft.myGameId ?? undefined,
          clearMyGame: this.draft.myGameId == null,
        });
      }

      if (result) {
        this.applyQuestMutation(result);
      }
      this.closeDraft();
    } catch {
      this.draftError = 'Quest could not be saved.';
    } finally {
      this.isSaving = false;
    }
  }

  protected async clearDraftSchedule(): Promise<void> {
    if (!this.draft || this.draft.mode !== 'edit' || this.draft.questId == null || this.isSaving) return;
    this.isSaving = true;
    try {
      const result = await this.questService.updateQuest(this.draft.questId, { clearSchedule: true });
      this.applyQuestMutation(result);
      this.closeDraft();
    } catch {
      this.draftError = 'Schedule could not be cleared.';
    } finally {
      this.isSaving = false;
    }
  }

  protected async deleteDraftQuest(): Promise<void> {
    if (!this.draft || this.draft.mode !== 'edit' || this.draft.questId == null || this.isSaving) return;
    this.isSaving = true;
    try {
      await this.questService.deleteQuest(this.draft.questId);
      this.removeQuest(this.draft.questId);
      this.closeDraft();
    } catch {
      this.draftError = 'Quest could not be deleted.';
    } finally {
      this.isSaving = false;
    }
  }

  protected async toggleDraftComplete(): Promise<void> {
    if (!this.draft || this.draft.mode !== 'edit' || this.draft.questId == null || this.isSaving) return;
    const quest = this.quests.find((item) => item.id === this.draft!.questId);
    if (!quest) return;
    this.isSaving = true;
    try {
      const result = await this.questService.updateQuest(quest.id, { completed: !quest.completed });
      this.applyQuestMutation(result);
      this.closeDraft();
    } catch {
      this.draftError = 'Quest could not be updated.';
    } finally {
      this.isSaving = false;
    }
  }

  protected openBlock(event: MouseEvent, block: WeekScheduleBlock): void {
    event.stopPropagation();
    if (block.kind === 'quest') {
      this.openQuest(block);
      return;
    }

    this.openSession(block);
  }

  protected openSession(block: WeekScheduleBlock): void {
    if (block.kind !== 'session') return;
    const sessionId = Number(block.id.replace('session-', ''));
    this.openSessionById(sessionId);
  }

  protected openQuestById(questId: number): void {
    const quest = this.quests.find((item) => item.id === questId);
    if (!quest) return;

    this.draft = editQuestDraftForQuest(quest, {
      defaultStartMinutes: this.defaultCalendarStartMinutes,
    });
    this.sessionDraft = null;
    this.slotActionDraft = null;
    this.draftError = '';
  }

  protected openSessionById(sessionId: number): void {
    const session = this.sessions.find((item) => item.id === sessionId)
      ?? this.overviewSessions.find((item) => item.id === sessionId);
    if (!session) return;

    this.sessionDraft = sessionDraftForSession(session);
    this.draft = null;
    this.slotActionDraft = null;
    this.sessionDraftError = '';
  }

  protected async saveSessionDraft(): Promise<void> {
    if (!this.sessionDraft || this.isSaving) return;
    const draft = this.sessionDraft;

    const scheduledAt = combineDateTime(draft.scheduledDate, draft.startTime);
    if (!scheduledAt || draft.durationMinutes <= 0) {
      this.sessionDraftError = 'Pick a valid date, time, and duration.';
      return;
    }

    const session = this.sessions.find((item) => item.id === draft.sessionId)
      ?? this.overviewSessions.find((item) => item.id === draft.sessionId);
    if (!session) return;

    this.isSaving = true;
    this.sessionDraftError = '';
    try {
      const updated = await firstValueFrom(this.sessionService.update(session.id, {
        id: session.id,
        myGameId: draft.myGameId,
        scheduledAt: scheduledAt.toISOString(),
        durationMinutes: draft.durationMinutes,
        completed: draft.completed,
        completedAt: session.completedAt,
        notes: draft.notes.trim() || null,
      }));
      this.upsertSession(updated);
      this.closeSessionDraft();
    } catch {
      this.sessionDraftError = 'Gaming session could not be saved.';
    } finally {
      this.isSaving = false;
    }
  }

  protected async toggleSessionDraftComplete(): Promise<void> {
    if (!this.sessionDraft || this.isSaving) return;
    this.sessionDraft.completed = !this.sessionDraft.completed;
    await this.saveSessionDraft();
  }

  protected async deleteSessionDraft(): Promise<void> {
    if (!this.sessionDraft || this.isSaving) return;
    const sessionId = this.sessionDraft.sessionId;
    this.isSaving = true;
    try {
      await firstValueFrom(this.sessionService.remove(sessionId));
      this.removeSession(sessionId);
      this.closeSessionDraft();
    } catch {
      this.sessionDraftError = 'Gaming session could not be deleted.';
    } finally {
      this.isSaving = false;
    }
  }

  protected selectGameForNextSlot(game: LibraryGameOption): void {
    this.pendingGameId = this.pendingGameId === game.myGameId ? null : game.myGameId;
    if (this.pendingGameId !== null) {
      this.pendingQuestId = null;
    }
  }

  protected selectQuestForNextSlot(quest: Quest): void {
    this.pendingQuestId = this.pendingQuestId === quest.id ? null : quest.id;
    if (this.pendingQuestId !== null) {
      this.pendingGameId = null;
    }
  }

  protected dragQuest(event: DragEvent, quest: Quest): void {
    this.beginDrag(event, { type: 'quest', questId: quest.id }, {
      kind: 'quest',
      title: quest.title,
      subtitle: quest.gameName ?? quest.folderName ?? priorityLabel(quest.priority),
      action: quest.scheduledStartAt ? 'Move quest' : 'Schedule quest',
      color: this.folderColor(quest.folderId),
    });
  }

  protected dragGame(event: DragEvent, game: LibraryGameOption): void {
    this.beginDrag(event, { type: 'game', myGameId: game.myGameId }, {
      kind: 'game',
      title: game.gameName,
      subtitle: game.playtime ? `${game.playtime}h estimate` : 'Library game',
      action: 'Plan session',
      color: '#06b6d4',
    });
  }

  protected dragBlock(event: DragEvent, block: WeekScheduleBlock): void {
    if (block.projected) {
      event.preventDefault();
      return;
    }

    if (block.kind === 'quest') {
      const questId = Number(block.id.replace('quest-', ''));
      this.beginDrag(event, { type: 'quest', questId }, this.previewFromBlock(block, 'Move quest'));
    } else {
      const sessionId = Number(block.id.replace('session-', ''));
      this.beginDrag(event, { type: 'session', sessionId }, this.previewFromBlock(block, 'Move session'));
    }
  }

  protected endDrag(): void {
    this.clearDragState();
  }

  protected allowDrop(event: DragEvent, day: WeekScheduleDay, minutes: number): void {
    if (!this.dragged) return;
    event.preventDefault();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = this.dragged.type === 'game' ? 'copy' : 'move';
    }
    this.activeDropKey = this.slotKey(day, minutes);
    this.updateDragPreviewPosition(event);
  }

  protected clearDropSlot(key: string): void {
    if (this.activeDropKey === key) {
      this.activeDropKey = null;
    }
  }

  protected async dropOnSlot(event: DragEvent, day: WeekScheduleDay, minutes: number): Promise<void> {
    if (!this.dragged) return;
    event.preventDefault();
    const payload = this.dragged;
    this.clearDragState();

    if (payload.type === 'quest') {
      const quest = this.quests.find((item) => item.id === payload.questId);
      if (!quest) return;
      await this.scheduleQuestAt(quest, day.date, minutes);
      return;
    }

    if (payload.type === 'session') {
      const session = this.sessions.find((item) => item.id === payload.sessionId);
      if (!session) return;
      await this.rescheduleSessionAt(session, day.date, minutes);
      return;
    }

    await this.createSessionAt(payload.myGameId, day.date, minutes);
  }

  protected isDraggingItem(id: string): boolean {
    return this.draggingId === id;
  }

  protected isActiveDropSlot(day: WeekScheduleDay, minutes: number): boolean {
    return this.activeDropKey === this.slotKey(day, minutes);
  }

  protected slotKey(day: WeekScheduleDay, minutes: number): string {
    return `${day.key}-${minutes}`;
  }

  protected dragPreviewTransform(): string {
    if (!this.dragPreview) {
      return 'translate3d(0, 0, 0)';
    }

    return `translate3d(${this.dragPreview.x + 18}px, ${this.dragPreview.y + 18}px, 0)`;
  }

  private beginDrag(
    event: DragEvent,
    payload: DragPayload,
    preview: Omit<DragPreview, 'x' | 'y'>,
  ): void {
    this.dragged = payload;
    this.draggingId = this.dragId(payload);
    this.activeDropKey = null;
    this.dragPreview = {
      ...preview,
      x: event.clientX,
      y: event.clientY,
    };

    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = payload.type === 'game' ? 'copy' : 'move';
      event.dataTransfer.setData('text/plain', this.dragData(payload));
      this.setTransparentDragImage(event.dataTransfer);
    }
  }

  private clearDragState(): void {
    this.dragged = null;
    this.dragPreview = null;
    this.activeDropKey = null;
    this.draggingId = null;
  }

  private updateDragPreviewPosition(event: DragEvent): void {
    if (!this.dragPreview) return;
    const nextX = event.clientX || this.dragPreview.x;
    const nextY = event.clientY || this.dragPreview.y;
    this.dragPreview = {
      ...this.dragPreview,
      x: nextX,
      y: nextY,
    };
  }

  private setTransparentDragImage(dataTransfer: DataTransfer): void {
    if (!this.transparentDragImage) {
      this.transparentDragImage = document.createElement('canvas');
      this.transparentDragImage.width = 1;
      this.transparentDragImage.height = 1;
    }

    dataTransfer.setDragImage(this.transparentDragImage, 0, 0);
  }

  private dragId(payload: DragPayload): string {
    if (payload.type === 'quest') {
      return `quest-${payload.questId}`;
    }

    if (payload.type === 'session') {
      return `session-${payload.sessionId}`;
    }

    return `game-${payload.myGameId}`;
  }

  private dragData(payload: DragPayload): string {
    if (payload.type === 'quest') {
      return `quest:${payload.questId}`;
    }

    if (payload.type === 'session') {
      return `session:${payload.sessionId}`;
    }

    return `game:${payload.myGameId}`;
  }

  private previewFromBlock(block: WeekScheduleBlock, action: string): Omit<DragPreview, 'x' | 'y'> {
    return {
      kind: block.kind,
      title: block.title,
      subtitle: `${formatTimeRange(block.startAt, block.endAt)}${block.subtitle ? ` · ${block.subtitle}` : ''}`,
      action,
      color: block.color,
    };
  }

  protected blockTop(block: WeekScheduleBlock): number {
    return eventTop(block.startAt);
  }

  protected blockHeight(block: WeekScheduleBlock): number {
    return Math.max(32, eventHeight(block.startAt, block.endAt) - 4);
  }

  protected blockLeft(block: WeekScheduleBlock): string {
    return `calc(${(block.lane / block.laneCount) * 100}% + 5px)`;
  }

  protected blockWidth(block: WeekScheduleBlock): string {
    return `calc(${100 / block.laneCount}% - 10px)`;
  }

  protected slotTop(minutes: number): number {
    return ((minutes - WEEK_START_HOUR * 60) / 60) * HOUR_HEIGHT;
  }

  protected hourLabel(hour: number): string {
    return `${String(hour).padStart(2, '0')}:00`;
  }

  protected slotAriaLabel(day: WeekScheduleDay, minutes: number): string {
    return `Plan ${day.label} ${day.dayNumber} at ${timeLabelFromMinutes(minutes)}`;
  }

  protected slotTimeLabel(minutes: number): string {
    return timeLabelFromMinutes(minutes);
  }

  protected formatBlockTime(block: WeekScheduleBlock): string {
    return formatTimeRange(block.startAt, block.endAt);
  }

  protected recurrenceLabel(recurrence: QuestRecurrence | null | undefined): string {
    return recurrence === 'daily'
      ? 'Daily'
      : recurrence === 'weekly'
        ? 'Weekly'
        : recurrence === 'monthly'
          ? 'Monthly'
          : 'No repeat';
  }

  protected recurrenceShortLabel(recurrence: QuestRecurrence | null | undefined): string {
    return recurrence === 'daily'
      ? 'D'
      : recurrence === 'weekly'
        ? 'W'
        : recurrence === 'monthly'
          ? 'M'
          : '';
  }

  protected hasRecurrence(recurrence: QuestRecurrence | null | undefined): boolean {
    return hasCalendarRecurrence(recurrence);
  }

  protected draftRecurrencePreview(draft: QuestScheduleDraft): string {
    if (!this.hasRecurrence(draft.recurrence)) {
      return '';
    }

    const start = combineDateTime(draft.scheduledDate, draft.startTime);
    if (!start) {
      return `${this.recurrenceLabel(draft.recurrence)} repeat`;
    }

    return `Next ${this.recurrenceLabel(draft.recurrence).toLowerCase()} occurrence: ${this.formatDateTime(shiftForRecurrence(start, draft.recurrence))}`;
  }

  protected questScheduleLabel(quest: Quest): string {
    if (!quest.scheduledStartAt || !quest.scheduledEndAt) {
      return '';
    }

    return formatTimeRange(quest.scheduledStartAt, quest.scheduledEndAt);
  }

  protected currentTimeTop(): number {
    return this.slotTop(minutesSinceDayStart(new Date()));
  }

  protected isCurrentWeek(): boolean {
    const todayKey = dateKey(new Date());
    return this.weekDays.some((day) => day.key === todayKey);
  }

  protected folderColor(folderId: number | null | undefined): string {
    return scheduleFolderColor(this.folders, folderId);
  }

  protected folderName(folder: QuestFolder | null): string {
    return folder ? `${folder.emoji} ${folder.name}`.trim() : 'Unfiled';
  }

  protected folderOptions(): QuestFolder[] {
    return this.folders;
  }

  protected gameOptions(): LibraryGameOption[] {
    return this.games;
  }

  protected selectedQuest(): Quest | null {
    if (!this.draft || this.draft.questId == null) return null;
    return this.quests.find((quest) => quest.id === this.draft!.questId) ?? null;
  }

  protected trackByDay(_: number, day: WeekScheduleDay): string {
    return day.key;
  }

  protected trackByOverviewDay(_: number, day: PlanningCalendarDay): string {
    return day.key;
  }

  protected trackByOverviewEvent(_: number, event: TimelineEvent): string {
    return event.id;
  }

  protected trackByBlock(_: number, block: WeekScheduleBlock): string {
    return block.id;
  }

  protected trackByQuest(_: number, quest: Quest): number {
    return quest.id;
  }

  protected trackByQuestGroup(_: number, group: SidebarQuestGroup): string {
    return group.key;
  }

  protected trackByGame(_: number, game: LibraryGameOption): number {
    return game.myGameId;
  }

  protected overviewEventIcon(kind: TimelineEventKind): string {
    switch (kind) {
      case 'release':
        return 'rocket-outline';
      case 'quest':
        return 'flag-outline';
      case 'session':
      default:
        return 'time-outline';
    }
  }

  private rebuildCalendar(): void {
    this.weekDays = buildWeekDays(this.weekAnchor);
    this.title = weekTitle(this.weekAnchor);
    this.blocksByDay = buildWeeklyScheduleBlocksByDay({
      days: this.weekDays,
      quests: this.quests,
      sessions: this.sessions,
      folders: this.folders,
      groupVisibility: this.groupVisibility,
    });
  }

  private buildOverviewCalendar(): void {
    const calendar = this.planningCalendar.build({
      mode: this.overviewMode,
      anchor: this.overviewAnchor,
      sessions: this.overviewSessions,
      games: this.allGames,
      quests: this.quests,
    });
    this.overviewTitle = calendar.title;
    this.overviewDays = calendar.days;
  }

  private async refreshOverviewCalendar(): Promise<void> {
    const window = this.overviewSessionWindow();
    if (!this.overviewSessionRangeContains(window)) {
      this.isOverviewLoading = true;
      this.errorMessage = '';
      try {
        const expandedWindow = this.expandedOverviewSessionWindow(window);
        const sessions = await firstValueFrom(this.sessionService.list(expandedWindow));
        this.setOverviewSessionCache(expandedWindow, sessions ?? []);
      } catch {
        this.errorMessage = 'Calendar sessions could not be loaded.';
      } finally {
        this.isOverviewLoading = false;
      }
    }

    this.buildOverviewCalendar();
  }

  private overviewSessionWindow(): SessionWindow {
    const start = this.overviewMode === 'week'
      ? startOfWeek(this.overviewAnchor)
      : startOfCalendarMonthGrid(this.overviewAnchor);
    const to = new Date(start);
    to.setDate(start.getDate() + (this.overviewMode === 'week' ? 7 : 42));
    return { from: start, to };
  }

  private expandedOverviewSessionWindow(window: SessionWindow): SessionWindow {
    const from = new Date(window.from);
    from.setDate(from.getDate() - this.overviewCachePaddingDays);
    const to = new Date(window.to);
    to.setDate(to.getDate() + this.overviewCachePaddingDays);
    return { from, to };
  }

  private overviewSessionRangeContains(window: SessionWindow): boolean {
    return this.overviewSessionRange !== null
      && this.overviewSessionRange.from.getTime() <= window.from.getTime()
      && this.overviewSessionRange.to.getTime() >= window.to.getTime();
  }

  private setOverviewSessionCache(window: SessionWindow, sessions: GamingSession[]): void {
    this.overviewSessionRange = {
      from: new Date(window.from),
      to: new Date(window.to),
    };
    this.overviewSessions = sessions;
  }

  private async scheduleQuestAt(quest: Quest, day: Date, minutes: number): Promise<void> {
    const start = dateAtMinutes(day, minutes);
    const end = new Date(start);
    end.setMinutes(start.getMinutes() + clampedDuration(minutes, questDurationMinutes(quest)));
    const previousQuests = this.quests;
    this.upsertQuest({
      ...quest,
      dueDate: dateKey(start),
      scheduledStartAt: start.toISOString(),
      scheduledEndAt: end.toISOString(),
    });
    try {
      const result = await this.questService.updateQuest(quest.id, {
        dueDate: dateKey(start),
        scheduledStartAt: start.toISOString(),
        scheduledEndAt: end.toISOString(),
      });
      this.applyQuestMutation(result);
    } catch {
      this.quests = previousQuests;
      this.rebuildCalendar();
      this.buildOverviewCalendar();
      this.errorMessage = 'Quest could not be scheduled.';
    }
  }

  private async createSessionAt(myGameId: number | null, day: Date, minutes: number): Promise<void> {
    const start = dateAtMinutes(day, minutes);
    const durationMinutes = clampedDuration(minutes, 90);
    try {
      const created = await firstValueFrom(this.sessionService.create({
        myGameId,
        scheduledAt: start.toISOString(),
        durationMinutes,
        completed: false,
        notes: null,
      }));
      this.upsertSession(created);
    } catch {
      this.errorMessage = 'Gaming session could not be planned.';
    }
  }

  private async rescheduleSessionAt(session: GamingSession, day: Date, minutes: number): Promise<void> {
    const start = dateAtMinutes(day, minutes);
    const durationMinutes = clampedDuration(minutes, session.durationMinutes);
    const previousSessions = this.sessions;
    const previousOverviewSessions = this.overviewSessions;
    this.upsertSession({
      ...session,
      scheduledAt: start.toISOString(),
      durationMinutes,
    });
    try {
      const updated = await firstValueFrom(this.sessionService.update(session.id, {
        id: session.id,
        myGameId: session.myGameId,
        scheduledAt: start.toISOString(),
        durationMinutes,
        completed: session.completed,
        completedAt: session.completedAt,
        notes: session.notes,
      }));
      this.upsertSession(updated);
    } catch {
      this.sessions = previousSessions;
      this.overviewSessions = previousOverviewSessions;
      this.rebuildCalendar();
      this.buildOverviewCalendar();
      this.errorMessage = 'Gaming session could not be rescheduled.';
    }
  }

  private applyQuestMutation(result: QuestMutationResult): void {
    this.upsertQuest(result.quest);
    if (result.spawnedQuest) {
      this.upsertQuest(result.spawnedQuest);
    }
  }

  private upsertQuest(quest: Quest): void {
    const existingIndex = this.quests.findIndex((item) => item.id === quest.id);
    this.quests = existingIndex === -1
      ? [quest, ...this.quests]
      : this.quests.map((item) => item.id === quest.id ? quest : item);
    this.rebuildCalendar();
    this.buildOverviewCalendar();
  }

  private removeQuest(questId: number): void {
    this.quests = this.quests.filter((quest) => quest.id !== questId);
    this.rebuildCalendar();
    this.buildOverviewCalendar();
  }

  private upsertSession(session: GamingSession): void {
    this.sessions = this.upsertSessionInWindow(this.sessions, session, this.weekSessionWindow());
    if (this.overviewSessionRange) {
      this.overviewSessions = this.upsertSessionInWindow(this.overviewSessions, session, this.overviewSessionRange);
    }
    this.rebuildCalendar();
    this.buildOverviewCalendar();
  }

  private removeSession(sessionId: number): void {
    this.sessions = this.sessions.filter((session) => session.id !== sessionId);
    this.overviewSessions = this.overviewSessions.filter((session) => session.id !== sessionId);
    this.rebuildCalendar();
    this.buildOverviewCalendar();
  }

  private upsertSessionInWindow(sessions: GamingSession[], session: GamingSession, window: SessionWindow): GamingSession[] {
    const withoutSession = sessions.filter((item) => item.id !== session.id);
    return this.sessionInWindow(session, window)
      ? [...withoutSession, session]
      : withoutSession;
  }

  private sessionInWindow(session: GamingSession, window: SessionWindow): boolean {
    const time = new Date(session.scheduledAt).getTime();
    return time >= window.from.getTime() && time < window.to.getTime();
  }

  private weekSessionWindow(): SessionWindow {
    const from = startOfWeek(this.weekAnchor);
    const to = new Date(from);
    to.setDate(from.getDate() + 7);
    return { from, to };
  }

  private formatDateTime(value: Date): string {
    return new Intl.DateTimeFormat('en', {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    }).format(value);
  }

  private questSortKey(quest: Quest): string {
    const schedule = quest.scheduledStartAt ?? quest.dueDate ?? '9999';
    return `${schedule}|${quest.sortOrder.toString().padStart(5, '0')}|${quest.title.toLowerCase()}`;
  }

  private libraryFilter(): MediaFilter {
    const filter = new MediaFilter();
    filter.Paging.Count = 500;
    return filter;
  }

  private queueCalendarAutoScroll(): void {
    if (this.calendarAutoScrollQueued) {
      return;
    }

    this.calendarAutoScrollQueued = true;
    setTimeout(() => {
      this.calendarAutoScrollQueued = false;
      this.scrollCalendarToHelpfulTime();
    });
  }

  private scrollCalendarToHelpfulTime(): void {
    const scroller = this.calendarScroll?.nativeElement;
    if (!scroller || this.isLoading || this.viewMode !== 'planner') {
      return;
    }

    const targetTop = this.slotTop(this.calendarScrollTargetMinutes());
    const maxScrollTop = Math.max(0, scroller.scrollHeight - scroller.clientHeight);
    scroller.scrollTop = Math.min(Math.max(0, targetTop), maxScrollTop);
  }

  private calendarScrollTargetMinutes(): number {
    const visibleStartMinutes = WEEK_START_HOUR * 60;
    const fallbackStartMinutes = Math.max(visibleStartMinutes, this.defaultCalendarStartMinutes);

    if (!this.isCurrentWeek()) {
      return fallbackStartMinutes;
    }

    const leadMinutes = 90;
    const latestUsefulStart = WEEK_END_HOUR * 60 - SLOT_MINUTES;
    const currentMinutes = minutesSinceDayStart(new Date());
    return Math.min(
      Math.max(fallbackStartMinutes, currentMinutes - leadMinutes),
      latestUsefulStart,
    );
  }

  private resolveTimezoneLabel(): string {
    return new Intl.DateTimeFormat(undefined, { timeZoneName: 'short' })
      .formatToParts(new Date())
      .find((part) => part.type === 'timeZoneName')?.value ?? 'Local';
  }
}

function priorityLabel(priority: QuestPriority): string {
  return priority === 'high'
    ? 'High priority'
    : priority === 'low'
      ? 'Low priority'
      : 'Medium priority';
}
