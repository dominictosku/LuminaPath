import { Injectable, OnDestroy, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { DirectMessage } from '../models/friend.model';

@Injectable({ providedIn: 'root' })
export class MessagesHubService implements OnDestroy {
  private apiEndpoint = inject(ApiEndpointService);

  private connection: HubConnection | null = null;
  private startPromise: Promise<void> | null = null;

  private readonly received$ = new Subject<DirectMessage>();
  private readonly sent$ = new Subject<DirectMessage>();
  private readonly read$ = new Subject<{ byUserId: string }>();

  readonly messageReceived: Observable<DirectMessage> = this.received$.asObservable();
  readonly messageSent: Observable<DirectMessage> = this.sent$.asObservable();
  readonly messagesRead: Observable<{ byUserId: string }> = this.read$.asObservable();

  async ensureStarted(): Promise<void> {
    if (this.connection && this.connection.state === HubConnectionState.Connected) {
      return;
    }
    if (this.startPromise) {
      return this.startPromise;
    }
    this.connection = new HubConnectionBuilder()
      .withUrl(this.hubUrl(), { withCredentials: true })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('MessageReceived', (message: DirectMessage) => this.received$.next(message));
    this.connection.on('MessageSent', (message: DirectMessage) => this.sent$.next(message));
    this.connection.on('MessagesRead', (payload: { byUserId: string }) => this.read$.next(payload));

    this.startPromise = this.connection.start().catch((error) => {
      this.startPromise = null;
      throw error;
    });
    return this.startPromise;
  }

  async send(recipientId: string, content: string): Promise<DirectMessage | null> {
    await this.ensureStarted();
    return (await this.connection!.invoke<DirectMessage | null>('Send', recipientId, content)) ?? null;
  }

  async markRead(otherUserId: string): Promise<void> {
    await this.ensureStarted();
    await this.connection!.invoke('MarkRead', otherUserId);
  }

  async stop(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      this.startPromise = null;
    }
  }

  ngOnDestroy(): void {
    void this.stop();
  }

  private hubUrl(): string {
    const apiBase = this.apiEndpoint.endpoint().replace(/\/api\/?$/, '');
    return `${apiBase}/hubs/messages`;
  }
}
