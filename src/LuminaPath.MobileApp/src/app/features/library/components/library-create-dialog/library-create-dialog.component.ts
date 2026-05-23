import { ChangeDetectionStrategy, Component, computed, inject, input, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { IonButton, IonIcon, IonModal, IonSpinner } from '@ionic/angular/standalone';

import { MediaModeOption } from 'src/app/shared/services/media-mode.service';
import { Platforms } from 'src/app/features/games/models/games.model';
import { FileUploadService } from 'src/app/shared/services/file-upload.service';
import { extractErrorMessage } from 'src/app/shared/utils/extract-error';

import { MediaFile } from '../../models/mediaFile.model';
import { MediaStatusOption } from '../../models/media-library-form.model';
import { CreateMediaForm } from './library-create-dialog.model';

/**
 * Admin-only modal for creating a new catalog entry (game, anime, series).
 * The page owns the open/close state and the form draft via `model()` so
 * this component stays mostly presentational. Submit just emits — the
 * page is responsible for POSTing the right entity and (optionally) the
 * matching personal library entry when `createLibraryEntry` is enabled.
 *
 * Cover upload is handled inline because it's a self-contained workflow:
 * the dialog calls `/api/files`, drops the returned metadata onto the
 * form's `cover` field, and the page picks it up on submit. A local
 * object URL drives the preview so the user sees the picked image before
 * the upload completes.
 *
 * Styling is fully self-contained — no inheritance from library.page.scss
 * — so the dialog matches Ionic's surface palette in both light and dark
 * themes.
 */
@Component({
  selector: 'app-library-create-dialog',
  templateUrl: './library-create-dialog.component.html',
  styleUrls: ['./library-create-dialog.component.scss'],
  imports: [FormsModule, IonButton, IonIcon, IonModal, IonSpinner],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LibraryCreateDialogComponent {
  private readonly fileUpload = inject(FileUploadService);

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

  /** Local upload state — kept out of the form model so resets don't lose it mid-flight. */
  readonly isUploadingCover = signal(false);
  readonly coverUploadError = signal<string>('');

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

  /**
   * File-input change handler. Shows a local preview immediately, then
   * uploads to `/api/files`. The returned blob info gets stored on
   * `form.cover` so the page can attach it to the new entity on POST.
   */
  async onCoverPicked(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.coverUploadError.set('');
    // Local preview via object URL — replaces any previous one to avoid leaks.
    this.revokePreview();
    const previewUrl = URL.createObjectURL(file);
    this.form.update((current) => ({ ...current, coverPreviewUrl: previewUrl }));

    this.isUploadingCover.set(true);
    try {
      const uploaded = await firstValueFrom(this.fileUpload.upload(file));
      const cover: MediaFile = {
        id: 0,
        name: uploaded.name,
        storageName: uploaded.storageName,
        contentType: uploaded.contentType,
        uri: null,
        url: uploaded.url,
      };
      this.form.update((current) => ({ ...current, cover }));
    } catch (error) {
      this.coverUploadError.set(
        extractErrorMessage(error, 'Cover upload failed. Try again.'),
      );
      // Drop the failed preview so the user knows to retry.
      this.revokePreview();
      this.form.update((current) => ({ ...current, coverPreviewUrl: null, cover: null }));
    } finally {
      this.isUploadingCover.set(false);
      // Clear the input so picking the same file twice still fires `change`.
      input.value = '';
    }
  }

  removeCover(): void {
    this.revokePreview();
    this.coverUploadError.set('');
    this.form.update((current) => ({ ...current, cover: null, coverPreviewUrl: null }));
  }

  /** Effective preview source — picked file URL first, then the persisted URL. */
  readonly coverDisplayUrl = computed(() => {
    const draft = this.form();
    return draft.coverPreviewUrl ?? draft.cover?.url ?? null;
  });

  /** Releases the picked-file object URL if one is active. */
  private revokePreview(): void {
    const draft = this.form();
    if (draft.coverPreviewUrl) {
      URL.revokeObjectURL(draft.coverPreviewUrl);
    }
  }
}
