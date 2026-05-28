import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';

import { StatusSlice } from '../../models/statistic.model';
import { statusClass } from '../../domain/statistic.computations';

/** Horizontal status bar + matching legend. Hidden by the parent when empty. */
@Component({
  selector: 'app-statistic-status-mix',
  templateUrl: './statistic-status-mix.component.html',
  imports: [IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticStatusMixComponent {
  readonly slices = input<StatusSlice[]>([]);

  statusClass(label: string): string {
    return statusClass(label);
  }

  trackByStatus(_: number, slice: StatusSlice): string {
    return slice.label;
  }
}
