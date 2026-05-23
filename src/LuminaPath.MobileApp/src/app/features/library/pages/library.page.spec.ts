import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideIonicAngular } from '@ionic/angular/standalone';
import { Observable, of, throwError } from 'rxjs';
import { signal } from '@angular/core';

import { LibraryPage } from './library.page';
import { ReleaseNotificationService } from 'src/app/shared/services/release-notification.service';
import { PaginateResult } from 'src/app/core/entities/paginatedResult';
import { MediaLibraryFacade } from '../services/media-library.facade';
import { MediaItem, UserMediaEntry } from '../models/media-item.model';
import { GameStatus } from '../models/library-status.model';
import { GameService } from 'src/app/features/games/services/game.service';
import { AnimeService } from 'src/app/features/animes/services/anime.service';
import { SeriesService } from 'src/app/features/series/services/series.service';
import { AuthService } from 'src/app/core/auth/services/auth.service';
import { Game } from 'src/app/features/games/models/games.model';
import { Anime } from 'src/app/features/animes/models/animes.model';
import { Series } from 'src/app/features/series/models/series.model';

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

function pageOf(games: MediaItem[], pageIndex = 1, totalPages = 1): PaginateResult<MediaItem> {
  const result = new PaginateResult<MediaItem>();
  result.data = games;
  result.pageIndex = pageIndex;
  result.totalPages = totalPages;
  return result;
}

