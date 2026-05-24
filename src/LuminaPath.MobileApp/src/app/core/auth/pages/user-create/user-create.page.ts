import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { IonContent } from '@ionic/angular/standalone';
import { firstValueFrom } from 'rxjs';

import { AuthService, RegisterFieldError } from '../../services/auth.service';

/**
 * Self-registration page. Mirrors LoginPage in look + interaction model
 * (orb background, dark card, gradient sign-in button) so the two screens
 * read as one coherent flow when the user toggles between them.
 *
 * Two notable bits of behaviour:
 *
 * 1. We do NOT auto-sign-in after a successful POST /register. With the
 *    admin-approval gate on (production default), sign-in would just
 *    bounce off LuminaSignInManager.CanSignInAsync and confuse the
 *    user. Instead we show a "success" screen explaining that the
 *    account exists, with a link back to the login page. Operators
 *    running with approval-off can sign in from there immediately.
 *
 * 2. The form is a real <form> with ngSubmit so the browser fires submit
 *    on Enter from any field — matches LoginPage's keyboard semantics.
 */
@Component({
  selector: 'app-user-create',
  templateUrl: './user-create.page.html',
  styleUrls: ['./user-create.page.scss'],
  imports: [IonContent, FormsModule, RouterLink],
})
export class UserCreatePage {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  email = '';
  password = '';
  confirmPassword = '';

  isSubmitting = false;
  /** General-purpose form-level error (validation, network, etc.). */
  errorMessage = '';
  /**
   * Per-field errors from the backend's ValidationProblemDetails (e.g.
   * `PasswordTooShort` against the `Password` field). Rendered as a
   * bullet list under the error banner.
   */
  fieldErrors: RegisterFieldError[] = [];
  /** Flipped to true on a successful POST /register. */
  registrationComplete = false;

  async register() {
    this.errorMessage = '';
    this.fieldErrors = [];

    const validationMessage = this.validateInput();
    if (validationMessage) {
      this.errorMessage = validationMessage;
      return;
    }

    this.isSubmitting = true;
    try {
      const result = await firstValueFrom(
        this.authService.register(this.email.trim(), this.password),
      );

      if (result.created) {
        this.registrationComplete = true;
        // Clear sensitive fields once the account exists — the user
        // doesn't need them on screen anymore, and we don't want them
        // round-tripped if the page is restored from the bfcache.
        this.password = '';
        this.confirmPassword = '';
        return;
      }

      this.fieldErrors = result.errors;
      this.errorMessage = result.errors.length === 1
        ? result.errors[0].message
        : 'We couldn\'t create the account. See the issues below.';
    } catch {
      this.errorMessage = 'Registration failed. Check the API is running and try again.';
    } finally {
      this.isSubmitting = false;
    }
  }

  goToLogin() {
    this.router.navigate(['/auth/login']);
  }

  private validateInput(): string {
    if (!this.email.trim()) {
      return 'Enter your email.';
    }
    if (!this.password) {
      return 'Choose a password.';
    }
    if (this.password !== this.confirmPassword) {
      return 'Passwords don\'t match.';
    }
    // The backend enforces the full policy (length, complexity, unique
    // chars) and we surface its message verbatim. Doing a lightweight
    // pre-check here just for UX so common typos don't roundtrip.
    if (this.password.length < 10) {
      return 'Password must be at least 10 characters long.';
    }
    return '';
  }
}
