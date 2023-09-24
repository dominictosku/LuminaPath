import axios from "axios";
import { UseFetchOptions } from "nuxt/dist/app/composables/fetch";
import { IBasicInfo } from "~/utils/interfaces/iBasicInfo";
import { PaginateResult } from "~/utils/paginatedResult";
import { type Credentials } from "~/utils/user";

const getApiUrl = () => {
  const runtimeConfig = useRuntimeConfig();
  return runtimeConfig.public.API_ENDPOINT;
};

const fetchConfig = <T>(method: 'GET' | 'POST' | 'DELETE' | 'PUT', param?: any, body?: T) : UseFetchOptions<T> => {
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

const apiCall = async <T>(endpoint: string, fetchConfig: any) => {
  try {
    return await $fetch<T>("/api" + endpoint, fetchConfig);
  } catch (error) {
    // Handle error if needed
    console.error("API call failed:", error);
    throw error;
  }
};

export async function fetchPaginatedMedia<T>(
  prefix: string,
  mediaFilter: MediaFilter
): Promise<PaginateResult<T>> {
  const url = `/${prefix}`;
  const config = fetchConfig<T>('GET', mediaFilter)
  const result = await apiCall<PaginateResult<T>>(url, config);
  return result;
}

export async function fetchMedia<T>(
  prefix: string,
  mediaFilter?: MediaFilter
): Promise<Array<T>> {
  const url = `/${prefix}`;
  const config = fetchConfig<T>('GET', mediaFilter)
  const result = await apiCall<PaginateResult<T>>(url, config);
  return result.data;
}

export async function fetchMediaAll<T>(
  howMany: number,
  prefix: string
): Promise<Array<T>> {
  const url = `/${prefix}/all/${howMany}`;
  const config = fetchConfig<T>('GET')
  const result = await apiCall<Array<T>>(url, config);
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

// User requests

export async function LoginUser(Credentials: Credentials) {
  const url = "/LuminaUser/BearerToken";
  const config = fetchConfig<Credentials>('POST', null, Credentials)
  await apiCall(url, config);
  return "data.token";
}

export async function CreateUser(Credentials: Credentials) {
  const url = "/LuminaUser";
  const config = fetchConfig<Credentials>('POST', null, Credentials)
  await apiCall(url, config);
}
