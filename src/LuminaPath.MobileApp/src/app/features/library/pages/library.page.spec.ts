import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { LibraryPage } from './library.page';
import { ReleaseNotificationService } from 'src/app/shared/services/release-notification.service';
import { PaginateResult } from 'src/app/core/entities/paginatedResult';
import { MediaLibraryFacade } from '../services/media-library.facade';
import { MediaItem, UserMediaEntry } from '../models/media-item.model';

function makeGame(overrides: Partial<MediaItem> = {}): MediaItem {
  return {
    id: 0,
    name: '',
    description: '',
    releaseDate: null,
    genre: '',
    image: null,
    kind: 'games',
    libraryEntry: null,
    platforms: 0,
    playtime: 0,
    ...overrides,
  };
}

function makeEntry(overrides: Partial<UserMediaEntry> = {}): UserMediaEntry {
  return {
    id: 0,
    rating: null,
    startDate: null,
    endDate: null,
    status: 1,
    timeSpend: 0,
    ...overrides,
  };
}

function pageOf(games: MediaItem[]): PaginateResult<MediaItem> {
  const result = new PaginateResult<MediaItem>();
  result.data = games;
  return result;
}

describe('LibraryPage', () => {
  let component: LibraryPage;
  let fixture: ComponentFixture<LibraryPage>;

  let mediaLibrary: jasmine.SpyObj<MediaLibraryFacade>;
  let releaseNotifications: jasmine.SpyObj<ReleaseNotificationService>;
  let router: jasmine.SpyObj<Router>;

  function configure(getAllResponse: Observable<PaginateResult<MediaItem>>) {
    mediaLibrary = jasmine.createSpyObj<MediaLibraryFacade>('MediaLibraryFacade', [
      'getAll',
      'addToLibrary',
      'updateLibraryEntry',
      'detailsRoute',
    ]);
    releaseNotifications = jasmine.createSpyObj<ReleaseNotificationService>(
      'ReleaseNotificationService',
      ['syncForGames'],
    );
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);

    mediaLibrary.getAll.and.returnValue(getAllResponse);
    mediaLibrary.detailsRoute.and.callFake((item) => ['/library', item.kind, item.id]);
    releaseNotifications.syncForGames.and.resolveTo();
    router.navigate.and.resolveTo(true);

    TestBed.configureTestingModule({
      imports: [LibraryPage],
      providers: [
        { provide: MediaLibraryFacade, useValue: mediaLibrary },
        { provide: ReleaseNotificationService, useValue: releaseNotifications },
        { provide: Router, useValue: router },
      ],
    });

    fixture = TestBed.createComponent(LibraryPage);
    component = fixture.componentInstance;
  }

  it('loads games on init and clears the loading flag', () => {
    const games = [
      makeGame({ id: 1, name: 'Apex Legends' }),
      makeGame({ id: 2, name: 'Baldurs Gate' }),
    ];
    configure(of(pageOf(games)));

    fixture.detectChanges(); // triggers ngOnInit

    expect(mediaLibrary.getAll).toHaveBeenCalledTimes(1);
    expect(component.games.length).toBe(2);
    expect(component.filteredGames.length).toBe(2);
    expect(component.isLoading).toBeFalse();
    expect(component.errorMessage).toBe('');
  });

  it('shows an error message when the games request fails', () => {
    configure(throwError(() => new Error('boom')));

    fixture.detectChanges();

    expect(component.games).toEqual([]);
    expect(component.filteredGames).toEqual([]);
    expect(component.errorMessage).toBe('Games could not be loaded.');
    expect(component.isLoading).toBeFalse();
  });

  it('filters games by ownership and search term', () => {
    const owned = makeGame({
      id: 1,
      name: 'Owned Game',
      libraryEntry: makeEntry({ id: 10, status: 2 }),
    });
    const catalog = makeGame({ id: 2, name: 'Catalog Game' });
    configure(of(pageOf([owned, catalog])));
    fixture.detectChanges();

    component.ownershipFilter = 'mine';
    component.applyFilters();
    expect(component.filteredGames.map((g) => g.id)).toEqual([1]);

    component.ownershipFilter = 'catalog';
    component.applyFilters();
    expect(component.filteredGames.map((g) => g.id)).toEqual([2]);

    component.ownershipFilter = 'all';
    component.searchTerm = 'catalog';
    component.applyFilters();
    expect(component.filteredGames.map((g) => g.id)).toEqual([2]);
  });

  it('submitAddGame calls addToLibrary for a catalog game and updates state on success', fakeAsync(() => {
    const game = makeGame({ id: 7, name: 'Hades' });
    configure(of(pageOf([game])));
    fixture.detectChanges();

    const persistedMyGame = makeEntry({
      id: 99,
      status: 1,
      timeSpend: 5,
    });
    mediaLibrary.addToLibrary.and.returnValue(of(persistedMyGame));

    component.openGameListDialog(game);
    component.addGameForm = {
      status: 1,
      timeSpend: 5,
      rating: null,
      startDate: '',
      endDate: '',
      currentEpisode: null,
    };

    component.submitAddGame();
    tick();

    expect(mediaLibrary.addToLibrary).toHaveBeenCalledOnceWith(7, {
      status: 1,
      timeSpend: 5,
      rating: null,
      startDate: null,
      endDate: null,
      currentEpisode: null,
    });
    expect(mediaLibrary.updateLibraryEntry).not.toHaveBeenCalled();
    expect(game.libraryEntry).toBe(persistedMyGame);
    expect(component.successMessage).toBe('Hades was added to your game list.');
    expect(component.isAddDialogOpen).toBeFalse();
    expect(component.isAdding(game)).toBeFalse();
  }));

  it('submitAddGame routes to updateLibraryEntry when the game is already owned', fakeAsync(() => {
    const existing = makeEntry({ id: 99, status: 1 });
    const game = makeGame({ id: 7, name: 'Hades', libraryEntry: existing });
    configure(of(pageOf([game])));
    fixture.detectChanges();

    const updated = makeEntry({ id: 99, status: 2 });
    mediaLibrary.updateLibraryEntry.and.returnValue(of(updated));

    component.openGameListDialog(game);
    component.addGameForm = {
      status: 2,
      timeSpend: 0,
      rating: null,
      startDate: '',
      endDate: '',
      currentEpisode: null,
    };

    component.submitAddGame();
    tick();

    expect(mediaLibrary.updateLibraryEntry).toHaveBeenCalledOnceWith(99, 7, {
      status: 2,
      timeSpend: 0,
      rating: null,
      startDate: null,
      endDate: null,
      currentEpisode: null,
    });
    expect(mediaLibrary.addToLibrary).not.toHaveBeenCalled();
    expect(game.libraryEntry).toBe(updated);
    expect(component.successMessage).toBe('Hades was saved.');
  }));

  it('submitAddGame surfaces backend duplicate-error messages', fakeAsync(() => {
    const game = makeGame({ id: 7, name: 'Hades' });
    configure(of(pageOf([game])));
    fixture.detectChanges();

    mediaLibrary.addToLibrary.and.returnValue(
      throwError(() => ({ error: 'This is game already added' })),
    );

    component.openGameListDialog(game);
    component.submitAddGame();
    tick();

    expect(component.errorMessage).toBe('This is game already added');
    expect(component.isAdding(game)).toBeFalse();
    // Dialog remains open so the user can correct or close manually.
    expect(component.isAddDialogOpen).toBeTrue();
  }));

  it('submitAddGame ignores re-entry while a request is in-flight', () => {
    const game = makeGame({ id: 7, name: 'Hades' });
    configure(of(pageOf([game])));
    fixture.detectChanges();

    mediaLibrary.addToLibrary.and.returnValue(new Observable<UserMediaEntry>(() => {}));

    component.openGameListDialog(game);
    component.submitAddGame();
    component.submitAddGame();

    expect(mediaLibrary.addToLibrary).toHaveBeenCalledTimes(1);
  });

  it('does not call any mutating service when no game is selected', () => {
    configure(of(pageOf([])));
    fixture.detectChanges();

    component.submitAddGame();

    expect(mediaLibrary.addToLibrary).not.toHaveBeenCalled();
    expect(mediaLibrary.updateLibraryEntry).not.toHaveBeenCalled();
  });

  it('openDetails uses the facade route', () => {
    configure(of(pageOf([])));
    fixture.detectChanges();

    component.openDetails(makeGame({ id: 42, name: 'Hades' }));

    expect(mediaLibrary.detailsRoute).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledOnceWith(['/library', 'games', 42]);
  });
});
