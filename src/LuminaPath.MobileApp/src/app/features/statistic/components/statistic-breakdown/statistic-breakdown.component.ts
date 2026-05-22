import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { IonIcon, IonProgressBar } from '@ionic/angular/standalone';

import { KindBreakdown } from '../../models/statistic.model';
import { formatHours } from '../../statistic.computations';

/** Per-media-type breakdown card grid at the bottom of the page. */
@Component({
  selector: 'app-statistic-breakdown',
  templateUrl: './statistic-breakdown.component.html',
  imports: [IonIcon, IonProgressBar],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticBreakdownComponent {
  readonly rows = input<KindBreakdown[]>([]);

  formatHours(value: number): string {
    return formatHours(value);
  }

  trackByBreakdown(_: number, row: KindBreakdown): string {
    return row.kind;
  }
}
