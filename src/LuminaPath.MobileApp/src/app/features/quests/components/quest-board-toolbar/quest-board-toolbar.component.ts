import { Component, input, model, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IonBadge, IonIcon, IonLabel, IonSegment, IonSegmentButton } from '@ionic/angular/standalone';

type QuestFilter = 'today' | 'upcoming' | 'inbox' | 'all';

type FilterOption = {
  value: QuestFilter;
  label: string;
  icon: string;
};

/**
 * Filter segment + search input + tag chips. Owns no business state itself —
 * the active filter and search query are two-way bound from the parent, and tag
 * selection is delegated through an output so the page can store it.
 */
@Component({
  selector: 'app-quest-board-toolbar',
  templateUrl: './quest-board-toolbar.component.html',
  imports: [FormsModule, IonBadge, IonIcon, IonLabel, IonSegment, IonSegmentButton],
})
export class QuestBoardToolbarComponent {
  readonly filterOptions = input<FilterOption[]>([]);
  readonly filter = input<QuestFilter>('today');
  readonly searchQuery = model<string>('');
  readonly availableTags = input<string[]>([]);
  readonly tagFilter = input<string | null>(null);
  readonly filterCount = input<(filter: QuestFilter) => number>(() => 0);

  readonly filterChange = output<QuestFilter>();
  readonly tagToggle = output<string>();
  readonly tagClear = output<void>();
  readonly searchClear = output<void>();

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
