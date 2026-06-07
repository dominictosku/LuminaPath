import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IonContent, IonIcon, IonSpinner } from '@ionic/angular/standalone';
import { firstValueFrom } from 'rxjs';

import { MediaFilter } from 'src/app/core/entities/mediaFilter';
import { MyGame } from '../../games/models/games.model';
import { MyGameService } from '../../my-games/services/my-game.service';
import { QuestBoardService } from '../../quests/services/quest-board.service';
import type {
  Quest,
  QuestFolder,
  QuestPriority,
  QuestRecurrence,
  QuestType,
} from '../../quests/services/quest-board.service';
import { GamingSession, GamingSessionService } from '../../planning/services/gaming-session.service';
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
  layoutOverlappingBlocks,
  minutesSinceDayStart,
  shiftWeek,
  startOfWeek,
  timeLabelFromMinutes,
  weekTitle,
} from '../domain/weekly-schedule.helpers';

type CalendarGroupId = 'quests' | 'sessions';

type LibraryGameOption = {
  myGameId: number;
  gameName: string;
  playtime: number | null;
};

type SidebarQuestGroup = {
  key: string;
  folder: QuestFolder | null;
  quests: Quest[];
};

type QuestScheduleDraft = {
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

type DragPayload =
  | { type: 'quest'; questId: number }
  | { type: 'game'; myGameId: number };

@Component({
  selector: 'app-weekly-schedule',
  templateUrl: './weekly-schedule.page.html',
  styleUrls: ['./weekly-schedule.page.scss'],
  imports: [
    FormsModule,
    IonContent,
    IonIcon,
    IonSpinner,
  ],
})
export class WeeklySchedulePage implements OnInit {
  private questService = inject(QuestBoardService);
  private myGameService = inject(MyGameService);
  private sessionService = inject(GamingSessionService);

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

  protected weekAnchor = startOfWeek(new Date());
  protected weekDays: WeekScheduleDay[] = buildWeekDays(this.weekAnchor);
  protected title = weekTitle(this.weekAnchor);
  protected isLoading = true;
  protected isSaving = false;
  protected errorMessage = '';
  protected questSearch = '';
  protected gameSearch = '';
  protected draft: QuestScheduleDraft | null = null;
  protected draftError = '';
  protected pendingGameId: number | null = null;
  protected readonly groupVisibility: Record<CalendarGroupId, boolean> = {
    quests: true,
    sessions: true,
  };

  private quests: Quest[] = [];
  private folders: QuestFolder[] = [];
  private sessions: GamingSession[] = [];
  private games: LibraryGameOption[] = [];
  private dragged: DragPayload | null = null;
  protected blocksByDay = new Map<string, WeekScheduleBlock[]>();

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  protected async load(): Promise<void> {
    this.isLoading = true;
    this.errorMessage = '';
    try {
      const weekStart = startOfWeek(this.weekAnchor);
      const weekEnd = new Date(weekStart);
      weekEnd.setDate(weekStart.getDate() + 7);

      const [board, sessions, myGames] = await Promise.all([
        this.questService.getBoard(),
        firstValueFrom(this.sessionService.list({ from: weekStart, to: weekEnd })),
        firstValueFrom(this.myGameService.getAll(this.libraryFilter())),
      ]);

      this.quests = board.quests ?? [];
      this.folders = [...(board.folders ?? [])].sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name));
      this.sessions = sessions ?? [];
      this.games = (myGames.data ?? [])
        .map((item) => this.toLibraryGame(item))
        .filter((item): item is LibraryGameOption => item !== null)
        .sort((a, b) => a.gameName.localeCompare(b.gameName));
      this.rebuildCalendar();
    } catch {
      this.errorMessage = 'Weekly schedule could not be loaded.';
    } finally {
      this.isLoading = false;
    }
  }

  protected async shift(direction: -1 | 1): Promise<void> {
    this.weekAnchor = shiftWeek(this.weekAnchor, direction);
    await this.load();
  }

  protected async today(): Promise<void> {
    this.weekAnchor = startOfWeek(new Date());
    await this.load();
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
        count: this.quests.filter((quest) => this.isScheduledQuest(quest)).length,
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
    const start = dateAtMinutes(day.date, minutes);
    const endMinutes = this.clampedEndMinutes(minutes, 60);
    const selectedGame = this.pendingGameId == null
      ? null
      : this.games.find((game) => game.myGameId === this.pendingGameId) ?? null;

    this.draft = {
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
    this.draftError = '';
  }

  protected openQuest(block: WeekScheduleBlock): void {
    if (block.kind !== 'quest') return;
    const questId = Number(block.id.replace('quest-', ''));
    const quest = this.quests.find((item) => item.id === questId);
    if (!quest || !quest.scheduledStartAt || !quest.scheduledEndAt) return;

    const start = new Date(quest.scheduledStartAt);
    const end = new Date(quest.scheduledEndAt);
    this.draft = {
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
    this.draftError = '';
  }

  protected closeDraft(): void {
    this.draft = null;
    this.draftError = '';
  }

  protected async saveDraft(): Promise<void> {
    if (!this.draft || this.isSaving) return;
    const title = this.draft.title.trim();
    if (!title) {
      this.draftError = 'Title is required.';
      return;
    }

    const range = this.draftRange(this.draft);
    if (!range) {
      this.draftError = 'Pick a valid time range.';
      return;
    }

    this.isSaving = true;
    this.draftError = '';
    try {
      if (this.draft.mode === 'create') {
        await this.questService.createQuest({
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
        await this.questService.updateQuest(this.draft.questId, {
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

      this.closeDraft();
      await this.load();
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
      await this.questService.updateQuest(this.draft.questId, { clearSchedule: true });
      this.closeDraft();
      await this.load();
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
      this.closeDraft();
      await this.load();
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
      await this.questService.updateQuest(quest.id, { completed: !quest.completed });
      this.closeDraft();
      await this.load();
    } catch {
      this.draftError = 'Quest could not be updated.';
    } finally {
      this.isSaving = false;
    }
  }

  protected selectGameForNextQuest(game: LibraryGameOption): void {
    this.pendingGameId = this.pendingGameId === game.myGameId ? null : game.myGameId;
  }

  protected dragQuest(event: DragEvent, quest: Quest): void {
    this.dragged = { type: 'quest', questId: quest.id };
    event.dataTransfer?.setData('text/plain', `quest:${quest.id}`);
    event.dataTransfer?.setDragImage?.(event.currentTarget as Element, 16, 16);
  }

  protected dragGame(event: DragEvent, game: LibraryGameOption): void {
    this.dragged = { type: 'game', myGameId: game.myGameId };
    event.dataTransfer?.setData('text/plain', `game:${game.myGameId}`);
    event.dataTransfer?.setDragImage?.(event.currentTarget as Element, 16, 16);
  }

  protected endDrag(): void {
    this.dragged = null;
  }

  protected allowDrop(event: DragEvent): void {
    if (!this.dragged) return;
    event.preventDefault();
  }

  protected async dropOnSlot(event: DragEvent, day: WeekScheduleDay, minutes: number): Promise<void> {
    if (!this.dragged) return;
    event.preventDefault();
    const payload = this.dragged;
    this.dragged = null;

    if (payload.type === 'quest') {
      const quest = this.quests.find((item) => item.id === payload.questId);
      if (!quest) return;
      await this.scheduleQuestAt(quest, day.date, minutes);
      return;
    }

    await this.createSessionAt(payload.myGameId, day.date, minutes);
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

  protected formatBlockTime(block: WeekScheduleBlock): string {
    return formatTimeRange(block.startAt, block.endAt);
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
    if (folderId == null) return '#7c3aed';
    return this.folders.find((folder) => folder.id === folderId)?.color || '#7c3aed';
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

  private rebuildCalendar(): void {
    this.weekDays = buildWeekDays(this.weekAnchor);
    this.title = weekTitle(this.weekAnchor);
    const map = new Map<string, WeekScheduleBlock[]>();

    for (const day of this.weekDays) {
      map.set(day.key, layoutOverlappingBlocks(this.blocksFor(day)));
    }

    this.blocksByDay = map;
  }

  private blocksFor(day: WeekScheduleDay): WeekScheduleBlock[] {
    const blocks: WeekScheduleBlock[] = [];

    if (this.groupVisibility.quests) {
      for (const quest of this.quests) {
        if (!this.isScheduledQuest(quest) || dateKey(new Date(quest.scheduledStartAt!)) !== day.key) {
          continue;
        }
        blocks.push({
          id: `quest-${quest.id}`,
          kind: 'quest',
          title: quest.title,
          subtitle: quest.gameName ?? quest.folderName ?? priorityLabel(quest.priority),
          startAt: quest.scheduledStartAt!,
          endAt: quest.scheduledEndAt!,
          color: this.folderColor(quest.folderId),
          completed: quest.completed,
          lane: 0,
          laneCount: 1,
        });
      }
    }

    if (this.groupVisibility.sessions) {
      for (const session of this.sessions) {
        const start = new Date(session.scheduledAt);
        if (dateKey(start) !== day.key) {
          continue;
        }
        const end = new Date(start);
        end.setMinutes(start.getMinutes() + session.durationMinutes);
        blocks.push({
          id: `session-${session.id}`,
          kind: 'session',
          title: session.gameName ?? 'Gaming session',
          subtitle: session.notes || `${session.durationMinutes} min`,
          startAt: start.toISOString(),
          endAt: end.toISOString(),
          color: '#06b6d4',
          completed: session.completed,
          lane: 0,
          laneCount: 1,
        });
      }
    }

    return blocks;
  }

  private async scheduleQuestAt(quest: Quest, day: Date, minutes: number): Promise<void> {
    const start = dateAtMinutes(day, minutes);
    const end = new Date(start);
    end.setMinutes(start.getMinutes() + this.clampedDuration(minutes, this.questDurationMinutes(quest)));
    try {
      await this.questService.updateQuest(quest.id, {
        dueDate: dateKey(start),
        scheduledStartAt: start.toISOString(),
        scheduledEndAt: end.toISOString(),
      });
      await this.load();
    } catch {
      this.errorMessage = 'Quest could not be scheduled.';
    }
  }

  private async createSessionAt(myGameId: number, day: Date, minutes: number): Promise<void> {
    const start = dateAtMinutes(day, minutes);
    const durationMinutes = this.clampedDuration(minutes, 90);
    try {
      await firstValueFrom(this.sessionService.create({
        myGameId,
        scheduledAt: start.toISOString(),
        durationMinutes,
        completed: false,
        notes: null,
      }));
      await this.load();
    } catch {
      this.errorMessage = 'Gaming session could not be planned.';
    }
  }

  private questDurationMinutes(quest: Quest): number {
    if (!quest.scheduledStartAt || !quest.scheduledEndAt) {
      return 60;
    }
    const start = new Date(quest.scheduledStartAt).getTime();
    const end = new Date(quest.scheduledEndAt).getTime();
    const minutes = Math.round((end - start) / 60_000);
    return Number.isFinite(minutes) && minutes > 0 ? minutes : 60;
  }

  private clampedDuration(startMinutes: number, requestedMinutes: number): number {
    const remainingMinutes = WEEK_END_HOUR * 60 - startMinutes;
    return Math.max(SLOT_MINUTES, Math.min(requestedMinutes, remainingMinutes));
  }

  private clampedEndMinutes(startMinutes: number, requestedMinutes: number): number {
    return Math.min(startMinutes + this.clampedDuration(startMinutes, requestedMinutes), WEEK_END_HOUR * 60 - 1);
  }

  private draftRange(draft: QuestScheduleDraft): { start: Date; end: Date } | null {
    const start = this.combineDateTime(draft.scheduledDate, draft.startTime);
    const end = this.combineDateTime(draft.scheduledDate, draft.endTime);
    if (!start || !end || end <= start) {
      return null;
    }
    return { start, end };
  }

  private combineDateTime(dateValue: string, timeValue: string): Date | null {
    const [year, month, day] = dateValue.split('-').map(Number);
    const [hour, minute] = timeValue.split(':').map(Number);
    if ([year, month, day, hour, minute].some((value) => !Number.isFinite(value))) {
      return null;
    }
    return new Date(year, month - 1, day, hour, minute, 0, 0);
  }

  private isScheduledQuest(quest: Quest): boolean {
    return Boolean(quest.scheduledStartAt && quest.scheduledEndAt);
  }

  private questSortKey(quest: Quest): string {
    const schedule = quest.scheduledStartAt ?? quest.dueDate ?? '9999';
    return `${schedule}|${quest.sortOrder.toString().padStart(5, '0')}|${quest.title.toLowerCase()}`;
  }

  private toLibraryGame(myGame: MyGame): LibraryGameOption | null {
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

  private libraryFilter(): MediaFilter {
    const filter = new MediaFilter();
    filter.Paging.Count = 500;
    return filter;
  }
}

function priorityLabel(priority: QuestPriority): string {
  return priority === 'high'
    ? 'High priority'
    : priority === 'low'
      ? 'Low priority'
      : 'Medium priority';
}
