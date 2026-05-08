import { Component, OnDestroy, OnInit } from '@angular/core';
import { IonIcon, IonHeader } from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  calendarClearOutline,
  checkmarkCircleOutline,
  gameControllerOutline,
  gridOutline,
  hourglassOutline,
  libraryOutline,
  logOutOutline,
  notificationsOutline,
  personCircleOutline,
  rocketOutline,
  settingsOutline,
  sparklesOutline,
  peopleOutline,
  timeOutline,
} from 'ionicons/icons';
import { AuthService } from 'src/app/core/auth/services/auth.service';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Subscription, filter, finalize } from 'rxjs';
import { NotificationItem, NotificationsService } from 'src/app/shared/services/notifications.service';
import { shouldHideAppNavigation } from 'src/app/shared/utils/app-shell-navigation';

@Component({
    selector: 'app-nav-bar',
    templateUrl: './nav-bar.component.html',
    styleUrls: ['./nav-bar.component.scss'],
    imports: [CommonModule, RouterLink, IonIcon, IonHeader]
})
export class NavBarComponent implements OnInit, OnDestroy {
  isLoggingOut = false;
  openMenu: 'notifications' | 'apps' | 'profile' | null = null;
  notifications: NotificationItem[] = [];
  notificationsLoading = false;

  private routerSub?: Subscription;
  private notificationsSub?: Subscription;

  constructor(
    private authService: AuthService,
    public router: Router,
    private notificationsService: NotificationsService,
  ) {
    addIcons({
      calendarClearOutline,
      checkmarkCircleOutline,
      gameControllerOutline,
      gridOutline,
      hourglassOutline,
      libraryOutline,
      logOutOutline,
      notificationsOutline,
      personCircleOutline,
      rocketOutline,
      settingsOutline,
      sparklesOutline,
      timeOutline,
      peopleOutline,
    });
  }

  ngOnInit() {
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
  }

  get showShellNavigation() {
    return !shouldHideAppNavigation(this.router.url);
  }

  isActive(path: string): boolean {
    return this.router.url === path || this.router.url.startsWith(`${path}/`);
  }

  toggleMenu(menu: 'notifications' | 'apps' | 'profile') {
    const next = this.openMenu === menu ? null : menu;
    this.openMenu = next;
    if (next === 'notifications') {
      this.refreshNotifications();
    }
  }

  closeMenus() {
    this.openMenu = null;
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
