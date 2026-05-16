import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  IonBadge,
  IonButton,
  IonContent,
  IonIcon,
  IonProgressBar,
  IonRefresher,
  IonRefresherContent,
  IonSegment,
  IonSegmentButton,
  IonSelect,
  IonSelectOption,
  IonSpinner,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  addOutline,
  calendarClearOutline,
  checkmarkCircle,
  checkmarkCircleOutline,
  chevronBackOutline,
  chevronForwardOutline,
  flagOutline,
  gameControllerOutline,
  hourglassOutline,
  rocketOutline,
  timeOutline,
  trashOutline,
} from 'ionicons/icons';
import { firstValueFrom } from 'rxjs';

import { Game } from '../../games/models/games.model';
import { GameService } from '../../games/services/game.service';
import { Quest, QuestBoardService } from '../../quests/services/quest-board.service';
import { ReleasePlanComponent } from '../../release-calendar/components/release-plan.component';
import { GameForecast, GamingSession, GamingSessionService } from '../services/gaming-session.service';
import { MediaFilter } from 'src/app/core/entities/mediaFilter';

type PlanMode = 'sessions' | 'calendar' | 'releases';
type CalendarMode = 'week' | 'month';
type TimelineEventKind = 'session' | 'release' | 'quest';

type DraftSession = {
  myGameId: number | null;
  scheduledDate: string;
  scheduledTime: string;
  durationMinutes: number;
  notes: string;
};

type DayBucket = {
  key: string;
  label: string;
  sessions: GamingSession[];
};

type TimelineEvent = {
  id: string;
  kind: TimelineEventKind;
  title: string;
  subtitle: string;
  timeLabel: string;
  completed: boolean;
};

type PlanningCalendarDay = {
  key: string;
  date: Date;
  number: number;
  label: string;
  isToday: boolean;
  isCurrentMonth: boolean;
  events: TimelineEvent[];
};

@Component({
  selector: 'app-planing',
  templateUrl: './planing.page.html',
  styleUrls: ['./planing.page.scss'],
  imports: [
    CommonModule,
    FormsModule,
    IonBadge,
    IonButton,
    IonContent,
    IonIcon,
    IonProgressBar,
    IonRefresher,
    IonRefresherContent,
    IonSegment,
    IonSegmentButton,
    IonSelect,
    IonSelectOption,
    IonSpinner,
    ReleasePlanComponent,
  ],
})
export class PlaningPage implements OnInit {
  isLoading = true;
  mode: PlanMode = 'sessions';
  errorMessage = '';
  sessions: GamingSession[] = [];
  buckets: DayBucket[] = [];
  forecasts: GameForecast[] = [];
  quests: Quest[] = [];
  calendarMode: CalendarMode = 'week';
  calendarAnchor = this.startOfToday();
  calendarDays: PlanningCalendarDay[] = [];
  calendarTitle = '';
  libraryGames: { myGameId: number; gameName: string; playtime: number | null }[] = [];
  allGames: Game[] = [];

  draft: DraftSession = this.emptyDraft();

  constructor(
    private gameService: GameService,
    private sessionService: GamingSessionService,
    private questBoardService: QuestBoardService,
  ) {
    addIcons({
      addOutline,
      calendarClearOutline,
      checkmarkCircle,
      checkmarkCircleOutline,
      chevronBackOutline,
      chevronForwardOutline,
      flagOutline,
      gameControllerOutline,
      hourglassOutline,
      rocketOutline,
      timeOutline,
      trashOutline,
    });
  }

  async ngOnInit(): Promise<void> {
    await this.refresh();
  }

