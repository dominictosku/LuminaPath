import { Component, input } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';

import { AchievementInfo } from '../../services/quest-board.service';

/** Grid of unlocked achievement cards. Hidden by the parent when the list is empty. */
@Component({
  selector: 'app-quest-achievements',
  templateUrl: './quest-achievements.component.html',
  imports: [IonIcon],
})
export class QuestAchievementsComponent {
  readonly achievements = input<AchievementInfo[]>([]);

  trackByAchievement(_: number, achievement: AchievementInfo): string {
    return achievement.code;
  }
}
