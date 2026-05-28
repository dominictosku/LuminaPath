import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';

import { DashboardMediaItem } from '../../models/dashboard.model';
import { playedLabel, remainingLabel } from '../../domain/dashboard-view.helpers';

/** Biggest backlog commitments (highest remaining hours among planned items). */
@Component({
  selector: 'app-dashboard-backlog',
  templateUrl: './dashboard-backlog.component.html',
  styleUrls: ['./dashboard-backlog.component.scss'],
  imports: [IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardBacklogComponent {
  readonly items = input<DashboardMediaItem[]>([]);

  playedLabel(item: DashboardMediaItem): string {
    return playedLabel(item);
  }

  remainingLabel(item: DashboardMediaItem): string {
    return remainingLabel(item);
  }

  trackByItem(_: number, item: DashboardMediaItem): string {
    return `${item.kind}-${item.id}`;
  }
}
