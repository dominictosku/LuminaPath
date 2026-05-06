import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { of } from 'rxjs';

import { PlaningPage } from './planing.page';
import { GameService } from '../../games/services/game.service';
import { GamingSessionService, GamingSession, GameForecast } from '../services/gaming-session.service';
import { Game, MyGame } from '../../games/models/games.model';
import { PaginateResult } from 'src/app/core/entities/paginatedResult';

function makeGame(overrides: Partial<Game> = {}): Game {
  return Object.assign(new Game(), overrides);
}

function pageOf(games: Game[]): PaginateResult<Game> {
  const page = new PaginateResult<Game>();
  page.data = games;
  return page;
}

function makeSession(overrides: Partial<GamingSession> = {}): GamingSession {
  return {
    id: 1,
    myGameId: null,
    gameName: null,
    scheduledAt: '2026-06-04T19:00:00.000Z',
    durationMinutes: 90,
    completed: false,
    completedAt: null,
    notes: null,
    createdAt: '2026-06-01T00:00:00.000Z',
    ...overrides,
  };
}

function makeForecast(overrides: Partial<GameForecast> = {}): GameForecast {
  return {
    myGameId: 5,
    gameName: 'Hades',
    playtimeEstimateHours: 20,
    playedHours: 5,
    remainingHours: 15,
    scheduledHours: 6,
    upcomingSessionCount: 4,
    sessionsToCompletion: null,
    projectedCompletionDate: null,
    weeklyHours: 1.5,
    additionalHoursNeeded: 9,
    weeksAtCurrentPace: 6,
    ...overrides,
  };
}

describe('PlaningPage', () => {
  let component: PlaningPage;
  let fixture: ComponentFixture<PlaningPage>;
  let gameService: jasmine.SpyObj<GameService>;
  let sessionService: jasmine.SpyObj<GamingSessionService>;

  function configure(opts: { games?: Game[]; sessions?: GamingSession[]; forecast?: GameForecast }) {
    gameService = jasmine.createSpyObj<GameService>('GameService', ['getAll']);
    sessionService = jasmine.createSpyObj<GamingSessionService>('GamingSessionService', [
      'list',
      'create',
      'update',
      'remove',
      'forecast',
    ]);

    gameService.getAll.and.returnValue(of(pageOf(opts.games ?? [])));
    sessionService.list.and.returnValue(of(opts.sessions ?? []));
    sessionService.forecast.and.returnValue(of(opts.forecast ?? makeForecast()));

    TestBed.configureTestingModule({
      imports: [PlaningPage],
      providers: [
        { provide: GameService, useValue: gameService },
        { provide: GamingSessionService, useValue: sessionService },
      ],
    });

    fixture = TestBed.createComponent(PlaningPage);
    component = fixture.componentInstance;
  }

  it('loads library, sessions, and forecasts on init, grouping sessions by day', fakeAsync(() => {
    const owned = makeGame({
      id: 1,
      name: 'Hades',
      playtime: 20,
      myGames: Object.assign(new MyGame(1), { id: 5 }),
    });
    const sessions = [
      makeSession({ id: 1, scheduledAt: '2026-06-04T18:00:00.000Z', myGameId: 5, gameName: 'Hades' }),
      makeSession({ id: 2, scheduledAt: '2026-06-04T20:00:00.000Z', myGameId: 5, gameName: 'Hades' }),
      makeSession({ id: 3, scheduledAt: '2026-06-05T18:00:00.000Z' }),
    ];

    configure({ games: [owned], sessions });
    fixture.detectChanges();
    tick();

    expect(component.libraryGames.length).toBe(1);
    expect(component.libraryGames[0].myGameId).toBe(5);

    expect(component.sessions.length).toBe(3);
    expect(component.buckets.length).toBe(2);
    expect(component.buckets[0].sessions.length).toBe(2);

    expect(sessionService.forecast).toHaveBeenCalledOnceWith(5);
    expect(component.forecasts.length).toBe(1);
  }));

  it('addSession refuses to call create when duration is zero', fakeAsync(() => {
    configure({});
    fixture.detectChanges();
    tick();

    component.draft = {
      myGameId: null,
      scheduledDate: '2026-06-04',
      scheduledTime: '19:00',
      durationMinutes: 0,
      notes: '',
    };

    component.addSession();
    tick();

    expect(sessionService.create).not.toHaveBeenCalled();
    expect(component.errorMessage).toBe('Pick a date, time, and a duration.');
  }));

  it('addSession posts the combined date/time and reloads', fakeAsync(() => {
    configure({});
    fixture.detectChanges();
    tick();

    sessionService.create.and.returnValue(of(makeSession({ id: 99 })));
    sessionService.list.and.returnValue(of([makeSession({ id: 99 })]));

    component.draft = {
      myGameId: 5,
      scheduledDate: '2026-06-04',
      scheduledTime: '19:30',
      durationMinutes: 90,
      notes: 'Boss',
    };

    component.addSession();
    tick();

    expect(sessionService.create).toHaveBeenCalledTimes(1);
    const payload = sessionService.create.calls.mostRecent().args[0];
    expect(payload.myGameId).toBe(5);
    expect(payload.durationMinutes).toBe(90);
    expect(payload.notes).toBe('Boss');
    expect(payload.scheduledAt).toMatch(/2026-06-04/);
  }));

  it('toggleComplete sends the inverted completed flag', fakeAsync(() => {
    const session = makeSession({ id: 7, completed: false });
    configure({ sessions: [session] });
    fixture.detectChanges();
    tick();

    sessionService.update.and.returnValue(of({ ...session, completed: true }));

    component.toggleComplete(session);
    tick();

    expect(sessionService.update).toHaveBeenCalledWith(7, jasmine.objectContaining({
      id: 7,
      completed: true,
    }));
  }));

  it('forecastSummary describes a covered schedule with the projected ETA', () => {
    configure({});
    const forecast = makeForecast({
      sessionsToCompletion: 4,
      projectedCompletionDate: '2026-06-25T22:00:00.000Z',
      weeksAtCurrentPace: null,
      additionalHoursNeeded: 0,
    });

    expect(component.forecastSummary(forecast)).toContain('4 sessions');
    expect(component.forecastSummary(forecast)).toContain('ETA');
  });

  it('forecastSummary falls back to weekly pace when schedule is short', () => {
    configure({});
    const forecast = makeForecast({
      sessionsToCompletion: null,
      projectedCompletionDate: null,
      additionalHoursNeeded: 12,
      weeklyHours: 2,
      weeksAtCurrentPace: 6,
    });

    expect(component.forecastSummary(forecast)).toContain('12h more');
    expect(component.forecastSummary(forecast)).toContain('6 weeks');
  });
});
