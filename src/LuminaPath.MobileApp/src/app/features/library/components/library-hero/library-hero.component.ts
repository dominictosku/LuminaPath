import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';

/**
 * Library hero card: eyebrow chip + headline + descriptive blurb + the 4
 * summary stats. Pure presentation — the page owns the data and computes
 * the labels via MediaLibraryViewService.
 */
@Component({
  selector: 'app-library-hero',
  templateUrl: './library-hero.component.html',
  styleUrls: ['../../pages/library.page.scss'],
  imports: [IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LibraryHeroComponent {
  readonly mediaModeSingular = input.required<string>();
  readonly heroTitle = input.required<string>();
  readonly heroDescription = input.required<string>();
  readonly totalGames = input.required<number>();
  readonly ownedGames = input.required<number>();
  readonly playingGames = input.required<number>();
  readonly remainingHours = input.required<number>();
  readonly isGamesMode = input<boolean>(false);
}
