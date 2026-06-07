import { Component, OnChanges, SimpleChanges, inject, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  IonButton,
  IonIcon,
  IonSegment,
  IonSegmentButton,
  IonSpinner,
} from '@ionic/angular/standalone';
import { firstValueFrom } from 'rxjs';

import { EmptyStateComponent } from 'src/app/shared/components/empty-state/empty-state.component';
import { DayBucket, PlanningCalendarService } from 'src/app/features/planning/services/planning-calendar.service';
import { GamingSession, GamingSessionService } from 'src/app/features/planning/services/gaming-session.service';

type SessionRange = 'upcoming' | 'past' | 'all';

type DraftSession = {
  scheduledDate: string;
  scheduledTime: string;
  durationMinutes: number;
  notes: string;
};

@Component({
  selector: 'app-game-sessions',
  templateUrl: './game-sessions.component.html',
  styleUrls: ['../../pages/my-game-details.page.scss', './game-sessions.component.scss'],
  imports: [
    FormsModule,
    IonButton,
    IonIcon,
    IonSegment,
    IonSegmentButton,
    IonSpinner,
    EmptyStateComponent,
  ],
})
export class GameSessionsComponent implements OnChanges {
  private readonly sessionService = inject(GamingSessionService);
  private readonly planningCalendar = inject(PlanningCalendarService);

  readonly myGameId = input<number | null>(null);
  readonly isInLibrary = input<boolean>(false);
  readonly gameName = input<string | null>(null);
  readonly sessionsChanged = output<void>();

  isLoading = false;
  errorMessage = '';
  sessionRange: SessionRange = 'upcoming';
  sessions: GamingSession[] = [];
  buckets: DayBucket[] = [];
  draft: DraftSession = this.emptyDraft();

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['myGameId']) {
      this.draft = this.emptyDraft();
      void this.refresh();
    }
  }

  async refresh(): Promise<void> {
    const id = this.myGameId();
    if (id == null) {
      this.sessions = [];
      this.buckets = [];
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    try {
      this.sessions = await firstValueFrom(
        this.sessionService.list({
          myGameId: id,
          ...this.sessionWindow(),
        }),
      );
      this.rebuildBuckets();
    } catch {
      this.errorMessage = 'Sessions could not be loaded.';
      this.sessions = [];
      this.buckets = [];
    } finally {
      this.isLoading = false;
    }
  }

  async addSession(): Promise<void> {
    const id = this.myGameId();
    const scheduledAt = this.combineDateTime(this.draft.scheduledDate, this.draft.scheduledTime);

    if (id == null) {
      this.errorMessage = 'Add this game to your library before planning sessions.';
      return;
    }

    if (!scheduledAt || this.draft.durationMinutes <= 0) {
      this.errorMessage = 'Pick a date, time, and a duration.';
      return;
    }

    try {
      await firstValueFrom(
        this.sessionService.create({
          myGameId: id,
          scheduledAt: scheduledAt.toISOString(),
          durationMinutes: this.draft.durationMinutes,
          completed: false,
          notes: this.draft.notes.trim() || null,
        }),
      );
      this.draft = this.emptyDraft();
      this.errorMessage = '';
      await this.refresh();
      this.sessionsChanged.emit();
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
          completedAt: session.completedAt,
          notes: session.notes,
        }),
      );
      await this.refresh();
      this.sessionsChanged.emit();
    } catch {
      this.errorMessage = 'Could not update session.';
    }
  }

  async removeSession(session: GamingSession): Promise<void> {
    try {
      await firstValueFrom(this.sessionService.remove(session.id));
      await this.refresh();
      this.sessionsChanged.emit();
    } catch {
      this.errorMessage = 'Could not delete session.';
    }
  }

  async setSessionRange(value: unknown): Promise<void> {
    const range = this.toSessionRange(value);
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
    if (!this.isInLibrary()) return 'Sessions are locked';
    if (this.sessionRange === 'past') return 'No past sessions';
    if (this.sessionRange === 'all') return 'No sessions';
    return 'No sessions planned';
  }

  sessionEmptyMessage(): string {
    if (!this.isInLibrary()) return 'Add this game to your library before planning sessions.';
    if (this.sessionRange === 'past') return 'Older sessions for this game will show here.';
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

  sessionTitle(session: GamingSession): string {
    return session.gameName ?? this.gameName() ?? 'Gaming session';
  }

  trackBySession(_: number, session: GamingSession): number {
    return session.id;
  }

  trackByBucket(_: number, bucket: DayBucket): string {
    return bucket.key;
  }

  private rebuildBuckets(): void {
    this.buckets = this.planningCalendar.groupSessionsByDay(this.sessions);
    if (this.sessionRange !== 'upcoming') {
      this.buckets = this.buckets
        .map((bucket) => ({ ...bucket, sessions: [...bucket.sessions].reverse() }))
        .reverse();
    }
  }

  private emptyDraft(): DraftSession {
    return {
      scheduledDate: this.planningCalendar.dateKey(this.planningCalendar.startOfToday()),
      scheduledTime: '20:00',
      durationMinutes: 90,
      notes: '',
    };
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

  private combineDateTime(dateString: string, timeString: string): Date | null {
    if (!dateString || !timeString) return null;
    const [year, month, day] = dateString.split('-').map(Number);
    const [hours, minutes] = timeString.split(':').map(Number);
    if ([year, month, day, hours, minutes].some((value) => Number.isNaN(value))) return null;
    return new Date(year, month - 1, day, hours, minutes, 0, 0);
  }

  private toSessionRange(value: unknown): SessionRange {
    return value === 'past' || value === 'all' ? value : 'upcoming';
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
