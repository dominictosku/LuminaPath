import { Component, OnInit, inject } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { IonContent } from '@ionic/angular/standalone';
import { Credentials } from 'src/app/core/auth/models/user.model';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';

@Component({
    selector: 'app-login',
    templateUrl: './login.page.html',
    styleUrls: ['./login.page.scss'],
    imports: [IonContent, FormsModule]
})
export class LoginPage implements OnInit {
  private authService = inject(AuthService);
  private route = inject(Router);
  private apiEndpoint = inject(ApiEndpointService);

  credentials = new Credentials()
  errorMessage = '';
  isSubmitting = false;
  apiSettingsOpen = false;
  apiEndpointDraft = '';
  currentApiEndpoint = '';
  apiSettingsMessage = '';

  constructor() {
    this.credentials.email = "admin@example.com"
    this.credentials.password = "Admin123*"
  }

  ngOnInit() {
    this.currentApiEndpoint = this.apiEndpoint.endpoint();
    this.apiEndpointDraft = this.currentApiEndpoint;
  }

  async login(){
    this.errorMessage = '';
    this.isSubmitting = true;

    try {
      await firstValueFrom(this.authService.login(this.credentials));
      this.route.navigate(['/home']);
    } catch {
      this.errorMessage = 'Login failed. Check the API is running and the credentials are correct.';
    } finally {
      this.isSubmitting = false;
    }
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
    this.authService.clearSession();
  }

  resetApiEndpoint() {
    this.currentApiEndpoint = this.apiEndpoint.resetEndpoint();
    this.apiEndpointDraft = this.currentApiEndpoint;
    this.apiSettingsMessage = 'Default API server restored.';
    this.errorMessage = '';
    this.authService.clearSession();
  }
}
