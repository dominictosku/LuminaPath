import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  IonAvatar,
  IonBadge,
  IonButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonIcon,
  IonItem,
  IonLabel,
  IonList,
  IonRefresher,
  IonRefresherContent,
  IonSearchbar,
  IonSegment,
  IonSegmentButton,
  IonSpinner,
  IonTitle,
  IonToolbar,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  chatbubbleEllipsesOutline,
  checkmarkOutline,
  closeOutline,
  personAddOutline,
  trashOutline,
} from 'ionicons/icons';
import { Subject, debounceTime, firstValueFrom, switchMap } from 'rxjs';
import { FriendUser, Friendship } from '../models/friend.model';
import { FriendsService } from '../services/friends.service';

type Tab = 'friends' | 'pending' | 'find';

@Component({
  selector: 'app-friends',
  templateUrl: './friends.page.html',
  styleUrls: ['./friends.page.scss'],
  imports: [
    CommonModule,
    FormsModule,
    IonAvatar,
    IonBadge,
    IonButton,
    IonButtons,
    IonContent,
    IonHeader,
    IonIcon,
    IonItem,
    IonLabel,
    IonList,
    IonRefresher,
    IonRefresherContent,
    IonSearchbar,
    IonSegment,
    IonSegmentButton,
    IonSpinner,
    IonTitle,
    IonToolbar,
  ],
})
export class FriendsPage implements OnInit {
  tab: Tab = 'friends';
  isLoading = false;
  errorMessage = '';

  friends: Friendship[] = [];
  pending: Friendship[] = [];

  searchQuery = '';
  searchResults: FriendUser[] = [];
  isSearching = false;
  private readonly searchInput$ = new Subject<string>();

  constructor(private friendsService: FriendsService, private router: Router) {
    addIcons({
      chatbubbleEllipsesOutline,
      checkmarkOutline,
      closeOutline,
      personAddOutline,
      trashOutline,
    });

    this.searchInput$
      .pipe(
        debounceTime(300),
        switchMap((query) => {
          this.isSearching = !!query.trim();
          return this.friendsService.search(query.trim());
        }),
      )
      .subscribe({
        next: (results) => {
          this.searchResults = this.filterOutExisting(results);
          this.isSearching = false;
        },
        error: () => {
          this.isSearching = false;
        },
      });
  }

  async ngOnInit(): Promise<void> {
    await this.refresh();
  }

  async refresh(event?: CustomEvent): Promise<void> {
    this.isLoading = !event;
    this.errorMessage = '';
    try {
      const all = await firstValueFrom(this.friendsService.list());
      this.friends = all.filter((f) => f.status === 'Accepted');
      this.pending = all.filter((f) => f.status === 'Pending');
    } catch {
      this.errorMessage = 'Could not load friends.';
    } finally {
      this.isLoading = false;
      (event?.target as HTMLIonRefresherElement | undefined)?.complete?.();
    }
  }

  setTab(value: string | number | undefined): void {
    if (value === 'friends' || value === 'pending' || value === 'find') {
      this.tab = value;
    }
  }

  onSearchChange(value: string | null | undefined): void {
    const text = value ?? '';
    this.searchQuery = text;
    if (!text.trim()) {
      this.searchResults = [];
      this.isSearching = false;
      return;
    }
    this.searchInput$.next(text);
  }

  async sendRequest(user: FriendUser): Promise<void> {
    try {
      await firstValueFrom(this.friendsService.sendRequest(user.id));
      this.searchResults = this.searchResults.filter((u) => u.id !== user.id);
      await this.refresh();
    } catch {
      this.errorMessage = 'Could not send request.';
    }
  }

  async accept(friendship: Friendship): Promise<void> {
    await firstValueFrom(this.friendsService.accept(friendship.id));
    await this.refresh();
  }

  async decline(friendship: Friendship): Promise<void> {
    await firstValueFrom(this.friendsService.decline(friendship.id));
    await this.refresh();
  }

  async removeFriend(friendship: Friendship): Promise<void> {
    await firstValueFrom(this.friendsService.remove(friendship.id));
    await this.refresh();
  }

  openChat(friendship: Friendship): void {
    this.router.navigate(['/chat', friendship.user.id]);
  }

  initial(name: string): string {
    return name?.trim()?.[0]?.toUpperCase() ?? '?';
  }

  private filterOutExisting(results: FriendUser[]): FriendUser[] {
    const known = new Set([
      ...this.friends.map((f) => f.user.id),
      ...this.pending.map((f) => f.user.id),
    ]);
    return results.filter((u) => !known.has(u.id));
  }
}
