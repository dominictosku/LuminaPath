import { Component, OnInit } from '@angular/core';
import { IonButton, IonIcon, IonHeader } from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import { logoAmplify } from 'ionicons/icons';
import { AuthService } from 'src/app/core/auth/services/auth.service';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';

@Component({
    selector: 'app-nav-bar',
    templateUrl: './nav-bar.component.html',
    styleUrls: ['./nav-bar.component.scss'],
    imports: [CommonModule, IonButton, IonIcon, IonHeader]
})
export class NavBarComponent implements OnInit {
  isLoggingOut = false;

  constructor(private authService: AuthService, public router: Router) {
    addIcons({ logoAmplify });
  }

  ngOnInit() { }

  get isAuthPage() {
    return this.router.url.startsWith('/auth');
  }

  logout() {
    if (this.isLoggingOut) {
      return;
    }

    this.isLoggingOut = true;
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
