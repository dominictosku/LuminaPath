import { Component, OnInit, inject } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { IonContent } from '@ionic/angular/standalone';
import { Credentials } from 'src/app/core/auth/models/user.model';
import { AuthService } from '../../services/auth.service';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';

@Component({
    selector: 'app-login',
    templateUrl: './login.page.html',
    styleUrls: ['./login.page.scss'],
    imports: [IonContent, FormsModule, RouterLink]
})
export class LoginPage implements OnInit {
  private authService = inject(AuthService);
  private route = inject(Router);
  private apiEndpoint = inject(ApiEndpointService);

  credentials = new Credentials()
  twoFactorRequired = false;
  twoFactorCode = '';
  recoveryCode = '';
  useRecoveryCode = false;
  errorMessage = '';
  isSubmitting = false;
  apiSettingsOpen = false;
  apiEndpointDraft = '';
  currentApiEndpoint = '';
  apiSettingsMessage = '';

  ngOnInit() {
    this.currentApiEndpoint = this.apiEndpoint.endpoint();
    this.apiEndpointDraft = this.currentApiEndpoint;
  }

  async login(){
    this.errorMessage = '';

    const validationMessage = this.validateLoginInput();
    if (validationMessage) {
      this.errorMessage = validationMessage;
      return;
    }

    this.isSubmitting = true;

    try {
      const result = await firstValueFrom(this.authService.login(this.createLoginPayload()));
      if (result.requiresTwoFactor) {
        this.twoFactorRequired = true;
        this.twoFactorCode = '';
        this.recoveryCode = '';
        return;
      }

      this.clearSensitiveFields();
      this.route.navigate(['/home']);
    } catch (error: unknown) {
      this.errorMessage = this.resolveLoginErrorMessage(error);
    } finally {
      this.isSubmitting = false;
    }
  }

  /**
   * Picks the right error copy to surface in the red error box. Order
   * matters: the 2FA-step error wins when the user is on the code
   * screen (otherwise they'd see "awaiting approval" when the code is
   * just wrong), then we check for the inactive-account block-reason
   * header from the backend, then fall back to the generic message.
   */
  private resolveLoginErrorMessage(error: unknown): string {
    if (this.twoFactorRequired) {
      return 'Verification failed. Check the code and try again.';
    }

    if (this.authService.getLoginBlockedReason(error) === 'InactiveAccount') {
      return 'Your account is awaiting administrator approval. ' +
        'An administrator has to activate it before you can sign in.';
    }

    return 'Login failed. Check the API is running and the credentials are correct.';
  }

  showAuthenticatorCode() {
    this.useRecoveryCode = false;
    this.errorMessage = '';
  }

  showRecoveryCode() {
    this.useRecoveryCode = true;
    this.errorMessage = '';
  }

  backToPasswordLogin() {
    this.twoFactorRequired = false;
    this.twoFactorCode = '';
    this.recoveryCode = '';
    this.errorMessage = '';
  }

  toggleApiSettings() {
    this.apiSettingsOpen = !this.apiSettingsOpen;
    this.apiSettingsMessage = '';
    this.apiEndpointDraft = this.apiEndpoint.endpoint();
  }

  saveApiEndpoint() {
    this.currentApiEndpoint = this.apiEndpoint.setEndpoint(this.apiEndpointDraft);
    this.apiEndpointDraft = this.currentApiEndpoint;
    this.apiSettingsMessage = 'API server saved.';
    this.errorMessage = '';
    this.backToPasswordLogin();
    this.authService.clearSession();
  }

  resetApiEndpoint() {
    this.currentApiEndpoint = this.apiEndpoint.resetEndpoint();
    this.apiEndpointDraft = this.currentApiEndpoint;
    this.apiSettingsMessage = 'Default API server restored.';
    this.errorMessage = '';
    this.backToPasswordLogin();
    this.authService.clearSession();
  }

  private createLoginPayload(): Credentials {
    const payload = new Credentials();
    payload.email = this.credentials.email.trim();
    payload.password = this.credentials.password;

    if (!this.twoFactorRequired) {
      return payload;
    }

    if (this.useRecoveryCode) {
      payload.twoFactorRecoveryCode = this.recoveryCode.replace(/\s+/g, '');
    } else {
      payload.twoFactorCode = this.twoFactorCode.replace(/[\s-]+/g, '');
    }

    return payload;
  }

  private validateLoginInput(): string {
    if (!this.credentials.email.trim() || !this.credentials.password) {
      return 'Enter your email and password.';
    }

    if (!this.twoFactorRequired) {
      return '';
    }

    if (this.useRecoveryCode) {
      return this.recoveryCode.trim() ? '' : 'Enter a recovery code.';
    }

    return this.twoFactorCode.trim() ? '' : 'Enter your authenticator code.';
  }

  private clearSensitiveFields() {
    this.credentials.password = '';
    this.twoFactorRequired = false;
    this.twoFactorCode = '';
    this.recoveryCode = '';
    this.useRecoveryCode = false;
  }
}
