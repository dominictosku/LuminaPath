import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  IonBadge,
  IonContent,
  IonIcon,
  IonRefresher,
  IonRefresherContent,
  IonToggle,
} from '@ionic/angular/standalone';
import {
  NotificationCategory,
  NotificationCategoryPreference,
  NotificationItem,
  NotificationsService,
} from 'src/app/shared/services/notifications.service';

@Component({
  selector: 'app-notifications-page',
  templateUrl: './notifications.page.html',
  styleUrls: ['./notifications.page.scss'],
  imports: [
    RouterLink,
    IonBadge,
    IonContent,
    IonIcon,
    IonRefresher,
    IonRefresherContent,
    IonToggle,
  ],
})
export class NotificationsPage implements OnInit {
  private notificationsService = inject(NotificationsService);

  items: NotificationItem[] = [];
  preferences: NotificationCategoryPreference[] = [];
  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.preferences = this.notificationsService.preferences();
    void this.load();
  }

  get activeItems(): NotificationItem[] {
    return this.items.filter((item) => item.active !== false);
  }

  get historyItems(): NotificationItem[] {
    return this.items.filter((item) => item.active === false);
  }

  get enabledCategoryCount(): number {
    return this.preferences.filter((pref) => pref.enabled).length;
  }

  async load(event?: CustomEvent): Promise<void> {
    this.isLoading = true;
    this.errorMessage = '';

    try {
      this.items = await firstValueFrom(this.notificationsService.loadFeed());
    } catch {
      this.items = [];
      this.errorMessage = 'Notifications could not be loaded.';
    } finally {
      this.isLoading = false;
      this.completeRefresh(event);
    }
  }

  setPreference(category: NotificationCategory, enabled: boolean): void {
    this.preferences = this.notificationsService.setCategoryEnabled(category, enabled);
    void this.load();
  }

  dateLabel(item: NotificationItem): string {
    const date = new Date(item.sortAt);
    if (Number.isNaN(date.getTime())) return '';
    return new Intl.DateTimeFormat('en', {
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    }).format(date);
  }

  statusLabel(item: NotificationItem): string {
    return item.active === false ? 'History' : 'Active';
  }

  trackByItem(_: number, item: NotificationItem): string {
    return item.id;
  }

  trackByPreference(_: number, item: NotificationCategoryPreference): string {
    return item.id;
  }

  private completeRefresh(event?: CustomEvent): void {
    const target = event?.target as HTMLIonRefresherElement | undefined;
    target?.complete();
  }
}

