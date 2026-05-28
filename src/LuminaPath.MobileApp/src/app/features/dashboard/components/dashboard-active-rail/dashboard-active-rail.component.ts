import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { IonBadge, IonIcon } from '@ionic/angular/standalone';

import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { DashboardMediaItem } from '../../models/dashboard.model';
import { progressOf, remainingLabel } from '../../domain/dashboard-view.helpers';

/** Horizontally scrolling rail of "currently active" media (status === Playing). */
@Component({
  selector: 'app-dashboard-active-rail',
  templateUrl: './dashboard-active-rail.component.html',
  styleUrls: ['./dashboard-active-rail.component.scss'],
  imports: [IonBadge, IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardActiveRailComponent {
  readonly items = input<DashboardMediaItem[]>([]);

  imageFor(item: DashboardMediaItem): string {
    return mediaImageUrl(item.image);
  }

  progressOf(item: DashboardMediaItem): number {
    return progressOf(item);
  }

  remainingLabel(item: DashboardMediaItem): string {
    return remainingLabel(item);
  }

  trackByItem(_: number, item: DashboardMediaItem): string {
    return `${item.kind}-${item.id}`;
  }
}
