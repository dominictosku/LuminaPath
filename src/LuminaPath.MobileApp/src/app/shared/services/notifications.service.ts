import { Injectable, inject } from '@angular/core';
import { Observable, catchError, combineLatest, map, of } from 'rxjs';
import { startOfDay } from 'src/app/shared/utils/date-helpers';
import { Game } from 'src/app/features/games/models/games.model';
import { GameService } from 'src/app/features/games/services/game.service';
import { GamingSession, GamingSessionService } from 'src/app/features/planning/services/gaming-session.service';

export type NotificationKind = 'release' | 'session' | 'released' | 'friend-request' | 'direct-message' | 'quest';
export type NotificationCategory = 'releases' | 'sessions' | 'friend-requests' | 'direct-messages' | 'quests';

export interface NotificationItem {
  id: string;
  kind: NotificationKind;
  category: NotificationCategory;
  icon: string;
  title: string;
  body: string;
  link: string;
  sortAt: number;
  active?: boolean;
  firstSeenAt?: string;
  lastSeenAt?: string;
}

export interface NotificationCategoryPreference {
  id: NotificationCategory;
  label: string;
  description: string;
  icon: string;
  enabled: boolean;
}

const DAY_MS = 24 * 60 * 60 * 1000;
const RELEASE_WINDOW_DAYS = 7;
const POST_RELEASE_WINDOW_DAYS = 3;
const MAX_ITEMS = 6;
const MAX_HISTORY_ITEMS = 60;
const PREFS_KEY = 'luminapath.notification-preferences.v1';
const HISTORY_KEY = 'luminapath.notification-history.v1';

const CATEGORY_DEFINITIONS: Omit<NotificationCategoryPreference, 'enabled'>[] = [
  {
    id: 'releases',
    label: 'Releases',
    description: 'Upcoming releases and recently launched games in your library.',
    icon: 'rocket-outline',
  },
  {
    id: 'sessions',
    label: 'Sessions',
    description: 'Gaming sessions scheduled for today.',
    icon: 'calendar-clear-outline',
  },
  {
    id: 'friend-requests',
    label: 'Friend requests',
    description: 'Incoming social requests once server-side events are available.',
    icon: 'person-add-outline',
  },
  {
    id: 'direct-messages',
    label: 'Direct messages',
    description: 'Unread DMs once message notification events are available.',
    icon: 'mail-unread-outline',
  },
  {
    id: 'quests',
    label: 'Quest reminders',
    description: 'Due quest reminders once quest notification events are available.',
    icon: 'flag-outline',
  },
];

