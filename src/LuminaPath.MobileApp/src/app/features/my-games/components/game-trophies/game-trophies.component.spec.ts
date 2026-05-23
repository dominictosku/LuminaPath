import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideIonicAngular } from '@ionic/angular/standalone';

import { UserGameAchievement } from '../../models/my-game.model';
import { GameTrophiesComponent } from './game-trophies.component';

function trophy(overrides: Partial<UserGameAchievement> = {}): UserGameAchievement {
  return {
    id: 1,
    gameAchievementId: 1,
    provider: 2,
    providerName: 'Steam',
    sourceAchievementId: 'achv:1',
    title: 'First step',
    description: null,
    iconUrl: null,
    isHidden: false,
    trophyType: 'bronze',
    unlockedAt: null,
    syncedAt: '2026-05-10T00:00:00Z',
    ...overrides,
  };
}

describe('GameTrophiesComponent', () => {
  let fixture: ComponentFixture<GameTrophiesComponent>;
  let component: GameTrophiesComponent;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [GameTrophiesComponent],
      providers: [provideIonicAngular()],
    });
    fixture = TestBed.createComponent(GameTrophiesComponent);
    component = fixture.componentInstance;
  });

  describe('summaryLabel', () => {
    it('"Syncing" while loading', () => {
      fixture.componentRef.setInput('isLoading', true);
      fixture.componentRef.setInput('achievements', []);
      expect(component.summaryLabel()).toBe('Syncing');
    });

    it('"None earned yet" when not loading and empty', () => {
      fixture.componentRef.setInput('isLoading', false);
      fixture.componentRef.setInput('achievements', []);
      expect(component.summaryLabel()).toBe('None earned yet');
    });

    it('"N earned" when there are achievements', () => {
      fixture.componentRef.setInput('isLoading', false);
      fixture.componentRef.setInput('achievements', [trophy(), trophy({ id: 2 })]);
      expect(component.summaryLabel()).toBe('2 earned');
    });
  });

  describe('trophyTypeLabel', () => {
    it('capitalises a known type', () => {
      expect(component.trophyTypeLabel('bronze')).toBe('Bronze');
      expect(component.trophyTypeLabel('Silver')).toBe('Silver');
      expect(component.trophyTypeLabel('  gold  ')).toBe('Gold');
    });

    it('falls back to "Achievement" for empty / null', () => {
      expect(component.trophyTypeLabel(null)).toBe('Achievement');
      expect(component.trophyTypeLabel(undefined)).toBe('Achievement');
      expect(component.trophyTypeLabel('  ')).toBe('Achievement');
    });
  });

  describe('achievementDateLabel', () => {
    it('uses unlockedAt when present', () => {
      const out = component.achievementDateLabel(trophy({ unlockedAt: '2026-05-10T12:00:00Z' }));
      expect(out).not.toBe('Synced recently');
    });

    it('falls back to syncedAt when unlockedAt is null', () => {
      const synced = component.achievementDateLabel(trophy({ unlockedAt: null, syncedAt: '2026-05-11T12:00:00Z' }));
      expect(synced).not.toBe('Synced recently');
    });

    it('uses the "Synced recently" fallback when no date is usable', () => {
      const fallback = component.achievementDateLabel(trophy({ unlockedAt: null, syncedAt: '' }));
      expect(fallback).toBe('Synced recently');
    });
  });
});
