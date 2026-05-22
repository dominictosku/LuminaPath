import { Injectable, signal } from '@angular/core';

import { extractErrorMessage } from '../utils/extract-error';

export type AppErrorRecord = {
  /** User-facing message extracted from the thrown value. */
  message: string;
  /** Best-effort original error, kept for debugging via the banner's details. */
  source: unknown;
  /** Wall-clock timestamp the error was reported. */
  reportedAt: number;
};

/**
 * Signal-backed sink for app-wide errors. The GlobalErrorHandler pushes
 * uncaught errors here; the ErrorBannerComponent reads the signal to render
 * a recovery banner. Single-slot by design — newer errors replace older ones
 * so the user always sees the latest failure, not a stack.
 */
@Injectable({ providedIn: 'root' })
export class ErrorReporter {
  /** null when there's nothing to surface. */
  readonly current = signal<AppErrorRecord | null>(null);

  report(error: unknown): void {
    this.current.set({
      message: messageOf(error),
      source: error,
      reportedAt: Date.now(),
    });
  }

  dismiss(): void {
    this.current.set(null);
  }
}

/** Best-effort message extraction. HTTP-shape errors first (matches the
 *  rest of the app's handling), then raw Error.message, then a friendly
 *  fallback so the banner never shows "[object Object]". */
function messageOf(error: unknown): string {
  const fallback = 'Something went wrong.';
  if (error instanceof Error && error.message) {
    return error.message;
  }
  return extractErrorMessage(error, fallback);
}