  async refresh(event?: CustomEvent): Promise<void> {
    this.isLoading = !event;
    this.errorMessage = '';

    try {
      const gamesResult = await firstValueFrom(this.gameService.getAll(this.createPlanFilter()));
      this.allGames = gamesResult.data ?? [];
      this.libraryGames = this.allGames
        .filter((game): game is Game & { myGames: { id: number } } => !!game.myGames)
        .map((game) => ({
          myGameId: game.myGames!.id,
          gameName: game.name,
          playtime: game.playtime ?? null,
        }))
        .sort((a, b) => a.gameName.localeCompare(b.gameName));

      const horizon = new Date();
      horizon.setHours(0, 0, 0, 0);
      const future = new Date(horizon);
      future.setDate(horizon.getDate() + 60);

      this.sessions = await firstValueFrom(
        this.sessionService.list({ from: horizon, to: future }),
      );
      this.buckets = this.groupByDay(this.sessions);
      try {
        this.quests = (await this.questBoardService.getBoard()).quests ?? [];
      } catch {
        this.quests = [];
      }
      this.buildCalendarDays();

      const linkedIds = Array.from(
        new Set(this.sessions.map((s) => s.myGameId).filter((id): id is number => id != null)),
      );
      this.forecasts = await Promise.all(
        linkedIds.map((id) => firstValueFrom(this.sessionService.forecast(id))),
      );
    } catch {
      this.errorMessage = 'Schedule could not be loaded.';
    } finally {
      this.isLoading = false;
      const target = event?.target as HTMLIonRefresherElement | undefined;
      target?.complete();
    }
  }

  async addSession(): Promise<void> {
    const scheduledAt = this.combineDateTime(this.draft.scheduledDate, this.draft.scheduledTime);

    if (!scheduledAt || this.draft.durationMinutes <= 0) {
      this.errorMessage = 'Pick a date, time, and a duration.';
      return;
    }

    try {
      await firstValueFrom(
        this.sessionService.create({
          myGameId: this.draft.myGameId,
          scheduledAt: scheduledAt.toISOString(),
          durationMinutes: this.draft.durationMinutes,
          completed: false,
          notes: this.draft.notes || null,
        }),
      );
      this.draft = this.emptyDraft();
      this.errorMessage = '';
      await this.refresh();
    } catch (error) {
      this.errorMessage = this.errorTextFrom(error) ?? 'Session could not be saved.';
    }
  }

  async toggleComplete(session: GamingSession): Promise<void> {
    try {
      await firstValueFrom(
        this.sessionService.update(session.id, {
          id: session.id,
          myGameId: session.myGameId,
          scheduledAt: session.scheduledAt,
          durationMinutes: session.durationMinutes,
          completed: !session.completed,
          notes: session.notes,
        }),
      );
      await this.refresh();
    } catch {
      this.errorMessage = 'Could not update session.';
    }
  }

  async removeSession(session: GamingSession): Promise<void> {
    try {
      await firstValueFrom(this.sessionService.remove(session.id));
      await this.refresh();
    } catch {
      this.errorMessage = 'Could not delete session.';
    }
  }

  formatDuration(minutes: number): string {
    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;
    if (hours === 0) return `${remainingMinutes}m`;
    return remainingMinutes === 0 ? `${hours}h` : `${hours}h ${remainingMinutes}m`;
  }

  formatTime(isoString: string): string {
    const date = new Date(isoString);
    return new Intl.DateTimeFormat('en', { hour: 'numeric', minute: '2-digit' }).format(date);
  }

