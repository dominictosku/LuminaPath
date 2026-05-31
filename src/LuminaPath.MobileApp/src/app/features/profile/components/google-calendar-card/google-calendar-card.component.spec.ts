import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { AlertController } from '@ionic/angular/standalone';
import { of } from 'rxjs';

import { GoogleCalendarCardComponent } from './google-calendar-card.component';
import {
  GoogleCalendarService,
  GoogleCalendarStatus,
} from '../../services/google-calendar.service';

function status(overrides: Partial<GoogleCalendarStatus> = {}): GoogleCalendarStatus {
  return { configured: true, connected: false, email: null, lastSyncedAt: null, ...overrides };
}

describe('GoogleCalendarCardComponent', () => {
  let fixture: ComponentFixture<GoogleCalendarCardComponent>;
  let service: jasmine.SpyObj<GoogleCalendarService>;

  function setup(initial: GoogleCalendarStatus): void {
    service = jasmine.createSpyObj<GoogleCalendarService>('GoogleCalendarService', [
      'getStatus',
      'sync',
      'disconnect',
      'connectUrl',
    ]);
    service.getStatus.and.returnValue(of(initial));
    service.connectUrl.and.returnValue('/api/integrations/google/connect');

    TestBed.configureTestingModule({
      imports: [GoogleCalendarCardComponent],
      providers: [
        { provide: GoogleCalendarService, useValue: service },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({}) } } },
        { provide: Router, useValue: { navigate: jasmine.createSpy('navigate').and.resolveTo(true) } },
        {
          provide: AlertController,
          useValue: { create: () => Promise.resolve({ present: () => Promise.resolve() }) },
        },
      ],
    });

    fixture = TestBed.createComponent(GoogleCalendarCardComponent);
  }

  async function settle(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('hides entirely when the server has no Google credentials configured', async () => {
    setup(status({ configured: false }));
    await settle();

    expect(fixture.componentInstance.configured()).toBeFalse();
    expect(fixture.nativeElement.querySelector('.google-calendar-card')).toBeNull();
  });

  it('shows the connect action when configured but not linked', async () => {
    setup(status({ configured: true, connected: false }));
    await settle();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Connect Google Calendar');
    expect(text).not.toContain('Sync now');
  });

  it('shows the account and sync/disconnect actions when connected', async () => {
    setup(status({ configured: true, connected: true, email: 'gamer@example.com' }));
    await settle();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('gamer@example.com');
    expect(text).toContain('Sync now');
    expect(text).toContain('Disconnect');
  });

  it('syncNow() pushes a sync and refreshes status', async () => {
    setup(status({ configured: true, connected: true, email: 'g@e.com' }));
    await settle();

    service.sync.and.returnValue(
      of({ releaseEvents: 1, questEvents: 2, deleted: 0, message: 'Synced 1 release(s) and 2 quest(s).' }),
    );
    service.getStatus.calls.reset();
    service.getStatus.and.returnValue(of(status({ configured: true, connected: true, email: 'g@e.com', lastSyncedAt: '2026-06-01T00:00:00Z' })));

    await fixture.componentInstance.syncNow();

    expect(service.sync).toHaveBeenCalledTimes(1);
    expect(service.getStatus).toHaveBeenCalledTimes(1); // reload after sync
    expect(fixture.componentInstance.message()).toContain('Synced 1 release');
    expect(fixture.componentInstance.messageTone()).toBe('success');
  });
});
