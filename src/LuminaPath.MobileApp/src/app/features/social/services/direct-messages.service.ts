import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { DirectMessage } from '../models/friend.model';

@Injectable({ providedIn: 'root' })
export class DirectMessagesService {
  private http = inject(HttpClient);
  private apiEndpoint = inject(ApiEndpointService);

  private readonly httpConfig = { withCredentials: true };

  conversation(otherUserId: string, options: { take?: number; before?: Date | string } = {}): Observable<DirectMessage[]> {
    let params = new HttpParams();
    if (options.take != null) {
      params = params.set('take', String(options.take));
    }
    if (options.before) {
      const iso = options.before instanceof Date ? options.before.toISOString() : options.before;
      params = params.set('before', iso);
    }
    return this.http.get<DirectMessage[]>(this.apiEndpoint.url(`directmessages/${otherUserId}`), {
      ...this.httpConfig,
      params,
    });
  }

  send(recipientId: string, content: string): Observable<DirectMessage> {
    return this.http.post<DirectMessage>(
      this.apiEndpoint.url('directmessages'),
      { recipientId, content },
      this.httpConfig,
    );
  }

  markRead(otherUserId: string): Observable<number> {
    return this.http.post<number>(this.apiEndpoint.url(`directmessages/${otherUserId}/read`), {}, this.httpConfig);
  }
}
