import { Component, OnDestroy, effect, input, model, output } from '@angular/core';
import { IonBadge, IonIcon, IonLabel, IonSegment, IonSegmentButton } from '@ionic/angular/standalone';

type QuestFilter = 'today' | 'upcoming' | 'inbox' | 'all';

type FilterOption = {
  value: QuestFilter;
  label: string;
  icon: string;
};

export type QuestViewMode = 'cards' | 'compact';

/**
 * Filter segment + search input + tag chips. Owns no business state itself —
 * the active filter and search query are two-way bound from the parent, and tag
 * selection is delegated through an output so the page can store it.
 *
 * Typing in the search box updates the upstream `searchQuery` model on a
 * 200 ms debounce. The visible `<input>` mirrors a local field that updates
 * synchronously so typing feels instant; the model write — which triggers the
 * page's quest filtering / sectioning recompute — only fires once the user
 * pauses. Parent-side writes (e.g. clearing via the searchClear output) are
 * mirrored back into the local field via an `effect()`.
 */
@Component({
  selector: 'app-quest-board-toolbar',
  templateUrl: './quest-board-toolbar.component.html',
  imports: [IonBadge, IonIcon, IonLabel, IonSegment, IonSegmentButton],
})
export class QuestBoardToolbarComponent implements OnDestroy {
  readonly filterOptions = input<FilterOption[]>([]);
  readonly filter = input<QuestFilter>('today');
  readonly searchQuery = model<string>('');
  readonly availableTags = input<string[]>([]);
  readonly tagFilter = input<string | null>(null);
  readonly filterCount = input<(filter: QuestFilter) => number>(() => 0);

  readonly viewMode = input<QuestViewMode>('cards');

  readonly filterChange = output<QuestFilter>();
  readonly tagToggle = output<string>();
  readonly tagClear = output<void>();
  readonly searchClear = output<void>();
  readonly viewModeChange = output<QuestViewMode>();

  /** Local mirror of the input so typing is responsive even while the upstream
   *  debounced write is still pending. */
  inputValue = '';
  private debounceTimer: number | undefined;
  private static readonly DEBOUNCE_MS = 200;

  constructor() {
    // Mirror parent writes (e.g. clearSearch, preset) back into the input.
    effect(() => {
      const next = this.searchQuery();
      if (next !== this.inputValue) {
        this.inputValue = next;
      }
    });
  }

  onInput(value: string): void {
    this.inputValue = value;
    if (this.debounceTimer !== undefined) {
      window.clearTimeout(this.debounceTimer);
    }
    this.debounceTimer = window.setTimeout(() => {
      this.debounceTimer = undefined;
      this.searchQuery.set(value);
    }, QuestBoardToolbarComponent.DEBOUNCE_MS);
  }

  ngOnDestroy(): void {
    if (this.debounceTimer !== undefined) {
      window.clearTimeout(this.debounceTimer);
    }
  }

  onSegmentChange(event: Event): void {
    const value = (event as CustomEvent<{ value: string }>).detail.value;
    if (value === 'today' || value === 'upcoming' || value === 'inbox' || value === 'all') {
      this.filterChange.emit(value);
    }
  }

  trackByText(_: number, item: string): string {
    return item;
  }
}
