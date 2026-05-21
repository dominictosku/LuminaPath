import { Component, input } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';

import { HealthFlag } from '../../models/statistic.model';

/** Four interpretation cards that translate raw stats into plain language. */
@Component({
  selector: 'app-statistic-health-flags',
  templateUrl: './statistic-health-flags.component.html',
  imports: [IonIcon],
})
export class StatisticHealthFlagsComponent {
  readonly flags = input<HealthFlag[]>([]);

  trackByFlag(_: number, flag: HealthFlag): string {
    return flag.title;
  }
}
