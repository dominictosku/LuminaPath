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
import { RequestCache } from 'src/app/shared/services/request-cache.service';
import { extractErrorMessage } from 'src/app/shared/utils/extract-error';
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
    const cache = inject(RequestCache);

    /** Library mutations affect both the dashboard's library count + featured
     *  items and the statistic page's owned-item derivations. Expire both. */
    const invalidateDerivedCaches = () => cache.invalidate(/^(home|statistic):/);

    /** Cache key for the persisted list of items for a given media kind.
     *  Lives in RequestCache → IndexedDB so the list paints from cache on
     *  cold launch / when offline. */
    const listCacheKey = (kind: MediaKind) => `library:list:${kind}`;

    function ensureKind(kind: MediaKind): void {
      if (store.loadedKind() === kind) return;
      // Hydrate from cache so cross-page detail navigation (and offline
      // deep-links into /library/<kind>/:id) finds items via findById
      // without waiting for a network refetch.
      const cached = cache.get<MediaItem[]>(listCacheKey(kind));
      patchState(store, setAllEntities<MediaItem>(cached ?? []), {
        ...initialMediaState,
        loadedKind: kind,
      });
    }

    function patchEntry(mediaId: number, changes: Partial<MediaItem>): void {
      patchState(store, updateEntity<MediaItem>({ id: mediaId, changes }));
      persistCurrentList();
    }

    /** Re-save the current list to the persistent cache so that offline
     *  reads see the latest mutations. Cheap: one fire-and-forget IDB
     *  put per call. Only persists when we have a loaded kind. */
    function persistCurrentList(): void {
      const kind = store.loadedKind();
      if (!kind) return;
      cache.set(listCacheKey(kind), store.entities());
    }

    const extractError = (error: unknown) => extractErrorMessage(error, 'Operation failed.');

    return {
      // -----------------------------------------------------------------------
      // Library-page operations: mode-bound, route through the facade.
      // -----------------------------------------------------------------------

      /** Replace the cache with the first page of results for the current media mode. */
      async loadCatalog(filter?: MediaFilter): Promise<void> {
        const kind = mediaMode.mode().id;
        ensureKind(kind);
        // If we already have something painted from the persisted cache,
        // don't flash a skeleton — refetch silently in the background.
        const hadCached = store.entities().length > 0;
        patchState(store, { isLoading: !hadCached, error: null });
        try {
          const page = await firstValueFrom(facade.getAll(filter));
          const items = page.data ?? [];
          patchState(store, setAllEntities<MediaItem>(items), {
            isLoading: false,
            page: page.pageIndex ?? 1,
            totalPages: page.totalPages ?? 1,
          });
          cache.set(listCacheKey(kind), items);
        } catch {
          patchState(store, {
            isLoading: false,
            // Suppress the error message when we have a cached painting
            // to show — typical offline case. The OfflineBanner already
            // tells the user something is wrong.
            error: hadCached ? null : `${mediaMode.mode().label} could not be loaded.`,
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
          persistCurrentList();
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
          invalidateDerivedCaches();
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
          invalidateDerivedCaches();
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
        persistCurrentList();
      },

      /** Replace just the libraryEntry on a cached item. No-op if not cached.
       *  Always invalidates derived caches — the per-kind detail pages call
       *  this after a successful add/update/remove via their own services. */
      setLibraryEntry(mediaId: number, entry: UserMediaEntry | null): void {
        patchEntry(mediaId, { libraryEntry: entry });
        invalidateDerivedCaches();
      },

      /** Drop a cached item entirely. */
      removeItem(mediaId: number): void {
        patchState(store, removeEntity(mediaId));
        persistCurrentList();
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
