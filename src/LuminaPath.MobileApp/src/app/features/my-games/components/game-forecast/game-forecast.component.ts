import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { IonButton, IonIcon, IonProgressBar } from '@ionic/angular/standalone';

import { GameForecast } from 'src/app/features/planning/services/gaming-session.service';
import {
  ForecastBarParts,
  ForecastSegment,
  forecastBarParts,
  forecastHours,
  forecastProgress,
  forecastSummary,
  forecastWidth,
} from '../../forecast.helpers';

/**
 * Completion-forecast panel: three-segment bar (played/scheduled/remaining)
 * with chip legend, or a fallback indeterminate-style progress bar when
 * there's no playtime estimate. Pure presentation — the page injects the
 * GameForecast, this component just renders.
 */
@Component({
  selector: 'app-game-forecast',
  templateUrl: './game-forecast.component.html',
  styleUrls: ['../../pages/my-game-details.page.scss'],
  imports: [IonButton, IonIcon, IonProgressBar],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GameForecastComponent {
  readonly forecast = input.required<GameForecast>();
  readonly goToPlanning = output<void>();

  readonly bar = computed<ForecastBarParts>(() => forecastBarParts(this.forecast()));
  readonly summary = computed(() => forecastSummary(this.forecast()));
  readonly progress = computed(() => forecastProgress(this.forecast()));

  segmentWidth(segment: ForecastSegment): number {
    return forecastWidth(this.forecast(), segment);
  }

  hours(value: number): string {
    return forecastHours(value);
  }
}
