import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';

import { BacklogItem } from '../../models/statistic.model';

/** Favourites list: highest-rated owned items. Parent hides when empty. */
@Component({
  selector: 'app-statistic-top-rated',
  templateUrl: './statistic-top-rated.component.html',
  imports: [IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticTopRatedComponent {
  readonly items = input<BacklogItem[]>([]);

  trackByItem(_: number, item: BacklogItem): string {
    return `${item.kind}-${item.id}`;
  }
}
