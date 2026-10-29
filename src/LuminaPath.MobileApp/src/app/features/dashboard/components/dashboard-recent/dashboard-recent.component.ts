import { RouterLink } from '@angular/router';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';

import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { DashboardMediaItem } from '../../models/dashboard.model';
import { dashboardItemRoute } from '../../domain/dashboard-view.helpers';

/** Recently added media as a compact card grid. */
@Component({
  selector: 'app-dashboard-recent',
  templateUrl: './dashboard-recent.component.html',
  styleUrls: ['./dashboard-recent.component.scss'],
  imports: [RouterLink, IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardRecentComponent {
  readonly itemRoute = dashboardItemRoute;

  readonly items = input<DashboardMediaItem[]>([]);

  imageFor(item: DashboardMediaItem): string {
    return mediaImageUrl(item.image);
  }

  trackByItem(_: number, item: DashboardMediaItem): string {
    return `${item.kind}-${item.id}`;
  }
}
