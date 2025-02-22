import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { MediaFilter } from 'src/app/core/entities/mediaFilter';
import { PaginateResult } from 'src/app/core/entities/paginatedResult';
import { Credentials } from 'src/app/core/auth/models/user.model';
import { IBasicInfo } from 'src/app/core/interfaces/iBasicInfo';
import { HttpClient } from '@angular/common/http';
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
  
  getAll(): Observable<T[]> {
    return this.http.get<T[]>(this.apiUrl, this.httpConfig);
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

  getAllPaginated(
    mediaFilter?: MediaFilter
  ) {
    // otherwise the asp.net api does not recognize the paging
    let params = null;
    const filterParams = {
      "searchString": mediaFilter?.SearchString,
      "myMedia": mediaFilter?.MyMedia,
      "status": mediaFilter?.Status,
      "paging.pageIndex": mediaFilter?.Paging.PageIndex,
      "paging.count": mediaFilter?.Paging.Count
    }
    if (mediaFilter) {
      params = filterParams;
    }
    return this.http.get<PaginateResult<T>>(this.apiUrl, { withCredentials: true});
  }
}
