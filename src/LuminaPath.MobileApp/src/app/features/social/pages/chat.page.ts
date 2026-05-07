import { CommonModule } from '@angular/common';
import { AfterViewChecked, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
  IonBackButton,
  IonButton,
  IonButtons,
  IonContent,
  IonFooter,
  IonHeader,
  IonIcon,
  IonInput,
  IonSpinner,
  IonTitle,
  IonToolbar,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import { sendOutline } from 'ionicons/icons';
import { Subscription, firstValueFrom } from 'rxjs';
import { DirectMessage, FriendUser } from '../models/friend.model';
import { DirectMessagesService } from '../services/direct-messages.service';
import { FriendsService } from '../services/friends.service';
import { MessagesHubService } from '../services/messages-hub.service';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.page.html',
  styleUrls: ['./chat.page.scss'],
  imports: [
    CommonModule,
    FormsModule,
    IonBackButton,
    IonButton,
    IonButtons,
    IonContent,
    IonFooter,
    IonHeader,
    IonIcon,
    IonInput,
    IonSpinner,
    IonTitle,
    IonToolbar,
  ],
})
export class ChatPage implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild(IonContent) private contentRef?: IonContent;

  otherUserId = '';
  otherUser: FriendUser | null = null;
  currentUserId = '';
  messages: DirectMessage[] = [];
  draft = '';
  isLoading = true;
  errorMessage = '';
  isSending = false;

  private shouldScroll = false;
  private subscriptions: Subscription[] = [];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private friendsService: FriendsService,
    private messagesService: DirectMessagesService,
    private hub: MessagesHubService,
  ) {
    addIcons({ sendOutline });
  }

  async ngOnInit(): Promise<void> {
    this.otherUserId = this.route.snapshot.paramMap.get('userId') ?? '';
    if (!this.otherUserId) {
      this.router.navigate(['/friends']);
      return;
    }

    try {
      const friends = await firstValueFrom(this.friendsService.list());
      const match = friends.find((f) => f.user.id === this.otherUserId && f.status === 'Accepted');
      if (!match) {
        this.errorMessage = 'You are not friends with this user.';
        this.isLoading = false;
        return;
      }
      this.otherUser = match.user;

      const history = await firstValueFrom(this.messagesService.conversation(this.otherUserId, { take: 100 }));
      this.messages = history;
      this.currentUserId = this.deriveCurrentUserId(history) ?? '';
      this.shouldScroll = true;
    } catch {
      this.errorMessage = 'Could not load conversation.';
    } finally {
      this.isLoading = false;
    }

    try {
      await this.hub.ensureStarted();
      this.subscriptions.push(
        this.hub.messageReceived.subscribe((message) => this.onIncoming(message)),
        this.hub.messageSent.subscribe((message) => this.onIncoming(message)),
      );
      await this.hub.markRead(this.otherUserId);
    } catch {
      // hub is best-effort; REST send still works.
    }
  }

  async send(): Promise<void> {
    const content = this.draft.trim();
    if (!content || this.isSending) return;
    this.isSending = true;
    try {
      const sent = await this.hub.send(this.otherUserId, content).catch(() => null);
      if (sent) {
        this.draft = '';
      } else {
        const fallback = await firstValueFrom(this.messagesService.send(this.otherUserId, content));
        this.appendIfNew(fallback);
        this.draft = '';
      }
    } catch {
      this.errorMessage = 'Failed to send message.';
    } finally {
      this.isSending = false;
    }
  }

  isMine(message: DirectMessage): boolean {
    return !!this.currentUserId && message.senderId === this.currentUserId;
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll) {
      this.shouldScroll = false;
      void this.contentRef?.scrollToBottom(0);
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach((sub) => sub.unsubscribe());
  }

  private onIncoming(message: DirectMessage): void {
    const involves = (message.senderId === this.otherUserId && message.recipientId === this.currentUserId)
      || (message.senderId === this.currentUserId && message.recipientId === this.otherUserId)
      || (!this.currentUserId && (message.senderId === this.otherUserId || message.recipientId === this.otherUserId));

    if (!involves) return;
    if (!this.currentUserId) {
      this.currentUserId = message.senderId === this.otherUserId ? message.recipientId : message.senderId;
    }
    this.appendIfNew(message);
    if (message.senderId === this.otherUserId) {
      void this.hub.markRead(this.otherUserId).catch(() => undefined);
    }
  }

  private appendIfNew(message: DirectMessage): void {
    if (this.messages.some((existing) => existing.id === message.id)) return;
    this.messages = [...this.messages, message];
    this.shouldScroll = true;
  }

  private deriveCurrentUserId(history: DirectMessage[]): string | null {
    for (const message of history) {
      if (message.senderId !== this.otherUserId) return message.senderId;
      if (message.recipientId !== this.otherUserId) return message.recipientId;
    }
    return null;
  }
}
