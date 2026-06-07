
import { Component, OnInit, inject } from '@angular/core';
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
import { firstValueFrom } from 'rxjs';

import { Game } from '../../games/models/games.model';
import { GameService } from '../../games/services/game.service';
import { ReleasePlanComponent } from '../../release-calendar/components/release-plan.component';
import { EmptyStateComponent } from 'src/app/shared/components/empty-state/empty-state.component';
import { GameForecast, GamingSession, GamingSessionService } from '../services/gaming-session.service';
import { MediaFilter } from 'src/app/core/entities/mediaFilter';
import { DayBucket, PlanningCalendarService } from '../services/planning-calendar.service';

type PlanMode = 'sessions' | 'releases';
type SessionRange = 'upcoming' | 'past' | 'all';

type DraftSession = {
  myGameId: number | null;
  scheduledDate: string;
  scheduledTime: string;
  durationMinutes: number;
  notes: string;
};

@Component({
  selector: 'app-planning',
  templateUrl: './planning.page.html',
  styleUrls: ['./planning.page.scss'],
  imports: [
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
    EmptyStateComponent,
],
})
export class PlanningPage implements OnInit {
  private gameService = inject(GameService);
  private sessionService = inject(GamingSessionService);
  private planningCalendar = inject(PlanningCalendarService);

  isLoading = true;
  mode: PlanMode = 'sessions';
  sessionRange: SessionRange = 'upcoming';
  errorMessage = '';
  sessions: GamingSession[] = [];
  buckets: DayBucket[] = [];
  forecasts: GameForecast[] = [];
  libraryGames: { myGameId: number; gameName: string; playtime: number | null }[] = [];
  allGames: Game[] = [];

  draft: DraftSession;

  constructor() {
    this.draft = this.emptyDraft();

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

      this.sessions = await firstValueFrom(
        this.sessionService.list(this.sessionWindow()),
      );
      this.buckets = this.planningCalendar.groupSessionsByDay(this.sessions);
      if (this.sessionRange !== 'upcoming') {
        this.buckets = this.buckets
          .map((bucket) => ({ ...bucket, sessions: [...bucket.sessions].reverse() }))
          .reverse();
      }

      const linkedIds = Array.from(
        new Set(this.sessions
          .filter((session) => this.sessionRange !== 'past')
          .map((s) => s.myGameId)
          .filter((id): id is number => id != null)),
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

  async setSessionRange(range: SessionRange): Promise<void> {
    if (this.sessionRange === range) {
      return;
    }

    this.sessionRange = range;
    await this.refresh();
  }

  sessionListTitle(): string {
    if (this.sessionRange === 'past') return 'Past sessions';
    if (this.sessionRange === 'all') return 'All sessions';
    return 'Upcoming sessions';
  }

  sessionEmptyTitle(): string {
    if (this.sessionRange === 'past') return 'No past sessions';
    if (this.sessionRange === 'all') return 'No sessions';
    return 'No sessions planned';
  }

  sessionEmptyMessage(): string {
    if (this.sessionRange === 'past') return 'Older sessions will show here once you have sessions before today.';
    if (this.sessionRange === 'all') return 'Add a session above to start building your schedule.';
    return 'Add a session above to start projecting completion.';
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
    return this.planningCalendar.dateKey(this.planningCalendar.startOfToday());
  }

  private combineDateTime(dateString: string, timeString: string): Date | null {
    if (!dateString || !timeString) return null;
    const [year, month, day] = dateString.split('-').map(Number);
    const [hours, minutes] = timeString.split(':').map(Number);
    if ([year, month, day, hours, minutes].some((v) => Number.isNaN(v))) return null;
    return new Date(year, month - 1, day, hours, minutes, 0, 0);
  }

  private createPlanFilter(): MediaFilter {
    const filter = new MediaFilter();
    filter.Paging.Count = 500;
    return filter;
  }

  private sessionWindow(): { from?: Date; to?: Date } {
    const today = this.planningCalendar.startOfToday();
    if (this.sessionRange === 'past') {
      return { to: today };
    }

    if (this.sessionRange === 'all') {
      return {};
    }

    const future = new Date(today);
    future.setDate(today.getDate() + 60);
    return { from: today, to: future };
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
