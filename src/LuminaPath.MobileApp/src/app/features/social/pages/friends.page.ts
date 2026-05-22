import { CommonModule } from '@angular/common';
import { AfterViewChecked, Component, ElementRef, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  IonBadge,
  IonButton,
  IonContent,
  IonIcon,
  IonInput,
  IonRefresher,
  IonRefresherContent,
  IonSearchbar,
  IonSegment,
  IonSegmentButton,
  IonSpinner,
} from '@ionic/angular/standalone';
import { Subject, Subscription, debounceTime, firstValueFrom, switchMap } from 'rxjs';
import { DirectMessage, FriendUser, Friendship } from '../models/friend.model';
import { DirectMessagesService } from '../services/direct-messages.service';
import { FriendsService } from '../services/friends.service';
import { MessagesHubService } from '../services/messages-hub.service';

type Tab = 'friends' | 'pending' | 'find';

@Component({
  selector: 'app-friends',
  templateUrl: './friends.page.html',
  styleUrls: ['./friends.page.scss'],
  imports: [
    CommonModule,
    FormsModule,
    IonBadge,
    IonButton,
    IonContent,
    IonIcon,
    IonInput,
    IonRefresher,
    IonRefresherContent,
    IonSearchbar,
    IonSegment,
    IonSegmentButton,
    IonSpinner,
  ],
})
export class FriendsPage implements OnInit, OnDestroy, AfterViewChecked {
  private friendsService = inject(FriendsService);
  private messagesService = inject(DirectMessagesService);
  private hub = inject(MessagesHubService);

  @ViewChild('chatMessages') private chatMessagesRef?: ElementRef<HTMLDivElement>;

  tab: Tab = 'friends';
  isLoading = false;
  errorMessage = '';

  friends: Friendship[] = [];
  pending: Friendship[] = [];

  searchQuery = '';
  searchResults: FriendUser[] = [];
  isSearching = false;

  activeChat: Friendship | null = null;
  currentUserId = '';
  messages: DirectMessage[] = [];
  draft = '';
  isChatLoading = false;
  chatErrorMessage = '';
  isSending = false;
  chatPosition = { x: 24, y: 96 };

  private readonly searchInput$ = new Subject<string>();
  private readonly subscriptions: Subscription[] = [];
  private chatSubscriptions: Subscription[] = [];
  private dragStart: { pointerId: number; x: number; y: number; left: number; top: number } | null = null;
  private shouldScrollChat = false;

