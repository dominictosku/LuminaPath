import { Injectable } from '@angular/core';

import { LibraryFilterPreset } from '../models/library-filter.model';

const STORAGE_PREFIX = 'luminapath.library.presets.v1';

@Injectable({ providedIn: 'root' })
export class LibraryFilterPresetService {
  load(mediaModeId: string): LibraryFilterPreset[] {
    try {
      const raw = globalThis.localStorage?.getItem(this.storageKey(mediaModeId));
      const parsed = raw ? JSON.parse(raw) : [];
      return Array.isArray(parsed)
        ? parsed.filter((item): item is LibraryFilterPreset => this.isPreset(item))
        : [];
    } catch {
      return [];
    }
  }

  save(mediaModeId: string, presets: LibraryFilterPreset[]): void {
    try {
      globalThis.localStorage?.setItem(this.storageKey(mediaModeId), JSON.stringify(presets));
    } catch {
    }
  }

  private storageKey(mediaModeId: string): string {
    return `${STORAGE_PREFIX}.${mediaModeId}`;
  }

  private isPreset(value: unknown): value is LibraryFilterPreset {
    return !!value
      && typeof value === 'object'
      && 'name' in value
      && typeof (value as { name: unknown }).name === 'string';
  }
}
