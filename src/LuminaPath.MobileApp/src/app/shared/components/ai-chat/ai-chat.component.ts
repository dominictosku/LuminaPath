import { CommonModule } from '@angular/common';
import { AfterViewChecked, Component, ElementRef, OnDestroy, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IonIcon } from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  chatbubbleEllipsesOutline,
  closeOutline,
  hourglassOutline,
  paperPlaneOutline,
  refreshOutline,
  sparklesOutline,
} from 'ionicons/icons';
import { Subscription } from 'rxjs';
import { AiChatService, ChatMessage } from 'src/app/shared/services/ai-chat.service';

interface DisplayMessage extends ChatMessage {
  pending?: boolean;
  toolUse?: { name: string; server: string }[];
  errored?: boolean;
}

@Component({
  selector: 'app-ai-chat',
  templateUrl: './ai-chat.component.html',
  styleUrls: ['./ai-chat.component.scss'],
  imports: [CommonModule, FormsModule, IonIcon],
})
export class AiChatComponent implements AfterViewChecked, OnDestroy {
  open = false;
  draft = '';
  messages: DisplayMessage[] = [];
  isStreaming = false;
  private streamSub?: Subscription;
  private shouldScroll = false;

  @ViewChild('scrollAnchor') private scrollAnchor?: ElementRef<HTMLDivElement>;

  constructor(private chat: AiChatService) {
    addIcons({
      chatbubbleEllipsesOutline,
      closeOutline,
      hourglassOutline,
      paperPlaneOutline,
      refreshOutline,
      sparklesOutline,
    });
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll) {
      this.shouldScroll = false;
      this.scrollAnchor?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'end' });
    }
  }

  ngOnDestroy(): void {
    this.streamSub?.unsubscribe();
  }

  toggle(): void {
    this.open = !this.open;
    if (this.open) {
      queueMicrotask(() => this.shouldScroll = true);
    }
  }

  reset(): void {
    this.cancelStream();
    this.messages = [];
    this.draft = '';
  }

  send(): void {
    const content = this.draft.trim();
    if (!content || this.isStreaming) return;

    this.draft = '';
    this.messages.push({ role: 'user', content });
    const assistant: DisplayMessage = { role: 'assistant', content: '', pending: true };
    this.messages.push(assistant);
    this.shouldScroll = true;
    this.isStreaming = true;

    const history = this.messages
      .filter((m) => m !== assistant)
      .map(({ role, content }) => ({ role, content }));

    this.streamSub?.unsubscribe();
    this.streamSub = this.chat.stream(history).subscribe({
      next: (event) => {
        switch (event.kind) {
          case 'text':
            assistant.content += event.text;
            assistant.pending = false;
            this.shouldScroll = true;
            break;
          case 'tool_call':
            assistant.toolUse ??= [];
            assistant.toolUse.push({ name: event.name, server: event.server });
            this.shouldScroll = true;
            break;
          case 'error':
            assistant.content = (assistant.content ? assistant.content + '\n\n' : '') + event.message;
            assistant.errored = true;
            assistant.pending = false;
            break;
          case 'done':
            assistant.pending = false;
            this.isStreaming = false;
            break;
        }
      },
      error: () => {
        assistant.content = assistant.content || 'Something went wrong.';
        assistant.errored = true;
        assistant.pending = false;
        this.isStreaming = false;
      },
    });
  }

  cancelStream(): void {
    this.streamSub?.unsubscribe();
    this.streamSub = undefined;
    this.isStreaming = false;
    const last = this.messages[this.messages.length - 1];
    if (last?.role === 'assistant' && last.pending) {
      last.pending = false;
    }
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  trackByIndex(index: number): number {
    return index;
  }

  shortToolLabel(name: string): string {
    if (name.startsWith('mcp__')) {
      const parts = name.split('__');
      return parts.length >= 3 ? `${parts[1]}: ${parts[2]}` : name;
    }
    return name;
  }
}