describe('LibraryPage', () => {
  let component: LibraryPage;
  let fixture: ComponentFixture<LibraryPage>;

  let mediaLibrary: jasmine.SpyObj<MediaLibraryFacade>;
  let releaseNotifications: jasmine.SpyObj<ReleaseNotificationService>;
  let router: jasmine.SpyObj<Router>;
  let gameService: jasmine.SpyObj<GameService>;
  let animeService: jasmine.SpyObj<AnimeService>;
  let seriesService: jasmine.SpyObj<SeriesService>;
  let authIsAdmin: ReturnType<typeof signal<boolean>>;

  beforeEach(() => {
    localStorage.clear();
  });

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

    // Catalog services for the admin create flow — stubs by default so the
    // page can construct; individual tests opt in to behavior they need.
    gameService = jasmine.createSpyObj<GameService>('GameService', ['post']);
    animeService = jasmine.createSpyObj<AnimeService>('AnimeService', ['post']);
    seriesService = jasmine.createSpyObj<SeriesService>('SeriesService', ['post']);
    gameService.post.and.returnValue(of(new Game()));
    animeService.post.and.returnValue(of(new Anime()));
    seriesService.post.and.returnValue(of(new Series()));

    // Default: not an admin. Tests that need the create button flip this
    // before the relevant assertions.
    authIsAdmin = signal(false);
    const authStub = {
      isAdmin: authIsAdmin.asReadonly(),
      isAuthenticated: signal(true).asReadonly(),
      roles: signal<readonly string[]>([]).asReadonly(),
      canEditCatalog: signal(false).asReadonly(),
    } as unknown as AuthService;

    TestBed.configureTestingModule({
      imports: [LibraryPage],
      providers: [
        provideIonicAngular(),
        { provide: MediaLibraryFacade, useValue: mediaLibrary },
        { provide: ReleaseNotificationService, useValue: releaseNotifications },
        { provide: Router, useValue: router },
        { provide: GameService, useValue: gameService },
        { provide: AnimeService, useValue: animeService },
        { provide: SeriesService, useValue: seriesService },
        { provide: AuthService, useValue: authStub },
      ],
    });
    TestBed.overrideComponent(LibraryPage, { set: { template: '' } });

    fixture = TestBed.createComponent(LibraryPage);
    component = fixture.componentInstance;
  }

  it('loads games on init and clears the loading flag', fakeAsync(() => {
    const games = [
      makeGame({ id: 1, name: 'Apex Legends' }),
      makeGame({ id: 2, name: 'Baldurs Gate' }),
    ];
    configure(of(pageOf(games)));

    fixture.detectChanges(); // triggers ngOnInit
    tick();

    expect(mediaLibrary.getAll).toHaveBeenCalledTimes(1);
    expect(component.games.length).toBe(2);
    expect(component.filteredGames.length).toBe(2);
    expect(component.isLoading).toBeFalse();
    expect(component.errorMessage).toBe('');
  }));

  it('loads and appends the next page when infinite scroll fires', fakeAsync(() => {
    const pageOne = [makeGame({ id: 1, name: 'Apex Legends' })];
    const pageTwo = [makeGame({ id: 2, name: 'Baldurs Gate' })];
    configure(of(pageOf(pageOne, 1, 2)));
    mediaLibrary.getAll.and.returnValues(
      of(pageOf(pageOne, 1, 2)),
      of(pageOf(pageTwo, 2, 2)),
    );
    fixture.detectChanges();
    tick();
    TestBed.flushEffects();

    const complete = jasmine.createSpy('complete');
    component.loadMoreGames({ target: { complete } } as unknown as CustomEvent);
    tick();
    TestBed.flushEffects();

    expect(mediaLibrary.getAll).toHaveBeenCalledTimes(2);
    expect(component.games.map((game) => game.id)).toEqual([1, 2]);
    expect(component.filteredGames.map((game) => game.id)).toEqual([1, 2]);
    expect(component.hasMorePages).toBeFalse();
    expect(complete).toHaveBeenCalled();
  }));

  it('reloads from the first page with a custom release-date range', () => {
    configure(of(pageOf([])));
    fixture.detectChanges();

    component.releaseDateFilter = 'custom';
    component.releaseDateFrom = '2024-01-15';
    component.releaseDateTo = '2024-02-20';
    component.onCustomReleaseDateChange();

    expect(mediaLibrary.getAll).toHaveBeenCalledTimes(2);
    const filter = mediaLibrary.getAll.calls.mostRecent().args[0];
    expect(filter?.Paging.PageIndex).toBe(1);
    expect(filter?.From).toBe('2024-01-15');
    expect(filter?.To).toBe('2024-02-21');
  });

  it('clearing filters reloads when a server-side release-date filter was active', () => {
    configure(of(pageOf([])));
    fixture.detectChanges();

    component.releaseDateFilter = 'upcoming';
    component.clearFilters();

    expect(component.releaseDateFilter).toBe('all');
    expect(mediaLibrary.getAll).toHaveBeenCalledTimes(2);
  });

  it('shows an error message when the games request fails', fakeAsync(() => {
    configure(throwError(() => new Error('boom')));

    fixture.detectChanges();
    tick();

    expect(component.games).toEqual([]);
    expect(component.filteredGames).toEqual([]);
    expect(component.errorMessage).toBe('Games could not be loaded.');
    expect(component.isLoading).toBeFalse();
  }));

  it('reloads from the first page with ownership and search filters', () => {
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
    let filter = mediaLibrary.getAll.calls.mostRecent().args[0];
    expect(filter?.Paging.PageIndex).toBe(1);
    expect(filter?.Ownership).toBe('mine');

    component.ownershipFilter = 'catalog';
    component.applyFilters();
    filter = mediaLibrary.getAll.calls.mostRecent().args[0];
    expect(filter?.Ownership).toBe('catalog');

    component.ownershipFilter = 'all';
    component.searchTerm = 'catalog';
    component.applyFilters();
    filter = mediaLibrary.getAll.calls.mostRecent().args[0];
    expect(filter?.Ownership).toBe('all');
    expect(filter?.SearchString).toBe('catalog');
  });

  it('sends smart filters to the server', () => {
    const shortGame = makeGame({
      id: 1,
      name: 'Tiny Quest',
      playtime: 8,
      libraryEntry: makeEntry({ id: 11, status: GameStatus.Planned, timeSpend: 0 }),
    });
    const stalledGame = makeGame({
      id: 2,
      name: 'Old Epic',
      playtime: 50,
      libraryEntry: makeEntry({ id: 12, status: GameStatus.OnHold, timeSpend: 7 }),
    });
    const activeGame = makeGame({
      id: 3,
      name: 'Current Run',
      playtime: 30,
      libraryEntry: makeEntry({ id: 13, status: GameStatus.Playing, timeSpend: 6 }),
    });
    configure(of(pageOf([shortGame, stalledGame, activeGame])));
    fixture.detectChanges();

    component.setSmartFilter('short');
    let filter = mediaLibrary.getAll.calls.mostRecent().args[0];
    expect(filter?.SmartFilter).toBe('short');
    expect(filter?.Paging.PageIndex).toBe(1);

    component.setSmartFilter('abandoned');
    filter = mediaLibrary.getAll.calls.mostRecent().args[0];
    expect(filter?.SmartFilter).toBe('abandoned');
  });

  it('sends sort modes to the server', () => {
    const shorter = makeGame({
      id: 1,
      name: 'Shorter',
      releaseDate: '2024-01-01',
      playtime: 8,
      libraryEntry: makeEntry({ id: 11, rating: 6, status: GameStatus.Playing, timeSpend: 2 }),
    });
    const longer = makeGame({
      id: 2,
      name: 'Longer',
      releaseDate: '2025-01-01',
      playtime: 40,
      libraryEntry: makeEntry({ id: 12, rating: 9, status: GameStatus.Playing, timeSpend: 5 }),
    });
    configure(of(pageOf([shorter, longer])));
    fixture.detectChanges();

    component.sortMode = 'remaining-asc';
    component.applyFilters();
    let filter = mediaLibrary.getAll.calls.mostRecent().args[0];
    expect(filter?.SortBy).toBe('remaining-asc');

    component.sortMode = 'rating-desc';
    component.applyFilters();
    filter = mediaLibrary.getAll.calls.mostRecent().args[0];
    expect(filter?.SortBy).toBe('rating-desc');

    component.sortMode = 'release-desc';
    component.applyFilters();
    filter = mediaLibrary.getAll.calls.mostRecent().args[0];
    expect(filter?.SortBy).toBe('release-desc');
  });

  it('saves and applies library filter presets', () => {
    configure(of(pageOf([])));
    fixture.detectChanges();

    component.searchTerm = 'metroidvania';
    component.ownershipFilter = 'mine';
    component.sortMode = 'rating-desc';
    component.smartFilter = 'short';
    component.presetName = 'Short gems';
    component.saveCurrentPreset();

    expect(component.savedPresets.length).toBe(1);
    component.clearFilters(false);
    component.applyPreset(component.savedPresets[0]);

    expect(component.searchTerm).toBe('metroidvania');
    expect(component.ownershipFilter).toBe('mine');
    expect(component.sortMode).toBe('rating-desc');
    expect(component.smartFilter).toBe('short');
    expect(mediaLibrary.getAll).toHaveBeenCalledTimes(2);
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

  describe('admin create flow', () => {
    it('canCreateCatalogEntry is false for non-admins', () => {
      configure(of(pageOf([])));
      fixture.detectChanges();

      authIsAdmin.set(false);
      expect(component.canCreateCatalogEntry).toBeFalse();
    });

    it('canCreateCatalogEntry is true for admins on creatable modes', () => {
      configure(of(pageOf([])));
      fixture.detectChanges();

      authIsAdmin.set(true);
      // Default media mode is games — creatable.
      expect(component.canCreateCatalogEntry).toBeTrue();
    });

    it('openCreateDialog is a no-op when the user is not an admin', () => {
      configure(of(pageOf([])));
      fixture.detectChanges();

      authIsAdmin.set(false);
      component.openCreateDialog();
      expect(component.isCreateDialogOpen).toBeFalse();
    });

    it('openCreateDialog resets the form and opens the modal for admins', () => {
      configure(of(pageOf([])));
      fixture.detectChanges();

      authIsAdmin.set(true);
      component.createForm.name = 'leftover';
      component.openCreateDialog();

      expect(component.isCreateDialogOpen).toBeTrue();
      expect(component.createForm.name).toBe('');
      expect(component.createForm.createLibraryEntry).toBeFalse();
    });

    it('submitCreate flags a missing title without calling the service', fakeAsync(() => {
      configure(of(pageOf([])));
      fixture.detectChanges();

      authIsAdmin.set(true);
      component.openCreateDialog();
      component.createForm.name = '   ';

      component.submitCreate();
      tick();

      expect(component.createErrorMessage).toBe('Title is required.');
      expect(gameService.post).not.toHaveBeenCalled();
    }));

    it('submitCreate posts a game and closes the dialog on success', fakeAsync(() => {
      configure(of(pageOf([])));
      fixture.detectChanges();

      authIsAdmin.set(true);
      component.openCreateDialog();
      component.createForm.name = 'Hades';
      component.createForm.description = 'Escape the underworld';
      component.createForm.platforms = 8;
      component.createForm.playtime = 25;

      const created = Object.assign(new Game(), { id: 101, name: 'Hades' });
      gameService.post.and.returnValue(of(created));

      component.submitCreate();
      tick();

      expect(gameService.post).toHaveBeenCalledTimes(1);
      const sent = gameService.post.calls.mostRecent().args[0];
      expect(sent.name).toBe('Hades');
      expect(sent.description).toBe('Escape the underworld');
      expect(sent.platforms).toBe(8);
      expect(sent.playtime).toBe(25);

      expect(component.isCreateDialogOpen).toBeFalse();
      expect(component.successMessage).toBe('Hades was created.');
      expect(component.isCreatingCatalogEntry).toBeFalse();
    }));

    it('submitCreate surfaces backend errors and keeps the dialog open', fakeAsync(() => {
      configure(of(pageOf([])));
      fixture.detectChanges();

      authIsAdmin.set(true);
      component.openCreateDialog();
      component.createForm.name = 'Hades';

      gameService.post.and.returnValue(throwError(() => ({ error: 'Game already exists' })));

      component.submitCreate();
      tick();

      expect(component.createErrorMessage).toBe('Game already exists');
      expect(component.isCreateDialogOpen).toBeTrue();
      expect(component.isCreatingCatalogEntry).toBeFalse();
    }));

    it('submitCreate also POSTs the library entry when the toggle is on', fakeAsync(() => {
      configure(of(pageOf([])));
      fixture.detectChanges();

      authIsAdmin.set(true);
      component.openCreateDialog();
      component.createForm.name = 'Hades';
      component.createForm.createLibraryEntry = true;
      component.createForm.libraryEntry = {
        status: 2,
        timeSpend: 7,
        rating: 9,
        startDate: '2026-05-10',
        endDate: null,
        personalNotes: null,
        currentEpisode: null,
      };

      const created = Object.assign(new Game(), { id: 202, name: 'Hades' });
      gameService.post.and.returnValue(of(created));
      mediaLibrary.addToLibrary.and.returnValue(
        of({ id: 999, rating: 9, startDate: '2026-05-10', endDate: null, status: 2, timeSpend: 7 } as UserMediaEntry),
      );

      component.submitCreate();
      tick();

      expect(mediaLibrary.addToLibrary).toHaveBeenCalledOnceWith(202, jasmine.objectContaining({
        status: 2,
        timeSpend: 7,
        rating: 9,
        startDate: '2026-05-10',
      }));
    }));
  });
});
