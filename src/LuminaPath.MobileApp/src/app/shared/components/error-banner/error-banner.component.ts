import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';
import { ErrorReporter } from '../../services/error-reporter.service';

/**
 * Sticky banner that surfaces uncaught errors pushed into ErrorReporter by
 * the GlobalErrorHandler. Lives in app.component.html so it's always mounted
 * regardless of the active route — even when a route component crashes on
 * its own, this still renders above it.
 *
 * Recovery options: Reload reloads the page; Dismiss hides the banner so
 * the user can keep trying things.
 */
@Component({
  selector: 'app-error-banner',
  templateUrl: './error-banner.component.html',
  styleUrls: ['./error-banner.component.scss'],
  imports: [IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ErrorBannerComponent {
  private readonly reporter = inject(ErrorReporter);

  readonly current = this.reporter.current;
  readonly visible = computed(() => this.current() !== null);

  dismiss(): void {
    this.reporter.dismiss();
  }

  reload(): void {
    location.reload();
  }
}
