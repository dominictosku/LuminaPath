import { Injectable, signal } from '@angular/core';

/**
 * Signal-backed network status, consistent with `MediaModeService` /
 * `AuthService` / `ApiEndpointService`. Reads `navigator.onLine` and
 * listens for `online` / `offline` events on the window.
 *
 * `navigator.onLine` is not 100% accurate — captive portals and bad
 * Wi-Fi can still report online — but it's good enough for the offline
 * banner and for suppressing error toasts when a fetch fails because
 * we're already known to be offline. The Capacitor Network plugin would
 * give a more accurate signal on Android; we keep this dependency-free
 * for now and the call sites are isolated enough to swap later.
 */
@Injectable({ providedIn: 'root' })
export class NetworkStatusService {
  private readonly onlineSignal = signal<boolean>(this.readInitialOnlineState());

  readonly isOnline = this.onlineSignal.asReadonly();

  constructor() {
    if (typeof window === 'undefined') return;
    window.addEventListener('online', () => this.onlineSignal.set(true));
    window.addEventListener('offline', () => this.onlineSignal.set(false));
  }

  /** Test seam — force the signal to a given state. */
  _setForTests(online: boolean): void {
    this.onlineSignal.set(online);
  }

  private readInitialOnlineState(): boolean {
    if (typeof navigator === 'undefined' || typeof navigator.onLine !== 'boolean') {
      // Assume online when we can't tell — better to attempt a fetch and
      // fail than to never try.
      return true;
    }
    return navigator.onLine;
  }
}
