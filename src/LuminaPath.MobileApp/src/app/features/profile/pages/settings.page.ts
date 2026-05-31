
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  IonButton,
  IonContent,
  IonIcon,
  IonToggle,
} from '@ionic/angular/standalone';
import { firstValueFrom, finalize } from 'rxjs';
import { AuthService } from 'src/app/core/auth/services/auth.service';
import { ReleaseNotificationService } from 'src/app/shared/services/release-notification.service';
import { RequestCache } from 'src/app/shared/services/request-cache.service';
import { ThemePreferenceService } from 'src/app/shared/services/theme-preference.service';
import { DataExportService } from '../services/data-export.service';
import { ProfileService } from '../services/profile.service';
import { TwoFactorCardComponent } from '../components/two-factor-card/two-factor-card.component';
import { GoogleCalendarCardComponent } from '../components/google-calendar-card/google-calendar-card.component';

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
    IonIcon,
    IonToggle,
    TwoFactorCardComponent,
    GoogleCalendarCardComponent,
],
})
export class SettingsPage {
  private profileService = inject(ProfileService);
  private releaseNotifications = inject(ReleaseNotificationService);
  private dataExport = inject(DataExportService);
  private themePreference = inject(ThemePreferenceService);
  private requestCache = inject(RequestCache);
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

  exportState: 'idle' | 'working' = 'idle';
  exportMessage = '';
  exportMessageTone: 'success' | 'error' | 'neutral' = 'neutral';

  offlineCacheState: 'idle' | 'working' = 'idle';
  offlineCacheMessage = '';

  darkThemeEnabled = this.themePreference.darkMode;

  isLoggingOut = false;

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

  async exportData(): Promise<void> {
    if (this.exportState === 'working') return;

    this.exportState = 'working';
    this.exportMessage = '';
    this.exportMessageTone = 'neutral';

    try {
      const download = await firstValueFrom(this.dataExport.downloadJson());
      triggerDownload(download.blob, download.fileName);
      this.exportMessage = 'Export downloaded.';
      this.exportMessageTone = 'success';
    } catch (error) {
      this.exportMessage = errorTextFrom(error) ?? 'Could not export your data.';
      this.exportMessageTone = 'error';
    } finally {
      this.exportState = 'idle';
    }
  }

  setDarkTheme(enabled: boolean): void {
    this.themePreference.setDarkMode(enabled);
  }

  /** Drop all persisted view snapshots (dashboard, statistic, library by
   *  kind, media detail per id). Next visit refetches from the server.
   *  Useful when the cached state diverged from the server (e.g. an
   *  external import) or when freeing disk space. */
  async clearOfflineCache(): Promise<void> {
    if (this.offlineCacheState === 'working') return;
    this.offlineCacheState = 'working';
    this.offlineCacheMessage = '';
    try {
      this.requestCache.clear();
      this.offlineCacheMessage = 'Offline cache cleared.';
    } catch {
      this.offlineCacheMessage = 'Could not clear the offline cache.';
    } finally {
      this.offlineCacheState = 'idle';
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

function triggerDownload(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName;
  anchor.rel = 'noopener';
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  setTimeout(() => URL.revokeObjectURL(url), 0);
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
