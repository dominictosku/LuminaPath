import { ChangeDetectionStrategy, Component, OnDestroy, effect, input, model, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  IonButton,
  IonIcon,
  IonSearchbar,
  IonSegment,
  IonSegmentButton,
  IonSelect,
  IonSelectOption,
} from '@ionic/angular/standalone';

import {
  LibraryFilterPreset,
  OwnershipFilter,
  ReleaseDateFilter,
  SmartFilter,
  SortMode,
  ViewMode,
} from '../../models/library-filter.model';

type StatusOption = { value: string | number; label: string };
type PlatformOption = { value: string | number; label: string };
type SortOption = { value: SortMode; label: string };

/**
 * Top-of-page filter / sort / preset toolbar. Owns the input event handling
 * and a debounced search mirror; emits a single `apply` whenever any
 * control changes so the page can reload. Preset CRUD bubbles up as
 * dedicated events because the page persists them via service.
 *
 * Search uses a 200 ms local debounce so typing feels instant while the
 * upstream `searchTerm` write — which triggers a reload — only fires once
 * the user pauses.
 */
@Component({
  selector: 'app-library-toolbar',
  templateUrl: './library-toolbar.component.html',
  styleUrls: ['../../pages/library.page.scss'],
  imports: [
    FormsModule,
    IonButton,
    IonIcon,
    IonSearchbar,
    IonSegment,
    IonSegmentButton,
    IonSelect,
    IonSelectOption,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LibraryToolbarComponent implements OnDestroy {
  // Two-way bound filter state — the page is the source of truth, the
  // toolbar mutates these as the user touches controls.
  readonly searchTerm = model<string>('');
  readonly viewMode = model<ViewMode>('grid');
  readonly ownershipFilter = model<OwnershipFilter>('all');
  readonly statusFilter = model<string>('all');
  readonly platformFilter = model<string>('all');
  readonly releaseDateFilter = model<ReleaseDateFilter>('all');
  readonly releaseDateFrom = model<string>('');
  readonly releaseDateTo = model<string>('');
  readonly sortMode = model<SortMode>('title');
  readonly smartFilter = model<SmartFilter>('none');
  readonly presetName = model<string>('');

  // Read-only context the toolbar renders against.
  readonly statusOptions = input<StatusOption[]>([]);
  readonly platforms = input<PlatformOption[]>([]);
  readonly sortOptions = input<SortOption[]>([]);
  readonly savedPresets = input<LibraryFilterPreset[]>([]);
  readonly shortGameCount = input<number>(0);
  readonly abandonedGameCount = input<number>(0);
  readonly hasNextBest = input<boolean>(false);
  readonly isGamesMode = input<boolean>(false);

  /** When true, the admin-only "Create" button is shown next to the search. */
  readonly canCreate = input<boolean>(false);

  readonly apply = output<void>();
  readonly savePreset = output<void>();
  readonly applyPreset = output<LibraryFilterPreset>();
  readonly deletePreset = output<LibraryFilterPreset>();
  readonly create = output<void>();

  /** Local search mirror so the input stays responsive during the debounce. */
  inputValue = '';
  private debounceTimer: number | undefined;
  private static readonly DEBOUNCE_MS = 200;

  constructor() {
    // Mirror parent writes (preset apply, clearFilters) back into the input.
    effect(() => {
      const next = this.searchTerm();
      if (next !== this.inputValue) {
        this.inputValue = next;
      }
    });
  }

  ngOnDestroy(): void {
    if (this.debounceTimer !== undefined) {
      window.clearTimeout(this.debounceTimer);
    }
  }

  onSearchInput(value: string): void {
    this.inputValue = value;
    if (this.debounceTimer !== undefined) {
      window.clearTimeout(this.debounceTimer);
    }
    this.debounceTimer = window.setTimeout(() => {
      this.debounceTimer = undefined;
      this.searchTerm.set(value);
      this.apply.emit();
    }, LibraryToolbarComponent.DEBOUNCE_MS);
  }

  /** Reset the custom-range inputs when the user picks a non-custom preset. */
  onReleaseDateFilterChange(): void {
    if (this.releaseDateFilter() !== 'custom') {
      this.releaseDateFrom.set('');
      this.releaseDateTo.set('');
    }
    this.apply.emit();
  }

  onCustomReleaseDateChange(): void {
    if (this.releaseDateFilter() !== 'custom') return;
    this.apply.emit();
  }

  toggleSmartFilter(target: SmartFilter): void {
    const next = this.smartFilter() === target ? 'none' : target;
    this.smartFilter.set(next);
    // "best" is special: it pre-configures the other filters so the result
    // is meaningful. The page's old setSmartFilter did the same.
    if (next === 'best') {
      this.ownershipFilter.set('mine');
      this.statusFilter.set('all');
      this.platformFilter.set('all');
      this.searchTerm.set('');
      this.sortMode.set('best-finish');
      this.inputValue = '';
    }
    this.apply.emit();
  }

  trackByPreset(_: number, preset: LibraryFilterPreset): string {
    return preset.id;
  }
}
