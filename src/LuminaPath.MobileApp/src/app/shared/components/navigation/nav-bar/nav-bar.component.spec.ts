import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { IonicModule } from '@ionic/angular';
import { of } from 'rxjs';

import { NavBarComponent } from './nav-bar.component';
import { NotificationsService } from 'src/app/shared/services/notifications.service';
import { LiveSessionTrackerService } from 'src/app/shared/services/live-session-tracker.service';

describe('NavBarComponent', () => {
  let component: NavBarComponent;
  let fixture: ComponentFixture<NavBarComponent>;
  let notificationsService: jasmine.SpyObj<NotificationsService>;
  let liveSessionTracker: LiveSessionTrackerService;

  beforeEach(async () => {
    notificationsService = jasmine.createSpyObj<NotificationsService>('NotificationsService', ['load']);
    notificationsService.load.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [NavBarComponent, IonicModule.forRoot()],
      providers: [
        provideRouter([]),
        { provide: NotificationsService, useValue: notificationsService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(NavBarComponent);
    component = fixture.componentInstance;
    liveSessionTracker = TestBed.inject(LiveSessionTrackerService);
    fixture.detectChanges();
  });

  afterEach(() => {
    fixture.destroy();
    window.localStorage.clear();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows running live sessions from local storage', () => {
    window.localStorage.setItem('luminapath.liveSession.games.42', JSON.stringify({
      gameId: 42,
      myGameId: 7,
      gameName: 'Hades',
      startedAt: '2026-05-26T10:00:00.000Z',
      notes: '',
    }));

    liveSessionTracker.refresh();
    fixture.detectChanges();

    expect(component.liveSessions().length).toBe(1);
  });
});
