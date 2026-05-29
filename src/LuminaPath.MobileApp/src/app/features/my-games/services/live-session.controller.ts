import { firstValueFrom } from 'rxjs';
import { extractErrorMessage } from 'src/app/shared/utils/extract-error';
import { LiveSessionTrackerService } from 'src/app/shared/services/live-session-tracker.service';
import { GamingSessionService } from 'src/app/features/planning/services/gaming-session.service';
import { MyGameService } from 'src/app/features/my-games/services/my-game.service';
import { GameLibraryEntry } from 'src/app/features/my-games/models/my-game.model';
import {
  addTrackedHours,
  buildLibraryUpdate,
  nextLiveSessionStatus,
} from '../pages/my-game-details.helpers';

/** Per-page wiring supplied by the host my-game-details page. */
export interface LiveSessionConfig {
  /** Catalog id of the game being viewed (null while loading). */
  gameId: () => number | null;
  /** The user's library-entry id for this game (null when not in library). */
  myGameId: () => number | null;
  /** Display name of the game (for the persisted snapshot). */
  gameName: () => string | null;
  /** The current library entry, used to default the status/hours update. */
  libraryEntry: () => GameLibraryEntry | null;
  /** Reload the host page after a session is logged. */
  reload: (gameId: number) => Promise<void>;
}

/**
 * Drives the "live session" timer card on the my-game-details page: starting,
 * stopping (logging a GamingSession + bumping tracked hours), discarding, and
 * restoring a session persisted by {@link LiveSessionTrackerService}. Owns the
 * second-by-second timer so the page component stays focused on the game.
 */
export class LiveSessionController {
  startedAt: string | null = null;
  notes = '';
  elapsedSeconds = 0;
  isSaving = false;
  message = '';
  messageTone: 'success' | 'error' | 'neutral' = 'neutral';

  private timerId: number | null = null;
  private sessionGameId: number | null = null;

  constructor(
    private readonly tracker: LiveSessionTrackerService,
    private readonly sessionService: GamingSessionService,
    private readonly myGameService: MyGameService,
    private readonly config: LiveSessionConfig,
  ) {}

  get active(): boolean {
    return this.startedAt !== null;
  }

  get durationLabel(): string {
    return formatLiveDuration(this.elapsedSeconds);
  }

  get startedLabel(): string {
    if (!this.startedAt) {
      return 'Ready';
    }

    const started = new Date(this.startedAt);
    if (Number.isNaN(started.getTime())) {
      return 'Running';
    }

    return `Started ${started.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
  }

  start(now = new Date()): void {
    const gameId = this.config.gameId();
    const myGameId = this.config.myGameId();
    if (!gameId || myGameId == null || this.active || this.isSaving) {
      return;
    }

    this.sessionGameId = gameId;
    this.startedAt = now.toISOString();
    this.elapsedSeconds = 0;
    this.notes = '';
    this.message = '';
    this.messageTone = 'neutral';
    this.tracker.start({
      gameId,
      myGameId,
      gameName: this.config.gameName(),
      startedAt: this.startedAt,
      notes: '',
    });
    this.startTimer();
  }

  async stop(now = new Date()): Promise<void> {
    const gameId = this.config.gameId() ?? this.sessionGameId;
    const myGameId = this.config.myGameId();
    const startedAt = this.startedAt;
    if (!gameId || myGameId == null || !startedAt || this.isSaving) {
      return;
    }

    const started = new Date(startedAt);
    if (Number.isNaN(started.getTime())) {
      this.discard();
      return;
    }

    const durationMinutes = Math.max(1, Math.round((now.getTime() - started.getTime()) / 60000));
    const note = this.notes.trim() || null;

    this.isSaving = true;
    this.message = '';
    this.messageTone = 'neutral';

    try {
      await firstValueFrom(this.sessionService.create({
        myGameId,
        scheduledAt: started.toISOString(),
        durationMinutes,
        completed: true,
        completedAt: now.toISOString(),
        notes: note,
      }));

      let playtimeUpdated = true;
      try {
        const entry = this.config.libraryEntry();
        await firstValueFrom(
          this.myGameService.updateLibraryEntry(myGameId, gameId, buildLibraryUpdate(entry, {
            status: nextLiveSessionStatus(entry),
            timeSpend: addTrackedHours(entry, durationMinutes),
          })),
        );
      } catch {
        playtimeUpdated = false;
      }

      this.tracker.clear(gameId);
      this.resetState();
      await this.config.reload(gameId);
      this.message = playtimeUpdated
        ? `Logged ${formatLiveDuration(durationMinutes * 60)}.`
        : 'Session logged. Playtime could not be updated.';
      this.messageTone = playtimeUpdated ? 'success' : 'error';
    } catch (error) {
      this.message = extractErrorMessage(error, 'Session could not be logged.');
      this.messageTone = 'error';
    } finally {
      this.isSaving = false;
    }
  }

  discard(): void {
    this.tracker.clear(this.config.gameId() ?? this.sessionGameId);
    this.resetState();
    this.message = 'Session discarded.';
    this.messageTone = 'neutral';
  }

  persist(): void {
    const gameId = this.config.gameId() ?? this.sessionGameId;
    const myGameId = this.config.myGameId();
    if (!gameId || myGameId == null || !this.startedAt) {
      return;
    }

    this.tracker.start({
      gameId,
      myGameId,
      gameName: this.config.gameName(),
      startedAt: this.startedAt,
      notes: this.notes,
    });
  }

  restore(gameId: number): void {
    this.clearTimer();
    this.resetState();
    this.message = '';
    this.messageTone = 'neutral';

    const snapshot = this.tracker.get(gameId);
    if (!snapshot) {
      return;
    }

    if (snapshot.myGameId !== this.config.myGameId()) {
      this.tracker.clear(gameId);
      return;
    }

    this.sessionGameId = gameId;
    this.startedAt = snapshot.startedAt;
    this.notes = snapshot.notes;
    this.updateElapsed();
    this.startTimer();
  }

  /** Clears the running timer. Call from the host page's ngOnDestroy. */
  destroy(): void {
    this.clearTimer();
  }

  private startTimer(): void {
    this.clearTimer();
    this.updateElapsed();
    this.timerId = window.setInterval(() => this.updateElapsed(), 1000);
  }

  private updateElapsed(): void {
    if (!this.startedAt) {
      this.elapsedSeconds = 0;
      return;
    }

    const started = new Date(this.startedAt);
    this.elapsedSeconds = Number.isNaN(started.getTime())
      ? 0
      : Math.max(0, Math.floor((Date.now() - started.getTime()) / 1000));
  }

  private clearTimer(): void {
    if (this.timerId !== null) {
      window.clearInterval(this.timerId);
      this.timerId = null;
    }
  }

  private resetState(): void {
    this.clearTimer();
    this.startedAt = null;
    this.sessionGameId = null;
    this.notes = '';
    this.elapsedSeconds = 0;
  }
}

function formatLiveDuration(totalSeconds: number): string {
  const seconds = Math.max(0, Math.floor(totalSeconds));
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const remainingSeconds = seconds % 60;

  if (hours > 0) {
    return `${hours}:${String(minutes).padStart(2, '0')}:${String(remainingSeconds).padStart(2, '0')}`;
  }

  return `${minutes}:${String(remainingSeconds).padStart(2, '0')}`;
}
