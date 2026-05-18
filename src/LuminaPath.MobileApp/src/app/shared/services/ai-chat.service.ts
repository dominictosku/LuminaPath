import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiEndpointService } from './api-endpoint.service';

export type ChatRole = 'user' | 'assistant';

export interface ChatMessage {
  role: ChatRole;
  content: string;
}

export type ChatStreamEvent =
  | { kind: 'text'; text: string }
  | { kind: 'tool_call'; name: string; server: string }
  | { kind: 'error'; message: string }
  | { kind: 'done' };

@Injectable({ providedIn: 'root' })
export class AiChatService {
  private apiEndpoint = inject(ApiEndpointService);


  stream(messages: ChatMessage[]): Observable<ChatStreamEvent> {
    return new Observable<ChatStreamEvent>((subscriber) => {
      const controller = new AbortController();

      (async () => {
        let response: Response;
        try {
          response = await fetch(this.apiEndpoint.url('chat/stream'), {
            method: 'POST',
            credentials: 'include',
            headers: {
              'Content-Type': 'application/json',
              Accept: 'text/event-stream',
            },
            body: JSON.stringify({ messages }),
            signal: controller.signal,
          });
        } catch (error) {
          if (!controller.signal.aborted) {
            subscriber.next({ kind: 'error', message: 'Could not reach the chat service.' });
            subscriber.next({ kind: 'done' });
          }
          subscriber.complete();
          return;
        }

        if (!response.ok) {
          const message = response.status === 401
            ? 'Please sign in to use the chat.'
            : `Chat error (${response.status}).`;
          subscriber.next({ kind: 'error', message });
          subscriber.next({ kind: 'done' });
          subscriber.complete();
          return;
        }

        if (!response.body) {
          subscriber.next({ kind: 'error', message: 'Streaming is not supported by this browser.' });
          subscriber.next({ kind: 'done' });
          subscriber.complete();
          return;
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = '';

        try {
          while (true) {
            const { value, done } = await reader.read();
            if (done) break;
            buffer += decoder.decode(value, { stream: true });

            let separatorIndex: number;
            while ((separatorIndex = buffer.indexOf('\n\n')) !== -1) {
              const block = buffer.slice(0, separatorIndex);
              buffer = buffer.slice(separatorIndex + 2);
              const event = parseSseBlock(block);
              if (event) {
                subscriber.next(event);
                if (event.kind === 'done') {
                  subscriber.complete();
                  return;
                }
              }
            }
          }
          subscriber.next({ kind: 'done' });
          subscriber.complete();
        } catch (error) {
          if (!controller.signal.aborted) {
            subscriber.next({ kind: 'error', message: 'Connection lost.' });
            subscriber.next({ kind: 'done' });
          }
          subscriber.complete();
        }
      })();

      return () => controller.abort();
    });
  }
}

function parseSseBlock(block: string): ChatStreamEvent | null {
  let event: string | null = null;
  let data = '';
  for (const rawLine of block.split('\n')) {
    const line = rawLine.replace(/\r$/, '');
    if (line.startsWith('event:')) {
      event = line.slice(6).trim();
    } else if (line.startsWith('data:')) {
      data += line.slice(5).trim();
    }
  }

  if (!event) return null;

  let payload: Record<string, unknown> = {};
  if (data) {
    try {
      payload = JSON.parse(data);
    } catch {
      payload = {};
    }
  }

  switch (event) {
    case 'text':
      return { kind: 'text', text: typeof payload['text'] === 'string' ? (payload['text'] as string) : '' };
    case 'tool_call':
      return {
        kind: 'tool_call',
        name: typeof payload['name'] === 'string' ? (payload['name'] as string) : '',
        server: typeof payload['server'] === 'string' ? (payload['server'] as string) : '',
      };
    case 'error':
      return { kind: 'error', message: typeof payload['message'] === 'string' ? (payload['message'] as string) : 'Unknown error' };
    case 'done':
      return { kind: 'done' };
    default:
      return null;
  }
}
