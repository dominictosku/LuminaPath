import axios from "axios";
import { IBasicInfo } from "~/utils/basicInfo";
import { PaginateResult } from "~/utils/paginatedResult";
import { type Credentials } from "~/utils/user";

const getApiUrl = (endpoint: string) => {
  const runtimeConfig = useRuntimeConfig();
  return runtimeConfig.public.API_ENDPOINT + "/api" + endpoint;
};

const apiCall = async (url: string, data: any) => {
  try {
    return await axios.post(url, data, {
      withCredentials: true, // This is crucial to include the HttpOnly cookie in the request
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
    },
  });
  } catch (error) {
    // Handle error if needed
    console.error("API call failed:", error);
    throw error;
  }
};

export async function fetchPaginatedMedia<T>(prefix: string): Promise<PaginateResult<T>> {
  const url = getApiUrl(`/${prefix}`);
  const result: PaginateResult<T> = await $fetch<PaginateResult<T>>(url);
  return result;
}

export async function fetchMedia<T>(howMany: number, prefix: string): Promise<Array<T>> {
  const url = getApiUrl(`/${prefix}/all/${howMany}`);
  const result: Array<T> = await $fetch<Array<T>>(url);
  return result;
}

export async function fetchMediaById<T>(id: number, prefix: string): Promise<T> {
  const url = getApiUrl(`/${prefix}/${id}`);
  const result: T = await $fetch<T>(url);
  return result;
}

export async function deleteMedia<T>(id: number, prefix: string) {
  const url = getApiUrl(`/${prefix}?id=${id}`);
  await axios.delete(url, {
    withCredentials: true, // This is crucial to include the HttpOnly cookie in the request
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
    },
  });
}

export async function PostMedia<T>(media: IBasicInfo, prefix: string) {
  const url = getApiUrl(`/${prefix}`);
  await apiCall(url, media);
}

export async function PutMedia(media: IBasicInfo, prefix: string) {
  const url = getApiUrl(`/${prefix}/${media.id}`);
  await axios.put(url, media, {
    withCredentials: true, // This is crucial to include the HttpOnly cookie in the request
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
    },
  });
}

// User requests

export async function LoginUser(Credentials: Credentials) {
  const url = getApiUrl("/LuminaUser/BearerToken");
  await apiCall(url, Credentials);
  return 'data.token';
}

export async function CreateUser(Credentials: Credentials) {
  const url = getApiUrl("/LuminaUser");
  await apiCall(url, Credentials);
}
