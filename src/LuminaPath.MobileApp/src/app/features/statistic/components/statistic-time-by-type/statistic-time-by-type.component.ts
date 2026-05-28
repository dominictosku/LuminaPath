import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';

import { TimeBar } from '../../models/statistic.model';
import { formatHours } from '../../domain/statistic.computations';

/** Stacked time-budget bars per media kind: hours logged vs remaining. */
@Component({
  selector: 'app-statistic-time-by-type',
  templateUrl: './statistic-time-by-type.component.html',
  imports: [IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticTimeByTypeComponent {
  readonly bars = input<TimeBar[]>([]);

  formatHours(value: number): string {
    return formatHours(value);
  }

  trackByTimeBar(_: number, bar: TimeBar): string {
    return bar.kind;
  }
}
