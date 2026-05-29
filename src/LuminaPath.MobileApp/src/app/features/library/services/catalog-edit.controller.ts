import { Router } from '@angular/router';
import { AlertController } from '@ionic/angular/standalone';
import { Observable, firstValueFrom } from 'rxjs';
import { capitalize } from 'src/app/shared/utils/format';
import { extractErrorMessage } from 'src/app/shared/utils/extract-error';
import { MediaStore } from '../state/media.store';
import {
  CreateMediaForm,
  emptyCreateForm,
} from '../components/library-create-dialog/library-create-dialog.model';

/**
 * Per-media-type wiring for {@link CatalogEditController}. The controller owns
 * the dialog/saving/deleting flow; these hooks supply the type-specific bits
 * (payload building, form mapping, reload, labels) that genuinely differ
 * between games, movies, and episodic media.
 */
export interface CatalogEditConfig<TEntity> {
  /** Whether the signed-in user may mutate the shared catalog entry. */
  canEdit: () => boolean;
  /** The currently loaded catalog entity (null while loading / not found). */
  entity: () => TEntity | null;
  /** Numeric id of the entity — used to evict the media store on delete. */
  id: (entity: TEntity) => number;
  /** Display name of the entity — used in dialog headers + messages. */
  name: (entity: TEntity) => string;
  /** Whether a personal library entry references this catalog entity. */
  isInLibrary: () => boolean;
  /** Map the loaded entity into a create-dialog form draft for editing. */
  toForm: (entity: TEntity) => CreateMediaForm;
  /** Persist catalog edits. `name` is already trimmed + validated non-empty. */
  saveEdit: (entity: TEntity, name: string, form: CreateMediaForm) => Observable<unknown>;
  /** Delete the shared catalog entity. */
  deleteEntity: (entity: TEntity) => Observable<unknown>;
  /** Reload the page after a successful edit. */
  reload: (entity: TEntity) => Promise<void>;
  /** Surface a delete failure on the host page (e.g. its main error banner). */
  reportError: (message: string) => void;
  labels: {
    /** Lower-case singular noun, e.g. "game", "movie", "anime". */
    singular: string;
    /**
     * Clause spliced after "...including its metadata" in the delete confirm.
     * Defaults to " and cover link"; games pass a longer cascade description.
     */
    deleteCascade?: string;
  };
}

/**
 * Drives the admin/editor "edit catalog entry" dialog and "delete catalog
 * entry" confirmation shared by every media detail page. A plain class (not a
 * service) so each page owns its own instance and the per-type behaviour stays
 * explicit at the call site.
 */
export class CatalogEditController<TEntity> {
  isEditDialogOpen = false;
  isSavingCatalogEdit = false;
  editErrorMessage = '';
  editForm: CreateMediaForm = emptyCreateForm();
  isDeletingCatalogEntry = false;

  constructor(
    private readonly alertController: AlertController,
    private readonly mediaStore: InstanceType<typeof MediaStore>,
    private readonly router: Router,
    private readonly config: CatalogEditConfig<TEntity>,
  ) {}

  openEditDialog(): void {
    const entity = this.config.entity();
    if (!this.config.canEdit() || !entity) return;
    this.editErrorMessage = '';
    this.editForm = this.config.toForm(entity);
    this.isEditDialogOpen = true;
  }

  closeEditDialog(): void {
    if (this.isSavingCatalogEdit) return;
    this.isEditDialogOpen = false;
  }

  async submitCatalogEdit(): Promise<void> {
    const entity = this.config.entity();
    if (!this.config.canEdit() || !entity || this.isSavingCatalogEdit) return;
    const name = this.editForm.name.trim();
    if (!name) {
      this.editErrorMessage = 'Title is required.';
      return;
    }

    this.isSavingCatalogEdit = true;
    this.editErrorMessage = '';
    try {
      await firstValueFrom(this.config.saveEdit(entity, name, this.editForm));
      this.isEditDialogOpen = false;
      await this.config.reload(entity);
    } catch (error) {
      this.editErrorMessage = extractErrorMessage(
        error,
        `${capitalize(this.config.labels.singular)} could not be updated.`,
      );
    } finally {
      this.isSavingCatalogEdit = false;
    }
  }

  async confirmDeleteCatalogEntry(): Promise<void> {
    const entity = this.config.entity();
    if (!this.config.canEdit() || !entity || this.isDeletingCatalogEntry) return;

    const { singular, deleteCascade = ' and cover link' } = this.config.labels;
    const libraryWarning = this.config.isInLibrary()
      ? ' It is currently referenced by your personal library entry.'
      : '';

    const alert = await this.alertController.create({
      header: `Delete "${this.config.name(entity)}"?`,
      message:
        `This removes the shared catalog ${singular}, including its metadata${deleteCascade}.${libraryWarning} This cannot be undone.`,
      cssClass: 'media-confirm-alert',
      buttons: [
        { text: 'Keep', role: 'cancel' },
        {
          text: 'Delete',
          role: 'destructive',
          cssClass: 'media-confirm-alert__destructive',
          handler: () => {
            void this.deleteCatalogEntry(entity);
          },
        },
      ],
    });
    await alert.present();
  }

  private async deleteCatalogEntry(entity: TEntity): Promise<void> {
    if (this.isDeletingCatalogEntry) return;
    this.isDeletingCatalogEntry = true;
    try {
      await firstValueFrom(this.config.deleteEntity(entity));
      this.mediaStore.removeItem(this.config.id(entity));
      void this.router.navigateByUrl('/library');
    } catch (error) {
      this.config.reportError(
        extractErrorMessage(error, `${capitalize(this.config.labels.singular)} could not be deleted.`),
      );
    } finally {
      this.isDeletingCatalogEntry = false;
    }
  }
}
