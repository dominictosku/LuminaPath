import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { IonIcon, IonSkeletonText } from '@ionic/angular/standalone';

import { StatMetric } from '../../models/statistic.model';

/** Eight-tile grid of headline stats. Renders skeleton placeholders while loading. */
@Component({
  selector: 'app-statistic-metrics',
  templateUrl: './statistic-metrics.component.html',
  imports: [IonIcon, IonSkeletonText],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticMetricsComponent {
  readonly metrics = input<StatMetric[]>([]);
  readonly isLoading = input<boolean>(false);

  readonly loadingSlots = [1, 2, 3, 4, 5, 6, 7, 8];

  trackByMetric(_: number, metric: StatMetric): string {
    return metric.label;
  }
}
