import { Component, DestroyRef, HostBinding, Inject, inject, OnInit, Renderer2, signal } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { IonApp, IonRouterOutlet } from '@ionic/angular/standalone';
import { initFlowbite } from 'flowbite';
import { NavBarComponent } from './shared/components/navigation/nav-bar/nav-bar.component'
import { AiChatComponent } from './shared/components/ai-chat/ai-chat.component';
import { ReleaseNotificationService } from './shared/services/release-notification.service';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { shouldHideAppNavigation } from './shared/utils/app-shell-navigation';
import { MediaModeService } from './shared/services/media-mode.service';

@Component({
    selector: 'app-root',
    templateUrl: 'app.component.html',
    styleUrls: ['app.component.scss'],
    imports: [IonApp, IonRouterOutlet, NavBarComponent, AiChatComponent]
})
export class AppComponent implements OnInit {
  private destroyRef = inject(DestroyRef);

  constructor(
    private releaseNotifications: ReleaseNotificationService,
    private router: Router,
    private mediaMode: MediaModeService,
    private renderer: Renderer2,
    @Inject(DOCUMENT) private document: Document,
  ) { }

  title = 'web-app';
  darkMode = signal<boolean>(true);
  showShellNavigation = signal<boolean>(true);

  @HostBinding('class.dark') get mode() { return this.darkMode(); }

  ngOnInit(): void {
    initFlowbite();
    this.releaseNotifications.init();
    this.mediaMode.mode$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((mode) => this.applyMediaTheme(mode.themeClass));
    this.updateShellState(this.router.url);
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((event) => this.updateShellState(event.urlAfterRedirects));
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
}
