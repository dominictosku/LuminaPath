import { computed, effect, inject } from '@angular/core';
import { signalStore, withComputed, withHooks, withMethods, withState, patchState } from '@ngrx/signals';
import {
  setAllEntities,
  removeEntity,
  updateEntity,
  upsertEntity,
  upsertEntities,
  withEntities,
} from '@ngrx/signals/entities';
import { firstValueFrom } from 'rxjs';
import { MediaFilter } from 'src/app/core/entities/mediaFilter';
import { MediaModeService } from 'src/app/shared/services/media-mode.service';
import {
  LibraryEntryDetails,
  MediaItem,
  MediaKind,
  UserMediaEntry,
} from '../models/media-item.model';
import { MediaLibraryFacade } from '../services/media-library.facade';

/**
 * In-memory cache of MediaItems shared between the library page and the
 * per-kind detail pages. Mutations made through this store (or pushed in
 * via setLibraryEntry / upsertItem) propagate to every reader without a
 * refetch.
 *
 * Scope: holds items for ONE media kind at a time (the currently selected
 * media mode). Switching modes clears the cache. Detail-page upserts that
 * cross kinds also clear.
 */

interface MediaListState {
  loadedKind: MediaKind | null;
  isLoading: boolean;
  isMutating: boolean;
  error: string | null;
  page: number;
  totalPages: number;
}

const initialMediaState: MediaListState = {
  loadedKind: null,
  isLoading: false,
  isMutating: false,
  error: null,
  page: 0,
  totalPages: 0,
};

export const MediaStore = signalStore(
  { providedIn: 'root' },
  withState(initialMediaState),
  withEntities<MediaItem>(),
  withComputed((store) => ({
    items: computed(() => store.entities()),
    libraryItems: computed(() => store.entities().filter((item) => item.libraryEntry != null)),
    catalogOnlyItems: computed(() => store.entities().filter((item) => item.libraryEntry == null)),
    hasMorePages: computed(() => store.page() > 0 && store.page() < store.totalPages()),
    isEmpty: computed(() => store.entities().length === 0),
  })),
  withMethods((store) => {
    const facade = inject(MediaLibraryFacade);
    const mediaMode = inject(MediaModeService);

    function ensureKind(kind: MediaKind): void {
      if (store.loadedKind() === kind) return;
      patchState(store, setAllEntities<MediaItem>([]), {
        ...initialMediaState,
        loadedKind: kind,
      });
    }

    function patchEntry(mediaId: number, changes: Partial<MediaItem>): void {
      patchState(store, updateEntity<MediaItem>({ id: mediaId, changes }));
    }

    function extractError(error: unknown): string {
      if (
        error &&
        typeof error === 'object' &&
        'error' in error &&
        typeof (error as { error: unknown }).error === 'string'
      ) {
        return (error as { error: string }).error;
      }
      return 'Operation failed.';
    }

    return {
      // -----------------------------------------------------------------------
      // Library-page operations: mode-bound, route through the facade.
      // -----------------------------------------------------------------------

      /** Replace the cache with the first page of results for the current media mode. */
      async loadCatalog(filter?: MediaFilter): Promise<void> {
        const kind = mediaMode.mode().id;
        ensureKind(kind);
        patchState(store, { isLoading: true, error: null });
        try {
          const page = await firstValueFrom(facade.getAll(filter));
          patchState(store, setAllEntities<MediaItem>(page.data ?? []), {
            isLoading: false,
            page: page.pageIndex ?? 1,
            totalPages: page.totalPages ?? 1,
          });
        } catch {
          patchState(store, {
            isLoading: false,
            error: `${mediaMode.mode().label} could not be loaded.`,
          });
        }
      },

      /** Append the next page. Caller passes a filter with Paging.PageIndex bumped. */
      async loadNextPage(filter: MediaFilter): Promise<void> {
        if (!store.hasMorePages() || store.isLoading()) return;
        patchState(store, { isLoading: true, error: null });
        try {
          const page = await firstValueFrom(facade.getAll(filter));
          patchState(store, upsertEntities<MediaItem>(page.data ?? []), {
            isLoading: false,
            page: page.pageIndex ?? store.page() + 1,
            totalPages: page.totalPages ?? store.totalPages(),
          });
        } catch {
          patchState(store, {
            isLoading: false,
            error: `More ${mediaMode.mode().label.toLowerCase()} could not be loaded.`,
          });
        }
      },

      /** Add the catalog item to the user's library and patch the cached entity. */
      async addToLibrary(mediaId: number, details: LibraryEntryDetails): Promise<UserMediaEntry> {
        patchState(store, { isMutating: true, error: null });
        try {
          const entry = await firstValueFrom(facade.addToLibrary(mediaId, details));
          patchEntry(mediaId, { libraryEntry: entry });
          patchState(store, { isMutating: false });
          return entry;
        } catch (error) {
          patchState(store, { isMutating: false, error: extractError(error) });
          throw error;
        }
      },

      /** Update an existing library entry and patch the cached entity. */
      async updateLibraryEntry(
        libraryEntryId: number,
        mediaId: number,
        details: LibraryEntryDetails,
      ): Promise<UserMediaEntry> {
        patchState(store, { isMutating: true, error: null });
        try {
          const entry = await firstValueFrom(
            facade.updateLibraryEntry(libraryEntryId, mediaId, details),
          );
          patchEntry(mediaId, { libraryEntry: entry });
          patchState(store, { isMutating: false });
          return entry;
        } catch (error) {
          patchState(store, { isMutating: false, error: extractError(error) });
          throw error;
        }
      },

      // -----------------------------------------------------------------------
      // Detail-page cache hooks: no service call, just keep the cache in sync.
      // Use these from per-kind detail pages that own their typed services.
      // -----------------------------------------------------------------------

      /** Upsert a MediaItem. Crossing media kinds clears the cache first. */
      upsertItem(item: MediaItem): void {
        ensureKind(item.kind);
        patchState(store, upsertEntity<MediaItem>(item));
      },

      /** Replace just the libraryEntry on a cached item. No-op if not cached. */
      setLibraryEntry(mediaId: number, entry: UserMediaEntry | null): void {
        patchEntry(mediaId, { libraryEntry: entry });
      },

      /** Drop a cached item entirely. */
      removeItem(mediaId: number): void {
        patchState(store, removeEntity(mediaId));
      },

      /** Synchronous lookup. Returns undefined if the item isn't cached. */
      findById(mediaId: number): MediaItem | undefined {
        return store.entities().find((item) => item.id === mediaId);
      },

      /** Reset the cache and all flags. */
      clear(): void {
        patchState(store, setAllEntities<MediaItem>([]), { ...initialMediaState });
      },
    };
  }),
  withHooks({
    onInit(store) {
      const mediaMode = inject(MediaModeService);
      effect(() => {
        const kind = mediaMode.mode().id;
        const loaded = store.loadedKind();
        if (loaded !== null && loaded !== kind) {
          store.clear();
        }
      });
    },
  }),
);
