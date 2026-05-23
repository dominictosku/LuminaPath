import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiEndpointService } from './api-endpoint.service';

/**
 * Shape returned by `POST /api/files`. Mirrors the backend's response
 * (camel-cased by System.Text.Json defaults) so callers can attach the
 * uploaded file straight onto a fresh entity's `image` field.
 */
export interface UploadedFile {
  message: string;
  name: string;
  storageName: string;
  contentType: string;
  url: string;
}

@Injectable({ providedIn: 'root' })
export class FileUploadService {
  private readonly http = inject(HttpClient);
  private readonly apiEndpoint = inject(ApiEndpointService);

  /**
   * Uploads a single file (typically a cover image) to the catalog file
   * store. Returns the persisted blob's metadata so the caller can wire
   * `name` + `storageName` + `contentType` onto its entity in the
   * subsequent POST.
   */
  upload(file: File): Observable<UploadedFile> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<UploadedFile>(this.apiEndpoint.url('files'), form, {
      withCredentials: true,
    });
  }
}