  formatDate(isoString: string): string {
    return new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date(isoString));
  }

  formatHours(value: number): string {
    return `${Math.round(value * 10) / 10}h`;
  }

  shiftCalendar(direction: -1 | 1): void {
    const next = new Date(this.calendarAnchor);
    if (this.calendarMode === 'week') {
      next.setDate(next.getDate() + direction * 7);
    } else {
      next.setMonth(next.getMonth() + direction);
    }
    this.calendarAnchor = this.startOfDay(next);
    this.buildCalendarDays();
  }

  goToToday(): void {
    this.calendarAnchor = this.startOfToday();
    this.buildCalendarDays();
  }

  setCalendarMode(mode: CalendarMode): void {
    this.calendarMode = mode;
    this.buildCalendarDays();
  }

  trackByCalendarDay(_: number, day: PlanningCalendarDay): string {
    return day.key;
  }

  trackByTimelineEvent(_: number, event: TimelineEvent): string {
    return event.id;
  }

  eventIcon(kind: TimelineEventKind): string {
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

  trackBySession(_: number, session: GamingSession): number {
    return session.id;
  }

  trackByForecast(_: number, forecast: GameForecast): number {
    return forecast.myGameId;
  }

  trackByBucket(_: number, bucket: DayBucket): string {
    return bucket.key;
  }

  trackByLibrary(_: number, game: { myGameId: number }): number {
    return game.myGameId;
  }

  forecastSummary(forecast: GameForecast): string {
    if (forecast.remainingHours == null) {
      return 'No playtime estimate yet';
    }

    if (forecast.remainingHours <= 0) {
      return 'Already past the estimated playtime';
    }

    if (forecast.projectedCompletionDate) {
      const sessions = forecast.sessionsToCompletion ?? 0;
      return `${sessions} session${sessions === 1 ? '' : 's'} to finish · ETA ${this.formatDate(forecast.projectedCompletionDate)}`;
    }

    if (forecast.weeksAtCurrentPace != null) {
      return `Need ${this.formatHours(forecast.additionalHoursNeeded)} more · ~${forecast.weeksAtCurrentPace} weeks at ${this.formatHours(forecast.weeklyHours)}/week`;
    }

    return `Need ${this.formatHours(forecast.additionalHoursNeeded)} more — schedule sessions to project an ETA`;
  }

  forecastProgress(forecast: GameForecast): number {
    if (!forecast.playtimeEstimateHours || forecast.playtimeEstimateHours <= 0) {
      return 0;
    }
    return Math.min(1, forecast.playedHours / forecast.playtimeEstimateHours);
  }

  private emptyDraft(): DraftSession {
    return {
      myGameId: null,
      scheduledDate: this.todayIso(),
      scheduledTime: '20:00',
      durationMinutes: 90,
      notes: '',
    };
  }

  private todayIso(): string {
    return this.dateKey(this.startOfToday());
  }

  private combineDateTime(dateString: string, timeString: string): Date | null {
    if (!dateString || !timeString) return null;
    const [year, month, day] = dateString.split('-').map(Number);
    const [hours, minutes] = timeString.split(':').map(Number);
    if ([year, month, day, hours, minutes].some((v) => Number.isNaN(v))) return null;
    return new Date(year, month - 1, day, hours, minutes, 0, 0);
  }

  private groupByDay(sessions: GamingSession[]): DayBucket[] {
    const map = new Map<string, DayBucket>();

    for (const session of sessions) {
      const date = new Date(session.scheduledAt);
      const key = date.toISOString().slice(0, 10);
      let bucket = map.get(key);
      if (!bucket) {
        bucket = {
          key,
          label: new Intl.DateTimeFormat('en', { weekday: 'short', month: 'short', day: 'numeric' }).format(date),
          sessions: [],
        };
        map.set(key, bucket);
      }
      bucket.sessions.push(session);
    }

    return Array.from(map.values()).sort((a, b) => a.key.localeCompare(b.key));
  }

  private createPlanFilter(): MediaFilter {
    const filter = new MediaFilter();
    filter.Paging.Count = 500;
    return filter;
  }

  private buildCalendarDays(): void {
    const today = this.startOfToday();
    const start = this.calendarMode === 'week'
      ? this.startOfWeek(this.calendarAnchor)
      : this.startOfCalendarMonth(this.calendarAnchor);
    const dayCount = this.calendarMode === 'week' ? 7 : 42;
    const currentMonth = this.calendarAnchor.getMonth();

    this.calendarTitle = this.calendarMode === 'week'
      ? this.weekRangeLabel(start)
      : new Intl.DateTimeFormat('en', { month: 'long', year: 'numeric' }).format(this.calendarAnchor);

    this.calendarDays = Array.from({ length: dayCount }, (_, index) => {
      const date = new Date(start);
      date.setDate(start.getDate() + index);
      return {
        key: this.dateKey(date),
        date,
        number: date.getDate(),
        label: new Intl.DateTimeFormat('en', { weekday: 'short' }).format(date),
        isToday: this.sameDay(date, today),
        isCurrentMonth: date.getMonth() === currentMonth,
        events: this.eventsForDay(date),
      };
    });
  }

  private eventsForDay(date: Date): TimelineEvent[] {
    const events: TimelineEvent[] = [];

    for (const session of this.sessions) {
      const scheduledAt = new Date(session.scheduledAt);
      if (!this.sameDay(scheduledAt, date)) {
        continue;
      }
      events.push({
        id: `session-${session.id}`,
        kind: 'session',
        title: session.gameName ?? 'Gaming session',
        subtitle: session.notes || this.formatDuration(session.durationMinutes),
        timeLabel: this.formatTime(session.scheduledAt),
        completed: session.completed,
      });
    }

    for (const game of this.allGames) {
      const releaseDate = this.validDate(game.releaseDate);
      if (!releaseDate || !this.sameDay(releaseDate, date)) {
        continue;
      }
      events.push({
        id: `release-${game.id}`,
        kind: 'release',
        title: game.name,
        subtitle: 'Release',
        timeLabel: 'Release',
        completed: releaseDate < this.startOfToday(),
      });
    }

    for (const quest of this.quests) {
      const dueDate = this.validDate(quest.dueDate);
      if (!dueDate || !this.sameDay(dueDate, date)) {
        continue;
      }
      events.push({
        id: `quest-${quest.id}`,
        kind: 'quest',
        title: quest.title,
        subtitle: quest.gameName ?? quest.skillName ?? this.questPriorityLabel(quest.priority),
        timeLabel: quest.completed ? 'Done' : this.questPriorityLabel(quest.priority),
        completed: quest.completed,
      });
    }

    return events.sort((a, b) => this.eventRank(a) - this.eventRank(b) || a.title.localeCompare(b.title));
  }

  private eventRank(event: TimelineEvent): number {
    if (event.kind === 'session') return 0;
    if (event.kind === 'quest') return 1;
    return 2;
  }

  private questPriorityLabel(priority: Quest['priority']): string {
    return priority === 'high' ? 'High priority' : priority === 'low' ? 'Low priority' : 'Medium priority';
  }

  private weekRangeLabel(start: Date): string {
    const end = new Date(start);
    end.setDate(start.getDate() + 6);
    const formatter = new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric' });
    return `${formatter.format(start)} - ${formatter.format(end)}`;
  }

  private startOfCalendarMonth(date: Date): Date {
    const monthStart = new Date(date.getFullYear(), date.getMonth(), 1);
    const start = new Date(monthStart);
    start.setDate(monthStart.getDate() - ((monthStart.getDay() + 6) % 7));
    return this.startOfDay(start);
  }

  private startOfWeek(date: Date): Date {
    const start = this.startOfDay(date);
    start.setDate(start.getDate() - ((start.getDay() + 6) % 7));
    return start;
  }

  private startOfToday(): Date {
    return this.startOfDay(new Date());
  }

  private startOfDay(value: Date): Date {
    return new Date(value.getFullYear(), value.getMonth(), value.getDate());
  }

  private sameDay(a: Date, b: Date): boolean {
    return a.getFullYear() === b.getFullYear()
      && a.getMonth() === b.getMonth()
      && a.getDate() === b.getDate();
  }

  private dateKey(value: Date): string {
    return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}`;
  }

  private validDate(value: Date | string | null | undefined): Date | null {
    if (!value) {
      return null;
    }
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? null : date;
  }

  private errorTextFrom(error: unknown): string | null {
    const payload = (error as { error?: unknown })?.error;
    if (typeof payload === 'string') return payload;
    if (payload && typeof payload === 'object' && 'message' in payload) {
      return String((payload as { message: unknown }).message);
    }
    if (payload && typeof payload === 'object' && 'errorMessage' in payload) {
      const errorMessage = (payload as { errorMessage: unknown }).errorMessage;
      return Array.isArray(errorMessage) ? errorMessage.join(' ') : String(errorMessage);
    }
    return null;
  }
}
