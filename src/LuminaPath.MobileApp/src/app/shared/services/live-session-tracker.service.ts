import { Injectable, signal } from '@angular/core';

export type LiveGameSession = {
  gameId: number;
  myGameId: number;
  gameName: string | null;
  startedAt: string;
  notes: string;
};

const STORAGE_PREFIX = 'luminapath.liveSession.games.';

@Injectable({
  providedIn: 'root',
})
export class LiveSessionTrackerService {
  private readonly sessionsSignal = signal<readonly LiveGameSession[]>([]);
  readonly sessions = this.sessionsSignal.asReadonly();

  constructor() {
    this.refresh();

    if (typeof window !== 'undefined') {
      window.addEventListener('storage', (event) => {
        if (!event.key || event.key.startsWith(STORAGE_PREFIX)) {
          this.refresh();
        }
      });
    }
  }

  refresh(): void {
    this.sessionsSignal.set(this.readSessions());
  }

  get(gameId: number): LiveGameSession | null {
    return this.readSession(this.storageKey(gameId));
  }

  start(session: LiveGameSession): void {
    this.writeSession(session);
  }

  updateNotes(gameId: number, notes: string): void {
    const session = this.get(gameId);
    if (!session) {
      return;
    }

    this.writeSession({ ...session, notes });
  }

  clear(gameId: number | null | undefined): void {
    if (!gameId) {
      return;
    }

    try {
      globalThis.localStorage?.removeItem(this.storageKey(gameId));
    } catch {
      // Nothing to clear when storage is unavailable.
    }

    this.refresh();
  }

  durationSeconds(session: Pick<LiveGameSession, 'startedAt'>, nowMs = Date.now()): number {
    const started = new Date(session.startedAt);
    return Number.isNaN(started.getTime())
      ? 0
      : Math.max(0, Math.floor((nowMs - started.getTime()) / 1000));
  }

  private writeSession(session: LiveGameSession): void {
    try {
      globalThis.localStorage?.setItem(this.storageKey(session.gameId), JSON.stringify(session));
    } catch {
      // Live tracking still works for the current page when storage is blocked.
    }

    this.refresh();
  }

  private readSessions(): LiveGameSession[] {
    const storage = globalThis.localStorage;
    if (!storage) {
      return [];
    }

    const sessions: LiveGameSession[] = [];
    try {
      for (let index = 0; index < storage.length; index++) {
        const key = storage.key(index);
        if (!key?.startsWith(STORAGE_PREFIX)) {
          continue;
        }

        const session = this.readSession(key);
        if (session) {
          sessions.push(session);
        }
      }
    } catch {
      return [];
    }

    return sessions.sort((a, b) => a.startedAt.localeCompare(b.startedAt));
  }

  private readSession(key: string): LiveGameSession | null {
    let raw: string | null = null;
    try {
      raw = globalThis.localStorage?.getItem(key) ?? null;
    } catch {
      raw = null;
    }
    if (!raw) {
      return null;
    }

    const gameId = Number(key.slice(STORAGE_PREFIX.length));
    if (!Number.isInteger(gameId) || gameId <= 0) {
      return null;
    }

    try {
      const snapshot = JSON.parse(raw) as Partial<LiveGameSession>;
      const myGameId = Number(snapshot.myGameId);
      const startedAt = typeof snapshot.startedAt === 'string' ? snapshot.startedAt : '';
      const started = new Date(startedAt);
      if (!Number.isInteger(myGameId) || myGameId <= 0 || Number.isNaN(started.getTime())) {
        this.clear(gameId);
        return null;
      }

      return {
        gameId,
        myGameId,
        startedAt: started.toISOString(),
        gameName: typeof snapshot.gameName === 'string' && snapshot.gameName.trim().length
          ? snapshot.gameName
          : null,
        notes: typeof snapshot.notes === 'string' ? snapshot.notes : '',
      };
    } catch {
      this.clear(gameId);
      return null;
    }
  }

  private storageKey(gameId: number): string {
    return `${STORAGE_PREFIX}${gameId}`;
  }
}
