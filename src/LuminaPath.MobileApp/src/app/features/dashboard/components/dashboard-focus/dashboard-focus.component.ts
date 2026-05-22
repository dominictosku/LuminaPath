import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';

import { DashboardFocusItem } from '../../models/dashboard.model';

/** "Today's Focus" 2x2 grid of next-action cards. */
@Component({
  selector: 'app-dashboard-focus',
  templateUrl: './dashboard-focus.component.html',
  styleUrls: ['./dashboard-focus.component.scss'],
  imports: [IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardFocusComponent {
  readonly items = input<DashboardFocusItem[]>([]);

  trackByFocus(_: number, item: DashboardFocusItem): string {
    return `${item.icon}-${item.title}`;
  }
}
