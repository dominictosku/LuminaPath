import { Component, NgZone, OnDestroy, OnInit, effect, inject } from '@angular/core';
import { IonIcon, IonHeader } from '@ionic/angular/standalone';
import { AuthService } from 'src/app/core/auth/services/auth.service';
import { NavigationEnd, Router, RouterLink } from '@angular/router';

import { Subscription, filter, finalize } from 'rxjs';
import { NotificationItem, NotificationsService } from 'src/app/shared/services/notifications.service';
import { shouldHideAppNavigation } from 'src/app/shared/utils/app-shell-navigation';
import { MediaMode, MediaModeOption, MediaModeService } from 'src/app/shared/services/media-mode.service';
import { GlobalSearchService } from 'src/app/shared/services/global-search.service';
import { LiveGameSession, LiveSessionTrackerService } from 'src/app/shared/services/live-session-tracker.service';

@Component({
    selector: 'app-nav-bar',
    templateUrl: './nav-bar.component.html',
    styleUrls: ['./nav-bar.component.scss'],
    imports: [RouterLink, IonIcon, IonHeader]
})
export class NavBarComponent implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  router = inject(Router);
  private notificationsService = inject(NotificationsService);
  private mediaModeService = inject(MediaModeService);
  private globalSearch = inject(GlobalSearchService);
  private liveSessionTracker = inject(LiveSessionTrackerService);
  private zone = inject(NgZone);

  isLoggingOut = false;
  openMenu: 'media' | 'liveSessions' | 'notifications' | 'apps' | 'profile' | null = null;
  notifications: NotificationItem[] = [];
  notificationsLoading = false;
  mediaMode: MediaModeOption;
  readonly mediaModes: MediaModeOption[];
  readonly liveSessions = this.liveSessionTracker.sessions;
  liveSessionNow = Date.now();

  private routerSub?: Subscription;
  private notificationsSub?: Subscription;
  private liveSessionTimerId: number | null = null;

  constructor() {
    this.mediaMode = this.mediaModeService.mode();
    this.mediaModes = this.mediaModeService.options;
    effect(() => {
      this.mediaMode = this.mediaModeService.mode();
    });
  }

  ngOnInit() {
    this.zone.runOutsideAngular(() => {
      this.liveSessionTimerId = window.setInterval(() => {
        this.zone.run(() => {
          this.liveSessionNow = Date.now();
        });
      }, 30000);
    });
    this.refreshNotifications();
    this.routerSub = this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe(() => {
        this.closeMenus();
        this.refreshNotifications();
      });
  }

  ngOnDestroy() {
    this.routerSub?.unsubscribe();
    this.notificationsSub?.unsubscribe();
    if (this.liveSessionTimerId !== null) {
      window.clearInterval(this.liveSessionTimerId);
    }
  }

  get showShellNavigation() {
    return !shouldHideAppNavigation(this.router.url);
  }

  isActive(path: string): boolean {
    return this.router.url === path || this.router.url.startsWith(`${path}/`);
  }

  toggleMenu(menu: 'media' | 'liveSessions' | 'notifications' | 'apps' | 'profile') {
    const next = this.openMenu === menu ? null : menu;
    this.openMenu = next;
    if (next === 'notifications') {
      this.refreshNotifications();
    }
  }

  closeMenus() {
    this.openMenu = null;
  }

  openSearch(): void {
    this.closeMenus();
    this.globalSearch.open();
  }

  selectMediaMode(mode: MediaMode) {
    this.mediaModeService.select(mode);
    this.closeMenus();
    if (this.router.url.startsWith('/library/')) {
      void this.router.navigate(['/library']);
    }
  }

  logout() {
    if (this.isLoggingOut) {
      return;
    }

    this.isLoggingOut = true;
    this.closeMenus();
    this.authService.logout()
      .pipe(finalize(() => this.isLoggingOut = false))
      .subscribe({
        next: () => this.router.navigate(['/auth/login'], { replaceUrl: true }),
        error: () => {
          this.authService.clearSession();
          this.router.navigate(['/auth/login'], { replaceUrl: true });
        },
      });
  }

  trackByNotification(_: number, item: NotificationItem): string {
    return item.id;
  }

  trackByLiveSession(_: number, item: LiveGameSession): number {
    return item.gameId;
  }

  liveSessionTitle(session: LiveGameSession): string {
    return session.gameName ?? 'Running session';
  }

  liveSessionDurationLabel(session: LiveGameSession): string {
    return formatLiveDuration(this.liveSessionTracker.durationSeconds(session, this.liveSessionNow));
  }

  liveSessionStartedLabel(session: LiveGameSession): string {
    const started = new Date(session.startedAt);
    if (Number.isNaN(started.getTime())) {
      return 'Running now';
    }

    return `Started ${started.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
  }

  private refreshNotifications() {
    if (!this.showShellNavigation) {
      this.notifications = [];
      return;
    }

    this.notificationsLoading = true;
    this.notificationsSub?.unsubscribe();
    this.notificationsSub = this.notificationsService.load().subscribe({
      next: (items) => {
        this.notifications = items;
        this.notificationsLoading = false;
      },
      error: () => {
        this.notifications = [];
        this.notificationsLoading = false;
      },
    });
  }
}

function formatLiveDuration(totalSeconds: number): string {
  const seconds = Math.max(0, Math.floor(totalSeconds));
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);

  if (hours > 0) {
    return `${hours}h ${String(minutes).padStart(2, '0')}m`;
  }

  return `${minutes}m`;
}
