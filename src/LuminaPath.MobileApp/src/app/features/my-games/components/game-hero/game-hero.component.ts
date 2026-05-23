import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IonBadge, IonIcon } from '@ionic/angular/standalone';

import { GameWithFlexibleLibrary } from '../../models/my-game.model';

/**
 * Hero card at the top of the my-game-details page: backdrop image,
 * cover, library-status badge, parent-game backlink, description, and
 * the 4-cell meta grid (genre / platform / release / playtime).
 */
@Component({
  selector: 'app-game-hero',
  templateUrl: './game-hero.component.html',
  styleUrls: ['../../pages/my-game-details.page.scss'],
  imports: [RouterLink, IonBadge, IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GameHeroComponent {
  readonly game = input.required<GameWithFlexibleLibrary>();
  readonly imageUrl = input.required<string>();
  readonly libraryStatusLabel = input.required<string>();
  readonly isInLibrary = input<boolean>(false);
  readonly hasParent = input<boolean>(false);
  readonly genreLabel = input.required<string>();
  readonly platformLabel = input.required<string>();
  readonly releaseLabel = input.required<string>();
  readonly playtimeLabel = input.required<string>();
}
