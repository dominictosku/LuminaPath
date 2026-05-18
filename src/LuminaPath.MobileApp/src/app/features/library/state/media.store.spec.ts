import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { MediaStore } from './media.store';
import { MediaLibraryFacade } from '../services/media-library.facade';
import { MediaModeService } from 'src/app/shared/services/media-mode.service';
import { PaginateResult } from 'src/app/core/entities/paginatedResult';
import { MediaFilter } from 'src/app/core/entities/mediaFilter';
import { MediaItem, UserMediaEntry } from '../models/media-item.model';

function makeItem(overrides: Partial<MediaItem> = {}): MediaItem {
  return {
    id: 1,
    name: 'Item',
    description: '',
    releaseDate: null,
    genre: '',
    image: null,
    kind: 'games',
    libraryEntry: null,
    ...overrides,
  };
}

function makeEntry(overrides: Partial<UserMediaEntry> = {}): UserMediaEntry {
  return {
    id: 100,
    rating: null,
    startDate: null,
    endDate: null,
    status: 1,
    timeSpend: null,
    ...overrides,
  };
}

function pageOf(items: MediaItem[], pageIndex = 1, totalPages = 1): PaginateResult<MediaItem> {
  const result = new PaginateResult<MediaItem>();
  result.data = items;
  result.pageIndex = pageIndex;
  result.totalPages = totalPages;
  return result;
}

function filterForPage(pageIndex: number): MediaFilter {
  const filter = new MediaFilter();
  filter.setPageIndex(pageIndex);
  return filter;
}

