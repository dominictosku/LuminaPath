
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  IonButton,
  IonContent,
  IonIcon,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  keyOutline,
  lockClosedOutline,
  logOutOutline,
  notificationsOffOutline,
  notificationsOutline,
  personCircleOutline,
  saveOutline,
  settingsOutline,
  shieldCheckmarkOutline,
} from 'ionicons/icons';
import { firstValueFrom, finalize } from 'rxjs';
import { AuthService } from 'src/app/core/auth/services/auth.service';
import { ReleaseNotificationService } from 'src/app/shared/services/release-notification.service';
import { ProfileService } from '../services/profile.service';

type FormState = 'idle' | 'saving' | 'success' | 'error';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.page.html',
  styleUrls: ['./settings.page.scss'],
  imports: [
    FormsModule,
    RouterLink,
    IonButton,
    IonContent,
    IonIcon
],
})
export class SettingsPage {
  private profileService = inject(ProfileService);
  private releaseNotifications = inject(ReleaseNotificationService);
  private authService = inject(AuthService);
  private router = inject(Router);

  passwordDraft = {
    oldPassword: '',
    newPassword: '',
    confirmPassword: '',
  };

  passwordState: FormState = 'idle';
  passwordMessage = '';

  notificationsState: 'idle' | 'working' = 'idle';
  notificationsMessage = '';

  isLoggingOut = false;

  constructor() {
    addIcons({
      keyOutline,
      lockClosedOutline,
      logOutOutline,
      notificationsOffOutline,
      notificationsOutline,
      personCircleOutline,
      saveOutline,
      settingsOutline,
      shieldCheckmarkOutline,
    });
  }

  async changePassword(): Promise<void> {
    const { oldPassword, newPassword, confirmPassword } = this.passwordDraft;

    if (!oldPassword || !newPassword) {
      this.passwordState = 'error';
      this.passwordMessage = 'Enter both your current and new password.';
      return;
    }
    if (newPassword !== confirmPassword) {
      this.passwordState = 'error';
      this.passwordMessage = 'New password and confirmation do not match.';
      return;
    }
    if (newPassword.length < 6) {
      this.passwordState = 'error';
      this.passwordMessage = 'Pick a password with at least 6 characters.';
      return;
    }

    this.passwordState = 'saving';
    this.passwordMessage = '';

    try {
      await firstValueFrom(
        this.profileService.changePassword({ oldPassword, newPassword }),
      );
      this.passwordState = 'success';
      this.passwordMessage = 'Password changed.';
      this.passwordDraft = { oldPassword: '', newPassword: '', confirmPassword: '' };
    } catch (error) {
      this.passwordState = 'error';
      this.passwordMessage = errorTextFrom(error) ?? 'Could not change password.';
    }
  }

  async clearReleaseNotifications(): Promise<void> {
    this.notificationsState = 'working';
    this.notificationsMessage = '';
    try {
      await this.releaseNotifications.clearAll();
      this.notificationsMessage = 'Cleared all scheduled release reminders.';
    } catch {
      this.notificationsMessage = 'Could not clear release reminders.';
    } finally {
      this.notificationsState = 'idle';
    }
  }

  logout(): void {
    if (this.isLoggingOut) return;

    this.isLoggingOut = true;
    this.authService.logout()
      .pipe(finalize(() => (this.isLoggingOut = false)))
      .subscribe({
        next: () => this.router.navigate(['/auth/login'], { replaceUrl: true }),
        error: () => {
          this.authService.clearSession();
          this.router.navigate(['/auth/login'], { replaceUrl: true });
        },
      });
  }
}

function errorTextFrom(error: unknown): string | null {
  const payload = (error as { error?: unknown })?.error;
  if (typeof payload === 'string') return payload;
  if (payload && typeof payload === 'object') {
    const obj = payload as Record<string, unknown>;
    if (typeof obj['detail'] === 'string') return obj['detail'] as string;
    if (typeof obj['title'] === 'string') return obj['title'] as string;
    if (typeof obj['message'] === 'string') return obj['message'] as string;
    const errors = obj['errors'];
    if (errors && typeof errors === 'object') {
      const messages: string[] = [];
      for (const value of Object.values(errors as Record<string, unknown>)) {
        if (Array.isArray(value)) messages.push(...value.map((v) => String(v)));
      }
      if (messages.length) return messages.join(' ');
    }
  }
  return null;
}
