import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideIonicAngular } from '@ionic/angular/standalone';
import { of } from 'rxjs';

import { GamingSession, GamingSessionService } from 'src/app/features/planning/services/gaming-session.service';
import { GameSessionsComponent } from './game-sessions.component';

function makeSession(overrides: Partial<GamingSession> = {}): GamingSession {
  return {
    id: 1,
    myGameId: 7,
    gameName: 'Hades',
    scheduledAt: '2026-06-04T19:00:00.000Z',
    durationMinutes: 90,
    completed: false,
    completedAt: null,
    notes: null,
    createdAt: '2026-06-01T00:00:00.000Z',
    ...overrides,
  };
}

describe('GameSessionsComponent', () => {
  let fixture: ComponentFixture<GameSessionsComponent>;
  let component: GameSessionsComponent;
  let sessionService: jasmine.SpyObj<GamingSessionService>;

  function configure(sessions: GamingSession[] = []) {
    sessionService = jasmine.createSpyObj<GamingSessionService>('GamingSessionService', [
      'list',
      'create',
      'update',
      'remove',
      'forecast',
    ]);
    sessionService.list.and.returnValue(of(sessions));
    sessionService.create.and.returnValue(of(makeSession({ id: 99 })));
    sessionService.update.and.returnValue(of(makeSession()));
    sessionService.remove.and.returnValue(of(void 0));

    TestBed.configureTestingModule({
      imports: [GameSessionsComponent],
      providers: [
        provideIonicAngular(),
        { provide: GamingSessionService, useValue: sessionService },
      ],
    });

    fixture = TestBed.createComponent(GameSessionsComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('isInLibrary', true);
    fixture.componentRef.setInput('gameName', 'Hades');
  }

  it('loads sessions scoped to the current game library entry', fakeAsync(() => {
    const sessions = [
      makeSession({ id: 1, scheduledAt: '2026-06-04T18:00:00.000Z' }),
      makeSession({ id: 2, scheduledAt: '2026-06-04T20:00:00.000Z' }),
    ];
    configure(sessions);

    fixture.componentRef.setInput('myGameId', 7);
    fixture.detectChanges();
    tick();

    expect(sessionService.list).toHaveBeenCalled();
    expect(sessionService.list.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({ myGameId: 7 }));
    expect(component.sessions.length).toBe(2);
    expect(component.buckets.length).toBe(1);
  }));

  it('adds a session for the current game without selecting a game', fakeAsync(() => {
    configure();
    fixture.componentRef.setInput('myGameId', 7);
    fixture.detectChanges();
    tick();
    sessionService.list.calls.reset();

    component.draft = {
      scheduledDate: '2026-06-04',
      scheduledTime: '19:30',
      durationMinutes: 90,
      notes: 'Boss',
    };

    component.addSession();
    tick();

    expect(sessionService.create).toHaveBeenCalledTimes(1);
    const payload = sessionService.create.calls.mostRecent().args[0];
    expect(payload.myGameId).toBe(7);
    expect(payload.durationMinutes).toBe(90);
    expect(payload.notes).toBe('Boss');
    expect(payload.scheduledAt).toMatch(/2026-06-04/);
    expect(sessionService.list).toHaveBeenCalledWith(jasmine.objectContaining({ myGameId: 7 }));
  }));

  it('keeps game scope when switching to past and all sessions', fakeAsync(() => {
    configure([makeSession({ id: 1 })]);
    fixture.componentRef.setInput('myGameId', 7);
    fixture.detectChanges();
    tick();
    sessionService.list.calls.reset();

    component.setSessionRange('past');
    tick();

    const pastOptions = sessionService.list.calls.mostRecent().args[0] ?? {};
    expect(pastOptions.myGameId).toBe(7);
    expect(pastOptions.from).toBeUndefined();
    expect(pastOptions.to instanceof Date).toBeTrue();

    component.setSessionRange('all');
    tick();

    expect(sessionService.list.calls.mostRecent().args[0]).toEqual({ myGameId: 7 });
  }));

  it('toggles and removes sessions through the session service', fakeAsync(() => {
    const session = makeSession({ id: 5, completed: false });
    configure([session]);
    fixture.componentRef.setInput('myGameId', 7);
    fixture.detectChanges();
    tick();

    component.toggleComplete(session);
    tick();
    expect(sessionService.update).toHaveBeenCalledWith(5, jasmine.objectContaining({
      id: 5,
      myGameId: 7,
      completed: true,
    }));

    component.removeSession(session);
    tick();
    expect(sessionService.remove).toHaveBeenCalledOnceWith(5);
  }));
});
