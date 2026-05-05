import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { MediaPage } from './media.page';
import { GameService } from '../../games/services/game.service';
import { MyGameService } from '../../my-games/services/my-game.service';
import { ReleaseNotificationService } from 'src/app/shared/services/release-notification.service';
import { Game, MyGame } from '../../games/models/games.model';
import { PaginateResult } from 'src/app/core/entities/paginatedResult';

function makeGame(overrides: Partial<Game> = {}): Game {
  return Object.assign(new Game(), overrides);
}

function pageOf(games: Game[]): PaginateResult<Game> {
  const result = new PaginateResult<Game>();
  result.data = games;
  return result;
}

describe('MediaPage (Library)', () => {
  let component: MediaPage;
  let fixture: ComponentFixture<MediaPage>;

  let gameService: jasmine.SpyObj<GameService>;
  let myGameService: jasmine.SpyObj<MyGameService>;
  let releaseNotifications: jasmine.SpyObj<ReleaseNotificationService>;

  function configure(getAllResponse: Observable<PaginateResult<Game>>) {
    gameService = jasmine.createSpyObj<GameService>('GameService', ['getAll']);
    myGameService = jasmine.createSpyObj<MyGameService>('MyGameService', [
      'addToLibrary',
      'updateLibraryEntry',
    ]);
    releaseNotifications = jasmine.createSpyObj<ReleaseNotificationService>(
      'ReleaseNotificationService',
      ['syncForGames'],
    );

    gameService.getAll.and.returnValue(getAllResponse);
    releaseNotifications.syncForGames.and.resolveTo();

    TestBed.configureTestingModule({
      imports: [MediaPage],
      providers: [
        { provide: GameService, useValue: gameService },
        { provide: MyGameService, useValue: myGameService },
        { provide: ReleaseNotificationService, useValue: releaseNotifications },
      ],
    });

    fixture = TestBed.createComponent(MediaPage);
    component = fixture.componentInstance;
  }

  it('loads games on init and clears the loading flag', () => {
    const games = [
      makeGame({ id: 1, name: 'Apex Legends' }),
      makeGame({ id: 2, name: 'Baldurs Gate' }),
    ];
    configure(of(pageOf(games)));

    fixture.detectChanges(); // triggers ngOnInit

    expect(gameService.getAll).toHaveBeenCalledTimes(1);
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
      myGames: Object.assign(new MyGame(1), { id: 10, status: 2 }),
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

    const persistedMyGame = Object.assign(new MyGame(7), {
      id: 99,
      status: 1,
      timeSpend: 5,
    });
    myGameService.addToLibrary.and.returnValue(of(persistedMyGame));

    component.openGameListDialog(game);
    component.addGameForm = {
      status: 1,
      timeSpend: 5,
      rating: null,
      startDate: '',
      endDate: '',
    };

    component.submitAddGame();
    tick();

    expect(myGameService.addToLibrary).toHaveBeenCalledOnceWith(7, {
      status: 1,
      timeSpend: 5,
      rating: null,
      startDate: null,
      endDate: null,
    });
    expect(myGameService.updateLibraryEntry).not.toHaveBeenCalled();
    expect(game.myGames).toBe(persistedMyGame);
    expect(component.successMessage).toBe('Hades was added to your game list.');
    expect(component.isAddDialogOpen).toBeFalse();
    expect(component.isAdding(game)).toBeFalse();
  }));

  it('submitAddGame routes to updateLibraryEntry when the game is already owned', fakeAsync(() => {
    const existing = Object.assign(new MyGame(7), { id: 99, status: 1 });
    const game = makeGame({ id: 7, name: 'Hades', myGames: existing });
    configure(of(pageOf([game])));
    fixture.detectChanges();

    const updated = Object.assign(new MyGame(7), { id: 99, status: 2 });
    myGameService.updateLibraryEntry.and.returnValue(of(updated));

    component.openGameListDialog(game);
    component.addGameForm = {
      status: 2,
      timeSpend: 0,
      rating: null,
      startDate: '',
      endDate: '',
    };

    component.submitAddGame();
    tick();

    expect(myGameService.updateLibraryEntry).toHaveBeenCalledOnceWith(99, 7, {
      status: 2,
      timeSpend: 0,
      rating: null,
      startDate: null,
      endDate: null,
    });
    expect(myGameService.addToLibrary).not.toHaveBeenCalled();
    expect(game.myGames).toBe(updated);
    expect(component.successMessage).toBe('Hades was saved.');
  }));

  it('submitAddGame surfaces backend duplicate-error messages', fakeAsync(() => {
    const game = makeGame({ id: 7, name: 'Hades' });
    configure(of(pageOf([game])));
    fixture.detectChanges();

    myGameService.addToLibrary.and.returnValue(
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

    myGameService.addToLibrary.and.returnValue(new Observable<MyGame>(() => {}));

    component.openGameListDialog(game);
    component.submitAddGame();
    component.submitAddGame();

    expect(myGameService.addToLibrary).toHaveBeenCalledTimes(1);
  });

  it('does not call any mutating service when no game is selected', () => {
    configure(of(pageOf([])));
    fixture.detectChanges();

    component.submitAddGame();

    expect(myGameService.addToLibrary).not.toHaveBeenCalled();
    expect(myGameService.updateLibraryEntry).not.toHaveBeenCalled();
  });
});
