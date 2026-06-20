import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiEndpointService } from './api-endpoint.service';

export interface DataExportDownload {
  blob: Blob;
  fileName: string;
}

interface DownloadOptions {
  path: string;
  emptyContentType: string;
  fallbackFileName: string;
  extension: string;
}

const jsonContentType = 'application/json';
const workbookContentType = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';

@Injectable({ providedIn: 'root' })
export class DataExportService {
  private http = inject(HttpClient);
  private apiEndpoint = inject(ApiEndpointService);

  downloadJson(): Observable<DataExportDownload> {
    return this.download({
      path: 'export/json',
      emptyContentType: jsonContentType,
      fallbackFileName: fallbackJsonExportFileName(),
      extension: 'json',
    });
  }

  downloadLibraryWorkbook(): Observable<DataExportDownload> {
    return this.download({
      path: 'export/library.xlsx',
      emptyContentType: workbookContentType,
      fallbackFileName: 'LuminaLibrary.xlsx',
      extension: 'xlsx',
    });
  }

  private download(options: DownloadOptions): Observable<DataExportDownload> {
    return this.http.get(this.apiEndpoint.url(options.path), {
      withCredentials: true,
      observe: 'response',
      responseType: 'blob',
    }).pipe(
      map((response) => ({
        blob: response.body ?? new Blob([], { type: options.emptyContentType }),
        fileName: exportFileNameFrom(response, options),
      })),
    );
  }
}

function exportFileNameFrom(response: HttpResponse<Blob>, options: DownloadOptions): string {
  const headerFileName = fileNameFromContentDisposition(
    response.headers.get('content-disposition'),
  );
  const appHeaderFileName = response.headers.get('x-luminapath-export-filename');

  return sanitizeExportFileName(
    headerFileName ?? appHeaderFileName ?? options.fallbackFileName,
    options,
  );
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

function fallbackJsonExportFileName(): string {
  const stamp = new Date().toISOString().slice(0, 10).replace(/-/g, '');
  return `LuminaPath-export-${stamp}.json`;
}

function sanitizeExportFileName(fileName: string, options: DownloadOptions): string {
  const sanitized = fileName.replace(/[\\/:*?"<>|]+/g, '-').trim();
  const fallback = options.fallbackFileName;
  const normalized = sanitized || fallback;
  return normalized.toLowerCase().endsWith(`.${options.extension}`)
    ? normalized
    : `${normalized}.${options.extension}`;
}
