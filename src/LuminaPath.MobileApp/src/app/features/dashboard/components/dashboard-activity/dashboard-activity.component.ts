import { Component, input } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';

import { DashboardActivityItem } from '../../models/dashboard.model';

/** Mixed-source activity feed (recent sessions, completed quests, releases, additions). */
@Component({
  selector: 'app-dashboard-activity',
  templateUrl: './dashboard-activity.component.html',
  styleUrls: ['./dashboard-activity.component.scss'],
  imports: [IonIcon],
})
export class DashboardActivityComponent {
  readonly items = input<DashboardActivityItem[]>([]);

  trackByActivity(_: number, item: DashboardActivityItem): string {
    return `${item.icon}-${item.title}-${item.detail}`;
  }
}
