import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';
import { NetworkStatusService } from '../../services/network-status.service';

/**
 * Sticky informational banner shown when the device is offline. Sibling
 * to the ErrorBanner in app.component.html so it's always mounted above
 * the router outlet. Dismissible per drop — once the network returns
 * the dismissed flag clears, so the next outage re-shows the banner.
 *
 * This is intentionally not an error: offline is a normal mode here.
 * Pages still render from the cache; the banner just informs the user
 * that what they see may be stale.
 */
@Component({
  selector: 'app-offline-banner',
  templateUrl: './offline-banner.component.html',
  styleUrls: ['./offline-banner.component.scss'],
  imports: [IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OfflineBannerComponent {
  private readonly network = inject(NetworkStatusService);
  private readonly dismissed = signal(false);

  readonly visible = computed(() => !this.network.isOnline() && !this.dismissed());

  constructor() {
    // Clear the dismissed flag whenever the network returns, so a future
    // outage re-shows the banner. The effect runs on signal changes only,
    // no polling.
    effect(() => {
      if (this.network.isOnline()) {
        this.dismissed.set(false);
      }
    });
  }

  dismiss(): void {
    this.dismissed.set(true);
  }
}
