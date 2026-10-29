import { RouterLink } from '@angular/router';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Library completion and total hours, paired with a link to statistics.
 */
@Component({
  selector: 'app-dashboard-momentum',
  templateUrl: './dashboard-momentum.component.html',
  styleUrls: ['./dashboard-momentum.component.scss'],
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardMomentumComponent {
  readonly playedHours = input<number>(0);
  readonly completionRate = input<number>(0);
}
