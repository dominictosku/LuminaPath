import { Injectable, signal } from '@angular/core';

const themeStorageKey = 'luminapath.theme';

@Injectable({ providedIn: 'root' })
export class ThemePreferenceService {
  private readonly darkModeSignal = signal(readInitialDarkMode());

  readonly darkMode = this.darkModeSignal.asReadonly();

  setDarkMode(enabled: boolean): void {
    this.darkModeSignal.set(enabled);
    try {
      globalThis.localStorage?.setItem(themeStorageKey, enabled ? 'dark' : 'light');
    } catch {
      // Ignore storage failures; the active session should still update.
    }
  }
}

function readInitialDarkMode(): boolean {
  try {
    const saved = globalThis.localStorage?.getItem(themeStorageKey);
    if (saved === 'light') return false;
    if (saved === 'dark') return true;
  } catch {
    return true;
  }

  return true;
}
