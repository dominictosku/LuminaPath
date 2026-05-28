import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';

import { DashboardMediaItem } from '../../models/dashboard.model';
import { daysUntil, releaseLabel } from '../../domain/dashboard-view.helpers';

/** Vertical list of the next upcoming media releases. */
@Component({
  selector: 'app-dashboard-releases',
  templateUrl: './dashboard-releases.component.html',
  styleUrls: ['./dashboard-releases.component.scss'],
  imports: [IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardReleasesComponent {
  readonly items = input<DashboardMediaItem[]>([]);

  daysUntil(item: DashboardMediaItem): string {
    return daysUntil(item);
  }

  releaseLabel(item: DashboardMediaItem): string {
    return releaseLabel(item);
  }

  trackByItem(_: number, item: DashboardMediaItem): string {
    return `${item.kind}-${item.id}`;
  }
}
