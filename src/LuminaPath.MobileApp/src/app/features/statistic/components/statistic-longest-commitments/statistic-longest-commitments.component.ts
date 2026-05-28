import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { IonIcon, IonProgressBar } from '@ionic/angular/standalone';

import { BacklogItem } from '../../models/statistic.model';
import { formatHours } from '../../domain/statistic.computations';

/** Top time sinks: the items consuming the largest share of remaining hours. */
@Component({
  selector: 'app-statistic-longest-commitments',
  templateUrl: './statistic-longest-commitments.component.html',
  imports: [IonIcon, IonProgressBar],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticLongestCommitmentsComponent {
  readonly items = input<BacklogItem[]>([]);

  formatHours(value: number): string {
    return formatHours(value);
  }

  trackByItem(_: number, item: BacklogItem): string {
    return `${item.kind}-${item.id}`;
  }
}
