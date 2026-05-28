import { Injectable, Signal, inject, signal } from '@angular/core';
import { AlertController } from '@ionic/angular/standalone';

import { MediaModeService } from 'src/app/shared/services/media-mode.service';
import { extractErrorMessage } from 'src/app/shared/utils/extract-error';
import { serializeDate } from 'src/app/shared/utils/date-helpers';

import { MediaStore } from '../state/media.store';
import { MediaLibraryViewService } from './media-library-view.service';
import { LibraryEntryDetails, MediaItem, UserMediaEntry } from '../models/media-item.model';
import { GameStatus } from '../models/library-status.model';
import {
  bulkSelectionSummary,
  pruneSelectedMediaIds,
  selectVisibleLibraryItemIds,
  selectableLibraryItems,
  selectedLibraryItems,
  toggleSelectedMediaId,
} from '../domain/library-selection';

interface BulkActionsConfig {
  /** Source of the currently filtered/visible items (the page's signal). */
  filteredGames: Signal<MediaItem[]>;
  onSuccess: (message: string) => void;
  onError: (message: string) => void;
}

/**
 * Multi-select mode for the library list: selection set, "select visible",
 * and bulk status-update / removal against {@link MediaStore}. The full item
 * set is read straight from the store; the page injects the filtered signal
 * and success/error reporters via {@link configure}. Provided at page level.
 */
@Injectable()
export class LibraryBulkActions {
  private readonly mediaStore = inject(MediaStore);
  private readonly alertController = inject(AlertController);
  private readonly mediaModeService = inject(MediaModeService);
  private readonly mediaView = inject(MediaLibraryViewService);

  selectionMode = false;
  selectedItemIds = new Set<number>();
  bulkStatusValue: number = GameStatus.Playing;
  isBulkMutating = false;

  private filteredGames: Signal<MediaItem[]> = signal<MediaItem[]>([]);
  private onSuccess: (message: string) => void = () => {};
  private onError: (message: string) => void = () => {};

  configure(config: BulkActionsConfig): void {
    this.filteredGames = config.filteredGames;
    this.onSuccess = config.onSuccess;
    this.onError = config.onError;
  }

  private get mediaMode() {
    return this.mediaModeService.mode();
  }

  private get games(): MediaItem[] {
    return this.mediaStore.items();
  }

  get visibleLibraryItems(): MediaItem[] {
    return selectableLibraryItems(this.filteredGames());
  }

  get selectedLibraryItems(): MediaItem[] {
    return selectedLibraryItems(this.games, this.selectedItemIds);
  }

  get selectedCount(): number {
    return this.selectedLibraryItems.length;
  }

  get selectionSummary(): string {
    return bulkSelectionSummary(
      this.visibleLibraryItems.length,
      this.selectedCount,
      this.mediaMode.singular,
      this.mediaMode.label.toLowerCase(),
    );
  }

  toggleSelectionMode(): void {
    this.selectionMode = !this.selectionMode;
    if (!this.selectionMode) {
      this.clearSelection();
    }
  }

  toggleItemSelection(item: MediaItem): void {
    this.selectedItemIds = toggleSelectedMediaId(this.selectedItemIds, item);
  }

  selectVisible(): void {
    this.selectedItemIds = selectVisibleLibraryItemIds(this.selectedItemIds, this.filteredGames());
  }

  clearSelection(): void {
    this.selectedItemIds = new Set<number>();
  }

  isSelected(item: MediaItem): boolean {
    return this.selectedItemIds.has(item.id);
  }

  isSelectable(item: MediaItem): boolean {
    return !!item.libraryEntry;
  }

  setBulkStatus(value: string | number): void {
    const next = Number(value);
    if (Number.isFinite(next)) {
      this.bulkStatusValue = next;
    }
  }

  /** Drop selected ids that are no longer present after a data refresh. */
  prune(): void {
    if (!this.selectedItemIds.size) return;
    this.selectedItemIds = pruneSelectedMediaIds(this.selectedItemIds, this.games);
  }

  /** Reset selection + default bulk status when the media mode changes. */
  resetForMode(): void {
    this.selectionMode = false;
    this.clearSelection();
    this.bulkStatusValue = this.mediaView.statusOptions(this.mediaMode)[0]?.value ?? GameStatus.Planned;
  }

  async bulkUpdateStatus(): Promise<void> {
    const selected = this.selectedLibraryItems;
    if (!selected.length || this.isBulkMutating) return;

    this.isBulkMutating = true;
    try {
      for (const item of selected) {
        const entry = item.libraryEntry;
        if (!entry) continue;
        await this.mediaStore.updateLibraryEntry(entry.id, item.id, this.detailsWithStatus(entry, this.bulkStatusValue));
      }
      this.onSuccess(this.bulkSuccessMessage(selected.length, 'updated'));
      this.clearSelection();
    } catch (error) {
      this.onError(
        extractErrorMessage(error, `Selected ${this.mediaMode.label.toLowerCase()} could not be updated.`),
      );
    } finally {
      this.isBulkMutating = false;
    }
  }

  async confirmBulkRemove(): Promise<void> {
    const selected = this.selectedLibraryItems;
    if (!selected.length || this.isBulkMutating) return;

    const count = selected.length;
    const alert = await this.alertController.create({
      header: `Remove ${count} ${count === 1 ? this.mediaMode.singular : this.mediaMode.label.toLowerCase()}?`,
      message: `This removes the selected ${this.mediaMode.label.toLowerCase()} from your personal library. Catalog entries stay available.`,
      cssClass: 'media-confirm-alert',
      buttons: [
        { text: 'Keep', role: 'cancel' },
        {
          text: 'Remove',
          role: 'destructive',
          cssClass: 'media-confirm-alert__destructive',
          handler: () => {
            void this.bulkRemoveFromLibrary();
          },
        },
      ],
    });
    await alert.present();
  }

  async bulkRemoveFromLibrary(): Promise<void> {
    const selected = this.selectedLibraryItems;
    if (!selected.length || this.isBulkMutating) return;

    this.isBulkMutating = true;
    try {
      for (const item of selected) {
        const entry = item.libraryEntry;
        if (entry) {
          await this.mediaStore.removeFromLibrary(entry.id, item.id);
        }
      }
      this.onSuccess(this.bulkSuccessMessage(selected.length, 'removed from your library'));
      this.clearSelection();
    } catch (error) {
      this.onError(
        extractErrorMessage(error, `Selected ${this.mediaMode.label.toLowerCase()} could not be removed.`),
      );
    } finally {
      this.isBulkMutating = false;
    }
  }

  private detailsWithStatus(entry: UserMediaEntry, status: number): LibraryEntryDetails {
    return {
      status,
      timeSpend: entry.timeSpend ?? null,
      rating: entry.rating ?? null,
      startDate: serializeDate(entry.startDate),
      endDate: serializeDate(entry.endDate),
      personalNotes: entry.personalNotes ?? null,
      currentEpisode: this.mediaView.isEpisodeMode(this.mediaMode) ? entry.currentEpisode ?? null : null,
    };
  }

  private bulkSuccessMessage(count: number, action: string): string {
    const subject = count === 1 ? this.mediaMode.singular : this.mediaMode.label.toLowerCase();
    return `${count} ${subject} ${action}.`;
  }
}
