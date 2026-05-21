import { Component, input } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';

import { GenreSlice } from '../../models/statistic.model';

/** Top-5 genre bars derived from each item's `genre` string. */
@Component({
  selector: 'app-statistic-top-genres',
  templateUrl: './statistic-top-genres.component.html',
  imports: [IonIcon],
})
export class StatisticTopGenresComponent {
  readonly genres = input<GenreSlice[]>([]);

  trackByGenre(_: number, genre: GenreSlice): string {
    return genre.name;
  }
}
