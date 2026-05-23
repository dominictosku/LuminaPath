import { ChangeDetectionStrategy, Component, computed, input, model, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IonButton, IonIcon, IonModal } from '@ionic/angular/standalone';

import { MediaModeOption } from 'src/app/shared/services/media-mode.service';
import { Platforms } from 'src/app/features/games/models/games.model';

import { MediaStatusOption } from '../../models/media-library-form.model';
import { CreateMediaForm } from './library-create-dialog.model';

/**
 * Admin-only modal for creating a new catalog entry (game, anime, series).
 * The page owns the open/close state and the form draft via `model()` so
 * this component stays presentational. Submit just emits — the page is
 * responsible for POSTing the right entity and (optionally) the matching
 * personal library entry when `createLibraryEntry` is enabled.
 *
 * The fields shown adapt to the active media mode:
 *   - games  → platforms (bitmask) + playtime hours
 *   - animes / series → episode count + per-episode + total minutes
 *
 * The bottom "Also add to my library" toggle reveals the same status /
 * rating / dates fields the existing add-to-library dialog uses.
 */
@Component({
  selector: 'app-library-create-dialog',
  templateUrl: './library-create-dialog.component.html',
  styleUrls: ['../../pages/library.page.scss', './library-create-dialog.component.scss'],
  imports: [FormsModule, IonButton, IonIcon, IonModal],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LibraryCreateDialogComponent {
  readonly isOpen = model<boolean>(false);
  readonly form = model.required<CreateMediaForm>();

  readonly mediaMode = input.required<MediaModeOption>();
  readonly statusOptions = input<MediaStatusOption[]>([]);
  readonly isSaving = input<boolean>(false);
  readonly errorMessage = input<string>('');

  readonly dismiss = output<void>();
  readonly submit = output<void>();

  /** All available platforms — used to render the bitmask multi-select. */
  readonly platformChoices = Platforms;

  readonly isGamesMode = computed(() => this.mediaMode().id === 'games');
  readonly isEpisodeMode = computed(() => {
    const id = this.mediaMode().id;
    return id === 'animes' || id === 'series';
  });

  readonly canSubmit = computed(() => this.form().name.trim().length > 0);

  togglePlatform(value: number): void {
    this.form.update((current) => ({
      ...current,
      platforms: (current.platforms & value) === value
        ? current.platforms & ~value
        : current.platforms | value,
    }));
  }

  isPlatformSelected(value: number): boolean {
    return (this.form().platforms & value) === value;
  }

  selectStatus(value: number): void {
    this.form.update((current) => ({
      ...current,
      libraryEntry: { ...current.libraryEntry, status: value },
    }));
  }

  setCreateLibraryEntry(value: boolean): void {
    this.form.update((current) => ({ ...current, createLibraryEntry: value }));
  }

  trackByValue(_: number, option: { value: number }): number {
    return option.value;
  }
}
