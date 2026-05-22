import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { IonIcon, IonSkeletonText } from '@ionic/angular/standalone';

import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { DashboardMediaItem } from '../../models/dashboard.model';
import { playedLabel, statusLabel } from '../../dashboard-view.helpers';

/**
 * Hero card at the top of the dashboard: featured media artwork, name, status,
 * and a progress strip. Shows a skeleton while the parent is loading.
 */
@Component({
  selector: 'app-dashboard-hero',
  templateUrl: './dashboard-hero.component.html',
  styleUrls: ['./dashboard-hero.component.scss'],
  imports: [IonIcon, IonSkeletonText],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardHeroComponent {
  readonly featuredItem = input<DashboardMediaItem | null>(null);
  readonly heroProgress = input<number>(0);
  readonly isLoading = input<boolean>(false);

  readonly imageSrc = computed(() => mediaImageUrl(this.featuredItem()?.image));
  readonly subtitle = computed(() => {
    const item = this.featuredItem();
    return item ? `${statusLabel(item)} · ${item.context}` : 'Log in and start shaping your media plan.';
  });
  readonly playedSummary = computed(() => {
    const item = this.featuredItem();
    return item ? playedLabel(item) : '';
  });
}
