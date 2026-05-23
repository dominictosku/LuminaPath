import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { AlertController, IonButton, IonIcon, IonSpinner } from '@ionic/angular/standalone';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

import { AuthService } from 'src/app/core/auth/services/auth.service';
import { extractErrorMessage } from 'src/app/shared/utils/extract-error';

import {
  TwoFactorResponse,
  TwoFactorService,
} from '../../services/two-factor.service';
import {
  authenticatorQrSvg,
  buildAuthenticatorUri,
  formatSharedKey,
} from './authenticator-qr';

type Phase = 'idle' | 'setup' | 'verifying' | 'enabled-success';

/**
 * Settings-page card for managing two-factor authentication.
 *
 * Drives the ASP.NET Core Identity `/manage/2fa` endpoint end to end:
 *   1. On init → POST {} → returns current status + an auto-generated
 *      shared key the user can enrol in their authenticator app.
 *   2. "Set up" reveals the QR + manual entry key + code input.
 *   3. Verify → POST { enable: true, twoFactorCode } → on success, the
 *      server includes one-time recovery codes the user must save.
 *   4. When enabled the card exposes disable / reset-key / regenerate
 *      recovery codes / forget-device controls.
 *
 * Recovery codes only come back on the request that generated them, so
 * we render them once with a clear "save these now" warning and clear
 * the state on any subsequent reload.
 */
