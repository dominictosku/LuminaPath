import { DecimalPipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { IonBadge, IonIcon } from '@ionic/angular/standalone';

import { RatingBucket } from '../../models/statistic.model';

/** Bar chart of personal ratings 1–10 with the average shown underneath. */
@Component({
  selector: 'app-statistic-rating-histogram',
  templateUrl: './statistic-rating-histogram.component.html',
  imports: [DecimalPipe, IonBadge, IonIcon],
})
export class StatisticRatingHistogramComponent {
  readonly buckets = input<RatingBucket[]>([]);
  readonly count = input<number>(0);
  readonly average = input<number>(0);

  trackByBucket(_: number, bucket: RatingBucket): number {
    return bucket.rating;
  }
}
