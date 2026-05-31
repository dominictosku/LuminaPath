import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AlertController, IonButton, IonIcon, IonSpinner } from '@ionic/angular/standalone';

import { extractErrorMessage } from 'src/app/shared/utils/extract-error';
import {
  GoogleCalendarService,
  GoogleCalendarStatus,
} from '../../services/google-calendar.service';

type Tone = 'success' | 'error' | 'neutral';

/**
 * Settings card for the Google Calendar integration: link/unlink the account
 * and push library releases + dated quests on demand ("Sync now"). The card
 * hides itself entirely when the server has no Google credentials configured.
 * Linking is a full-page navigation to the backend connect endpoint, so it is
 * an anchor (`ion-button [href]`) rather than an XHR.
 */
@Component({
  selector: 'app-google-calendar-card',
  templateUrl: './google-calendar-card.component.html',
  styleUrls: ['./google-calendar-card.component.scss'],
  imports: [DatePipe, IonButton, IonIcon, IonSpinner],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoogleCalendarCardComponent implements OnInit {
  private readonly service = inject(GoogleCalendarService);
  private readonly alertController = inject(AlertController);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly status = signal<GoogleCalendarStatus | null>(null);
  readonly isLoading = signal(true);
  readonly isSyncing = signal(false);
  readonly isDisconnecting = signal(false);
  readonly message = signal('');
  readonly messageTone = signal<Tone>('neutral');

  readonly configured = computed(() => this.status()?.configured ?? false);
  readonly connected = computed(() => this.status()?.connected ?? false);
  readonly email = computed(() => this.status()?.email ?? null);
  readonly lastSyncedAt = computed(() => this.status()?.lastSyncedAt ?? null);

  async ngOnInit(): Promise<void> {
    this.applyReturnStatus();
    await this.reload();
  }

  connectHref(): string {
    return this.service.connectUrl();
  }

  async syncNow(): Promise<void> {
    if (this.isSyncing()) return;
    this.isSyncing.set(true);
    this.message.set('');
    try {
      const result = await firstValueFrom(this.service.sync());
      this.setMessage(result.message, 'success');
      await this.reload();
    } catch (error) {
      this.setMessage(extractErrorMessage(error, 'Could not sync to Google Calendar.'), 'error');
    } finally {
      this.isSyncing.set(false);
    }
  }

  async confirmDisconnect(): Promise<void> {
    const alert = await this.alertController.create({
      header: 'Disconnect Google Calendar?',
      message:
        'LuminaPath will stop syncing and forget your Google authorization. Events already in your "LuminaPath" calendar stay until you delete them in Google.',
      buttons: [
        { text: 'Keep connected', role: 'cancel' },
        {
          text: 'Disconnect',
          role: 'destructive',
          handler: () => void this.disconnect(),
        },
      ],
    });
    await alert.present();
  }

  private async disconnect(): Promise<void> {
    if (this.isDisconnecting()) return;
    this.isDisconnecting.set(true);
    this.message.set('');
    try {
      await firstValueFrom(this.service.disconnect());
      this.setMessage('Google Calendar disconnected.', 'neutral');
      await this.reload();
    } catch (error) {
      this.setMessage(extractErrorMessage(error, 'Could not disconnect.'), 'error');
    } finally {
      this.isDisconnecting.set(false);
    }
  }

  private async reload(): Promise<void> {
    this.isLoading.set(true);
    try {
      this.status.set(await firstValueFrom(this.service.getStatus()));
    } catch {
      this.status.set(null);
    } finally {
      this.isLoading.set(false);
    }
  }

  /** Surface the outcome of the OAuth round-trip (?google=connected|error|unavailable). */
  private applyReturnStatus(): void {
    const outcome = this.route.snapshot.queryParamMap.get('google');
    if (!outcome) return;

    switch (outcome) {
      case 'connected':
        this.setMessage('Google Calendar connected.', 'success');
        break;
      case 'unavailable':
        this.setMessage("Google Calendar isn't configured on this server.", 'error');
        break;
      default:
        this.setMessage('Could not connect Google Calendar. Please try again.', 'error');
        break;
    }

    // Drop the param so a refresh doesn't replay the message.
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { google: null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  private setMessage(text: string, tone: Tone): void {
    this.message.set(text);
    this.messageTone.set(tone);
  }
}