describe('MediaStore', () => {
  let store: InstanceType<typeof MediaStore>;
  let facade: jasmine.SpyObj<MediaLibraryFacade>;
  let mediaMode: MediaModeService;

  beforeEach(() => {
    localStorage.clear();
    facade = jasmine.createSpyObj<MediaLibraryFacade>('MediaLibraryFacade', [
      'getAll',
      'addToLibrary',
      'updateLibraryEntry',
      'detailsRoute',
    ]);
    TestBed.configureTestingModule({
      providers: [{ provide: MediaLibraryFacade, useValue: facade }],
    });
    store = TestBed.inject(MediaStore);
    mediaMode = TestBed.inject(MediaModeService);
  });

  describe('loadCatalog', () => {
    it('populates entities and pagination from the first page', async () => {
      facade.getAll.and.returnValue(of(pageOf([makeItem({ id: 1 }), makeItem({ id: 2 })], 1, 3)));

      await store.loadCatalog();

      expect(store.items().map((item) => item.id)).toEqual([1, 2]);
      expect(store.page()).toBe(1);
      expect(store.totalPages()).toBe(3);
      expect(store.isLoading()).toBeFalse();
      expect(store.loadedKind()).toBe('games');
    });

    it('replaces entities on a subsequent call', async () => {
      facade.getAll.and.returnValue(of(pageOf([makeItem({ id: 1 })])));
      await store.loadCatalog();
      facade.getAll.and.returnValue(of(pageOf([makeItem({ id: 2 })])));
      await store.loadCatalog();

      expect(store.items().map((item) => item.id)).toEqual([2]);
    });

    it('records an error and stops loading when the facade fails', async () => {
      facade.getAll.and.returnValue(throwError(() => new Error('boom')));

      await store.loadCatalog();

      expect(store.error()).toBe('Games could not be loaded.');
      expect(store.isLoading()).toBeFalse();
    });
  });

  describe('loadNextPage', () => {
    it('appends new entities to the cache', async () => {
      facade.getAll.and.returnValue(of(pageOf([makeItem({ id: 1 })], 1, 2)));
      await store.loadCatalog();
      facade.getAll.and.returnValue(of(pageOf([makeItem({ id: 2 })], 2, 2)));

      await store.loadNextPage(filterForPage(2));

      expect(store.items().map((item) => item.id)).toEqual([1, 2]);
      expect(store.page()).toBe(2);
    });

    it('is a no-op when there are no more pages', async () => {
      facade.getAll.and.returnValue(of(pageOf([makeItem({ id: 1 })], 1, 1)));
      await store.loadCatalog();
      facade.getAll.calls.reset();

      await store.loadNextPage(filterForPage(2));

      expect(facade.getAll).not.toHaveBeenCalled();
    });
  });

  describe('addToLibrary', () => {
    beforeEach(async () => {
      facade.getAll.and.returnValue(of(pageOf([makeItem({ id: 1 })])));
      await store.loadCatalog();
    });

    it('patches the cached entity with the returned libraryEntry', async () => {
      const persisted = makeEntry({ id: 200, status: 2 });
      facade.addToLibrary.and.returnValue(of(persisted));

      await store.addToLibrary(1, { status: 2, timeSpend: null });

      expect(store.items()[0].libraryEntry).toBe(persisted);
      expect(store.libraryItems().length).toBe(1);
      expect(store.isMutating()).toBeFalse();
    });

    it('records an error and rethrows on failure', async () => {
      facade.addToLibrary.and.returnValue(throwError(() => ({ error: 'duplicate' })));

      await expectAsync(store.addToLibrary(1, { status: 1, timeSpend: null })).toBeRejected();

      expect(store.error()).toBe('duplicate');
      expect(store.isMutating()).toBeFalse();
      expect(store.items()[0].libraryEntry).toBeNull();
    });
  });

  describe('updateLibraryEntry', () => {
    beforeEach(async () => {
      facade.getAll.and.returnValue(
        of(pageOf([makeItem({ id: 1, libraryEntry: makeEntry({ id: 100, status: 1 }) })])),
      );
      await store.loadCatalog();
    });

    it('patches the cached entity with the updated libraryEntry', async () => {
      const persisted = makeEntry({ id: 100, status: 3 });
      facade.updateLibraryEntry.and.returnValue(of(persisted));

      await store.updateLibraryEntry(100, 1, { status: 3, timeSpend: null });

      expect(store.items()[0].libraryEntry).toBe(persisted);
      expect(store.items()[0].libraryEntry?.status).toBe(3);
    });
  });

  describe('upsertItem', () => {
    it('adds new items when the cache is empty', () => {
      store.upsertItem(makeItem({ id: 5, name: 'Five' }));

      expect(store.items().map((item) => item.id)).toEqual([5]);
      expect(store.loadedKind()).toBe('games');
    });

    it('replaces existing items by id', () => {
      store.upsertItem(makeItem({ id: 5, name: 'old' }));
      store.upsertItem(makeItem({ id: 5, name: 'new' }));

      expect(store.items()).toEqual([jasmine.objectContaining({ id: 5, name: 'new' })]);
    });

    it('clears the cache when the kind changes', () => {
      store.upsertItem(makeItem({ id: 1, kind: 'games' }));
      store.upsertItem(makeItem({ id: 2, kind: 'animes' }));

      expect(store.items().map((item) => item.id)).toEqual([2]);
      expect(store.loadedKind()).toBe('animes');
    });
  });

  describe('setLibraryEntry', () => {
    it('patches only the libraryEntry on a cached item', () => {
      store.upsertItem(makeItem({ id: 5, name: 'Five' }));
      const entry = makeEntry({ id: 50 });

      store.setLibraryEntry(5, entry);

      expect(store.items()[0].libraryEntry).toBe(entry);
      expect(store.items()[0].name).toBe('Five');
    });

    it('is a no-op when the item is not cached', () => {
      store.setLibraryEntry(99, makeEntry({ id: 1 }));

      expect(store.items()).toEqual([]);
    });
  });

  describe('removeItem and findById', () => {
    it('removeItem drops the entity', () => {
      store.upsertItem(makeItem({ id: 5 }));
      store.upsertItem(makeItem({ id: 6 }));

      store.removeItem(5);

      expect(store.items().map((item) => item.id)).toEqual([6]);
    });

    it('findById returns the cached item or undefined', () => {
      store.upsertItem(makeItem({ id: 5, name: 'Five' }));

      expect(store.findById(5)?.name).toBe('Five');
      expect(store.findById(999)).toBeUndefined();
    });
  });

  describe('clear', () => {
    it('resets entities and state flags', async () => {
      facade.getAll.and.returnValue(of(pageOf([makeItem({ id: 1 })], 1, 3)));
      await store.loadCatalog();

      store.clear();

      expect(store.items()).toEqual([]);
      expect(store.loadedKind()).toBeNull();
      expect(store.page()).toBe(0);
      expect(store.totalPages()).toBe(0);
      expect(store.error()).toBeNull();
    });
  });

  describe('mode switching', () => {
    it('clears the cache when MediaModeService changes mode', fakeAsync(() => {
      facade.getAll.and.returnValue(of(pageOf([makeItem({ id: 1 })])));
      void store.loadCatalog();
      tick();
      expect(store.items().length).toBe(1);
      expect(store.loadedKind()).toBe('games');

      mediaMode.select('animes');
      TestBed.flushEffects();

      expect(store.items()).toEqual([]);
      expect(store.loadedKind()).toBeNull();
    }));
  });

  describe('computed views', () => {
    it('libraryItems returns only items with a libraryEntry', () => {
      store.upsertItem(makeItem({ id: 1, libraryEntry: makeEntry({ id: 10 }) }));
      store.upsertItem(makeItem({ id: 2, libraryEntry: null }));

      expect(store.libraryItems().map((item) => item.id)).toEqual([1]);
      expect(store.catalogOnlyItems().map((item) => item.id)).toEqual([2]);
    });

    it('hasMorePages reflects pagination state', async () => {
      facade.getAll.and.returnValue(of(pageOf([makeItem({ id: 1 })], 1, 3)));
      await store.loadCatalog();
      expect(store.hasMorePages()).toBeTrue();

      facade.getAll.and.returnValue(of(pageOf([makeItem({ id: 1 })], 1, 1)));
      await store.loadCatalog();
      expect(store.hasMorePages()).toBeFalse();
    });

    it('isEmpty tracks whether the entity collection has any items', () => {
      expect(store.isEmpty()).toBeTrue();
      store.upsertItem(makeItem({ id: 1 }));
      expect(store.isEmpty()).toBeFalse();
    });
  });
});
