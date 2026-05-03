import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { environment } from 'src/environments/environment';

const apiEndpointStorageKey = 'luminapath.apiEndpoint';

export function normalizeApiEndpoint(value: string | null | undefined): string {
  const rawValue = (value ?? '').trim();
  const fallback = environment.endpoint.replace(/\/$/, '');
  const endpoint = rawValue || fallback;

  if (endpoint === '/api') {
    return endpoint;
  }

  const withoutTrailingSlash = endpoint.replace(/\/+$/, '');

  if (withoutTrailingSlash.endsWith('/api')) {
    return withoutTrailingSlash;
  }

  return `${withoutTrailingSlash}/api`;
}

export function resolveApiEndpoint(): string {
  try {
    const savedEndpoint = globalThis.localStorage?.getItem(apiEndpointStorageKey);
    return normalizeApiEndpoint(savedEndpoint || environment.endpoint);
  } catch {
    return normalizeApiEndpoint(environment.endpoint);
  }
}

@Injectable({
  providedIn: 'root',
})
export class ApiEndpointService {
  private readonly endpointSubject = new BehaviorSubject<string>(resolveApiEndpoint());
  readonly endpoint$ = this.endpointSubject.asObservable();

  get endpoint(): string {
    return this.endpointSubject.value;
  }

  get defaultEndpoint(): string {
    return normalizeApiEndpoint(environment.endpoint);
  }

  setEndpoint(value: string): string {
    const endpoint = normalizeApiEndpoint(value);
    globalThis.localStorage?.setItem(apiEndpointStorageKey, endpoint);
    this.endpointSubject.next(endpoint);
    return endpoint;
  }

  resetEndpoint(): string {
    globalThis.localStorage?.removeItem(apiEndpointStorageKey);
    const endpoint = this.defaultEndpoint;
    this.endpointSubject.next(endpoint);
    return endpoint;
  }

  url(path: string): string {
    const normalizedPath = path.replace(/^\/+/, '');
    return `${this.endpoint}/${normalizedPath}`;
  }
}
