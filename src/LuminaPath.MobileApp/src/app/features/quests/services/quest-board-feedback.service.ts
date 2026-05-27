import { Injectable, OnDestroy, signal } from '@angular/core';

import { AchievementInfo } from './quest-board.service';

const TOAST_MS = 2600;
const ACHIEVEMENT_TOAST_MS = 4500;
const CELEBRATION_MS = 1200;

@Injectable()
export class QuestBoardFeedbackService implements OnDestroy {
  readonly toastMessage = signal('');
  readonly achievementToast = signal<AchievementInfo | null>(null);
  readonly recentlyCompletedIds = signal<ReadonlySet<number>>(new Set());

  private toastTimer: number | undefined;
  private achievementTimer: number | undefined;
  private celebrationTimers = new Map<number, number>();

  ngOnDestroy(): void {
    this.clearTimers();
  }

  showToast(message: string): void {
    this.toastMessage.set(message);
    this.clearToastTimer();
    this.toastTimer = window.setTimeout(() => {
      this.toastMessage.set('');
      this.toastTimer = undefined;
    }, TOAST_MS);
  }

  clearToast(): void {
    this.toastMessage.set('');
    this.clearToastTimer();
  }

  showAchievementToast(achievement: AchievementInfo): void {
    this.achievementToast.set(achievement);
    this.clearAchievementTimer();
    this.achievementTimer = window.setTimeout(() => {
      if (this.achievementToast()?.code === achievement.code) {
        this.achievementToast.set(null);
      }
      this.achievementTimer = undefined;
    }, ACHIEVEMENT_TOAST_MS);
  }

  closeAchievementToast(): void {
    this.achievementToast.set(null);
    this.clearAchievementTimer();
  }

  triggerCelebration(questId: number): void {
    const existing = this.celebrationTimers.get(questId);
    if (existing !== undefined) {
      window.clearTimeout(existing);
    }

    this.recentlyCompletedIds.set(new Set(this.recentlyCompletedIds()).add(questId));
    const timerId = window.setTimeout(() => {
      const next = new Set(this.recentlyCompletedIds());
      next.delete(questId);
      this.recentlyCompletedIds.set(next);
      this.celebrationTimers.delete(questId);
    }, CELEBRATION_MS);
    this.celebrationTimers.set(questId, timerId);
  }

  isRecentlyCompleted(questId: number): boolean {
    return this.recentlyCompletedIds().has(questId);
  }

  clearTimers(): void {
    this.clearToastTimer();
    this.clearAchievementTimer();
    for (const timerId of this.celebrationTimers.values()) {
      window.clearTimeout(timerId);
    }
    this.celebrationTimers.clear();
  }

  private clearToastTimer(): void {
    if (this.toastTimer !== undefined) {
      window.clearTimeout(this.toastTimer);
      this.toastTimer = undefined;
    }
  }

  private clearAchievementTimer(): void {
    if (this.achievementTimer !== undefined) {
      window.clearTimeout(this.achievementTimer);
      this.achievementTimer = undefined;
    }
  }
}
