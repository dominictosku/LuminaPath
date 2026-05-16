import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideIonicAngular } from '@ionic/angular/standalone';
import { of } from 'rxjs';

import { PaginateResult } from 'src/app/core/entities/paginatedResult';
import { AnimeService } from '../../animes/services/anime.service';
import { GameService } from '../../games/services/game.service';
import { MovieService } from '../../movies/services/movie.service';
import { SeriesService } from '../../series/services/series.service';
import { StatisticPage } from './statistic.page';

function pageOf<T>(items: T[]): PaginateResult<T> {
  const page = new PaginateResult<T>();
  page.data = items;
  return page;
}

describe('StatisticPage', () => {
  let fixture: ComponentFixture<StatisticPage>;
  let component: StatisticPage;
  let gameService: jasmine.SpyObj<GameService>;
  let animeService: jasmine.SpyObj<AnimeService>;
  let movieService: jasmine.SpyObj<MovieService>;
  let seriesService: jasmine.SpyObj<SeriesService>;

  beforeEach(() => {
    gameService = jasmine.createSpyObj<GameService>('GameService', ['getAll']);
    animeService = jasmine.createSpyObj<AnimeService>('AnimeService', ['getAll']);
    movieService = jasmine.createSpyObj<MovieService>('MovieService', ['getAll']);
    seriesService = jasmine.createSpyObj<SeriesService>('SeriesService', ['getAll']);

    gameService.getAll.and.returnValue(of(pageOf([])));
    animeService.getAll.and.returnValue(of(pageOf([])));
    movieService.getAll.and.returnValue(of(pageOf([])));
    seriesService.getAll.and.returnValue(of(pageOf([])));

    TestBed.configureTestingModule({
      imports: [StatisticPage],
      providers: [
        provideIonicAngular(),
        { provide: GameService, useValue: gameService },
        { provide: AnimeService, useValue: animeService },
        { provide: MovieService, useValue: movieService },
        { provide: SeriesService, useValue: seriesService },
      ],
    });

    fixture = TestBed.createComponent(StatisticPage);
    component = fixture.componentInstance;
  });

  it('builds backlog health from owned media', fakeAsync(() => {
    gameService.getAll.and.returnValue(of(pageOf([
      {
        id: 1,
        name: 'Long RPG',
        playtime: 100,
        myGames: { status: 1, timeSpend: 10, myGameInfo: { trackedHours: 0 } },
      } as never,
      {
        id: 2,
        name: 'Finished Game',
        playtime: 20,
        myGames: { status: 4, timeSpend: 20, myGameInfo: { trackedHours: 0 } },
      } as never,
    ])));

    fixture.detectChanges();
    tick();

    expect(component.ownedItems.length).toBe(2);
    expect(component.backlogItems.length).toBe(1);
    expect(component.longCommitments[0].name).toBe('Long RPG');
    expect(component.healthScore).toBeGreaterThan(0);
  }));
});
