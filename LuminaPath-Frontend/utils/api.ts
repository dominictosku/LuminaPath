import axios from "axios";
import { IBasicInfo } from "~/utils/interfaces/iBasicInfo";
import { PaginateResult } from "~/utils/paginatedResult";
import { type Credentials } from "~/utils/user";

const getApiUrl = (endpoint: string) => {
  const runtimeConfig = useRuntimeConfig();
  return runtimeConfig.public.API_ENDPOINT + "/api" + endpoint;
};

const axiosConfig = {
  withCredentials: true, // This is crucial to include the HttpOnly cookie in the request
  headers: {
    Accept: "application/json",
    "Content-Type": "application/json",
  },
};

const axiosConfigWithParams = (param: any) => {
  return {
    params: param,
    withCredentials: true, // This is crucial to include the HttpOnly cookie in the request
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
    },
  };
};

const apiCall = async (url: string, data: any) => {
  try {
    return await axios.post(url, data, axiosConfig);
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
  const url = getApiUrl(`/${prefix}`);
  const { data: result } = await axios.get<PaginateResult<T>>(url, axiosConfigWithParams(mediaFilter));
  return result;
}

export async function fetchMedia<T>(
  prefix: string,
  mediaFilter?: MediaFilter
): Promise<Array<T>> {
  const url = getApiUrl(`/${prefix}`);
  const { data: result } = await axios.get<PaginateResult<T>>(url, axiosConfig);
  return result.data;
}

export async function fetchMediaAll<T>(
  howMany: number,
  prefix: string
): Promise<Array<T>> {
  const url = getApiUrl(`/${prefix}/all/${howMany}`);
  const { data: result } = await axios.get<Array<T>>(url, axiosConfig);
  return result;
}

export async function fetchMediaById<T>(
  id: number,
  prefix: string
): Promise<T> {
  const url = getApiUrl(`/${prefix}/${id}`);
  const { data: result } = await axios.get<T>(url, axiosConfig);
  return result;
}

export async function deleteMedia<T>(id: number, prefix: string) {
  const url = getApiUrl(`/${prefix}?id=${id}`);
  await axios.delete(url, axiosConfig);
}

export async function PostMedia<T>(media: IBasicInfo, prefix: string) {
  const url = getApiUrl(`/${prefix}`);
  await apiCall(url, media);
}

export async function PutMedia(media: IBasicInfo, prefix: string) {
  const url = getApiUrl(`/${prefix}/${media.id}`);
  await axios.put(url, media, axiosConfig);
}

// User requests

export async function LoginUser(Credentials: Credentials) {
  const url = getApiUrl("/LuminaUser/BearerToken");
  await apiCall(url, Credentials);
  return "data.token";
}

export async function CreateUser(Credentials: Credentials) {
  const url = getApiUrl("/LuminaUser");
  await apiCall(url, Credentials);
}
