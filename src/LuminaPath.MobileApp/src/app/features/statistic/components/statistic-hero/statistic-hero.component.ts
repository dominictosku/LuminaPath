import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { IonIcon, IonSkeletonText } from '@ionic/angular/standalone';

import { HealthTone } from '../../models/statistic.model';

/**
 * Page hero: eyebrow + title + intro paragraph, plus the conic health orbit
 * (replaced with a skeleton while the parent is loading).
 */
@Component({
  selector: 'app-statistic-hero',
  templateUrl: './statistic-hero.component.html',
  imports: [IonIcon, IonSkeletonText],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticHeroComponent {
  readonly healthScore = input<number>(100);
  readonly healthTone = input<HealthTone>('good');
  readonly isLoading = input<boolean>(false);
}
