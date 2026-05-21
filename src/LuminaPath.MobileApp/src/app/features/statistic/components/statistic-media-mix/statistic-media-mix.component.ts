import { Component, input } from '@angular/core';
import { IonBadge, IonIcon } from '@ionic/angular/standalone';

import { DONUT_RADIUS, DonutSegment } from '../../models/statistic.model';

/** Donut + legend showing how many items live in each media type. */
@Component({
  selector: 'app-statistic-media-mix',
  templateUrl: './statistic-media-mix.component.html',
  imports: [IonBadge, IonIcon],
})
export class StatisticMediaMixComponent {
  readonly segments = input<DonutSegment[]>([]);
  readonly total = input<number>(0);

  readonly donutRadius = DONUT_RADIUS;

  trackBySegment(_: number, item: DonutSegment): string {
    return item.kind;
  }
}
