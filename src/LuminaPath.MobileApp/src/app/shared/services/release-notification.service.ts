import { Injectable } from '@angular/core';
import { Capacitor } from '@capacitor/core';
import { LocalNotifications, ScheduleOptions } from '@capacitor/local-notifications';
import { Game } from 'src/app/features/games/models/games.model';

type ReminderKind = 'week' | 'day';

const STORAGE_KEY = 'luminapath.releaseNotificationIds';
const CHANNEL_ID = 'lumina-game-releases';
const REMINDER_OFFSETS: Record<ReminderKind, number> = {
  week: 7 * 24 * 60 * 60 * 1000,
  day: 0,
};

@Injectable({ providedIn: 'root' })
export class ReleaseNotificationService {
  private permissionGranted: boolean | null = null;
  private channelEnsured = false;

  async init(): Promise<void> {
    if (!this.isSupported()) {
      return;
    }

    await this.ensurePermission();
    await this.ensureChannel();
  }

  async syncForGames(games: Game[]): Promise<void> {
    if (!this.isSupported()) {
      return;
    }

    const granted = await this.ensurePermission();
    if (!granted) {
      return;
    }

    await this.ensureChannel();
    await this.cancelTracked();

    const now = Date.now();
    const schedule: ScheduleOptions['notifications'] = [];
    const scheduledIds: number[] = [];

    for (const game of games) {
      if (!game?.myGames || !game.releaseDate) {
        continue;
      }

      const releaseAt = new Date(game.releaseDate).getTime();
      if (Number.isNaN(releaseAt)) {
        continue;
      }

      for (const kind of ['week', 'day'] as ReminderKind[]) {
        const triggerAt = releaseAt - REMINDER_OFFSETS[kind];
        if (triggerAt <= now) {
          continue;
        }

        const id = this.notificationId(game.id, kind);
        scheduledIds.push(id);
        schedule.push({
          id,
          title: kind === 'week' ? 'Release week' : 'Out today',
          body:
            kind === 'week'
              ? `${game.name} releases in 7 days.`
              : `${game.name} releases today — get ready to play!`,
          schedule: { at: new Date(triggerAt), allowWhileIdle: true },
          channelId: CHANNEL_ID,
          extra: { gameId: game.id, kind },
        });
      }
    }

    if (schedule.length > 0) {
      try {
        await LocalNotifications.schedule({ notifications: schedule });
      } catch (error) {
        console.warn('Failed to schedule release notifications', error);
      }
    }

    this.persistIds(scheduledIds);
  }

  async clearAll(): Promise<void> {
    if (!this.isSupported()) {
      return;
    }
    await this.cancelTracked();
  }

  private async ensurePermission(): Promise<boolean> {
    if (this.permissionGranted !== null) {
      return this.permissionGranted;
    }

    try {
      const current = await LocalNotifications.checkPermissions();
      let display = current.display;
      if (display !== 'granted' && display !== 'denied') {
        const requested = await LocalNotifications.requestPermissions();
        display = requested.display;
      }
      this.permissionGranted = display === 'granted';
    } catch (error) {
      console.warn('LocalNotifications permission check failed', error);
      this.permissionGranted = false;
    }

    return this.permissionGranted;
  }

  private async ensureChannel(): Promise<void> {
    if (this.channelEnsured || Capacitor.getPlatform() !== 'android') {
      this.channelEnsured = true;
      return;
    }

    try {
      await LocalNotifications.createChannel({
        id: CHANNEL_ID,
        name: 'Game releases',
        description: 'Reminders for games on your list that are about to launch.',
        importance: 4,
      });
    } catch (error) {
      console.warn('Failed to create notification channel', error);
    }

    this.channelEnsured = true;
  }

  private async cancelTracked(): Promise<void> {
    const ids = this.readIds();
    if (ids.length === 0) {
      return;
    }

    try {
      await LocalNotifications.cancel({
        notifications: ids.map((id) => ({ id })),
      });
    } catch (error) {
      console.warn('Failed to cancel release notifications', error);
    }

    this.persistIds([]);
  }

  private notificationId(gameId: number, kind: ReminderKind): number {
    const offset = kind === 'week' ? 1 : 2;
    return gameId * 10 + offset;
  }

  private readIds(): number[] {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) {
        return [];
      }
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed.filter((value) => typeof value === 'number') : [];
    } catch {
      return [];
    }
  }

  private persistIds(ids: number[]): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(ids));
    } catch {
      /* ignore quota errors */
    }
  }

  private isSupported(): boolean {
    return Capacitor.isPluginAvailable('LocalNotifications');
  }
}
