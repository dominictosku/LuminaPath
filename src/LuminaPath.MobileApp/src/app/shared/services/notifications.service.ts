import { Injectable } from '@angular/core';
import { Observable, catchError, combineLatest, map, of } from 'rxjs';
import { Game } from 'src/app/features/games/models/games.model';
import { GameService } from 'src/app/features/games/services/game.service';
import { GamingSession, GamingSessionService } from 'src/app/features/planing/services/gaming-session.service';

export type NotificationKind = 'release' | 'session' | 'released';

export interface NotificationItem {
  id: string;
  kind: NotificationKind;
  icon: string;
  title: string;
  body: string;
  link: string;
  sortAt: number;
}

const DAY_MS = 24 * 60 * 60 * 1000;
const RELEASE_WINDOW_DAYS = 7;
const POST_RELEASE_WINDOW_DAYS = 3;
const MAX_ITEMS = 6;

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  constructor(
    private gameService: GameService,
    private sessionService: GamingSessionService,
  ) {}

  load(): Observable<NotificationItem[]> {
    const today = startOfDay(new Date());
    const horizon = new Date(today);
    horizon.setDate(today.getDate() + RELEASE_WINDOW_DAYS + 1);

    const games$ = this.gameService.getAll().pipe(
      map((res) => res.data ?? []),
      catchError(() => of([] as Game[])),
    );
    const sessions$ = this.sessionService.list({ from: today, to: horizon }).pipe(
      catchError(() => of([] as GamingSession[])),
    );

    return combineLatest([games$, sessions$]).pipe(
      map(([games, sessions]) => this.build(games, sessions, today)),
    );
  }

  private build(games: Game[], sessions: GamingSession[], today: Date): NotificationItem[] {
    const items: NotificationItem[] = [
      ...this.releaseItems(games, today),
      ...this.recentlyReleasedItems(games, today),
      ...this.todaySessionItems(sessions, today),
    ];

    return items
      .sort((a, b) => a.sortAt - b.sortAt)
      .slice(0, MAX_ITEMS);
  }

  private releaseItems(games: Game[], today: Date): NotificationItem[] {
    const now = today.getTime();
    const horizon = now + RELEASE_WINDOW_DAYS * DAY_MS;
    const items: NotificationItem[] = [];

    for (const game of games) {
      if (!game?.myGames || !game.releaseDate) continue;

      const releaseAt = new Date(game.releaseDate).getTime();
      if (Number.isNaN(releaseAt) || releaseAt < now || releaseAt > horizon) continue;

      const days = Math.round((releaseAt - now) / DAY_MS);
      const title =
        days <= 0
          ? `${game.name} is out today`
          : `${game.name} drops in ${days} day${days === 1 ? '' : 's'}`;

      items.push({
        id: `release-${game.id}`,
        kind: 'release',
        icon: 'rocket-outline',
        title,
        body: 'Plan a launch session before it lands.',
        link: '/planing',
        sortAt: releaseAt,
      });
    }

    return items;
  }

  private recentlyReleasedItems(games: Game[], today: Date): NotificationItem[] {
    const now = today.getTime();
    const earliest = now - POST_RELEASE_WINDOW_DAYS * DAY_MS;
    const items: NotificationItem[] = [];

    for (const game of games) {
      if (!game?.myGames || !game.releaseDate) continue;

      const releaseAt = new Date(game.releaseDate).getTime();
      if (Number.isNaN(releaseAt) || releaseAt >= now || releaseAt < earliest) continue;

      const status = Number(game.myGames.status ?? -1);
      const played = Number(game.myGames.timeSpend ?? 0) + Number(game.myGames.myGameInfo?.trackedHours ?? 0);
      if (status >= 2 || played > 0) continue;

      items.push({
        id: `released-${game.id}`,
        kind: 'released',
        icon: 'sparkles-outline',
        title: `${game.name} just released`,
        body: 'It is in your library — start playing?',
        link: `/library/games/${game.id}`,
        sortAt: releaseAt,
      });
    }

    return items;
  }

  private todaySessionItems(sessions: GamingSession[], today: Date): NotificationItem[] {
    const startOfToday = today.getTime();
    const startOfTomorrow = startOfToday + DAY_MS;
    const items: NotificationItem[] = [];

    for (const session of sessions) {
      if (session.completed) continue;

      const at = new Date(session.scheduledAt).getTime();
      if (Number.isNaN(at) || at < startOfToday || at >= startOfTomorrow) continue;

      const time = new Intl.DateTimeFormat('en', { hour: 'numeric', minute: '2-digit' }).format(new Date(at));
      const title = session.gameName
        ? `${session.gameName} session at ${time}`
        : `Gaming session at ${time}`;

      items.push({
        id: `session-${session.id}`,
        kind: 'session',
        icon: 'time-outline',
        title,
        body: `${session.durationMinutes} min planned today.`,
        link: '/planing',
        sortAt: at,
      });
    }

    return items;
  }
}

function startOfDay(date: Date): Date {
  const copy = new Date(date);
  copy.setHours(0, 0, 0, 0);
  return copy;
}
