import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { PaginateResult } from 'src/app/core/entities/paginatedResult';
import { Game } from '../../games/models/games.model';
import { GameService } from '../../games/services/game.service';
import { PlanningPage } from './planning.page';

function pageOf(games: Game[]): PaginateResult<Game> {
  const page = new PaginateResult<Game>();
  page.data = games;
  return page;
}

describe('PlanningPage', () => {
  let component: PlanningPage;
  let fixture: ComponentFixture<PlanningPage>;
  let gameService: jasmine.SpyObj<GameService>;

  function configure(games: Game[] = []) {
    gameService = jasmine.createSpyObj<GameService>('GameService', ['getAll']);
    gameService.getAll.and.returnValue(of(pageOf(games)));

    TestBed.configureTestingModule({
      imports: [PlanningPage],
      providers: [
        { provide: GameService, useValue: gameService },
      ],
    });

    fixture = TestBed.createComponent(PlanningPage);
    component = fixture.componentInstance;
  }

  it('loads games for the release plan on init', fakeAsync(() => {
    const game = Object.assign(new Game(), { id: 1, name: 'Hades II' });
    configure([game]);

    fixture.detectChanges();
    tick();

    expect(gameService.getAll).toHaveBeenCalled();
    expect(component.allGames).toEqual([game]);
    expect(component.errorMessage).toBe('');
  }));

  it('shows a release-plan error when games cannot be loaded', fakeAsync(() => {
    configure();
    gameService.getAll.and.returnValue(throwError(() => new Error('offline')));

    fixture.detectChanges();
    tick();

    expect(component.allGames).toEqual([]);
    expect(component.errorMessage).toBe('Release plan could not be loaded.');
    expect(component.isLoading).toBeFalse();
  }));
});
