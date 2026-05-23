import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IonIcon } from '@ionic/angular/standalone';

import { GameSummary } from 'src/app/features/games/models/games.model';
import { formatShortDate } from 'src/app/shared/utils/format';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';

/**
 * "Downloadable content" panel under the Overview tab. Renders the DLC
 * list as a row of links that route to each DLC's own details page.
 */
@Component({
  selector: 'app-game-dlc-list',
  templateUrl: './game-dlc-list.component.html',
  styleUrls: ['../../pages/my-game-details.page.scss'],
  imports: [RouterLink, IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GameDlcListComponent {
  readonly dlcs = input.required<GameSummary[]>();

  dlcReleaseLabel(dlc: GameSummary): string {
    return formatShortDate(dlc.releaseDate);
  }

  dlcImageUrl(dlc: GameSummary): string {
    return mediaImageUrl(dlc.image ?? null);
  }

  trackByDlc(_: number, dlc: GameSummary): number {
    return dlc.id;
  }
}
