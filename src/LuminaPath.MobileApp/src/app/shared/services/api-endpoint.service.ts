import { Injectable, signal } from '@angular/core';
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
  private readonly endpointSignal = signal<string>(resolveApiEndpoint());

  readonly endpoint = this.endpointSignal.asReadonly();

  get defaultEndpoint(): string {
    return normalizeApiEndpoint(environment.endpoint);
  }

  setEndpoint(value: string): string {
    const endpoint = normalizeApiEndpoint(value);
    globalThis.localStorage?.setItem(apiEndpointStorageKey, endpoint);
    this.endpointSignal.set(endpoint);
    return endpoint;
  }

  resetEndpoint(): string {
    globalThis.localStorage?.removeItem(apiEndpointStorageKey);
    const endpoint = this.defaultEndpoint;
    this.endpointSignal.set(endpoint);
    return endpoint;
  }

  url(path: string): string {
    const normalizedPath = path.replace(/^\/+/, '');
    return `${this.endpointSignal()}/${normalizedPath}`;
  }
}