  constructor() {
    this.subscriptions.push(
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
        }),
    );
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
    if (this.activeChat?.id === friendship.id) {
      this.closeChat();
    }
    await this.refresh();
  }

  async openChat(friendship: Friendship): Promise<void> {
    this.clearChatSubscriptions();
    this.activeChat = friendship;
    this.messages = [];
    this.draft = '';
    this.currentUserId = '';
    this.chatErrorMessage = '';
    this.isChatLoading = true;
    this.placeChatPanel();

    try {
      const history = await firstValueFrom(this.messagesService.conversation(friendship.user.id, { take: 100 }));
      this.messages = history;
      this.currentUserId = this.deriveCurrentUserId(history) ?? '';
      this.shouldScrollChat = true;
    } catch {
      this.chatErrorMessage = 'Could not load conversation.';
    } finally {
      this.isChatLoading = false;
    }

    try {
      await this.hub.ensureStarted();
      this.chatSubscriptions = [
        this.hub.messageReceived.subscribe((message) => this.onIncoming(message)),
        this.hub.messageSent.subscribe((message) => this.onIncoming(message)),
      ];
      await this.hub.markRead(friendship.user.id);
    } catch {
      // Realtime chat is best-effort; REST sending still works.
    }
  }

  closeChat(): void {
    this.clearChatSubscriptions();
    this.activeChat = null;
    this.messages = [];
    this.draft = '';
    this.chatErrorMessage = '';
  }

  async send(): Promise<void> {
    const userId = this.activeChat?.user.id;
    const content = this.draft.trim();
    if (!userId || !content || this.isSending) {
      return;
    }

    this.isSending = true;
    try {
      const sent = await this.hub.send(userId, content).catch(() => null);
      if (sent) {
        this.draft = '';
      } else {
        const fallback = await firstValueFrom(this.messagesService.send(userId, content));
        this.appendIfNew(fallback);
        this.draft = '';
      }
    } catch {
      this.chatErrorMessage = 'Failed to send message.';
    } finally {
      this.isSending = false;
    }
  }

  startChatDrag(event: PointerEvent): void {
    if (!this.activeChat) {
      return;
    }

    const element = event.target as HTMLElement | null;
    if (element?.closest('ion-button, button, a, input, textarea')) {
      return;
    }

    const target = event.currentTarget as HTMLElement;
    target.setPointerCapture(event.pointerId);
    this.dragStart = {
      pointerId: event.pointerId,
      x: event.clientX,
      y: event.clientY,
      left: this.chatPosition.x,
      top: this.chatPosition.y,
    };
  }

  moveChatDrag(event: PointerEvent): void {
    if (!this.dragStart || this.dragStart.pointerId !== event.pointerId) {
      return;
    }

    const maxX = Math.max(12, window.innerWidth - 420);
    const maxY = Math.max(12, window.innerHeight - 560);
    this.chatPosition = {
      x: this.clamp(this.dragStart.left + event.clientX - this.dragStart.x, 12, maxX),
      y: this.clamp(this.dragStart.top + event.clientY - this.dragStart.y, 12, maxY),
    };
  }

  endChatDrag(event: PointerEvent): void {
    if (this.dragStart?.pointerId === event.pointerId) {
      this.dragStart = null;
    }
  }

  isMine(message: DirectMessage): boolean {
    return !!this.currentUserId && message.senderId === this.currentUserId;
  }

  initial(name: string): string {
    return name?.trim()?.[0]?.toUpperCase() ?? '?';
  }

  displayName(user: FriendUser): string {
    return user.fullName || user.userName;
  }

  ngAfterViewChecked(): void {
    if (this.shouldScrollChat) {
      this.shouldScrollChat = false;
      const element = this.chatMessagesRef?.nativeElement;
      if (element) {
        element.scrollTop = element.scrollHeight;
      }
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach((sub) => sub.unsubscribe());
    this.clearChatSubscriptions();
  }

  private onIncoming(message: DirectMessage): void {
    const otherUserId = this.activeChat?.user.id;
    if (!otherUserId) {
      return;
    }

    const involves = (message.senderId === otherUserId && message.recipientId === this.currentUserId)
      || (message.senderId === this.currentUserId && message.recipientId === otherUserId)
      || (!this.currentUserId && (message.senderId === otherUserId || message.recipientId === otherUserId));

  if (!involves) {
      return;
    }

  if (!this.currentUserId) {
      this.currentUserId = message.senderId === otherUserId ? message.recipientId : message.senderId;
    }

    this.appendIfNew(message);
    if (message.senderId === otherUserId) {
      void this.hub.markRead(otherUserId).catch(() => undefined);
    }
  }

  private appendIfNew(message: DirectMessage): void {
    if (this.messages.some((existing) => existing.id === message.id)) {
      return;
    }

    this.messages = [...this.messages, message];
    this.shouldScrollChat = true;
  }

  private deriveCurrentUserId(history: DirectMessage[]): string | null {
    const otherUserId = this.activeChat?.user.id;
    if (!otherUserId) {
      return null;
    }

  for (const message of history) {
      if (message.senderId !== otherUserId) {
        return message.senderId;
      }
      if (message.recipientId !== otherUserId) {
        return message.recipientId;
      }
    }
    return null;
  }

  private filterOutExisting(results: FriendUser[]): FriendUser[] {
    const known = new Set([
      ...this.friends.map((f) => f.user.id),
      ...this.pending.map((f) => f.user.id),
    ]);
    return results.filter((u) => !known.has(u.id));
  }

  private clearChatSubscriptions(): void {
    this.chatSubscriptions.forEach((sub) => sub.unsubscribe());
    this.chatSubscriptions = [];
  }

  private placeChatPanel(): void {
    const isSmall = window.innerWidth < 700;
    this.chatPosition = isSmall
      ? { x: 12, y: 74 }
      : { x: Math.max(24, window.innerWidth - 456), y: 94 };
  }

  private clamp(value: number, min: number, max: number): number {
    return Math.min(max, Math.max(min, value));
  }
}
