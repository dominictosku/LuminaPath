import { Signal, computed, signal } from '@angular/core';

import { MediaItem } from '../models/media-item.model';
import { computeLibraryListWindow } from '../domain/library-virtual-list';

/**
 * Windowing state for the single-column list view: it tracks scroll position,
 * viewport height, and the list's offset inside the scroll container, and
 * derives the slice of items to render (plus the spacer height).
 *
 * Plain class (not a service): it's seeded with the page's `filteredGames`
 * signal and owns only reactive state — the DOM wiring (scroll/resize events,
 * anchor measurement) stays in the page where the element refs live.
 */
export class LibraryVirtualScroll {
  private readonly scrollTop = signal(0);
  // Sensible non-zero starting value so the *first* paint shows a real window
  // of rows instead of waiting for a resize event.
  private readonly viewportHeight = signal(typeof window !== 'undefined' ? window.innerHeight : 800);
  private readonly offsetTop = signal(0);

  /** Visible-row bounds + total spacer height for the current scroll state. */
  readonly window = computed(() =>
    computeLibraryListWindow({
      totalItems: this.items().length,
      scrollTop: this.scrollTop(),
      viewportHeight: this.viewportHeight(),
      offsetTop: this.offsetTop(),
    }),
  );

  /** The items inside the current window — the only rows actually rendered. */
  readonly visibleItems = computed(() => this.items().slice(this.window().start, this.window().end));

  constructor(private readonly items: Signal<MediaItem[]>) {}

  onScroll(scrollTop: number | undefined): void {
    if (typeof scrollTop === 'number') {
      this.scrollTop.set(scrollTop);
    }
  }

  syncViewportHeight(): void {
    if (typeof window !== 'undefined') {
      this.viewportHeight.set(window.innerHeight);
    }
  }

  setOffsetTop(value: number): void {
    this.offsetTop.set(value);
  }
}
