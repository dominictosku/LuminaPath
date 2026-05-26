import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  MediaVideo,
  MediaVideoUpdate,
  MediaVideoUpload,
} from '../models/media-video.model';
import { ApiEndpointService } from './api-endpoint.service';

@Injectable({
  providedIn: 'root',
})
export class MediaVideoService {
  private readonly http = inject(HttpClient);
  private readonly apiEndpoint = inject(ApiEndpointService);
  private readonly httpConfig = { withCredentials: true };

  list(mediaId: number): Observable<MediaVideo[]> {
    return this.http.get<MediaVideo[]>(this.apiEndpoint.url('media-videos'), {
      ...this.httpConfig,
      params: { mediaId },
    });
  }

  upload(payload: MediaVideoUpload): Observable<MediaVideo> {
    const form = new FormData();
    form.set('mediaId', String(payload.mediaId));
    form.set('title', payload.title);
    form.set('description', payload.description);
    form.set('kind', String(payload.kind));
    form.set('file', payload.file);

    return this.http.post<MediaVideo>(this.apiEndpoint.url('media-videos'), form, this.httpConfig);
  }

  update(id: number, update: MediaVideoUpdate): Observable<MediaVideo> {
    return this.http.put<MediaVideo>(
      this.apiEndpoint.url(`media-videos/${id}`),
      update,
      this.httpConfig,
    );
  }

  reorder(mediaId: number, videoIds: number[]): Observable<MediaVideo[]> {
    return this.http.put<MediaVideo[]>(
      this.apiEndpoint.url('media-videos/reorder'),
      { mediaId, videoIds },
      this.httpConfig,
    );
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(this.apiEndpoint.url(`media-videos/${id}`), this.httpConfig);
  }

  streamUrl(video: Pick<MediaVideo, 'id'>): string {
    return this.apiEndpoint.url(`media-videos/${video.id}/stream`);
  }
}
