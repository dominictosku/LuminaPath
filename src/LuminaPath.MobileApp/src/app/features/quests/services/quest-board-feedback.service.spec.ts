import { fakeAsync, TestBed, tick } from '@angular/core/testing';

import { QuestBoardFeedbackService } from './quest-board-feedback.service';

describe('QuestBoardFeedbackService', () => {
  let service: QuestBoardFeedbackService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [QuestBoardFeedbackService],
    });
    service = TestBed.inject(QuestBoardFeedbackService);
  });

  it('auto-clears toast messages', fakeAsync(() => {
    service.showToast('Saved');

    expect(service.toastMessage()).toBe('Saved');
    tick(2600);
    expect(service.toastMessage()).toBe('');
  }));

  it('tracks recently completed quests for the celebration window', fakeAsync(() => {
    service.triggerCelebration(42);

    expect(service.isRecentlyCompleted(42)).toBeTrue();
    tick(1200);
    expect(service.isRecentlyCompleted(42)).toBeFalse();
  }));

  it('clears only the current achievement toast', fakeAsync(() => {
    service.showAchievementToast({
      code: 'first',
      title: 'First',
      description: 'First achievement',
      icon: 'trophy-outline',
      unlockedAt: '2026-05-27T00:00:00.000Z',
    });
    tick(1000);
    service.showAchievementToast({
      code: 'second',
      title: 'Second',
      description: 'Second achievement',
      icon: 'star-outline',
      unlockedAt: '2026-05-27T00:00:00.000Z',
    });

    tick(3500);
    expect(service.achievementToast()?.code).toBe('second');
    tick(1000);
    expect(service.achievementToast()).toBeNull();
  }));
});
