import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { IonBadge, IonIcon, IonProgressBar } from '@ionic/angular/standalone';

/**
 * Top hero panel for the quest board: title/level, XP progress bar, today/overdue
 * stats, and streak pill. Pure display — all values come from the parent page.
 */
@Component({
  selector: 'app-quest-board-hero',
  templateUrl: './quest-board-hero.component.html',
  imports: [IonBadge, IonIcon, IonProgressBar],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuestBoardHeroComponent {
  readonly level = input<number>(1);
  readonly title = input<string>('Initiate');
  readonly xp = input<number>(0);
  readonly xpIntoLevel = input<number>(0);
  readonly xpProgress = input<number>(0);
  readonly todayCount = input<number>(0);
  readonly overdueCount = input<number>(0);
  readonly completedQuestCount = input<number>(0);
  readonly currentStreakDays = input<number>(0);
  readonly longestStreakDays = input<number>(0);
}
