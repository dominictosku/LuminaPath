import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { MediaFilter } from 'src/app/core/entities/mediaFilter';
import { PaginateResult } from 'src/app/core/entities/paginatedResult';
import { Credentials } from 'src/app/core/auth/models/user.model';
import { IBasicInfo } from 'src/app/core/interfaces/iBasicInfo';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class ApiService {

  constructor(private http: HttpClient) { }

  getApiUrl = () => {
    return ""
  };

  getHttpOptions = (param?: any, body?: any): any => {
    return {
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      },
      params: param,
      body: body,
      withCredentials: true
    }
  };

  apiCall<T>(method: 'GET' | 'POST' | 'DELETE' | 'PUT', endpoint: string, options: any) {
    try {
      return this.http.request<T>(method, this.getApiUrl() + "/api" + endpoint, options);
    } catch (error) {
      console.error("api call failed");
      throw error;
    }
  };

  fetchPaginatedMedia<T>(
    prefix: string,
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
    const url = `/${prefix}`;
    const config = this.getHttpOptions(params)
    const result = this.apiCall<PaginateResult<T>>('GET', url, config);
    return result;
  }

  fetchMediaById<T>(
    id: number,
    prefix: string
  ) {
    const url = `/${prefix}/${id}`;
    const config = this.getHttpOptions()
    const result = this.apiCall<T>('GET', url, config);
    return result;
  }

  deleteMedia(id: number, prefix: string) {
    const url = `/${prefix}?id=${id}`;
    const config = this.getHttpOptions()
    this.apiCall('DELETE', url, config);
  }

  PostMedia(media: IBasicInfo, prefix: string) {
    const url = `/${prefix}`;
    const config = this.getHttpOptions(null, media)
    this.apiCall('POST', url, config);
  }

  PutMedia(media: IBasicInfo, prefix: string) {
    const url = `/${prefix}/${media.id}`;
    const config = this.getHttpOptions(null, media)
    this.apiCall('PUT', url, config);
  }

  // files
  PostImage(id: number, forms: any, prefix: string) {
    const url = `/${prefix}`;
    const config = this.getHttpOptions(null, forms)
    this.apiCall('POST', url, config);
  }

  // User requests

  LoginUser(Credentials: Credentials) {
    const url = "/login?useCookies=true";
    const config = this.getHttpOptions(null, Credentials)
    this.apiCall('POST', url, config);
    return "data.token";
  }

  RefreshToken() {
    const url = "/refresh";
    const config = this.getHttpOptions()
    this.apiCall('POST', url, config);
  }

  GetStatus() {
    const url = "/refresh";
    const config = this.getHttpOptions()
    this.apiCall('POST', url, config);
  }

  CreateUser(Credentials: Credentials) {
    const url = "/LuminaUser";
    const config = this.getHttpOptions(null, Credentials)
    this.apiCall('POST', url, config);
  }
}
