import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { FriendUser, Friendship } from '../models/friend.model';

@Injectable({ providedIn: 'root' })
export class FriendsService {
  private http = inject(HttpClient);
  private apiEndpoint = inject(ApiEndpointService);

  private readonly httpConfig = { withCredentials: true };

  list(): Observable<Friendship[]> {
    return this.http.get<Friendship[]>(this.apiEndpoint.url('friends'), this.httpConfig);
  }

  search(query: string): Observable<FriendUser[]> {
    const params = new HttpParams().set('query', query);
    return this.http.get<FriendUser[]>(this.apiEndpoint.url('friends/search'), { ...this.httpConfig, params });
  }

  sendRequest(addresseeId: string): Observable<Friendship> {
    return this.http.post<Friendship>(this.apiEndpoint.url('friends/requests'), { addresseeId }, this.httpConfig);
  }

  accept(id: number): Observable<Friendship> {
    return this.http.post<Friendship>(this.apiEndpoint.url(`friends/requests/${id}/accept`), {}, this.httpConfig);
  }

  decline(id: number): Observable<Friendship> {
    return this.http.post<Friendship>(this.apiEndpoint.url(`friends/requests/${id}/decline`), {}, this.httpConfig);
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(this.apiEndpoint.url(`friends/${id}`), this.httpConfig);
  }
}