type NotificationPreferenceState = Partial<Record<NotificationCategory, boolean>>;

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private gameService = inject(GameService);
  private sessionService = inject(GamingSessionService);

  load(): Observable<NotificationItem[]> {
    return this.loadActive(MAX_ITEMS);
  }

  loadActive(limit?: number): Observable<NotificationItem[]> {
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
      map(([games, sessions]) => {
        const activeItems = this.filterEnabled(this.build(games, sessions, today));
        this.upsertHistory(activeItems);
        return activeItems
          .sort((a, b) => a.sortAt - b.sortAt)
          .slice(0, limit ?? activeItems.length);
      }),
    );
  }

  loadFeed(): Observable<NotificationItem[]> {
    return this.loadActive().pipe(
      map((activeItems) => {
        const activeIds = new Set(activeItems.map((item) => item.id));
        return this.filterEnabled(this.readHistory())
          .map((item) => ({ ...item, active: activeIds.has(item.id) }))
          .sort((a, b) => (b.lastSeenAt ?? '').localeCompare(a.lastSeenAt ?? '') || b.sortAt - a.sortAt);
      }),
    );
  }

  preferences(): NotificationCategoryPreference[] {
    const state = this.readPreferenceState();
    return CATEGORY_DEFINITIONS.map((category) => ({
      ...category,
      enabled: state[category.id] ?? true,
    }));
  }

  setCategoryEnabled(category: NotificationCategory, enabled: boolean): NotificationCategoryPreference[] {
    const state = this.readPreferenceState();
    state[category] = enabled;
    this.writePreferenceState(state);
    return this.preferences();
  }

  private build(games: Game[], sessions: GamingSession[], today: Date): NotificationItem[] {
    const items: NotificationItem[] = [
      ...this.releaseItems(games, today),
      ...this.recentlyReleasedItems(games, today),
      ...this.todaySessionItems(sessions, today),
    ];

    return items.sort((a, b) => a.sortAt - b.sortAt);
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
        category: 'releases',
        icon: 'rocket-outline',
        title,
        body: 'Plan a launch session before it lands.',
        link: '/planning',
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
        category: 'releases',
        icon: 'sparkles-outline',
        title: `${game.name} just released`,
        body: 'It is in your library - start playing?',
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
        category: 'sessions',
        icon: 'time-outline',
        title,
        body: `${session.durationMinutes} min planned today.`,
        link: '/planning',
        sortAt: at,
      });
    }

    return items;
  }

  private filterEnabled(items: NotificationItem[]): NotificationItem[] {
    const enabled = new Set(this.preferences().filter((pref) => pref.enabled).map((pref) => pref.id));
    return items.filter((item) => enabled.has(item.category));
  }

  private upsertHistory(activeItems: NotificationItem[]): NotificationItem[] {
    const now = new Date().toISOString();
    const currentIds = new Set(activeItems.map((item) => item.id));
    const byId = new Map<string, NotificationItem>();

    for (const item of this.readHistory()) {
      byId.set(item.id, { ...item, active: currentIds.has(item.id) });
    }

    for (const item of activeItems) {
      const previous = byId.get(item.id);
      byId.set(item.id, {
        ...item,
        active: true,
        firstSeenAt: previous?.firstSeenAt ?? now,
        lastSeenAt: now,
      });
    }

    const next = [...byId.values()]
      .sort((a, b) => (b.lastSeenAt ?? '').localeCompare(a.lastSeenAt ?? '') || b.sortAt - a.sortAt)
      .slice(0, MAX_HISTORY_ITEMS);
    this.writeHistory(next);
    return next;
  }

  private readPreferenceState(): NotificationPreferenceState {
    try {
      const raw = localStorage.getItem(PREFS_KEY);
      if (!raw) return {};
      const parsed = JSON.parse(raw);
      if (!parsed || typeof parsed !== 'object') return {};
      const state: NotificationPreferenceState = {};
      for (const category of CATEGORY_DEFINITIONS) {
        const value = (parsed as Record<string, unknown>)[category.id];
        if (typeof value === 'boolean') {
          state[category.id] = value;
        }
      }
      return state;
    } catch {
      return {};
    }
  }

  private writePreferenceState(state: NotificationPreferenceState): void {
    try {
      localStorage.setItem(PREFS_KEY, JSON.stringify(state));
    } catch {
      // ignore private mode / quota errors
    }
  }

  private readHistory(): NotificationItem[] {
    try {
      const raw = localStorage.getItem(HISTORY_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw);
      if (!Array.isArray(parsed)) return [];
      const categories = new Set(CATEGORY_DEFINITIONS.map((category) => category.id));
      return parsed
        .filter((item): item is NotificationItem => {
          if (!item || typeof item !== 'object') return false;
          const candidate = item as Partial<NotificationItem>;
          return typeof candidate.id === 'string'
            && typeof candidate.kind === 'string'
            && typeof candidate.category === 'string'
            && categories.has(candidate.category)
            && typeof candidate.icon === 'string'
            && typeof candidate.title === 'string'
            && typeof candidate.body === 'string'
            && typeof candidate.link === 'string'
            && typeof candidate.sortAt === 'number';
        })
        .map((item) => ({ ...item, active: !!item.active }));
    } catch {
      return [];
    }
  }

  private writeHistory(items: NotificationItem[]): void {
    try {
      localStorage.setItem(HISTORY_KEY, JSON.stringify(items));
    } catch {
      // ignore private mode / quota errors
    }
  }
}

