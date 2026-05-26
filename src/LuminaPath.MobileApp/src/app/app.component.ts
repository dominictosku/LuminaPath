import { Component, DestroyRef, HostBinding, inject, OnInit, Renderer2, effect, signal } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { IonApp, IonRouterOutlet, Platform, ToastController } from '@ionic/angular/standalone';
import { App } from '@capacitor/app';
import { Capacitor } from '@capacitor/core';
import { NavBarComponent } from './shared/components/navigation/nav-bar/nav-bar.component'
import { AiChatComponent } from './shared/components/ai-chat/ai-chat.component';
import { ErrorBannerComponent } from './shared/components/error-banner/error-banner.component';
import { GlobalSearchComponent } from './shared/components/global-search/global-search.component';
import { OfflineBannerComponent } from './shared/components/offline-banner/offline-banner.component';
import { ReleaseNotificationService } from './shared/services/release-notification.service';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { shouldHideAppNavigation } from './shared/utils/app-shell-navigation';
import { MediaModeService } from './shared/services/media-mode.service';
import { registerAppIcons } from './shared/icons/register-icons';
import { ThemePreferenceService } from './shared/services/theme-preference.service';

/**
 * Paths that count as "top-level" for back-button purposes — pressing back
 * here doesn't pop anything off the nav stack, so we arm the
 * double-tap-to-exit flow instead. Everything else (detail pages, chat
 * threads, etc.) navigates back through history.
 */
const ROOT_PAGES: ReadonlySet<string> = new Set([
  '/',
  '/home',
  '/library',
  '/browse',
  '/quests',
  '/skill-tree',
  '/planning',
  '/statistic',
  '/profile',
  '/friends',
  '/notifications',
  '/settings',
]);

@Component({
    selector: 'app-root',
    templateUrl: 'app.component.html',
    styleUrls: ['app.component.scss'],
    imports: [IonApp, IonRouterOutlet, NavBarComponent, AiChatComponent, ErrorBannerComponent, GlobalSearchComponent, OfflineBannerComponent]
})
export class AppComponent implements OnInit {
  private releaseNotifications = inject(ReleaseNotificationService);
  private router = inject(Router);
  private mediaMode = inject(MediaModeService);
  private themePreference = inject(ThemePreferenceService);
  private renderer = inject(Renderer2);
  private document = inject<Document>(DOCUMENT);
  private platform = inject(Platform);
  private toastCtrl = inject(ToastController);

  private destroyRef = inject(DestroyRef);

  title = 'web-app';
  darkMode = this.themePreference.darkMode;
  showShellNavigation = signal<boolean>(true);

  /** Set when the user taps back on a root page; cleared after 2 s. */
  private exitArmed = false;
  private exitArmedTimer: ReturnType<typeof setTimeout> | null = null;

  @HostBinding('class.dark') get mode() { return this.darkMode(); }

  constructor() {
    // Register every Ionicons name the app uses, once, at startup. Replaces
    // the foot-gun where each page had to remember every icon used by any
    // descendant. See shared/icons/register-icons.ts.
    registerAppIcons();
    effect(() => this.applyColorTheme(this.darkMode()));
    effect(() => this.applyMediaTheme(this.mediaMode.mode().themeClass));
  }

  ngOnInit(): void {
    this.releaseNotifications.init();
    this.updateShellState(this.router.url);
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((event) => this.updateShellState(event.urlAfterRedirects));

    this.registerAndroidBackButton();
  }

  /**
   * On Android, the hardware/gesture back button fires through Ionic's
   * Platform service. We register at a low priority (10) so any open modal,
   * popover, action sheet, or alert (Ionic registers its own handlers at
   * 100+) dismisses first. Once the gesture reaches us we decide:
   *
   * - Auth screens                       → exit (no real back)
   * - Detail / non-root pages            → pop via browser history (Angular
   *                                        router picks up the popstate)
   * - Root tab/standalone pages          → arm "press back again to exit"
   *                                        for 2 s, then exit on second tap
   *
   * iOS has no hardware back and web uses the browser's own button, so
   * we only wire this up on native Android.
   */
  private registerAndroidBackButton(): void {
    if (!Capacitor.isNativePlatform() || Capacitor.getPlatform() !== 'android') {
      return;
    }

    this.platform.backButton.subscribeWithPriority(10, () => {
      void this.handleAndroidBack();
    });
  }

  private async handleAndroidBack(): Promise<void> {
    const url = (this.router.url || '/').split(/[?#]/)[0] || '/';

    if (url.startsWith('/auth')) {
      await App.exitApp();
      return;
    }

    if (!ROOT_PAGES.has(url)) {
      // Use the browser/Capacitor history stack — Angular Router observes
      // popstate and navigates accordingly. Works the same inside the
      // tabs outlet and outside it.
      window.history.back();
      return;
    }

    if (this.exitArmed) {
      await App.exitApp();
      return;
    }

    this.exitArmed = true;
    if (this.exitArmedTimer) {
      clearTimeout(this.exitArmedTimer);
    }
    this.exitArmedTimer = setTimeout(() => {
      this.exitArmed = false;
      this.exitArmedTimer = null;
    }, 2000);

    const toast = await this.toastCtrl.create({
      message: 'Press back again to exit',
      duration: 1800,
      position: 'bottom',
      cssClass: 'app-exit-toast',
    });
    await toast.present();
  }

  private updateShellState(url: string): void {
    this.showShellNavigation.set(!shouldHideAppNavigation(url));
  }

  private applyMediaTheme(themeClass: string): void {
    for (const mode of this.mediaMode.options) {
      this.renderer.removeClass(this.document.body, mode.themeClass);
    }

    this.renderer.addClass(this.document.body, themeClass);
  }

  private applyColorTheme(darkMode: boolean): void {
    const root = this.document.documentElement;
    const body = this.document.body;

    if (darkMode) {
      this.renderer.addClass(root, 'dark');
      this.renderer.addClass(root, 'ion-palette-dark');
      this.renderer.removeClass(root, 'theme-light');
      this.renderer.removeClass(body, 'theme-light');
      this.renderer.addClass(body, 'theme-dark');
      this.renderer.setStyle(root, 'color-scheme', 'dark');
      return;
    }

    this.renderer.removeClass(root, 'dark');
    this.renderer.removeClass(root, 'ion-palette-dark');
    this.renderer.removeClass(body, 'theme-dark');
    this.renderer.addClass(root, 'theme-light');
    this.renderer.addClass(body, 'theme-light');
    this.renderer.setStyle(root, 'color-scheme', 'light');
  }
}
