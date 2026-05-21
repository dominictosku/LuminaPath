import { Component, input } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';

import { DashboardMetric } from '../../models/dashboard.model';

/** Four-tile metric grid (Library / Active / Completed / Ahead) at the top of the dashboard. */
@Component({
  selector: 'app-dashboard-metrics',
  templateUrl: './dashboard-metrics.component.html',
  styleUrls: ['./dashboard-metrics.component.scss'],
  imports: [IonIcon],
})
export class DashboardMetricsComponent {
  readonly metrics = input<DashboardMetric[]>([]);

  trackByMetric(_: number, metric: DashboardMetric): string {
    return metric.label;
  }
}
