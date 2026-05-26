import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';

export interface DataExportDownload {
  blob: Blob;
  fileName: string;
}

@Injectable({ providedIn: 'root' })
export class DataExportService {
  private http = inject(HttpClient);
  private apiEndpoint = inject(ApiEndpointService);

  downloadJson(): Observable<DataExportDownload> {
    return this.http.get(this.apiEndpoint.url('export/json'), {
      withCredentials: true,
      observe: 'response',
      responseType: 'blob',
    }).pipe(
      map((response) => ({
        blob: response.body ?? new Blob([], { type: 'application/json' }),
        fileName: exportFileNameFrom(response),
      })),
    );
  }
}

function exportFileNameFrom(response: HttpResponse<Blob>): string {
  const headerFileName = fileNameFromContentDisposition(
    response.headers.get('content-disposition'),
  );
  const appHeaderFileName = response.headers.get('x-luminapath-export-filename');
  return sanitizeExportFileName(headerFileName ?? appHeaderFileName ?? fallbackExportFileName());
}

function fileNameFromContentDisposition(value: string | null): string | null {
  if (!value) return null;

  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(value);
  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1].trim());
  }

  const quotedMatch = /filename="?([^";]+)"?/i.exec(value);
  return quotedMatch?.[1]?.trim() ?? null;
}

function fallbackExportFileName(): string {
  const stamp = new Date().toISOString().slice(0, 10).replace(/-/g, '');
  return `LuminaPath-export-${stamp}.json`;
}

function sanitizeExportFileName(fileName: string): string {
  const sanitized = fileName.replace(/[\\/:*?"<>|]+/g, '-').trim();
  if (!sanitized) return fallbackExportFileName();
  return sanitized.toLowerCase().endsWith('.json') ? sanitized : `${sanitized}.json`;
}
