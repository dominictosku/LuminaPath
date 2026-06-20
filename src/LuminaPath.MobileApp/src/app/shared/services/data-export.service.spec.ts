import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { ApiEndpointService } from './api-endpoint.service';
import { DataExportService } from './data-export.service';

describe('DataExportService', () => {
  let service: DataExportService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ApiEndpointService,
          useValue: { url: (path: string) => `/api/${path}` },
        },
      ],
    });

    service = TestBed.inject(DataExportService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('downloads the JSON export with credentials', () => {
    let fileName = '';

    service.downloadJson().subscribe((download) => {
      fileName = download.fileName;
    });

    const request = http.expectOne('/api/export/json');
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBeTrue();
    expect(request.request.responseType).toBe('blob');

    request.flush(new Blob(['{}'], { type: 'application/json' }), {
      headers: { 'X-LuminaPath-Export-Filename': 'PersonalData.json' },
    });

    expect(fileName).toBe('PersonalData.json');
  });

  it('downloads the library workbook and normalizes the filename extension', () => {
    let fileName = '';

    service.downloadLibraryWorkbook().subscribe((download) => {
      fileName = download.fileName;
    });

    const request = http.expectOne('/api/export/library.xlsx');
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBeTrue();
    expect(request.request.responseType).toBe('blob');

    request.flush(new Blob(['xlsx'], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    }), {
      headers: { 'content-disposition': 'attachment; filename="Library Backup"' },
    });

    expect(fileName).toBe('Library Backup.xlsx');
  });
});
