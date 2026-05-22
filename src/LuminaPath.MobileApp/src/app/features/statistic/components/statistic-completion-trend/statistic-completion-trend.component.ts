import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { IonBadge, IonIcon } from '@ionic/angular/standalone';

import { TREND_HEIGHT, TREND_WIDTH, TrendData } from '../../models/statistic.model';

/** 12-month completion line chart with month-axis and a peak note. */
@Component({
  selector: 'app-statistic-completion-trend',
  templateUrl: './statistic-completion-trend.component.html',
  imports: [IonBadge, IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticCompletionTrendComponent {
  readonly trend = input.required<TrendData>();

  readonly trendWidth = TREND_WIDTH;
  readonly trendHeight = TREND_HEIGHT;

  trackByTrend(_: number, point: { label: string }): string {
    return point.label;
  }
}
