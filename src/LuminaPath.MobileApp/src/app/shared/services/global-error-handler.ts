import { ErrorHandler, Injectable, NgZone, inject } from '@angular/core';

import { ErrorReporter } from './error-reporter.service';

/**
 * App-wide replacement for Angular's default ErrorHandler. Logs to the
 * console (same as the default) and pushes the error into ErrorReporter so
 * the ErrorBannerComponent can surface a recovery UI — important on mobile
 * where the console isn't visible.
 *
 * Wired in app.config.ts via { provide: ErrorHandler, useClass: GlobalErrorHandler }.
 */
@Injectable({ providedIn: 'root' })
export class GlobalErrorHandler implements ErrorHandler {
  private readonly reporter = inject(ErrorReporter);
  private readonly zone = inject(NgZone);

  handleError(error: unknown): void {
    // Keep the default console.error trail intact for devs and crash reporters.
    // Using console.error directly (not the parent ErrorHandler.handleError)
    // because we want our own zone-safe reporting path below.
    // eslint-disable-next-line no-console
    console.error(error);

    // The reporter mutates an Angular signal. If Angular's error path fired
    // outside the zone (uncaught promise rejection, async exception inside
    // an event handler that escaped zone tracking, etc.) we need to re-enter
    // the zone so change detection picks up the new signal value and the
    // banner repaints. Wrapped in try/catch so a failing reporter can never
    // re-trigger the handler and cause an infinite loop.
    try {
      this.zone.run(() => this.reporter.report(error));
    } catch {
      /* swallow — already logged above */
    }
  }
}
