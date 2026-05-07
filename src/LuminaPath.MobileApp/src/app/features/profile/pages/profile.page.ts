import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  IonButton,
  IonContent,
  IonIcon,
  IonRefresher,
  IonRefresherContent,
  IonSpinner,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  cardOutline,
  mailOutline,
  personCircleOutline,
  saveOutline,
  settingsOutline,
  timeOutline,
} from 'ionicons/icons';
import { firstValueFrom } from 'rxjs';
import { User } from 'src/app/core/auth/models/user.model';
import { AuthService } from 'src/app/core/auth/services/auth.service';
import { ProfileService, ProfileUpdate } from '../services/profile.service';

type FormState = 'idle' | 'saving' | 'success' | 'error';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.page.html',
  styleUrls: ['./profile.page.scss'],
  imports: [
    CommonModule,
    FormsModule,
    IonButton,
    IonContent,
    IonIcon,
    IonRefresher,
    IonRefresherContent,
    IonSpinner,
  ],
})
export class ProfilePage implements OnInit {
  user = new User();
  isLoading = true;
  state: FormState = 'idle';
  message = '';

  draft = {
    userName: '',
    email: '',
    age: 0,
    currentPassword: '',
  };

  constructor(
    private authService: AuthService,
    private profileService: ProfileService,
  ) {
    addIcons({
      cardOutline,
      mailOutline,
      personCircleOutline,
      saveOutline,
      settingsOutline,
      timeOutline,
    });
  }

  async ngOnInit(): Promise<void> {
    await this.refresh();
  }

  async refresh(event?: CustomEvent): Promise<void> {
    this.isLoading = !event;

    try {
      const info = await firstValueFrom(this.authService.getUserInfo());
      this.user = info;
      this.applyUserToDraft();
    } catch {
      this.state = 'error';
      this.message = 'Could not load your profile.';
    } finally {
      this.isLoading = false;
      const target = event?.target as HTMLIonRefresherElement | undefined;
      target?.complete();
    }
  }

  get emailChanged(): boolean {
    return (this.draft.email ?? '') !== (this.user.email ?? '');
  }

  get hasChanges(): boolean {
    return (
      this.draft.userName !== this.user.userName ||
      this.draft.email !== this.user.email ||
      Number(this.draft.age) !== Number(this.user.age)
    );
  }

  async save(): Promise<void> {
    if (!this.hasChanges) {
      this.state = 'idle';
      this.message = 'Nothing to update.';
      return;
    }

    if (this.emailChanged && !this.draft.currentPassword) {
      this.state = 'error';
      this.message = 'Confirm your current password to change email.';
      return;
    }

    const update: ProfileUpdate = {};
    if (this.draft.userName !== this.user.userName) {
      update.userName = this.draft.userName.trim();
    }
    if (this.draft.email !== this.user.email) {
      update.newEmail = this.draft.email.trim();
      update.oldPassword = this.draft.currentPassword;
    }
    if (Number(this.draft.age) !== Number(this.user.age)) {
      update.age = Number(this.draft.age);
    }

    this.state = 'saving';
    this.message = '';

    try {
      await firstValueFrom(this.profileService.updateProfile(update));
      this.state = 'success';
      this.message = 'Profile saved.';
      this.draft.currentPassword = '';
      await this.refresh();
    } catch (error) {
      this.state = 'error';
      this.message = errorTextFrom(error) ?? 'Could not save profile.';
    }
  }

  private applyUserToDraft(): void {
    this.draft = {
      userName: this.user.userName ?? '',
      email: this.user.email ?? '',
      age: Number(this.user.age ?? 0),
      currentPassword: '',
    };
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
