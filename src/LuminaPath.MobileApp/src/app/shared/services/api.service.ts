import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { MediaFilter } from 'src/app/core/entities/mediaFilter';
import { PaginateResult } from 'src/app/core/entities/paginatedResult';
import { Credentials } from 'src/app/core/auth/models/user.model';
import { IBasicInfo } from 'src/app/core/interfaces/iBasicInfo';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ApiService<T> {
  protected apiUrl: string;
  private httpConfig = { withCredentials: true };

  constructor(private http: HttpClient, endpoint: String) {
    this.apiUrl = `${environment.endpoint}/${endpoint}`;
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

    if (mediaFilter.Status !== undefined && mediaFilter.Status !== null) {
      params = params.set('status', String(mediaFilter.Status));
    }

    return params;
  }
}
