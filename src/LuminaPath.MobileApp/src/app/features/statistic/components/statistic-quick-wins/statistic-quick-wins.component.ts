import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { IonBadge, IonIcon } from '@ionic/angular/standalone';

import { BacklogItem } from '../../models/statistic.model';
import { formatHours } from '../../domain/statistic.computations';

/** Short backlog items (under 12h) the user could realistically finish soon. */
@Component({
  selector: 'app-statistic-quick-wins',
  templateUrl: './statistic-quick-wins.component.html',
  imports: [IonBadge, IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticQuickWinsComponent {
  readonly items = input<BacklogItem[]>([]);

  formatHours(value: number): string {
    return formatHours(value);
  }

  trackByItem(_: number, item: BacklogItem): string {
    return `${item.kind}-${item.id}`;
  }
}
