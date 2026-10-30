import { RouterLink } from '@angular/router';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { IonIcon, IonSkeletonText } from '@ionic/angular/standalone';

import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { DashboardMediaItem } from '../../models/dashboard.model';
import { dashboardItemRoute } from '../../domain/dashboard-view.helpers';
import { progressSummary, statusLabel } from '../../domain/dashboard-view.helpers';

/**
 * Hero card at the top of the dashboard: featured media artwork, name, status,
 * and a progress strip. Shows a skeleton while the parent is loading.
 */
@Component({
  selector: 'app-dashboard-hero',
  templateUrl: './dashboard-hero.component.html',
  styleUrls: ['./dashboard-hero.component.scss'],
  imports: [RouterLink, IonIcon, IonSkeletonText],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardHeroComponent {
  readonly itemRoute = dashboardItemRoute;

  readonly featuredItem = input<DashboardMediaItem | null>(null);
  readonly heroProgress = input<number>(0);
  readonly isLoading = input<boolean>(false);

  readonly imageSrc = computed(() => mediaImageUrl(this.featuredItem()?.image));
  readonly subtitle = computed(() => {
    const item = this.featuredItem();
    return item ? `${statusLabel(item)} · ${item.context}` : 'Games, films, anime, and series. Bring the stories you love together and find your next adventure.';
  });
  readonly playedSummary = computed(() => {
    const item = this.featuredItem();
    return item ? progressSummary(item) : '';
  });
}
