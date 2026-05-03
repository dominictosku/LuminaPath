import { Component, OnInit } from '@angular/core';
import { IonIcon, IonHeader } from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  calendarClearOutline,
  gameControllerOutline,
  gridOutline,
  hourglassOutline,
  libraryOutline,
  logOutOutline,
  notificationsOutline,
  personCircleOutline,
  sparklesOutline,
} from 'ionicons/icons';
import { AuthService } from 'src/app/core/auth/services/auth.service';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';

@Component({
    selector: 'app-nav-bar',
    templateUrl: './nav-bar.component.html',
    styleUrls: ['./nav-bar.component.scss'],
    imports: [CommonModule, IonIcon, IonHeader]
})
export class NavBarComponent implements OnInit {
  isLoggingOut = false;
  openMenu: 'notifications' | 'apps' | null = null;

  constructor(private authService: AuthService, public router: Router) {
    addIcons({
      calendarClearOutline,
      gameControllerOutline,
      gridOutline,
      hourglassOutline,
      libraryOutline,
      logOutOutline,
      notificationsOutline,
      personCircleOutline,
      sparklesOutline,
    });
  }

  ngOnInit() { }

  get isAuthPage() {
    return this.router.url.startsWith('/auth');
  }

  isActive(path: string): boolean {
    return this.router.url === path || this.router.url.startsWith(`${path}/`);
  }

  toggleMenu(menu: 'notifications' | 'apps') {
    this.openMenu = this.openMenu === menu ? null : menu;
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
}