@Component({
  selector: 'app-two-factor-card',
  templateUrl: './two-factor-card.component.html',
  styleUrls: ['./two-factor-card.component.scss'],
  imports: [FormsModule, IonButton, IonIcon, IonSpinner],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TwoFactorCardComponent implements OnInit {
  private readonly twoFactor = inject(TwoFactorService);
  private readonly auth = inject(AuthService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly alertController = inject(AlertController);

  readonly status = signal<TwoFactorResponse | null>(null);
  readonly phase = signal<Phase>('idle');
  readonly errorMessage = signal<string>('');
  readonly successMessage = signal<string>('');
  readonly isLoading = signal<boolean>(false);
  readonly isWorking = signal<boolean>(false);
  /** Codes are only available on the request that minted them. */
  readonly freshRecoveryCodes = signal<string[] | null>(null);
  readonly accountLabel = signal<string>('');
  readonly verificationCode = signal<string>('');

  readonly isEnabled = computed(() => this.status()?.isTwoFactorEnabled === true);
  readonly isInSetup = computed(() => this.phase() === 'setup' || this.phase() === 'verifying');
  readonly recoveryCodesLeft = computed(() => this.status()?.recoveryCodesLeft ?? 0);
  readonly isMachineRemembered = computed(() => this.status()?.isMachineRemembered === true);

  readonly formattedSharedKey = computed(() => {
    const key = this.status()?.sharedKey ?? '';
    return key ? formatSharedKey(key) : '';
  });

  readonly authenticatorUri = computed(() => {
    const key = this.status()?.sharedKey ?? '';
    return key ? buildAuthenticatorUri(this.accountLabel(), key) : '';
  });

  /** SVG string sanitised for [innerHTML]. */
  readonly qrSvg = computed<SafeHtml | null>(() => {
    const uri = this.authenticatorUri();
    if (!uri) return null;
    return this.sanitizer.bypassSecurityTrustHtml(authenticatorQrSvg(uri));
  });

  async ngOnInit(): Promise<void> {
    this.isLoading.set(true);
    try {
      // Best-effort enrich the otpauth label with the user's email.
      // Falls back silently if the call fails — the QR still works.
      const userInfo = await firstValueFrom(this.auth.getUserInfo()).catch(() => null);
      if (userInfo?.email) {
        this.accountLabel.set(userInfo.email);
      }
      const initial = await firstValueFrom(this.twoFactor.load());
      this.status.set(initial);
    } catch (error) {
      this.errorMessage.set(
        extractErrorMessage(error, 'Could not load two-factor status.'),
      );
    } finally {
      this.isLoading.set(false);
    }
  }

  beginSetup(): void {
    this.errorMessage.set('');
    this.successMessage.set('');
    this.verificationCode.set('');
    this.phase.set('setup');
  }

  cancelSetup(): void {
    if (this.phase() === 'verifying') return;
    this.phase.set('idle');
    this.verificationCode.set('');
    this.errorMessage.set('');
  }

  async verifyAndEnable(): Promise<void> {
    const raw = this.verificationCode().replace(/\s|-/g, '');
    if (raw.length < 6) {
      this.errorMessage.set('Enter the 6-digit code from your authenticator app.');
      return;
    }

    this.phase.set('verifying');
    this.errorMessage.set('');
    try {
      const response = await firstValueFrom(this.twoFactor.enable(raw));
      this.status.set(response);
      this.freshRecoveryCodes.set(response.recoveryCodes ?? null);
      this.successMessage.set(
        response.recoveryCodes?.length
          ? 'Two-factor authentication is on. Save your recovery codes below.'
          : 'Two-factor authentication is on.',
      );
      this.phase.set('enabled-success');
      this.verificationCode.set('');
    } catch (error) {
      this.phase.set('setup');
      this.errorMessage.set(
        extractErrorMessage(error, 'Verification code is invalid. Try again.'),
      );
    }
  }

  async confirmDisable(): Promise<void> {
    const alert = await this.alertController.create({
      header: 'Disable two-factor authentication?',
      message:
        'Your account will rely on the password alone. Your authenticator entry and recovery codes will stop working.',
      buttons: [
        { text: 'Keep enabled', role: 'cancel' },
        {
          text: 'Disable',
          role: 'destructive',
          handler: () => {
            void this.runAction(() => this.twoFactor.disable(), 'Two-factor authentication is off.');
          },
        },
      ],
    });
    await alert.present();
  }

  async confirmResetSharedKey(): Promise<void> {
    const alert = await this.alertController.create({
      header: 'Reset the authenticator key?',
      message:
        'This invalidates your current authenticator entry and disables 2FA until you set it up again.',
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: 'Reset',
          role: 'destructive',
          handler: () => {
            void this.runAction(
              () => this.twoFactor.resetSharedKey(),
              'Shared key reset. Re-enrol your authenticator app.',
              { goIntoSetup: true },
            );
          },
        },
      ],
    });
    await alert.present();
  }

  async confirmRegenerateRecoveryCodes(): Promise<void> {
    const alert = await this.alertController.create({
      header: 'Generate new recovery codes?',
      message: 'Your previous recovery codes will stop working immediately.',
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: 'Generate',
          handler: () => {
            void this.runAction(
              () => this.twoFactor.regenerateRecoveryCodes(),
              'New recovery codes generated. Save them now — they won\'t be shown again.',
              { keepRecoveryCodes: true },
            );
          },
        },
      ],
    });
    await alert.present();
  }

  async forgetMachine(): Promise<void> {
    await this.runAction(
      () => this.twoFactor.forgetMachine(),
      'This device will challenge for a code on next sign-in.',
    );
  }

  copyRecoveryCodes(): void {
    const codes = this.freshRecoveryCodes();
    if (!codes?.length || !navigator?.clipboard) return;
    void navigator.clipboard.writeText(codes.join('\n'));
    this.successMessage.set('Recovery codes copied to clipboard.');
  }

  dismissRecoveryCodes(): void {
    // Clear them from memory once the user confirms they're saved.
    this.freshRecoveryCodes.set(null);
    if (this.phase() === 'enabled-success') {
      this.phase.set('idle');
    }
  }

  private async runAction(
    action: () => ReturnType<TwoFactorService['load']>,
    okMessage: string,
    options: { goIntoSetup?: boolean; keepRecoveryCodes?: boolean } = {},
  ): Promise<void> {
    if (this.isWorking()) return;
    this.isWorking.set(true);
    this.errorMessage.set('');
    try {
      const response = await firstValueFrom(action());
      this.status.set(response);
      if (response.recoveryCodes?.length) {
        this.freshRecoveryCodes.set(response.recoveryCodes);
      } else if (!options.keepRecoveryCodes) {
        this.freshRecoveryCodes.set(null);
      }
      this.successMessage.set(okMessage);
      if (options.goIntoSetup) {
        this.phase.set('setup');
      }
    } catch (error) {
      this.errorMessage.set(extractErrorMessage(error, 'Action failed. Try again.'));
    } finally {
      this.isWorking.set(false);
    }
  }
}
