import { type UseFetchOptions } from "nuxt/dist/app/composables/fetch";
import { type IBasicInfo } from "~/utils/interfaces/iBasicInfo";
import { MediaFilter } from "~/utils/classes/mediaFilter";
import { PaginateResult } from "~/utils/classes/paginatedResult";
import { type Credentials } from "~/utils/model/user";

const getApiUrl = () => {
  const runtimeConfig = useRuntimeConfig();
  return runtimeConfig.public.API_ENDPOINT;
};

const fetchConfig = <T>(method: 'GET' | 'POST' | 'DELETE' | 'PUT', param?: any, body?: any): UseFetchOptions<T> => {
  return {
    method: method,
    baseURL: getApiUrl(),
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
    },
    credentials: 'include',
    params: param,
    body: JSON.stringify(body) ?? null,
  }
};

const fetchFormsConfig = <T>(method: 'GET' | 'POST' | 'DELETE' | 'PUT', param?: any, body?: any): UseFetchOptions<T> => {
  return {
    method: method,
    baseURL: getApiUrl(),
    headers: {
      Accept: "application/json",
      "Content-Type": 'multipart/form-data',
    },
    credentials: 'include',
    params: param,
    body: body,
  }
};

const apiCall = async <T>(endpoint: string, fetchConfig: any) => {
  try {
    return await $fetch<T>("/api" + endpoint, fetchConfig);
  } catch (error) {
    // Handle error if needed
    throw error;
  }
};

export async function fetchPaginatedMedia<T>(
  prefix: string,
  mediaFilter?: MediaFilter
): Promise<PaginateResult<T>> {
  // otherwise the asp.net api does not recognize the paging
  const params = {
    "searchString": mediaFilter?.SearchString,
    "status": mediaFilter?.Status,
    "paging.pageIndex": mediaFilter?.Paging.PageIndex,
    "paging.count": mediaFilter?.Paging.Count
  }
  const url = `/${prefix}`;
  const config = fetchConfig<T>('GET', params)
  const result = await apiCall<PaginateResult<T>>(url, config);
  return result;
}

export async function fetchMediaById<T>(
  id: number,
  prefix: string
): Promise<T> {
  const url = `/${prefix}/${id}`;
  const config = fetchConfig<T>('GET')
  const result = await apiCall<T>(url, config);
  return result;
}

export async function deleteMedia<T>(id: number, prefix: string) {
  const url = `/${prefix}?id=${id}`;
  const config = fetchConfig<T>('DELETE')
  await apiCall(url, config);
}

export async function PostMedia<T>(media: IBasicInfo, prefix: string) {
  const url = `/${prefix}`;
  const config = fetchConfig<IBasicInfo>('POST', null, media)
  await apiCall(url, config);
}

export async function PutMedia<T>(media: IBasicInfo, prefix: string) {
  const url = `/${prefix}/${media.id}`;
  const config = fetchConfig<IBasicInfo>('PUT', null, media)
  await apiCall(url, config);
}

// files
export async function PostImage<T>(id: number, forms: any, prefix: string) {
  const url = `/${prefix}`;
  const config = fetchFormsConfig<IBasicInfo>('POST', null, forms)
  await apiCall(url, config);
}

// User requests

export async function LoginUser(Credentials: Credentials) {
  const url = "/login?useCookies=true";
  const config = fetchConfig<Credentials>('POST', null, Credentials)
  await apiCall(url, config);
  return "data.token";
}

export async function RefreshToken() {
  const url = "/refresh";
  const config = fetchConfig('POST')
  await apiCall(url, config);
}

export async function GetStatus() {
  const url = "/refresh";
  const config = fetchConfig('POST')
  await apiCall(url, config);
}

export async function CreateUser(Credentials: Credentials) {
  const url = "/LuminaUser";
  const config = fetchConfig<Credentials>('POST', null, Credentials)
  await apiCall(url, config);
}

