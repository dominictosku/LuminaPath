import { ChangeDetectionStrategy, Component, inject, input, model, output, viewChild } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { MediaDialogHeroComponent } from 'src/app/shared/components/media-dialog-hero/media-dialog-hero.component';
import { FormsModule } from '@angular/forms';
import { IonButton, IonIcon, IonModal } from '@ionic/angular/standalone';

import { MediaModeOption } from 'src/app/shared/services/media-mode.service';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';

import { MediaItem } from '../../models/media-item.model';
import { MediaLibraryForm, MediaStatusOption } from '../../models/media-library-form.model';
import { MediaLibraryViewService } from '../../services/media-library-view.service';

/**
 * Modal that handles both "add to library" and "edit my entry" flows for
 * a MediaItem. The page owns the open/close state and the form draft;
 * this component just renders the editor and emits submit/dismiss.
 *
 * Display labels (status, played, remaining, duration, etc.) are reached
 * through the shared MediaLibraryViewService injected directly — same
 * pattern as the library-card / library-list-row sub-components.
 */
@Component({
  selector: 'app-library-add-dialog',
  templateUrl: './library-add-dialog.component.html',
  styleUrls: ['./library-add-dialog.component.scss'],
  imports: [FormsModule, DecimalPipe, IonButton, IonIcon, IonModal, MediaDialogHeroComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LibraryAddDialogComponent {
  readonly view = inject(MediaLibraryViewService);

  readonly isOpen = model<boolean>(false);
  readonly form = model.required<MediaLibraryForm>();

  readonly selectedGame = input<MediaItem | null>(null);
  readonly mediaMode = input.required<MediaModeOption>();
  readonly statusOptions = input<MediaStatusOption[]>([]);
  readonly isGamesMode = input<boolean>(false);
  readonly isEpisodeMode = input<boolean>(false);
  readonly isEditing = input<boolean>(false);
  /** True while the page is sending the add/update request — disables form. */
  readonly isSaving = input<boolean>(false);

  readonly errorMessage = input('');
  private readonly modal = viewChild(IonModal);
  readonly details = output<MediaItem>();

  async openDetails(): Promise<void> {
    const game = this.selectedGame();
    if (!game || this.isSaving()) return;
    const dismissed = await this.modal()?.dismiss();
    if (dismissed) this.details.emit(game);
  }
  readonly dismiss = output<void>();
  readonly canDismiss = () => !this.isSaving();
  readonly submitted = output<void>();

  imageFor(game: MediaItem): string {
    return mediaImageUrl(game.image);
  }

  selectStatus(value: number): void {
    this.form.update((current) => ({ ...current, status: value }));
  }
}
