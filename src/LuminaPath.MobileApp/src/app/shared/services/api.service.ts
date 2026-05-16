import { MediaFilter } from 'src/app/core/entities/mediaFilter';
import { PaginateResult } from 'src/app/core/entities/paginatedResult';
import { Credentials } from 'src/app/core/auth/models/user.model';
import { IBasicInfo } from 'src/app/core/interfaces/iBasicInfo';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiEndpointService } from './api-endpoint.service';

export class ApiService<T> {
  private httpConfig = { withCredentials: true };

  constructor(
    protected http: HttpClient,
    protected apiEndpoint: ApiEndpointService,
    protected endpoint: string,
  ) {}

  protected get apiUrl(): string {
    return this.apiEndpoint.url(this.endpoint);
  }
  
  getAll(mediaFilter?: MediaFilter): Observable<PaginateResult<T>> {
    return this.http.get<PaginateResult<T>>(this.apiUrl, {
      ...this.httpConfig,
      params: this.createFilterParams(mediaFilter),
    });
  }

  get(id: number): Observable<T> {
    return this.http.get<T>(`${this.apiUrl}/${id}`, this.httpConfig);
  }

  post(data: T): Observable<T> {
    return this.http.post<T>(this.apiUrl, data, this.httpConfig);
  }

  put(id: number, data: T): Observable<T> {
    return this.http.put<T>(`${this.apiUrl}/${id}`, data, this.httpConfig);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`, this.httpConfig);
  }

  getAllPaginated(mediaFilter?: MediaFilter): Observable<PaginateResult<T>> {
    return this.getAll(mediaFilter);
  }

  private createFilterParams(mediaFilter?: MediaFilter): HttpParams {
    if (!mediaFilter) {
      return new HttpParams();
    }

    let params = new HttpParams()
      .set('myMedia', String(mediaFilter.MyMedia))
      .set('paging.pageIndex', String(mediaFilter.Paging.PageIndex))
      .set('paging.count', String(mediaFilter.Paging.Count));

    if (mediaFilter.SearchString) {
      params = params.set('searchString', mediaFilter.SearchString);
    }

    if (mediaFilter.From) {
      params = params.set('from', mediaFilter.From);
    }

    if (mediaFilter.To) {
      params = params.set('to', mediaFilter.To);
    }

    if (mediaFilter.Status !== undefined && mediaFilter.Status !== null) {
      params = params.set('status', String(mediaFilter.Status));
    }

    if (mediaFilter.MediaStatus !== undefined && mediaFilter.MediaStatus !== null) {
      params = params.set('mediaStatus', String(mediaFilter.MediaStatus));
    }

    if (mediaFilter.Platform !== undefined && mediaFilter.Platform !== null) {
      params = params.set('platform', String(mediaFilter.Platform));
    }

    if (mediaFilter.Ownership) {
      params = params.set('ownership', mediaFilter.Ownership);
    }

    if (mediaFilter.SortBy) {
      params = params.set('sortBy', mediaFilter.SortBy);
    }

    if (mediaFilter.SmartFilter) {
      params = params.set('smartFilter', mediaFilter.SmartFilter);
    }

    return params;
  }
}
