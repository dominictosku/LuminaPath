import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { IonBadge, IonIcon } from '@ionic/angular/standalone';

import { EmptyStateComponent } from 'src/app/shared/components/empty-state/empty-state.component';
import { formatShortDate } from 'src/app/shared/utils/format';

import { UserGameAchievement } from '../../models/my-game.model';

/**
 * "Trophies" tab content: lazy-loaded list of unlocked achievements,
 * with three empty/loading/error states wrapped through app-empty-state.
 * Pure presentation; the page owns the data + loading flags.
 */
@Component({
  selector: 'app-game-trophies',
  templateUrl: './game-trophies.component.html',
  styleUrls: ['../../pages/my-game-details.page.scss'],
  imports: [IonBadge, IonIcon, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GameTrophiesComponent {
  readonly achievements = input<UserGameAchievement[]>([]);
  readonly isLoading = input<boolean>(false);
  readonly errorMessage = input<string>('');

  readonly summaryLabel = computed(() => {
    if (this.isLoading()) return 'Syncing';
    const count = this.achievements().length;
    return count === 0 ? 'None earned yet' : `${count} earned`;
  });

  achievementDateLabel(achievement: UserGameAchievement): string {
    const dateValue = achievement.unlockedAt ?? achievement.syncedAt;
    return formatShortDate(dateValue, 'Synced recently');
  }

  trophyTypeLabel(value?: string | null): string {
    const trophyType = value?.trim();
    if (!trophyType) return 'Achievement';
    return trophyType.charAt(0).toUpperCase() + trophyType.slice(1);
  }

  trackByAchievement(_: number, achievement: UserGameAchievement): number {
    return achievement.id;
  }
}
