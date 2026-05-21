import { Component, input } from '@angular/core';

/**
 * Split panel: total hours logged paired with a conic completion ring.
 * Sits between the metric grid and the per-section blocks.
 */
@Component({
  selector: 'app-dashboard-momentum',
  templateUrl: './dashboard-momentum.component.html',
  styleUrls: ['./dashboard-momentum.component.scss'],
})
export class DashboardMomentumComponent {
  readonly playedHours = input<number>(0);
  readonly completionRate = input<number>(0);
}
